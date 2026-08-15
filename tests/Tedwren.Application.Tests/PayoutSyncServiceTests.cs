using Tedwren.Application.Billing;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Tests for <see cref="PayoutSyncService"/> (Phase D): payouts are mirrored locally, a re-sync updates
/// rather than duplicates (deduped by GoCardless payout id), and an unconfigured provider is a safe no-op.
/// </summary>
public sealed class PayoutSyncServiceTests
{
    /// <summary>Returns a configurable list of payouts so the sync can be asserted without HTTP.</summary>
    private sealed class FakeGoCardlessClient : IGoCardlessClient
    {
        public List<GoCardlessPayout> Payouts { get; } = new();

        public Task<GoCardlessMandateSetup> StartMandateSetupAsync(GoCardlessMandateSetupRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GoCardlessMandate?> GetMandateAsync(string mandateId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GoCardlessMandate> CancelMandateAsync(string mandateId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GoCardlessPayment> CreatePaymentAsync(GoCardlessPaymentRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GoCardlessPayment?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GoCardlessPayment> RetryPaymentAsync(string paymentId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<GoCardlessPayout>> ListPayoutsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GoCardlessPayout>>(Payouts);
    }

    [Fact]
    public async Task Sync_AddsNewPayouts()
    {
        var client = new FakeGoCardlessClient();
        client.Payouts.Add(new GoCardlessPayout("PO1", "pending", 5000, "GBP", "AUG-01", new DateOnly(2026, 8, 20)));
        var repo = new InMemoryPayoutRepository();
        var service = new PayoutSyncService(client, repo);

        var synced = await service.SyncAsync();

        Assert.Equal(1, synced);
        var stored = await repo.GetByGoCardlessPayoutIdAsync("PO1");
        Assert.NotNull(stored);
        Assert.Equal(PayoutStatus.Pending, stored!.Status);
        Assert.Equal(5000, stored.AmountPence);
    }

    [Fact]
    public async Task Sync_SamePayoutAgain_UpdatesStatusWithoutDuplicating()
    {
        var client = new FakeGoCardlessClient();
        client.Payouts.Add(new GoCardlessPayout("PO2", "pending", 5000, "GBP", null, null));
        var repo = new InMemoryPayoutRepository();
        var service = new PayoutSyncService(client, repo);

        await service.SyncAsync();

        // The payout is now paid on the next sync.
        client.Payouts[0] = new GoCardlessPayout("PO2", "paid", 5000, "GBP", null, new DateOnly(2026, 8, 22));
        var synced = await service.SyncAsync();

        Assert.Equal(1, synced);
        var all = await repo.GetAllAsync();
        Assert.Single(all);
        Assert.Equal(PayoutStatus.Paid, all[0].Status);
    }

    [Fact]
    public async Task Sync_UnchangedPayout_IsNotCountedAgain()
    {
        var client = new FakeGoCardlessClient();
        client.Payouts.Add(new GoCardlessPayout("PO3", "paid", 5000, "GBP", null, null));
        var repo = new InMemoryPayoutRepository();
        var service = new PayoutSyncService(client, repo);

        await service.SyncAsync();
        var second = await service.SyncAsync();

        Assert.Equal(0, second);
    }

    [Fact]
    public async Task Sync_WithUnconfiguredProvider_IsNoOp()
    {
        var repo = new InMemoryPayoutRepository();
        var service = new PayoutSyncService(new UnconfiguredGoCardlessClient(), repo);

        var synced = await service.SyncAsync();

        Assert.Equal(0, synced);
        Assert.Empty(await repo.GetAllAsync());
    }
}
