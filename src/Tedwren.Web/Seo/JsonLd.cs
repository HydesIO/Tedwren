using System.Text.Json;
using Tedwren.Abstractions.Contracts.WebContent;

namespace Tedwren.Web.Seo;

/// <summary>
/// Builds schema.org JSON-LD documents for SEO (Web Plan §10): Organization sitewide, FAQPage on the
/// FAQ page, and SoftwareApplication on the pricing page. Each method returns a complete
/// <c>&lt;script type="application/ld+json"&gt;…&lt;/script&gt;</c> block emitted via <c>@Html.Raw</c>,
/// so neither the script tag nor the JSON is routed through Razor's attribute/HTML encoder. The default
/// serializer encoder still escapes <c>&lt;</c>, <c>&gt;</c> and <c>&amp;</c> inside the JSON, keeping it
/// valid and preventing a <c>&lt;/script&gt;</c> break-out.
/// </summary>
public static class JsonLd
{
    /// <summary>Organization schema (sitewide) built from site identity content.</summary>
    /// <param name="site">Site identity content.</param>
    /// <param name="baseUrl">Absolute site origin (e.g. https://tedwren.example).</param>
    public static string Organization(SiteContent site, string baseUrl)
    {
        var doc = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Organization",
            ["name"] = site.BrandName,
            ["legalName"] = site.LegalEntity,
            ["url"] = baseUrl,
        };

        if (site.Social.Count > 0)
        {
            doc["sameAs"] = site.Social.Select(s => s.Url).ToArray();
        }

        return Serialize(doc);
    }

    /// <summary>FAQPage schema built from the FAQ items.</summary>
    /// <param name="faqs">FAQ items.</param>
    public static string FaqPage(IReadOnlyList<FaqItem> faqs)
    {
        var doc = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "FAQPage",
            ["mainEntity"] = faqs.Select(f => new Dictionary<string, object?>
            {
                ["@type"] = "Question",
                ["name"] = f.Question,
                ["acceptedAnswer"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Answer",
                    ["text"] = f.Answer,
                },
            }).ToArray(),
        };

        return Serialize(doc);
    }

    /// <summary>SoftwareApplication schema for the pricing page.</summary>
    /// <param name="site">Site identity content.</param>
    /// <param name="url">Absolute URL of the pricing page.</param>
    public static string SoftwareApplication(SiteContent site, string url)
    {
        var doc = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "SoftwareApplication",
            ["name"] = site.BrandName,
            ["applicationCategory"] = "BusinessApplication",
            ["operatingSystem"] = "Web",
            ["url"] = url,
            ["offers"] = new Dictionary<string, object?>
            {
                ["@type"] = "Offer",
                ["priceCurrency"] = site.CurrencyCode,
            },
        };

        return Serialize(doc);
    }

    /// <summary>Serializes a schema document and wraps it in a complete JSON-LD script element.</summary>
    private static string Serialize(object doc) =>
        "<script type=\"application/ld+json\">" + JsonSerializer.Serialize(doc) + "</script>";
}
