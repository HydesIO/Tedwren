using System.Text;
using System.Xml;
using Tedwren.Web.Configuration;

namespace Tedwren.Web.Seo;

/// <summary>
/// Generates <c>sitemap.xml</c> and <c>robots.txt</c> from the site's route list rather than by hand
/// (Web Plan §9/§10): the indexable page set is derived from the same <see cref="SiteConfig"/> nav that
/// drives the header and footer, so a page added to navigation is discoverable to crawlers automatically
/// and cannot drift out of sync. Capability URLs (the private partner dashboard, referral redirects) are
/// deliberately excluded from the sitemap and disallowed in robots. Single responsibility: turn a route
/// list into the two crawler documents; it owns no HTTP or configuration concerns.
/// </summary>
public static class SitemapBuilder
{
    /// <summary>The home page, always indexable and listed first, and never present in the nav lists.</summary>
    private const string HomePath = "/";

    /// <summary>The demo conversion page — indexable but reached via CTAs, not the nav lists.</summary>
    private const string DemoPath = "/demo";

    /// <summary>
    /// Path prefixes that must never be crawled or indexed: the partner dashboard is a private
    /// capability URL keyed by referral code, and <c>/r/</c> are one-hop referral redirects (Plan §7).
    /// </summary>
    public static readonly IReadOnlyList<string> DisallowedPrefixes = new[] { "/partners/dashboard", "/r/" };

    /// <summary>
    /// The indexable page set, derived from the nav configuration (primary nav, then any additional
    /// footer links, then legal links) with the home and demo pages, de-duplicated in first-seen order.
    /// </summary>
    /// <param name="site">Site nav configuration — the route list.</param>
    public static IReadOnlyList<string> IndexablePaths(SiteConfig site)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var paths = new List<string>();

        void Add(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || IsDisallowed(path))
            {
                return;
            }

            if (seen.Add(path))
            {
                paths.Add(path);
            }
        }

        Add(HomePath);
        foreach (var link in site.PrimaryNav) Add(link.Href);
        Add(DemoPath);
        foreach (var link in site.FooterLinks) Add(link.Href);
        foreach (var link in site.LegalLinks) Add(link.Href);

        return paths;
    }

    /// <summary>Builds a valid urlset sitemap document for the given paths under the site origin.</summary>
    /// <param name="baseUrl">Absolute site origin (e.g. "https://tedwren.example"), no trailing slash.</param>
    /// <param name="paths">Site-relative paths to include.</param>
    public static string BuildXml(string baseUrl, IEnumerable<string> paths)
    {
        var origin = baseUrl.TrimEnd('/');
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 });
        writer.WriteStartDocument();
        writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");
        foreach (var path in paths)
        {
            writer.WriteStartElement("url");
            writer.WriteElementString("loc", origin + path);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Flush();
        return sb.ToString();
    }

    /// <summary>
    /// Builds a robots.txt that allows crawling, disallows the capability-URL prefixes, and advertises
    /// the sitemap absolute URL so crawlers discover the full route list.
    /// </summary>
    /// <param name="baseUrl">Absolute site origin, no trailing slash.</param>
    public static string BuildRobots(string baseUrl)
    {
        var origin = baseUrl.TrimEnd('/');
        var sb = new StringBuilder();
        sb.Append("User-agent: *\n");
        foreach (var prefix in DisallowedPrefixes)
        {
            sb.Append("Disallow: ").Append(prefix).Append('\n');
        }

        sb.Append("Sitemap: ").Append(origin).Append("/sitemap.xml\n");
        return sb.ToString();
    }

    /// <summary>True when a path sits under one of the disallowed capability-URL prefixes.</summary>
    private static bool IsDisallowed(string path) =>
        DisallowedPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
