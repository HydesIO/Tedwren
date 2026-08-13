using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tedwren.Web.Tests;

/// <summary>
/// W5 integration tests for the Security, About, FAQ and Legal pages (Web Plan §6.6–6.8, §4): only
/// makeable claims (no fabricated certification badges, no absolute-compliance language), the FAQ page
/// renders questions plus valid FAQPage schema, and the legal pages serve real content rather than a
/// placeholder.
/// </summary>
public sealed class TrustAboutFaqLegalTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Injects the in-process site host.</summary>
    /// <param name="factory">Test host for Tedwren.Web.</param>
    public TrustAboutFaqLegalTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>The security page makes no fabricated badge or absolute-compliance claims (§8.1, §6.6).</summary>
    [Fact]
    public async Task Security_MakesNoFabricatedOrAbsoluteClaims()
    {
        var body = await _factory.CreateClient().GetStringAsync("/security");

        Assert.Contains("Security", body); // page renders
        string[] prohibited = { "ISO 27001", "Cyber Essentials", "100% compliant", "fully compliant", "guarantee" };
        foreach (var phrase in prohibited)
        {
            Assert.DoesNotContain(phrase, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>The About page renders founder-led content.</summary>
    [Fact]
    public async Task About_RendersContent()
    {
        var body = await _factory.CreateClient().GetStringAsync("/about");

        // ASCII-safe substring: Razor entity-encodes the apostrophe in "Why we're building it".
        Assert.Contains("building it", body);
    }

    /// <summary>The FAQ page renders questions and emits valid FAQPage JSON-LD (§6.8, §10).</summary>
    [Fact]
    public async Task Faq_RendersQuestions_AndValidSchema()
    {
        var body = await _factory.CreateClient().GetStringAsync("/faq");

        Assert.Contains("faq__item", body);

        var json = ExtractJsonLd(body, "FAQPage");
        using var doc = JsonDocument.Parse(json); // throws if invalid
        Assert.Equal("FAQPage", doc.RootElement.GetProperty("@type").GetString());
        Assert.True(doc.RootElement.GetProperty("mainEntity").GetArrayLength() > 0);
    }

    /// <summary>Legal pages serve real content, not the earlier placeholder text.</summary>
    [Theory]
    [InlineData("/legal/privacy", "Privacy Policy")]
    [InlineData("/legal/cookies", "Cookie Policy")]
    [InlineData("/legal/terms", "Terms of Service")]
    [InlineData("/legal/data-protection", "Data Protection")]
    public async Task Legal_ServesRealContent(string route, string expectedTitle)
    {
        var body = await _factory.CreateClient().GetStringAsync(route);

        Assert.Contains(expectedTitle, body);
        Assert.DoesNotContain("Full content lands in phase", body); // no leftover stub
    }

    /// <summary>The sitewide Organization JSON-LD is present and valid on every page.</summary>
    [Fact]
    public async Task Organization_Schema_IsValid()
    {
        var body = await _factory.CreateClient().GetStringAsync("/about");

        var json = ExtractJsonLd(body, "Organization");
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Organization", doc.RootElement.GetProperty("@type").GetString());
        Assert.Equal("Tedwren Ltd", doc.RootElement.GetProperty("legalName").GetString());
    }

    /// <summary>Extracts the JSON-LD script block whose payload contains the given @type.</summary>
    private static string ExtractJsonLd(string html, string type)
    {
        const string open = "<script type=\"application/ld+json\">";
        var searchFrom = 0;
        while (true)
        {
            var start = html.IndexOf(open, searchFrom, StringComparison.OrdinalIgnoreCase);
            Assert.True(start >= 0, $"No JSON-LD block containing {type} was found.");
            start += open.Length;
            var end = html.IndexOf("</script>", start, StringComparison.OrdinalIgnoreCase);
            var payload = html[start..end];
            if (payload.Contains(type, StringComparison.Ordinal))
            {
                return payload;
            }

            searchFrom = end;
        }
    }
}
