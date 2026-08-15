using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Billing;
using Tedwren.Application.Billing;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Unit tests for <see cref="BillingService"/> (GoCardless direct debit, admin area). Uses the in-memory
/// billing repositories and a fake GoCardless transport so the state transitions are verified without any HTTP.
/// Covers mandate set-up, taking a payment (and the no-active-mandate guard), re-taking a returned payment,
/// cancellation and subscription upsert.
/// </summary>
public sealed class BillingServiceTests
{
    private static readonly Guid Company = Guid.Parse("88888888-8888-4888-8888-000000000001");

    /// <summary>Records calls and returns canned GoCardless responses so the service logic can be asserted.</summary>
    private sealed class FakeGoCardlessClient : IGoCardlessClient
    {
        public string PaymentStatus { get; set; } = "pending_submission";
        public string RetryStatus { get; set; } = "submitted";
        public int CreatePaymentCalls { get; private set; }
        public string? LastRetriedPaymentId { get; private set; }

        public Task<GoCardlessMandateSetup> StartMandateSetupAsync(GoCardlessMandateSetupRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GoCardlessMandateSetup("BRQ123", "https://pay.gocardless.com/flow/BRF123"));

        public Task<GoCardlessMandate?> GetMandateAsync(string mandateId, CancellationToken cancellationToken = default) =>
            Task.FromResult<GoCardlessMandate?>(new GoCardlessMandate(mandateId, "active", "REF", "CU123"));

        public Task<GoCardlessMandate> CancelMandateAsync(string mandateId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new GoCardlessMandate(mandateId, "cancelled", "REF", "CU123"));

        public Task<GoCardlessPayment> CreatePaymentAsync(GoCardlessPaymentRequest request, CancellationToken cancellationToken = default)
        {
            CreatePaymentCalls++;
            return Task.FromResult(new GoCardlessPayment("PM123", PaymentStatus, request.Amount, request.Currency, null, request.Description));
        }

        public Task<GoCardlessPayment?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<GoCardlessPayment?>(new GoCardlessPayment(paymentId, PaymentStatus, 1000, "GBP", null, null));

        public Task<GoCardlessPayment> RetryPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
        {
            LastRetriedPaymentId = paymentId;
            return Task.FromResult(new GoCardlessPayment(paymentId, RetryStatus, 1000, "GBP", null, null));
        }
    }

    private static (BillingService Service, InMemoryMandateRepository Mandates, InMemoryPaymentRepository Payments, FakeGoCardlessClient Client) Create()
    {
        var mandates = new InMemoryMandateRepository();
        var payments = new InMemoryPaymentRepository();
        var subscriptions = new InMemoryBillingSubscriptionRepository();
        var webhookEvents = new InMemoryWebhookEventRepository();
        var client = new FakeGoCardlessClient();
        var service = new BillingService(client, mandates, payments, subscriptions, webhookEvents, new GoCardlessOptions());
        return (service, mandates, payments, client);
    }

    // Seeds an active mandate for the company so payments can be taken against it.
    private static async Task<Mandate> SeedActiveMandateAsync(InMemoryMandateRepository mandates)
    {
        var mandate = new Mandate
        {
            CompanyId = Company,
            GoCardlessMandateId = "MD123",
            Status = MandateStatus.Active,
        };
        await mandates.AddAsync(mandate);
        return mandate;
    }

    [Fact]
    public async Task StartMandateSetup_CreatesPendingMandate_AndReturnsAuthorisationUrl()
    {
        var (service, mandates, _, _) = Create();

        var result = await service.StartMandateSetupAsync(Company);

        Assert.Equal("https://pay.gocardless.com/flow/BRF123", result.AuthorisationUrl);
        var stored = await mandates.GetCurrentForCompanyAsync(Company);
        Assert.NotNull(stored);
        Assert.Equal(MandateStatus.PendingCustomerApproval, stored!.Status);
        Assert.Equal("BRQ123", stored.GoCardlessBillingRequestId);
    }

    [Fact]
    public async Task StartMandateSetup_CalledTwice_ReusesTheSamePendingMandate()
    {
        var (service, mandates, _, _) = Create();

        var first = await service.StartMandateSetupAsync(Company);
        var second = await service.StartMandateSetupAsync(Company);

        Assert.Equal(first.MandateId, second.MandateId);
        var all = await mandates.GetAllAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task TakePayment_WithActiveMandate_CreatesPaymentWithMappedStatus()
    {
        var (service, mandates, payments, client) = Create();
        await SeedActiveMandateAsync(mandates);
        client.PaymentStatus = "submitted";

        var dto = await service.TakePaymentAsync(Company, new TakePaymentRequest(2500, "August"));

        Assert.Equal(PaymentStatus.Submitted.ToString(), dto.Status);
        Assert.Equal(2500, dto.AmountPence);
        Assert.Equal(1, client.CreatePaymentCalls);
        var stored = await payments.GetByIdAsync(dto.Id);
        Assert.Equal("PM123", stored!.GoCardlessPaymentId);
    }

    [Fact]
    public async Task TakePayment_WithoutActiveMandate_Throws()
    {
        var (service, _, _, _) = Create();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TakePaymentAsync(Company, new TakePaymentRequest(1000, "x")));
    }

    [Fact]
    public async Task TakePayment_ZeroAmount_Throws()
    {
        var (service, mandates, _, _) = Create();
        await SeedActiveMandateAsync(mandates);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.TakePaymentAsync(Company, new TakePaymentRequest(0, "x")));
    }

    [Fact]
    public async Task RetryPayment_OnReturnedPayment_ReCollectsAndUpdatesStatus()
    {
        var (service, mandates, payments, client) = Create();
        var mandate = await SeedActiveMandateAsync(mandates);
        var failed = new Payment
        {
            CompanyId = Company,
            MandateId = mandate.Id,
            AmountPence = 1000,
            GoCardlessPaymentId = "PM999",
            Status = PaymentStatus.Failed,
            FailureReason = "insufficient_funds",
        };
        await payments.AddAsync(failed);
        client.RetryStatus = "submitted";

        var dto = await service.RetryPaymentAsync(failed.Id);

        Assert.NotNull(dto);
        Assert.Equal(PaymentStatus.Submitted.ToString(), dto!.Status);
        Assert.Equal("PM999", client.LastRetriedPaymentId);
        var stored = await payments.GetByIdAsync(failed.Id);
        Assert.Null(stored!.FailureReason);
    }

    [Fact]
    public async Task RetryPayment_OnNonReturnedPayment_Throws()
    {
        var (service, mandates, payments, _) = Create();
        var mandate = await SeedActiveMandateAsync(mandates);
        var confirmed = new Payment
        {
            CompanyId = Company,
            MandateId = mandate.Id,
            AmountPence = 1000,
            GoCardlessPaymentId = "PM777",
            Status = PaymentStatus.Confirmed,
        };
        await payments.AddAsync(confirmed);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RetryPaymentAsync(confirmed.Id));
    }

    [Fact]
    public async Task RetryPayment_UnknownPayment_ReturnsNull()
    {
        var (service, _, _, _) = Create();

        var dto = await service.RetryPaymentAsync(Guid.NewGuid());

        Assert.Null(dto);
    }

    [Fact]
    public async Task CancelMandate_WithGoCardlessMandate_MapsCancelledStatus()
    {
        var (service, mandates, _, _) = Create();
        await SeedActiveMandateAsync(mandates);

        var cancelled = await service.CancelMandateAsync(Company);

        Assert.True(cancelled);
        var stored = await mandates.GetCurrentForCompanyAsync(Company);
        Assert.Equal(MandateStatus.Cancelled, stored!.Status);
    }

    [Fact]
    public async Task CancelMandate_WithNoMandate_ReturnsFalse()
    {
        var (service, _, _, _) = Create();

        Assert.False(await service.CancelMandateAsync(Company));
    }

    [Fact]
    public async Task SetSubscription_CreatesThenUpdates()
    {
        var (service, _, _, _) = Create();

        var created = await service.SetSubscriptionAsync(Company, new SetSubscriptionRequest("sites", "band-a", "Main contractor"));
        Assert.Equal("sites", created.MeterKey);

        var updated = await service.SetSubscriptionAsync(Company, new SetSubscriptionRequest("operatives", null, "Subcontractor"));
        Assert.Equal("operatives", updated.MeterKey);
        Assert.Equal(created.Id, updated.Id);
    }
}
