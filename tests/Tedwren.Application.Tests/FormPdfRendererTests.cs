using Tedwren.Application.Export;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the branded form PDF renders to valid PDF bytes (requirement 4) — including a section with an
/// embedded image (a signature/photo) — running headless without a display.
/// </summary>
public sealed class FormPdfRendererTests
{
    [Fact]
    public void Render_ProducesValidPdfBytes()
    {
        // A 1x1 PNG so the image branch renders.
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
        var model = new FormPdfModel(
            "Daily Site Inspection", 2, "Site", "Meridian Tower", "A. Inspector",
            DateTimeOffset.UtcNow, "Approved", "Looks good.",
            new List<FormPdfSection>
            {
                new("General", new List<FormPdfItem>
                {
                    new("Housekeeping", "Green", null),
                    new("Fire exits clear", "Yes", null),
                    new("Notes", "All good", null),
                }),
                new("Sign-off", new List<FormPdfItem>
                {
                    new("Signature", null, png),
                }),
            });

        var bytes = FormPdfRenderer.Render(model);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
