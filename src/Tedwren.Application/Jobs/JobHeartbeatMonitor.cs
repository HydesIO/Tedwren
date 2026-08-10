using Tedwren.Abstractions.Notifications;
using Tedwren.Application.Expiry;
using Tedwren.Application.Persistence;

namespace Tedwren.Application.Jobs;

/// <summary>
/// Checks that each scheduled job has run successfully within its expected interval and alerts ops if one
/// has silently stopped (SF-21, R12). "If they stop, nothing in the interface changes and the data quietly
/// goes stale until a worker is turned away" — so this check is itself the safety net.
/// </summary>
public sealed class JobHeartbeatMonitor
{
    private readonly IJobRunRepository _runs;
    private readonly IEmailSender _email;
    private readonly ExpiryJobOptions _options;

    /// <summary>Creates the monitor over the job-run repository, the email sender and the options.</summary>
    public JobHeartbeatMonitor(IJobRunRepository runs, IEmailSender email, ExpiryJobOptions options)
    {
        _runs = runs;
        _email = email;
        _options = options;
    }

    /// <summary>
    /// Raises an ops alert for each job whose last successful run is missing or older than its allowed
    /// interval. Returns the number of alerts raised.
    /// </summary>
    public async Task<int> CheckAsync(DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        var alerts = 0;
        alerts += await CheckJobAsync(JobNames.ExpiryScan, _options.ExpiryScanMaxInterval, asOf, cancellationToken);
        alerts += await CheckJobAsync(JobNames.WeeklyDigest, _options.WeeklyDigestMaxInterval, asOf, cancellationToken);
        return alerts;
    }

    /// <summary>Checks one job and alerts if its last success is missing or overdue.</summary>
    private async Task<int> CheckJobAsync(string jobName, TimeSpan maxInterval, DateTimeOffset asOf, CancellationToken cancellationToken)
    {
        var last = await _runs.GetLastSuccessAsync(jobName, cancellationToken);
        var lastSuccessUtc = last?.FinishedUtc;
        var overdue = lastSuccessUtc is null || asOf - lastSuccessUtc.Value > maxInterval;
        if (!overdue)
        {
            return 0;
        }

        var lastText = lastSuccessUtc is null ? "never" : lastSuccessUtc.Value.ToString("u");
        await _email.SendAsync(
            _options.OpsEmail,
            $"Scheduled job '{jobName}' may have stopped",
            $"The job '{jobName}' has no successful run within its expected interval (last success: {lastText}). " +
            "Expiry warnings depend on it — please investigate.",
            cancellationToken);
        return 1;
    }
}
