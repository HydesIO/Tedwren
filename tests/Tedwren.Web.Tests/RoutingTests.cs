using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tedwren.Web.Tests;

/// <summary>
/// W1 exit-criteria tests: every route in the sitemap (Web Plan §4) resolves, unknown routes render
/// the friendly error page, and the config-driven header/footer render. Uses the MVC test host, so
/// no network or database is involved.
/// </summary>
public sealed class RoutingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Injects the in-process site host.</summary>
    /// <param name="factory">Test host for Tedwren.Web.</param>
    public RoutingTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>Every sitemap route returns 200 OK.</summary>
    /// <param name="route">The path to request.</param>
    [Theory]
    [InlineData("/")]
    [InlineData("/subcontractors")]
    [InlineData("/main-contractors")]
    [InlineData("/worker-passport")]
    [InlineData("/pricing")]
    [InlineData("/security")]
    [InlineData("/about")]
    [InlineData("/faq")]
    [InlineData("/demo")]
    [InlineData("/partners")]
    [InlineData("/contact")]
    [InlineData("/legal/privacy")]
    [InlineData("/legal/cookies")]
    [InlineData("/legal/terms")]
    [InlineData("/legal/data-protection")]
    public async Task Route_Resolves_WithOk(string route)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(route);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>An unknown legal slug is rejected by the route constraint (renders the 404 page).</summary>
    [Fact]
    public async Task UnknownLegalSlug_ReturnsNotFound()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/legal/made-up");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>An unknown path re-executes the friendly error page instead of a bare 404.</summary>
    [Fact]
    public async Task UnknownRoute_RendersErrorPage()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/no-such-page");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("couldn't find that page", body);
    }

    /// <summary>The header and footer render brand/legal-entity/nav sourced from config.</summary>
    [Fact]
    public async Task Chrome_RendersFromConfig()
    {
        var client = _factory.CreateClient();

        var body = await client.GetStringAsync("/");

        Assert.Contains("Tedwren Ltd", body);          // footer legal entity (config)
        Assert.Contains("For Subcontractors", body);   // primary nav label (config)
        Assert.Contains("class=\"site-header\"", body);
        Assert.Contains("class=\"site-footer\"", body);
    }

    /// <summary>The shared stylesheets — including tokens.css sourced from the client — are served.</summary>
    /// <param name="cssPath">The stylesheet path to request.</param>
    [Theory]
    [InlineData("/css/tokens.css")]
    [InlineData("/css/site.css")]
    public async Task Stylesheet_IsServed(string cssPath)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(cssPath);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>The served tokens.css is the client's file (brand token present), not a fork.</summary>
    [Fact]
    public async Task TokensCss_IsSharedFromClient()
    {
        var client = _factory.CreateClient();

        var css = await client.GetStringAsync("/css/tokens.css");

        Assert.Contains("--color-brand", css);
    }

    /// <summary>The header CTA swaps to the Worker Passport action on that page only (Web Plan §5).</summary>
    [Fact]
    public async Task Header_SwapsCta_OnWorkerPassportPage()
    {
        var client = _factory.CreateClient();

        var home = await client.GetStringAsync("/");
        var passport = await client.GetStringAsync("/worker-passport");

        Assert.Contains("Book a demo", home);
        Assert.Contains("Get your Worker Passport", passport);
    }
}
