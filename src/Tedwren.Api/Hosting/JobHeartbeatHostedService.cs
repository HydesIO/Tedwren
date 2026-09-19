using Tedwren.Application.Jobs;

namespace Tedwren.Api.Hosting;

/// <summary>
/// Runs the <see cref="JobHeartbeatMonitor"/> on its own schedule, independently of the
/// <see cref="ExpirySchedulerHostedService"/> job-execution loop (R12). Because it does not share that loop, a
/// failure that stops the scheduled jobs from running does not also stop the check that is supposed to notice —
/// the watchdog keeps checking and alerting. It runs more frequently than the daily job cadence so a silent stop
/// is detected in hours, not a day. Gated by <c>Jobs:SchedulerEnabled</c> (off for the test host) and logs a
/// warning as a second channel alongside the monitor's email, so an alert is visible even if email is unconfigured.
/// </summary>
public sealed class JobHeartbeatHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobHeartbeatHostedService> _logger;
    private readonly bool _enabled;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _interval;

    /// <summary>Creates the watchdog, reading its cadence and enabled flag from configuration.</summary>
    public JobHeartbeatHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<JobHeartbeatHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _enabled = configuration.GetValue("Jobs:SchedulerEnabled", true);
        // A shorter, independent cadence than the 24h job loop so a stopped job is caught within hours.
        _initialDelay = TimeSpan.FromSeconds(configuration.GetValue("Jobs:InitialDelaySeconds", 30));
        _interval = TimeSpan.FromHours(configuration.GetValue("Jobs:HeartbeatIntervalHours", 6));
    }

    /// <summary>The watchdog loop: waits the initial delay, then checks the heartbeat every interval until shutdown.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Job heartbeat watchdog disabled (Jobs:SchedulerEnabled=false).");
            return;
        }

        try
        {
            await Task.Delay(_initialDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    /// <summary>Runs one heartbeat check in its own DI scope, logging a warning when any job is overdue.</summary>
    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var monitor = scope.ServiceProvider.GetRequiredService<JobHeartbeatMonitor>();
            var alerts = await monitor.CheckAsync(DateTimeOffset.UtcNow, cancellationToken);
            if (alerts > 0)
            {
                _logger.LogWarning(
                    "Job heartbeat watchdog raised {Alerts} alert(s): one or more scheduled jobs may have stopped (R12).",
                    alerts);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Job heartbeat watchdog check failed.");
        }
    }
}
