using System.IO.Compression;
using System.Text;
using Tedwren.Application.Export;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.CompliancePacks;

/// <summary>
/// Renders a compliance pack's fixed snapshot into the shared tabular model, so the web view, CSV, ZIP and PDF
/// all present <b>identical content</b> in different formats (SUB-16). SRP: composition only — the format
/// writers live in the Export helpers.
/// </summary>
public static class PackComposer
{
    /// <summary>Flattens a pack to a per-card tabular sheet (one row per operative card).</summary>
    public static TabularSheet ToSheet(CompliancePack pack)
    {
        var rows = new List<IReadOnlyList<string>>();
        foreach (var subject in pack.Subjects)
        {
            if (subject.Cards.Count == 0)
            {
                rows.Add(new[] { subject.Name, "(no cards)", string.Empty, string.Empty, string.Empty });
                continue;
            }

            foreach (var card in subject.Cards)
            {
                rows.Add(new[]
                {
                    subject.Name,
                    card.QualificationName,
                    card.StatusLabel,
                    card.ExpiresOn?.ToString("yyyy-MM-dd") ?? string.Empty,
                    card.VerificationLabel,
                });
            }
        }

        return new TabularSheet(
            pack.Title,
            new[] { "Operative", "Qualification", "Status", "Expires", "Verification" },
            rows);
    }

    /// <summary>Packs the CSV rendering plus a manifest into a ZIP archive (SUB-16).</summary>
    public static byte[] ToZip(CompliancePack pack, string csv)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "compliance-pack.csv", csv);
            WriteEntry(archive, "manifest.txt", Manifest(pack));
        }

        return buffer.ToArray();
    }

    /// <summary>Writes one archive entry from a UTF-8 string.</summary>
    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>A short manifest describing the fixed-at-send snapshot (R7).</summary>
    private static string Manifest(CompliancePack pack)
    {
        var sb = new StringBuilder();
        sb.Append("Title: ").Append(pack.Title).Append('\n');
        if (pack.SiteName is not null)
        {
            sb.Append("Site: ").Append(pack.SiteName).Append('\n');
        }

        if (pack.FromDate is { } from && pack.ToDate is { } to)
        {
            sb.Append("Period: ").Append(from.ToString("yyyy-MM-dd")).Append(" to ").Append(to.ToString("yyyy-MM-dd")).Append('\n');
        }

        sb.Append("Created: ").Append(pack.CreatedUtc.ToString("u")).Append(" by ").Append(pack.CreatedBy).Append('\n');
        sb.Append("Operatives: ").Append(pack.Subjects.Count).Append('\n');
        sb.Append("This pack is a fixed snapshot taken at the time it was sent.\n");
        return sb.ToString();
    }
}
