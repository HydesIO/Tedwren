namespace Tedwren.Domain.Jobs;

/// <summary>
/// A record of one scheduled-job execution (SF-21, R12). Every scheduled job records whether it ran and
/// what it did, so a job that silently stops can be detected and someone alerted — the most dangerous
/// failure mode in the product, because nothing in the interface would otherwise reveal it.
/// </summary>
public sealed class JobRun
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The job's name (e.g. "expiry-scan", "weekly-digest").</summary>
    public required string JobName { get; init; }

    /// <summary>When the run started (UTC).</summary>
    public DateTimeOffset StartedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the run finished, once it has (UTC).</summary>
    public DateTimeOffset? FinishedUtc { get; set; }

    /// <summary>The run's current status.</summary>
    public JobRunStatus Status { get; set; } = JobRunStatus.Running;

    /// <summary>How many items the run processed (e.g. cards evaluated).</summary>
    public int ItemsProcessed { get; set; }

    /// <summary>How many notifications the run sent.</summary>
    public int NotificationsSent { get; set; }

    /// <summary>The error message when the run failed, if any.</summary>
    public string? Error { get; set; }
}
