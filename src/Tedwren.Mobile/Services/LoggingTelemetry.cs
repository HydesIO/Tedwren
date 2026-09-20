using Microsoft.Extensions.Logging;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Services;

/// <summary>
/// The default MAUI telemetry / crash reporter (M8): records events, errors and timings through <see cref="ILogger"/>.
/// A real, UK-hosted analytics/crash vendor (R13) is a deployment decision — see <c>docs/mobile-store-readiness.md</c>;
/// this keeps the seam useful (local logs in dev, unhandled-exception capture) without committing to a vendor.
/// Every method is best-effort and never throws.
/// </summary>
public sealed class LoggingTelemetry : ITelemetry
{
    private readonly ILogger<LoggingTelemetry> _logger;

    /// <summary>Creates the telemetry over the app logger.</summary>
    public LoggingTelemetry(ILogger<LoggingTelemetry> logger) => _logger = logger;

    /// <inheritdoc />
    public void TrackEvent(string name, IReadOnlyDictionary<string, string>? properties = null)
    {
        try
        {
            _logger.LogInformation("telemetry event {Event}", name);
        }
        catch (Exception)
        {
            // Telemetry is best-effort — never let it surface to the caller.
        }
    }

    /// <inheritdoc />
    public void TrackError(Exception exception, string? context = null)
    {
        try
        {
            _logger.LogError(exception, "telemetry error {Context}", context ?? "(none)");
        }
        catch (Exception)
        {
            // Best-effort.
        }
    }

    /// <inheritdoc />
    public void TrackTiming(string name, TimeSpan duration)
    {
        try
        {
            _logger.LogInformation("telemetry timing {Timing} {Milliseconds}ms", name, (long)duration.TotalMilliseconds);
        }
        catch (Exception)
        {
            // Best-effort.
        }
    }
}
