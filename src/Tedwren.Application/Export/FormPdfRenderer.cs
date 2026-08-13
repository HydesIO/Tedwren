using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Tedwren.Application.Export;

/// <summary>
/// Renders a completed form submission to a clean, branded A4 PDF (requirement 4) using QuestPDF. The Tedwren
/// logo sits top-left; a header states the form, who submitted it and when (UTC stored, UK local displayed, R11)
/// and the review status; each section's answers follow, with photographs and signatures embedded inline. The
/// output is professional and content-driven — spacing and type come from one place here, mirroring the app's
/// design discipline.
/// </summary>
public static class FormPdfRenderer
{
    private static readonly byte[]? Logo = LoadLogo();
    private const string Brand = "#E8590C";      // --color-brand
    private const string Ink = "#101828";        // --color-text-primary
    private const string Muted = "#667085";      // --color-text-secondary
    private const string Line = "#E4E7EC";       // --color-border

    /// <summary>Sets the QuestPDF Community licence once (free under the revenue threshold).</summary>
    static FormPdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>Builds the branded PDF bytes for a submission.</summary>
    public static byte[] Render(FormPdfModel model)
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

    /// <summary>Logo top-left, form name and submission meta on the right.</summary>
    private static void ComposeHeader(IContainer container, FormPdfModel model)
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
                col.Item().Text(model.FormName).FontSize(16).Bold();
                col.Item().Text($"Version {model.Version} · {ScopeLabel(model)}").FontColor(Muted);
            });

            row.ConstantItem(150).AlignRight().Column(col =>
            {
                col.Item().AlignRight().Text(model.Status.ToUpperInvariant()).Bold().FontColor(StatusColour(model.Status));
                col.Item().AlignRight().Text(model.SubmittedUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm")).FontColor(Muted);
                col.Item().AlignRight().Text($"By {model.SubmittedBy}").FontColor(Muted);
            });
        });
    }

    /// <summary>The sections and their answered items, plus any review note.</summary>
    private static void ComposeContent(IContainer container, FormPdfModel model)
    {
        container.Column(col =>
        {
            col.Spacing(16);

            if (!string.IsNullOrWhiteSpace(model.ReviewNote))
            {
                col.Item().Background("#F9FAFB").Border(1).BorderColor(Line).Padding(8).Column(note =>
                {
                    note.Item().Text("Review note").Bold().FontColor(Muted);
                    note.Item().Text(model.ReviewNote);
                });
            }

            foreach (var section in model.Sections)
            {
                col.Item().Column(sec =>
                {
                    sec.Spacing(6);
                    sec.Item().Text(section.Title).FontSize(12).Bold().FontColor(Brand);

                    foreach (var item in section.Items)
                    {
                        sec.Item().BorderBottom(1).BorderColor(Line).PaddingBottom(6).Row(row =>
                        {
                            row.ConstantItem(180).Text(item.Label).SemiBold().FontColor(Muted);
                            if (item.Image is { Length: > 0 } image)
                            {
                                row.RelativeItem().Height(90).AlignLeft().Image(image).FitHeight();
                            }
                            else
                            {
                                row.RelativeItem().Text(string.IsNullOrWhiteSpace(item.ValueText) ? "—" : item.ValueText);
                            }
                        });
                    }
                });
            }
        });
    }

    /// <summary>A page number and a generated-on line.</summary>
    private static void ComposeFooter(IContainer container)
    {
        container.BorderTop(1).BorderColor(Line).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text($"Generated {DateTimeOffset.UtcNow.ToLocalTime():dd MMM yyyy HH:mm}").FontSize(8).FontColor(Muted);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.DefaultTextStyle(s => s.FontSize(8).FontColor(Muted));
                t.Span("Page ");
                t.CurrentPageNumber();
                t.Span(" of ");
                t.TotalPages();
            });
        });
    }

    /// <summary>The scope line, naming the site when site-scoped.</summary>
    private static string ScopeLabel(FormPdfModel model) =>
        model.Scope == "Site" && !string.IsNullOrWhiteSpace(model.SiteName) ? $"Site: {model.SiteName}" : model.Scope;

    /// <summary>Maps a submission status to a colour for the header badge.</summary>
    private static string StatusColour(string status) => status switch
    {
        "Approved" => "#17B26A",
        "Rejected" => "#F04438",
        "Submitted" => "#F79009",
        _ => Muted,
    };

    /// <summary>Loads the embedded Tedwren logo, or null if unavailable.</summary>
    private static byte[]? LoadLogo()
    {
        var assembly = typeof(FormPdfRenderer).Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase));
        if (name is null) return null;
        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null) return null;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
