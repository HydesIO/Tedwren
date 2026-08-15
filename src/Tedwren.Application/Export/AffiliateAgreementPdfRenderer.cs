using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Tedwren.Application.Affiliates;

namespace Tedwren.Application.Export;

/// <summary>The data an affiliate-agreement PDF needs, assembled by the affiliate service at signing.</summary>
/// <param name="AffiliateName">The affiliate's name.</param>
/// <param name="Company">The affiliate's company, if any.</param>
/// <param name="Clauses">The agreement clauses (same source as the HTML the affiliate read).</param>
/// <param name="CountersignatoryName">Tedwren's countersignatory name.</param>
/// <param name="CountersignatoryTitle">Tedwren's countersignatory title.</param>
/// <param name="SignedByName">The name the affiliate signed with.</param>
/// <param name="SignedUtc">When the affiliate signed.</param>
/// <param name="SignatureImage">The affiliate's drawn signature image bytes (PNG), if captured.</param>
public sealed record AffiliateAgreementPdfModel(
    string AffiliateName,
    string? Company,
    IReadOnlyList<AgreementClause> Clauses,
    string CountersignatoryName,
    string? CountersignatoryTitle,
    string SignedByName,
    DateTimeOffset SignedUtc,
    byte[]? SignatureImage);

/// <summary>
/// Renders a signed affiliate agreement to a clean, branded A4 PDF using QuestPDF, mirroring
/// <see cref="FormPdfRenderer"/>'s look: Tedwren logo and title in the header, the numbered clauses as content,
/// and a signature block carrying both parties — Tedwren's countersignatory and the affiliate's drawn
/// signature with a timestamp — the digital equivalent of a countersigned paper agreement.
/// </summary>
public static class AffiliateAgreementPdfRenderer
{
    private static readonly byte[]? Logo = LoadLogo();
    private const string Brand = "#E8590C";
    private const string Ink = "#101828";
    private const string Muted = "#667085";
    private const string Line = "#E4E7EC";

    /// <summary>Sets the QuestPDF Community licence once (free under the revenue threshold).</summary>
    static AffiliateAgreementPdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Builds the branded PDF bytes for a signed agreement. If the drawn signature bytes are not a valid image
    /// (corrupt or truncated), the render is retried with the signature omitted (falling back to the typed
    /// name), so a bad image can never fail the signing operation.
    /// </summary>
    public static byte[] Render(AffiliateAgreementPdfModel model)
    {
        try
        {
            return Compose(model);
        }
        catch (Exception) when (model.SignatureImage is not null)
        {
            return Compose(model with { SignatureImage = null });
        }
    }

    /// <summary>Composes the document for a model exactly as given.</summary>
    private static byte[] Compose(AffiliateAgreementPdfModel model)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Ink));

                page.Header().Element(h => ComposeHeader(h, model));
                page.Content().PaddingVertical(12).Element(c => ComposeContent(c, model));
                page.Footer().Element(ComposeFooter);
            });
        }).GeneratePdf();
    }

    /// <summary>Logo top-left, agreement title and party on the right.</summary>
    private static void ComposeHeader(IContainer container, AffiliateAgreementPdfModel model)
    {
        container.BorderBottom(1).BorderColor(Line).PaddingBottom(10).Row(row =>
        {
            if (Logo is not null)
            {
                row.ConstantItem(46).Height(46).AlignMiddle().Image(Logo).FitArea();
                row.ConstantItem(14);
            }

            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Tedwren").FontSize(13).Bold().FontColor(Brand);
                col.Item().Text("Affiliate Agreement").FontSize(16).Bold();
                var who = string.IsNullOrWhiteSpace(model.Company) ? model.AffiliateName : $"{model.AffiliateName} · {model.Company}";
                col.Item().Text(who).FontColor(Muted);
            });

            row.ConstantItem(150).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text("SIGNED").Bold().FontColor(Brand);
                col.Item().AlignRight().Text(model.SignedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm")).FontColor(Muted);
            });
        });
    }

    /// <summary>The numbered clauses followed by the two-party signature block.</summary>
    private static void ComposeContent(IContainer container, AffiliateAgreementPdfModel model)
    {
        container.Column(col =>
        {
            foreach (var clause in model.Clauses)
            {
                col.Item().PaddingTop(8).Text(clause.Heading).Bold();
                col.Item().PaddingTop(2).Text(clause.Body).FontColor(Ink);
            }

            col.Item().PaddingTop(24).BorderTop(1).BorderColor(Line).PaddingTop(12).Row(row =>
            {
                row.RelativeItem().Column(party =>
                {
                    party.Item().Text("For Tedwren Ltd").Bold();
                    party.Item().PaddingTop(18).Text(model.CountersignatoryName).FontSize(14);
                    party.Item().Text(model.CountersignatoryName).FontColor(Muted);
                    if (!string.IsNullOrWhiteSpace(model.CountersignatoryTitle))
                    {
                        party.Item().Text(model.CountersignatoryTitle!).FontColor(Muted);
                    }
                });

                row.ConstantItem(24);

                row.RelativeItem().Column(party =>
                {
                    party.Item().Text("The Affiliate").Bold();
                    if (model.SignatureImage is { } sig)
                    {
                        party.Item().PaddingTop(4).Height(40).AlignLeft().Image(sig).FitHeight();
                    }
                    else
                    {
                        party.Item().PaddingTop(18).Text(model.SignedByName).FontSize(14);
                    }

                    party.Item().Text(model.SignedByName).FontColor(Muted);
                    party.Item().Text(model.SignedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm")).FontColor(Muted);
                });
            });
        });
    }

    /// <summary>A thin branded footer line.</summary>
    private static void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(Line).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text("Tedwren Ltd — Affiliate Agreement").FontSize(8).FontColor(Muted);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Page ").FontSize(8).FontColor(Muted);
                t.CurrentPageNumber().FontSize(8).FontColor(Muted);
                t.Span(" of ").FontSize(8).FontColor(Muted);
                t.TotalPages().FontSize(8).FontColor(Muted);
            });
        });
    }

    /// <summary>Loads the embedded Tedwren logo, or null when it isn't present.</summary>
    private static byte[]? LoadLogo()
    {
        var assembly = typeof(AffiliateAgreementPdfRenderer).Assembly;
        var name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase));
        if (name is null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null)
        {
            return null;
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
