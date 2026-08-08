namespace Tedwren.Application.Expiry;

/// <summary>
/// Options for the expiry engine and heartbeat, supplied by the composition root (a plain object so the
/// Application layer takes no dependency on the configuration system). Defaults are sensible for dev.
/// </summary>
public sealed class ExpiryJobOptions
{
    /// <summary>Where "a job may have stopped" alerts are sent (R12).</summary>
    public string OpsEmail { get; init; } = "ops@tedwren.local";

    /// <summary>How long the expiry scan may go without a successful run before it is flagged overdue (R12).</summary>
    public TimeSpan ExpiryScanMaxInterval { get; init; } = TimeSpan.FromHours(25);

    /// <summary>How long the weekly digest may go without a successful run before it is flagged overdue (R12).</summary>
    public TimeSpan WeeklyDigestMaxInterval { get; init; } = TimeSpan.FromDays(8);
}
