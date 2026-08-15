using Tedwren.Application.Billing;

namespace Tedwren.Api.Hosting;

/// <summary>
/// Periodically reconciles local billing state with GoCardless (Phase C), backstopping the webhooks: if a
/// webhook is missed or late, the next pass still converges mandate/payment status. Modeled on
/// <see cref="ExpirySchedulerHostedService"/> — scoped per run, gated by <c>Jobs:SchedulerEnabled</c>, with an
/// initial delay. Does nothing when GoCardless is not configured (the service no-ops), so it is safe to leave on.
/// </summary>
public sealed class BillingReconciliationHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BillingReconciliationHostedService> _logger;
    private readonly bool _enabled;
    private readonly TimeSpan _initialDelay;
    private readonly TimeSpan _interval;

    /// <summary>Creates the reconciler, reading its cadence and enabled flag from configuration.</summary>
    public BillingReconciliationHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BillingReconciliationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _enabled = configuration.GetValue("Jobs:SchedulerEnabled", true);
        _initialDelay = TimeSpan.FromSeconds(configuration.GetValue("Jobs:InitialDelaySeconds", 30));
        _interval = TimeSpan.FromHours(configuration.GetValue("Jobs:ReconciliationIntervalHours", 6));
    }

    /// <summary>The loop: waits the initial delay, then reconciles every interval until shutdown.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Billing reconciliation disabled (Jobs:SchedulerEnabled=false).");
            return;
        }

        try
        {
            await Task.Delay(_initialDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await ReconcileAsync(stoppingToken);
                await Task.Delay(_interval, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    /// <summary>Runs one reconciliation pass in its own DI scope.</summary>
    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<BillingReconciliationService>();
            var result = await service.ReconcileAsync(cancellationToken);
            if (result.MandatesUpdated > 0 || result.PaymentsUpdated > 0)
            {
                _logger.LogInformation("Billing reconciliation updated {Mandates} mandate(s) and {Payments} payment(s).",
                    result.MandatesUpdated, result.PaymentsUpdated);
            }

            // Refresh BACS payouts on the same schedule (Phase D). Quiet no-op when GoCardless is unconfigured.
            var payoutSync = scope.ServiceProvider.GetRequiredService<PayoutSyncService>();
            var payoutsSynced = await payoutSync.SyncAsync(cancellationToken);
            if (payoutsSynced > 0)
            {
                _logger.LogInformation("Billing reconciliation synced {Payouts} payout(s).", payoutsSynced);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Billing reconciliation failed.");
        }
    }
}
