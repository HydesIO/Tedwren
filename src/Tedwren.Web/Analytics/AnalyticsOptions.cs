namespace Tedwren.Web.Analytics;

/// <summary>
/// Analytics options (Web Plan §8, §11), bound from the "Analytics" section. The GA4 measurement id is
/// empty by default, so no analytics tag is ever emitted unless it is both configured and consented to.
/// This is the belt to consent's braces: even with consent, nothing loads without an id.
/// </summary>
public sealed class AnalyticsOptions
{
    /// <summary>Configuration section name this options type binds from.</summary>
    public const string SectionName = "Analytics";

    /// <summary>GA4 measurement id (e.g. "G-XXXXXXX"). Empty means analytics is disabled entirely.</summary>
    public string? Ga4MeasurementId { get; set; }
}
