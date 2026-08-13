using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tedwren.Web.Tests;

/// <summary>
/// W4 integration tests for the Worker Passport and Pricing pages (Web Plan §6.4, §6.5): the W2
/// "never locked out" benefit is present, the price comes from the single configured source, the CSCS
/// positioning restriction holds in the title and meta description (§8.2), and the pricing page renders
/// numbers from config plus the plain-language clarifiers, with SoftwareApplication schema.
/// </summary>
public sealed class WorkerPassportAndPricingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Injects the in-process site host.</summary>
    /// <param name="factory">Test host for Tedwren.Web.</param>
    public WorkerPassportAndPricingTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>The Worker Passport page carries the "never locked out for non-payment" benefit (PRD W2).</summary>
    [Fact]
    public async Task WorkerPassport_HasNeverLockedOutBenefit()
    {
        var body = await _factory.CreateClient().GetStringAsync("/worker-passport");

        Assert.Contains("Never locked out for non-payment", body);
    }

    /// <summary>The Worker Passport price is rendered from the single configured source (£10).</summary>
    [Fact]
    public async Task WorkerPassport_ShowsPriceFromConfig()
    {
        var body = await _factory.CreateClient().GetStringAsync("/worker-passport");

        Assert.Contains("£10", body);
    }

    /// <summary>The title and meta description carry no CSCS rivalry/replacement phrasing (§8.2).</summary>
    [Fact]
    public async Task WorkerPassport_TitleAndMeta_AvoidCscsPositioning()
    {
        var body = await _factory.CreateClient().GetStringAsync("/worker-passport");

        // Isolate the <head> metadata (title + meta description) and scan for prohibited phrasing.
        var headEnd = body.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        var head = headEnd > 0 ? body[..headEnd] : body;

        string[] prohibited =
        {
            "replace cscs", "instead of cscs", "cscs alternative", "vs cscs",
            "on-demand verification", "my cscs", "digital skills passport",
        };
        foreach (var phrase in prohibited)
        {
            Assert.DoesNotContain(phrase, head, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>The pricing page renders the clarifiers and an unpriced band as "Pricing on request".</summary>
    [Fact]
    public async Task Pricing_RendersClarifiers_AndUnpricedBand()
    {
        var body = await _factory.CreateClient().GetStringAsync("/pricing");

        Assert.Contains("active operative", body);          // a plain-language clarifier
        Assert.Contains("Pricing on request", body);        // band seeded at 0 (open sign-off item)
        Assert.Contains("Sites are free to record", body);  // pricing trust note
    }

    /// <summary>The pricing page emits valid SoftwareApplication JSON-LD (Plan §10).</summary>
    [Fact]
    public async Task Pricing_EmitsValidSoftwareApplicationSchema()
    {
        var body = await _factory.CreateClient().GetStringAsync("/pricing");

        var json = ExtractJsonLd(body, "SoftwareApplication");
        using var doc = JsonDocument.Parse(json); // throws if invalid

        Assert.Equal("SoftwareApplication", doc.RootElement.GetProperty("@type").GetString());
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
