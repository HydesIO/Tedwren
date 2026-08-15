using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Billing;

/// <summary>
/// Pulls BACS payouts from GoCardless and mirrors them locally so the admin area can reconcile settlement.
/// Deduped by GoCardless payout id (a re-sync updates the existing row rather than duplicating). Safe when
/// GoCardless is not configured — it simply does nothing. Same shape as <see cref="BillingReconciliationService"/>.
/// </summary>
public sealed class PayoutSyncService
{
    private readonly IGoCardlessClient _goCardless;
    private readonly IPayoutRepository _payouts;

    /// <summary>Creates the service over the GoCardless client and the payout repository.</summary>
    public PayoutSyncService(IGoCardlessClient goCardless, IPayoutRepository payouts)
    {
        _goCardless = goCardless;
        _payouts = payouts;
    }

    /// <summary>Fetches payouts from GoCardless and upserts them locally. Returns how many were added/updated.</summary>
    public async Task<int> SyncAsync(CancellationToken cancellationToken = default)
    {
        var synced = 0;
        try
        {
            foreach (var remote in await _goCardless.ListPayoutsAsync(cancellationToken))
            {
                var status = GoCardlessStatusMap.ToPayoutStatus(remote.Status);
                var existing = await _payouts.GetByGoCardlessPayoutIdAsync(remote.Id, cancellationToken);
                if (existing is null)
                {
                    await _payouts.AddAsync(new Payout
                    {
                        GoCardlessPayoutId = remote.Id,
                        AmountPence = remote.Amount,
                        Currency = remote.Currency,
                        Status = status,
                        Reference = remote.Reference,
                        ArrivalDate = remote.ArrivalDate,
                    }, cancellationToken);
                    synced++;
                }
                else if (existing.Status != status || existing.AmountPence != remote.Amount)
                {
                    existing.Status = status;
                    existing.AmountPence = remote.Amount;
                    existing.Currency = remote.Currency;
                    existing.Reference = remote.Reference;
                    existing.ArrivalDate = remote.ArrivalDate;
                    existing.UpdatedUtc = DateTimeOffset.UtcNow;
                    await _payouts.UpdateAsync(existing, cancellationToken);
                    synced++;
                }
            }
        }
        catch (GoCardlessNotConfiguredException)
        {
            // No provider configured — nothing to sync.
        }

        return synced;
    }
}
