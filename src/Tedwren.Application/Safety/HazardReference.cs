namespace Tedwren.Application.Safety;

/// <summary>
/// Builds the human-readable hazard / near-miss report reference (PRD §8.2). Shared by the console
/// <see cref="HazardReportService"/> and the mobile <c>MobileHazardService</c> so both write paths produce the
/// same format.
/// </summary>
internal static class HazardReference
{
    /// <summary>A new reference: date + short random suffix, e.g. "HAZ-20260919-AB12CD".</summary>
    public static string New() =>
        $"HAZ-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
