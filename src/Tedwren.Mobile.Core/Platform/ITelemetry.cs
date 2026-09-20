namespace Tedwren.Mobile.Core.Platform;

/// <summary>
/// A minimal telemetry + crash-reporting seam (M8). The app records events, errors and timings through this
/// abstraction; the MAUI head supplies the implementation (a logging one by default — a real UK-hosted vendor,
/// R13, is a deployment decision). Core keeps a <see cref="NoOpTelemetry"/> default so logic that reports through
/// it stays testable off-device. Implementations must never throw — telemetry is best-effort.
/// </summary>
public interface ITelemetry
{
    /// <summary>Records a named event with optional string properties.</summary>
    void TrackEvent(string name, IReadOnlyDictionary<string, string>? properties = null);

    /// <summary>Records a handled or unhandled error, with optional context.</summary>
    void TrackError(Exception exception, string? context = null);

    /// <summary>Records a named timing (e.g. a network round-trip) for latency budgets such as R14.</summary>
    void TrackTiming(string name, TimeSpan duration);
}

/// <summary>The default no-op telemetry — used off-device and until a real reporter is wired (M8).</summary>
public sealed class NoOpTelemetry : ITelemetry
{
    /// <inheritdoc />
    public void TrackEvent(string name, IReadOnlyDictionary<string, string>? properties = null)
    {
    }

    /// <inheritdoc />
    public void TrackError(Exception exception, string? context = null)
    {
    }

    /// <inheritdoc />
    public void TrackTiming(string name, TimeSpan duration)
    {
    }
}
