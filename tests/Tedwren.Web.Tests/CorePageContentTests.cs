using Microsoft.AspNetCore.Mvc.Testing;

namespace Tedwren.Web.Tests;

/// <summary>
/// W3 integration tests: the core pages render from the content layer (Web Plan §6.1–6.3), the
/// required page-specific content is present ("company documents", the retrofit section), the
/// highlighted differentiator gets its distinct treatment, and product copy is not forked between the
/// home card and the dedicated page.
/// </summary>
public sealed class CorePageContentTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Injects the in-process site host.</summary>
    /// <param name="factory">Test host for Tedwren.Web.</param>
    public CorePageContentTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>Home renders hero, product cards, the how-it-works steps and the trust strip.</summary>
    [Fact]
    public async Task Home_RendersContentSections()
    {
        var body = await _factory.CreateClient().GetStringAsync("/");

        Assert.Contains("Compliance that works beyond the site gate", body); // hero heading (content)
        Assert.Contains("product-card", body);                              // product cards present
        Assert.Contains("how-it-works__step", body);                        // steps present
        Assert.Contains("trust-strip", body);                               // trust strip present
    }

    /// <summary>The strongest differentiator is rendered with the distinct highlight treatment (§6.1).</summary>
    [Fact]
    public async Task Home_HighlightsStrongestDifferentiator()
    {
        var body = await _factory.CreateClient().GetStringAsync("/");

        Assert.Contains("differentiator--highlight", body);
    }

    /// <summary>The subcontractor page includes the "company documents" feature (§6.2 dev-note).</summary>
    [Fact]
    public async Task Subcontractors_HasCompanyDocuments()
    {
        var body = await _factory.CreateClient().GetStringAsync("/subcontractors");

        Assert.Contains("Company documents", body);
    }

    /// <summary>The main-contractor page carries a distinct, emphasised retrofit section (§6.3).</summary>
    [Fact]
    public async Task MainContractors_HasEmphasisedRetrofitSection()
    {
        var body = await _factory.CreateClient().GetStringAsync("/main-contractors");

        // ASCII-safe substring: Razor entity-encodes the apostrophe in the full heading.
        Assert.Contains("Workforce management when there", body);
        Assert.Contains("product-section--emphasis", body);
    }

    /// <summary>Product copy is single-sourced: the tagline on the home card matches the product page.</summary>
    [Fact]
    public async Task ProductCopy_IsNotForked_BetweenHomeAndPage()
    {
        var client = _factory.CreateClient();
        // ASCII-safe prefix: the full tagline contains an em dash, which Razor entity-encodes.
        const string tagline = "Stay compliant on every site";

        var home = await client.GetStringAsync("/");
        var page = await client.GetStringAsync("/subcontractors");

        Assert.Contains(tagline, home);
        Assert.Contains(tagline, page);
    }
}
