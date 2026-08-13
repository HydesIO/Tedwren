using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Web.Configuration;
using Tedwren.Web.Seo;

namespace Tedwren.Web.Tests;

/// <summary>
/// W8 SEO infrastructure tests (Web Plan §9/§10, §14): the sitemap and robots documents are generated
/// from the route list and exclude capability URLs; every page declares a canonical URL, exactly one
/// H1, a title within the SERP budget and (where present) a meta description within budget.
/// </summary>
public sealed class SeoInfrastructureTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Injects the in-process site host.</summary>
    /// <param name="factory">Test host for Tedwren.Web.</param>
    public SeoInfrastructureTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>The indexable content pages whose SEO metadata is asserted.</summary>
    public static TheoryData<string> IndexablePages() => new()
    {
        "/", "/subcontractors", "/main-contractors", "/worker-passport", "/pricing",
        "/security", "/about", "/faq", "/partners", "/demo", "/contact",
        "/legal/privacy", "/legal/cookies", "/legal/terms", "/legal/data-protection",
    };

    // ---- sitemap.xml / robots.txt (generated from the route list) ----

    /// <summary>The sitemap is valid XML, lists core pages, and omits the private capability URLs.</summary>
    [Fact]
    public async Task Sitemap_ListsIndexablePages_ExcludesCapabilityUrls()
    {
        var response = await _factory.CreateClient().GetAsync("/sitemap.xml");
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);

        var xml = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(xml); // throws if the document is not well-formed
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var locs = doc.Descendants(ns + "loc").Select(e => e.Value).ToList();

        Assert.Contains(locs, l => l.EndsWith("/"));
        Assert.Contains(locs, l => l.EndsWith("/subcontractors"));
        Assert.Contains(locs, l => l.EndsWith("/pricing"));
        Assert.Contains(locs, l => l.EndsWith("/demo"));
        Assert.Contains(locs, l => l.EndsWith("/legal/privacy"));
        Assert.DoesNotContain(locs, l => l.Contains("/partners/dashboard"));
        Assert.DoesNotContain(locs, l => l.Contains("/r/"));
    }

    /// <summary>robots allows crawling, disallows capability URLs, and advertises the sitemap.</summary>
    [Fact]
    public async Task Robots_DisallowsCapabilityUrls_AndAdvertisesSitemap()
    {
        var response = await _factory.CreateClient().GetAsync("/robots.txt");
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/plain", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("User-agent: *", body);
        Assert.Contains("Disallow: /partners/dashboard", body);
        Assert.Contains("Disallow: /r/", body);
        Assert.Contains("/sitemap.xml", body);
    }

    // ---- per-page metadata ----

    /// <summary>Every indexable page declares exactly one canonical URL.</summary>
    [Theory]
    [MemberData(nameof(IndexablePages))]
    public async Task Page_DeclaresCanonical(string path)
    {
        var body = await _factory.CreateClient().GetStringAsync(path);

        var canonicalCount = Regex.Matches(body, "rel=\"canonical\"").Count;
        Assert.Equal(1, canonicalCount);
    }

    /// <summary>Every indexable page has exactly one H1 (SEO/accessibility, Plan §10).</summary>
    [Theory]
    [MemberData(nameof(IndexablePages))]
    public async Task Page_HasExactlyOneH1(string path)
    {
        var body = await _factory.CreateClient().GetStringAsync(path);

        var h1Count = Regex.Matches(body, "<h1[ >]", RegexOptions.IgnoreCase).Count;
        Assert.Equal(1, h1Count);
    }

    /// <summary>Every page's title tag is present and within the ~60-char SERP budget (Plan §10).</summary>
    [Theory]
    [MemberData(nameof(IndexablePages))]
    public async Task Page_TitleWithinBudget(string path)
    {
        var body = await _factory.CreateClient().GetStringAsync(path);
        var title = Regex.Match(body, "<title>(?<t>.*?)</title>", RegexOptions.Singleline).Groups["t"].Value;

        Assert.False(string.IsNullOrWhiteSpace(title));
        Assert.True(title.Length <= 60, $"Title too long ({title.Length}) on {path}: {title}");
    }

    /// <summary>Where a meta description is present it stays within the ~155-char SERP budget (Plan §10).</summary>
    [Theory]
    [MemberData(nameof(IndexablePages))]
    public async Task Page_MetaDescriptionWithinBudget(string path)
    {
        var body = await _factory.CreateClient().GetStringAsync(path);
        var match = Regex.Match(body, "<meta name=\"description\" content=\"(?<d>.*?)\"", RegexOptions.Singleline);

        if (match.Success)
        {
            var description = match.Groups["d"].Value;
            Assert.True(description.Length <= 155, $"Meta description too long ({description.Length}) on {path}");
        }
    }

    // ---- SitemapBuilder unit behaviour ----

    /// <summary>The route list is de-duplicated and never includes a disallowed capability prefix.</summary>
    [Fact]
    public void IndexablePaths_Deduplicates_AndDropsCapabilityUrls()
    {
        var site = new SiteConfig
        {
            PrimaryNav = { new NavLink { Href = "/pricing" }, new NavLink { Href = "/about" } },
            FooterLinks = { new NavLink { Href = "/pricing" }, new NavLink { Href = "/partners/dashboard/abc" } },
            LegalLinks = { new NavLink { Href = "/legal/privacy" } },
        };

        var paths = SitemapBuilder.IndexablePaths(site);

        Assert.Equal("/", paths[0]);
        Assert.Single(paths, p => p == "/pricing");                 // de-duplicated
        Assert.DoesNotContain("/partners/dashboard/abc", paths);    // capability URL dropped
        Assert.Contains("/demo", paths);
    }
}
