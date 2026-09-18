using System.Globalization;

namespace Tedwren.Abstractions.Common;

/// <summary>
/// Formats stored UTC instants in UK local time (R11). Times are persisted in UTC and displayed in UK local
/// time with a BST/GMT suffix so a reader is never off by an hour across the daylight-saving boundary, and the
/// zone is never ambiguous. Shared by the client (grid rendering) and the server (CSV export) so both agree.
/// </summary>
public static class UkTime
{
    /// <summary>The default display format (e.g. "18 Sep 2026, 14:30 BST"). CSV export uses a comma-free variant.</summary>
    public const string DefaultFormat = "dd MMM yyyy, HH:mm";

    /// <summary>
    /// The UK time zone, resolved once. The IANA id ("Europe/London") resolves on Linux and the browser (WASM,
    /// via ICU); the Windows id ("GMT Standard Time") is the fallback. If neither resolves, UTC is used so
    /// formatting never throws — the value is then simply the stored UTC.
    /// </summary>
    private static readonly TimeZoneInfo Zone = ResolveZone();

    /// <summary>Converts a stored UTC instant to UK local time.</summary>
    public static DateTimeOffset ToUk(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, Zone);

    /// <summary>
    /// Formats a stored UTC instant in UK local time with a trailing " BST"/" GMT" so the zone is explicit (R11).
    /// </summary>
    public static string Format(DateTimeOffset utc, string format = DefaultFormat)
    {
        var local = ToUk(utc);
        var suffix = Zone.IsDaylightSavingTime(local) ? " BST" : " GMT";
        return local.ToString(format, CultureInfo.InvariantCulture) + suffix;
    }

    /// <summary>Resolves the UK zone across platforms, falling back to UTC rather than throwing.</summary>
    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Europe/London", "GMT Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
