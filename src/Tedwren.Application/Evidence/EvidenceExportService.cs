using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Tedwren.Abstractions.Contracts.Evidence;
using Tedwren.Abstractions.Contracts.Havs;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Export;
using Tedwren.Application.Havs;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Evidence;

/// <summary>
/// Assembles a company's compliance evidence into a single downloadable pack (PRD §8.2). Each evidence type becomes
/// a <see cref="TabularSheet"/> (rendered to CSV via the shared <see cref="CsvWriter"/>, so the ZIP and any future
/// format present identical content), spanning permits, RAMS, plant, safety events, HAVs and document
/// acknowledgements. Everything is scoped to the company (R15); the pack is a fixed snapshot at generation time.
/// </summary>
public sealed class EvidenceExportService : IEvidenceExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IPermitRepository _permits;
    private readonly IRamsRepository _rams;
    private readonly IAssetRepository _assets;
    private readonly IHazardReportRepository _hazards;
    private readonly IIncidentReportRepository _incidents;
    private readonly IHavsExposureRepository _havs;
    private readonly IDocumentDistributionRepository _documents;

    /// <summary>Creates the service over the evidence repositories.</summary>
    public EvidenceExportService(
        IPermitRepository permits,
        IRamsRepository rams,
        IAssetRepository assets,
        IHazardReportRepository hazards,
        IIncidentReportRepository incidents,
        IHavsExposureRepository havs,
        IDocumentDistributionRepository documents)
    {
        _permits = permits;
        _rams = rams;
        _assets = assets;
        _hazards = hazards;
        _incidents = incidents;
        _havs = havs;
        _documents = documents;
    }

    /// <summary>Returns the sections the export will contain and their record counts, without generating the file.</summary>
    public async Task<EvidenceSummaryDto> GetSummaryAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var sections = await ComposeAsync(companyId, cancellationToken);
        var sectionDtos = sections.Select(s => new EvidenceSectionDto(s.Sheet.Name, s.Sheet.Rows.Count)).ToList();
        return new EvidenceSummaryDto(sectionDtos, sectionDtos.Sum(s => s.Count), DateTimeOffset.UtcNow);
    }

    /// <summary>Generates the evidence pack (a ZIP of one CSV per section plus a manifest).</summary>
    public async Task<EvidenceExportFileDto> BuildZipAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var sections = await ComposeAsync(companyId, cancellationToken);
        var generatedUtc = DateTimeOffset.UtcNow;

        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var section in sections)
            {
                WriteEntry(archive, section.FileName, CsvWriter.Write(section.Sheet));
            }

            WriteEntry(archive, "manifest.txt", Manifest(companyId, sections, generatedUtc));
        }

        var fileName = $"evidence-pack-{generatedUtc:yyyyMMdd-HHmmss}.zip";
        return new EvidenceExportFileDto(fileName, "application/zip", buffer.ToArray());
    }

    /// <summary>Gathers every evidence section for the company (R15) as a named CSV file + tabular sheet.</summary>
    private async Task<List<(string FileName, TabularSheet Sheet)>> ComposeAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var permits = await _permits.GetByCompanyAsync(companyId, cancellationToken);
        var rams = await _rams.GetByCompanyAsync(companyId, cancellationToken);
        var assets = await _assets.GetByCompanyAsync(companyId, cancellationToken);
        var hazards = await _hazards.GetByCompanyAsync(companyId, cancellationToken);
        var incidents = await _incidents.GetByCompanyAsync(companyId, cancellationToken);
        var havs = await _havs.GetByCompanyAsync(companyId, cancellationToken);
        var distributions = await _documents.GetByCompanyAsync(companyId, cancellationToken);
        var acknowledgements = await _documents.GetAcknowledgementsForCompanyAsync(companyId, cancellationToken);
        var acksByDistribution = acknowledgements.GroupBy(a => a.DistributionId).ToDictionary(g => g.Key, g => g.ToList());

        return new List<(string, TabularSheet)>
        {
            ("permits.csv", PermitSheet(permits)),
            ("rams.csv", RamsSheet(rams)),
            ("plant-and-equipment.csv", AssetSheet(assets)),
            ("hazards-and-near-misses.csv", HazardSheet(hazards)),
            ("accidents-and-incidents.csv", IncidentSheet(incidents)),
            ("havs-exposure.csv", HavsSheet(havs)),
            ("document-acknowledgements.csv", DocumentSheet(distributions, acksByDistribution)),
        };
    }

    /// <summary>Permit-to-work register sheet.</summary>
    private static TabularSheet PermitSheet(IReadOnlyList<Permit> permits) => new(
        "Permits",
        new[] { "Type", "Site", "Responsible", "Valid from", "Valid to", "High risk", "RAMS attached", "Status" },
        permits.Select(p => (IReadOnlyList<string>)new[]
        {
            p.PermitType, p.SiteName ?? string.Empty, p.ResponsiblePerson ?? string.Empty,
            Date(p.ValidFrom), Date(p.ValidTo), YesNo(p.HighRisk), YesNo(p.RamsAttached), p.Status.ToString(),
        }).ToList());

    /// <summary>RAMS review register sheet.</summary>
    private static TabularSheet RamsSheet(IReadOnlyList<RamsSubmission> rams) => new(
        "RAMS",
        new[] { "Reference", "Contractor", "Title", "Site", "Version", "Status", "Submitted" },
        rams.Select(r => (IReadOnlyList<string>)new[]
        {
            r.Reference, r.ContractorName, r.Title, r.SiteName ?? string.Empty,
            r.Version.ToString(), r.Status.ToString(), Date(r.SubmittedUtc),
        }).ToList());

    /// <summary>Plant &amp; equipment register sheet.</summary>
    private static TabularSheet AssetSheet(IReadOnlyList<Asset> assets) => new(
        "Plant & Equipment",
        new[] { "Name", "Type", "Serial", "Location", "Owner", "Certification expiry", "Next inspection", "Status" },
        assets.Select(a => (IReadOnlyList<string>)new[]
        {
            a.Name, a.AssetType ?? string.Empty, a.SerialNumber ?? string.Empty, a.Location ?? string.Empty,
            a.OwnerName ?? string.Empty, Date(a.CertificationExpiry), Date(a.NextInspectionDue), a.Status.ToString(),
        }).ToList());

    /// <summary>Hazard / near-miss register sheet.</summary>
    private static TabularSheet HazardSheet(IReadOnlyList<HazardReport> hazards) => new(
        "Hazards & Near-misses",
        new[] { "Reference", "Kind", "Description", "Severity", "Status", "Assigned to", "Reported by", "Reported" },
        hazards.Select(h => (IReadOnlyList<string>)new[]
        {
            h.Reference, h.Kind.ToString(), h.Description, h.Severity.ToString(), h.Status.ToString(),
            h.AssignedTo ?? string.Empty, h.ReportedBy, Date(h.ReportedUtc),
        }).ToList());

    /// <summary>Accident / incident register sheet (with the RIDDOR flag).</summary>
    private static TabularSheet IncidentSheet(IReadOnlyList<IncidentReport> incidents) => new(
        "Accidents & Incidents",
        new[] { "Reference", "Kind", "Description", "Occurred", "Severity", "RIDDOR reportable", "RIDDOR category", "Status" },
        incidents.Select(i => (IReadOnlyList<string>)new[]
        {
            i.Reference, i.Kind.ToString(), i.Description, Date(i.OccurredUtc), i.Severity.ToString(),
            YesNo(i.RiddorReportable), i.RiddorCategory ?? string.Empty, i.Status.ToString(),
        }).ToList());

    /// <summary>HAVs exposure sheet, with the derived daily A(8), points and band (HSE methodology).</summary>
    private static TabularSheet HavsSheet(IReadOnlyList<HavsExposureRecord> records) => new(
        "HAVs Exposure",
        new[] { "Person", "Date", "Tools", "A(8) m/s²", "Points", "Band" },
        records.Select(r =>
        {
            var usages = JsonSerializer.Deserialize<List<HavsToolUsageDto>>(r.ToolUsagesJson, JsonOptions) ?? new List<HavsToolUsageDto>();
            var result = HavsCalculator.Compute(usages);
            return (IReadOnlyList<string>)new[]
            {
                r.PersonName, r.ExposureDate.ToString("yyyy-MM-dd"), usages.Count.ToString(),
                result.DailyExposureA8.ToString("0.0"), result.ExposurePoints.ToString(), result.Band.ToString(),
            };
        }).ToList());

    /// <summary>Document distribution / acknowledgement sheet (the completion matrix summary).</summary>
    private static TabularSheet DocumentSheet(
        IReadOnlyList<DocumentDistribution> distributions,
        IReadOnlyDictionary<Guid, List<DocumentAcknowledgement>> acksByDistribution) => new(
        "Document Acknowledgements",
        new[] { "Title", "Category", "Audience", "Sent by", "Sent", "Acknowledged", "Total" },
        distributions.Select(d =>
        {
            var acks = acksByDistribution.TryGetValue(d.Id, out var list) ? list : new List<DocumentAcknowledgement>();
            return (IReadOnlyList<string>)new[]
            {
                d.Title, d.Category ?? string.Empty, d.Audience ?? string.Empty, d.SentBy, Date(d.SentUtc),
                acks.Count(a => a.AcknowledgedUtc is not null).ToString(), acks.Count.ToString(),
            };
        }).ToList());

    /// <summary>A short manifest describing the fixed-at-generation snapshot and per-section counts (R7).</summary>
    private static string Manifest(Guid companyId, IReadOnlyList<(string FileName, TabularSheet Sheet)> sections, DateTimeOffset generatedUtc)
    {
        var sb = new StringBuilder();
        sb.Append("Tedwren compliance evidence pack\n");
        sb.Append("Company: ").Append(companyId).Append('\n');
        sb.Append("Generated: ").Append(generatedUtc.ToString("u")).Append('\n');
        sb.Append("This pack is a fixed snapshot taken at the time it was generated.\n\n");
        sb.Append("Sections:\n");
        foreach (var section in sections)
        {
            sb.Append("  - ").Append(section.Sheet.Name).Append(": ").Append(section.Sheet.Rows.Count).Append(" record(s)\n");
        }

        return sb.ToString();
    }

    /// <summary>Writes one archive entry from a UTF-8 string.</summary>
    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Formats a date-only value as ISO yyyy-MM-dd (empty when null).</summary>
    private static string Date(DateOnly? value) => value?.ToString("yyyy-MM-dd") ?? string.Empty;

    /// <summary>Formats a timestamp's date part as ISO yyyy-MM-dd.</summary>
    private static string Date(DateTimeOffset value) => value.ToString("yyyy-MM-dd");

    /// <summary>Renders a boolean as Yes/No.</summary>
    private static string YesNo(bool value) => value ? "Yes" : "No";
}
