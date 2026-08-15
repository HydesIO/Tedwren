using Tedwren.Application.Billing;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Tests for <see cref="BillingReconciliationService"/> (Phase C): non-terminal records converge to the
/// provider's status, terminal records are left alone, and an unconfigured provider is a safe no-op.
/// </summary>
public sealed class BillingReconciliationServiceTests
{
    /// <summary>Returns canned remote statuses so reconciliation can be asserted without HTTP.</summary>
    private sealed class FakeGoCardlessClient : IGoCardlessClient
    {
        public string MandateStatus { get; set; } = "active";
        public string PaymentStatus { get; set; } = "confirmed";
        public int GetPaymentCalls { get; private set; }

        public Task<GoCardlessMandateSetup> StartMandateSetupAsync(GoCardlessMandateSetupRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<GoCardlessMandate?> GetMandateAsync(string mandateId, CancellationToken cancellationToken = default) =>
            Task.FromResult<GoCardlessMandate?>(new GoCardlessMandate(mandateId, MandateStatus, "REF", "CU"));
        public Task<GoCardlessMandate> CancelMandateAsync(string mandateId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<GoCardlessPayment> CreatePaymentAsync(GoCardlessPaymentRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<GoCardlessPayment?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
        {
            GetPaymentCalls++;
            return Task.FromResult<GoCardlessPayment?>(new GoCardlessPayment(paymentId, PaymentStatus, 1000, "GBP", null, null));
        }
        public Task<GoCardlessPayment> RetryPaymentAsync(string paymentId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<GoCardlessPayout>> ListPayoutsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GoCardlessPayout>>(Array.Empty<GoCardlessPayout>());
    }

    [Fact]
    public async Task Reconcile_UpdatesNonTerminalRecords_ToProviderStatus()
    {
        var mandates = new InMemoryMandateRepository();
        var payments = new InMemoryPaymentRepository();
        var client = new FakeGoCardlessClient { MandateStatus = "active", PaymentStatus = "confirmed" };
        var service = new BillingReconciliationService(client, mandates, payments);

        await mandates.AddAsync(new Mandate { CompanyId = Guid.NewGuid(), GoCardlessMandateId = "MD1", Status = MandateStatus.Submitted });
        await payments.AddAsync(new Payment { CompanyId = Guid.NewGuid(), MandateId = Guid.NewGuid(), AmountPence = 1000, GoCardlessPaymentId = "PM1", Status = PaymentStatus.Submitted });

        var result = await service.ReconcileAsync();

        Assert.Equal(1, result.MandatesUpdated);
        Assert.Equal(1, result.PaymentsUpdated);
        Assert.Equal(MandateStatus.Active, (await mandates.GetByGoCardlessMandateIdAsync("MD1"))!.Status);
        Assert.Equal(PaymentStatus.Confirmed, (await payments.GetByGoCardlessPaymentIdAsync("PM1"))!.Status);
    }

    [Fact]
    public async Task Reconcile_SkipsTerminalPayments()
    {
        var mandates = new InMemoryMandateRepository();
        var payments = new InMemoryPaymentRepository();
        var client = new FakeGoCardlessClient();
        var service = new BillingReconciliationService(client, mandates, payments);

        await payments.AddAsync(new Payment { CompanyId = Guid.NewGuid(), MandateId = Guid.NewGuid(), AmountPence = 1000, GoCardlessPaymentId = "PM-DONE", Status = PaymentStatus.PaidOut });

        var result = await service.ReconcileAsync();

        Assert.Equal(0, result.PaymentsUpdated);
        Assert.Equal(0, client.GetPaymentCalls);
    }

    [Fact]
    public async Task Reconcile_WithUnconfiguredProvider_IsNoOp()
    {
        var mandates = new InMemoryMandateRepository();
        var payments = new InMemoryPaymentRepository();
        var service = new BillingReconciliationService(new UnconfiguredGoCardlessClient(), mandates, payments);

        await payments.AddAsync(new Payment { CompanyId = Guid.NewGuid(), MandateId = Guid.NewGuid(), AmountPence = 1000, GoCardlessPaymentId = "PM1", Status = PaymentStatus.Submitted });

        var result = await service.ReconcileAsync();

        Assert.Equal(0, result.MandatesUpdated);
        Assert.Equal(0, result.PaymentsUpdated);
        Assert.Equal(PaymentStatus.Submitted, (await payments.GetByGoCardlessPaymentIdAsync("PM1"))!.Status);
    }
}
