using Microsoft.AspNetCore.Http;

namespace Tedwren.Web.Attribution;

/// <summary>
/// Captured UTM attribution parameters (Web Plan §4.1, §11). The compliance-pack viewer's "Book a demo"
/// link carries a <c>utm_source=pack</c> tag, and the demo form captures whatever UTMs the visitor
/// arrived with so a pack-driven booking is attributable (GTM Stage 2).
/// </summary>
/// <param name="Source">utm_source (e.g. "pack").</param>
/// <param name="Medium">utm_medium.</param>
/// <param name="Campaign">utm_campaign.</param>
public sealed record Utm(string? Source, string? Medium, string? Campaign)
{
    /// <summary>Reads the UTM parameters from a request query string.</summary>
    /// <param name="query">The request query collection.</param>
    public static Utm FromQuery(IQueryCollection query) => new(
        query["utm_source"].FirstOrDefault(),
        query["utm_medium"].FirstOrDefault(),
        query["utm_campaign"].FirstOrDefault());

    /// <summary>The canonical UTMs for the compliance-pack viewer's demo link (Plan §4.1).</summary>
    public static readonly Utm Pack = new("pack", "compliance_pack", "gtm_stage2");

    /// <summary>Builds a demo URL with these UTM parameters appended (omitting empty ones).</summary>
    public string ToDemoUrl()
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(Source)) parts.Add("utm_source=" + Uri.EscapeDataString(Source));
        if (!string.IsNullOrEmpty(Medium)) parts.Add("utm_medium=" + Uri.EscapeDataString(Medium));
        if (!string.IsNullOrEmpty(Campaign)) parts.Add("utm_campaign=" + Uri.EscapeDataString(Campaign));
        return parts.Count == 0 ? "/demo" : "/demo?" + string.Join('&', parts);
    }
}
