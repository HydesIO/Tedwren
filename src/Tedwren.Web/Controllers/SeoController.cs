using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tedwren.Web.Configuration;
using Tedwren.Web.Seo;

namespace Tedwren.Web.Controllers;

/// <summary>
/// Serves the crawler documents generated from the route list (Web Plan §9/§10): <c>/sitemap.xml</c>
/// and <c>/robots.txt</c>. Both are produced by <see cref="SitemapBuilder"/> from the live
/// <see cref="SiteConfig"/> nav so they are never hand-maintained and cannot drift from the pages that
/// actually exist.
/// </summary>
public sealed class SeoController : Controller
{
    private readonly SiteConfig _site;

    /// <summary>Creates the controller with the bound site nav configuration.</summary>
    /// <param name="site">Site nav configuration (the route list).</param>
    public SeoController(IOptions<SiteConfig> site) => _site = site.Value;

    /// <summary>The XML sitemap listing every indexable page under the current origin.</summary>
    [HttpGet("/sitemap.xml")]
    public IActionResult Sitemap()
    {
        var xml = SitemapBuilder.BuildXml(Origin(), SitemapBuilder.IndexablePaths(_site));
        return Content(xml, "application/xml");
    }

    /// <summary>The robots policy: crawlable, capability URLs disallowed, sitemap advertised.</summary>
    [HttpGet("/robots.txt")]
    public IActionResult Robots() => Content(SitemapBuilder.BuildRobots(Origin()), "text/plain");

    /// <summary>The absolute origin of the current request (scheme + host), no trailing slash.</summary>
    private string Origin() => $"{Request.Scheme}://{Request.Host}";
}
