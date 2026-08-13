using Microsoft.AspNetCore.Http;
using Tedwren.Web.Consent;

namespace Tedwren.Web.Analytics;

/// <summary>
/// Decides whether an analytics tag or conversion event may fire (Web Plan §8): only when the visitor
/// has granted analytics consent AND a GA4 measurement id is configured. This single rule is used by the
/// layout (to emit GA4) and by pages (to emit a conversion event), so nothing fires pre-consent anywhere.
/// </summary>
public static class AnalyticsState
{
    /// <summary>Returns the GA4 id to use when analytics may fire, or null when it must not.</summary>
    /// <param name="context">Current HTTP context.</param>
    /// <param name="options">Analytics options.</param>
    public static string? ActiveGa4Id(HttpContext context, AnalyticsOptions options)
    {
        if (string.IsNullOrEmpty(options.Ga4MeasurementId))
        {
            return null;
        }

        return ConsentReader.FromRequest(context).Analytics ? options.Ga4MeasurementId : null;
    }
}
