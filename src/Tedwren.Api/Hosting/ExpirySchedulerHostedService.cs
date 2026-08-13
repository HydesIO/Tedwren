using Tedwren.Application.Attendance;
using Tedwren.Application.Expiry;
using Tedwren.Application.Forms;
using Tedwren.Application.Jobs;

namespace Tedwren.Api.Hosting;

/// <summary>
/// Runs the expiry engine on a schedule in a real deployment: the expiry-warning scan and the job
/// heartbeat each interval, and the weekly digest once a week. Each run is scoped (repositories are
/// scoped) and recorded via <see cref="JobRunner"/> (SF-21). Gated by <c>Jobs:SchedulerEnabled</c> so
/// tests and ad-hoc runs can disable it; an initial delay keeps it out of the way of short-lived hosts.
/// </summary>
public sealed class ExpirySchedulerHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExpirySchedulerHostedService> _logger;
    private readonly bool _enabled;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _interval;

    /// <summary>Creates the scheduler, reading its cadence and enabled flag from configuration.</summary>
    public ExpirySchedulerHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ExpirySchedulerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _enabled = configuration.GetValue("Jobs:SchedulerEnabled", true);
        _initialDelay = TimeSpan.FromSeconds(configuration.GetValue("Jobs:InitialDelaySeconds", 30));
        _interval = TimeSpan.FromHours(configuration.GetValue("Jobs:IntervalHours", 24));
    }

    /// <summary>The scheduler loop: waits the initial delay, then runs the due jobs every interval.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Expiry scheduler disabled (Jobs:SchedulerEnabled=false).");
            return;
        }

        try
        {
            await Task.Delay(_initialDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await RunDueJobsAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    /// <summary>Runs the expiry scan + heartbeat every tick, and the weekly digest on Mondays.</summary>
    private async Task RunDueJobsAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider;
        var runner = provider.GetRequiredService<JobRunner>();

        try
        {
            var scan = provider.GetRequiredService<ExpiryWarningJob>();
            await runner.RunAsync(JobNames.ExpiryScan, async token =>
            {
                var result = await scan.RunAsync(today, token);
                return (result.CardsEvaluated, result.NotificationsSent);
            }, cancellationToken);

            if (DateTime.UtcNow.DayOfWeek == DayOfWeek.Monday)
            {
                var digest = provider.GetRequiredService<WeeklyDigestJob>();
                await runner.RunAsync(JobNames.WeeklyDigest, async token =>
                {
                    var result = await digest.RunAsync(today, token);
                    return (result.CompaniesProcessed, result.EmailsSent);
                }, cancellationToken);
            }

            // R12: remind admins of recurring forms (Daily/Weekly/Monthly) not yet completed this period.
            var reminders = provider.GetRequiredService<RecurringFormReminderJob>();
            await runner.RunAsync(JobNames.FormReminder, async token =>
            {
                var result = await reminders.RunAsync(DateTimeOffset.UtcNow, token);
                return (result.AssignmentsEvaluated, result.RemindersSent);
            }, cancellationToken);

            // SF-19: flag anyone left signed in before today started.
            var overnight = provider.GetRequiredService<OvernightSignInJob>();
            var cutoff = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
            await runner.RunAsync(JobNames.OvernightCheck, async token =>
            {
                var (flagged, notifications) = await overnight.RunAsync(cutoff, token);
                return (flagged, notifications);
            }, cancellationToken);

            var monitor = provider.GetRequiredService<JobHeartbeatMonitor>();
            await monitor.CheckAsync(DateTimeOffset.UtcNow, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Scheduled expiry jobs failed.");
        }
    }
}
