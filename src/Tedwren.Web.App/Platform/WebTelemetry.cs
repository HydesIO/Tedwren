using Microsoft.Extensions.Logging;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Web.App.Platform;

/// <summary>
/// Browser <see cref="ITelemetry"/> that records events/errors/timings through <see cref="ILogger"/> (the browser
/// console), mirroring the MAUI head's logging telemetry. Per the contract it must never throw, so every call is
/// guarded.
/// </summary>
public sealed class WebTelemetry : ITelemetry
{
    private readonly ILogger<WebTelemetry> _log;

    /// <summary>Creates the telemetry over the app logger.</summary>
    public WebTelemetry(ILogger<WebTelemetry> log) => _log = log;

    /// <summary>Records a named event with optional properties.</summary>
    public void TrackEvent(string name, IReadOnlyDictionary<string, string>? properties = null)
    {
        try
        {
            _log.LogInformation("telemetry event {EventName} {@Properties}", name, properties);
        }
        catch
        {
            // Telemetry is best-effort — never surface a reporting failure to the caller.
        }
    }

    /// <summary>Records a handled or unhandled error with optional context.</summary>
    public void TrackError(Exception exception, string? context = null)
    {
        try
        {
            _log.LogError(exception, "telemetry error {Context}", context);
        }
        catch
        {
            // Best-effort.
        }
    }

    /// <summary>Records a named timing (e.g. a network round-trip).</summary>
    public void TrackTiming(string name, TimeSpan duration)
    {
        try
        {
            _log.LogDebug("telemetry timing {TimingName} {Milliseconds}ms", name, duration.TotalMilliseconds);
        }
        catch
        {
            // Best-effort.
        }
    }
}
