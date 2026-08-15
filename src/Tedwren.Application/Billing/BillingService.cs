using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Billing;
using Tedwren.Abstractions.Services;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Tedwren.Application.Persistence;

namespace Tedwren.Application.Billing;

/// <summary>
/// Direct-debit billing over GoCardless for the platform admin area. Coordinates the local mandate/payment/
/// subscription stores with the GoCardless transport (<see cref="IGoCardlessClient"/>), keeping the two in
/// step and mapping provider statuses through <see cref="GoCardlessStatusMap"/>. Reads never touch the
/// provider; collection actions do. All operations target an explicit company (R15).
/// </summary>
public sealed class BillingService : IBillingService
{
    private readonly IGoCardlessClient _goCardless;
    private readonly IMandateRepository _mandates;
    private readonly IPaymentRepository _payments;
    private readonly IBillingSubscriptionRepository _subscriptions;
    private readonly IWebhookEventRepository _webhookEvents;
    private readonly IPayoutRepository _payouts;
    private readonly PayoutSyncService _payoutSync;
    private readonly GoCardlessOptions _options;

    /// <summary>Creates the service over the GoCardless client, the billing repositories and the options.</summary>
    public BillingService(
        IGoCardlessClient goCardless,
        IMandateRepository mandates,
        IPaymentRepository payments,
        IBillingSubscriptionRepository subscriptions,
        IWebhookEventRepository webhookEvents,
        IPayoutRepository payouts,
        PayoutSyncService payoutSync,
        GoCardlessOptions options)
    {
        _goCardless = goCardless;
        _mandates = mandates;
        _payments = payments;
        _subscriptions = subscriptions;
        _webhookEvents = webhookEvents;
        _payouts = payouts;
        _payoutSync = payoutSync;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MandateDto>> GetMandatesAsync(CancellationToken cancellationToken = default)
    {
        var mandates = await _mandates.GetAllAsync(cancellationToken);
        return mandates.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var payments = await _payments.GetAllAsync(cancellationToken);
        return payments.Select(ToDto).ToList();
    }

    /// <inheritdoc />
    public async Task<CompanyBillingOverviewDto> GetCompanyBillingAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var mandate = await _mandates.GetCurrentForCompanyAsync(companyId, cancellationToken);
        var subscription = await _subscriptions.GetForCompanyAsync(companyId, cancellationToken);
        var payments = await _payments.GetByCompanyAsync(companyId, cancellationToken);

        return new CompanyBillingOverviewDto(
            companyId,
            mandate is null ? null : ToDto(mandate),
            subscription is null ? null : ToDto(subscription),
            payments.Take(10).Select(ToDto).ToList());
    }

    /// <inheritdoc />
    public async Task<MandateSetupResultDto> StartMandateSetupAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        // Reuse an existing still-pending mandate rather than piling up rows; otherwise start a fresh one.
        var mandate = await _mandates.GetCurrentForCompanyAsync(companyId, cancellationToken);
        if (mandate is null || mandate.Status != MandateStatus.PendingCustomerApproval)
        {
            mandate = new Mandate { CompanyId = companyId };
            await _mandates.AddAsync(mandate, cancellationToken);
        }

        var setup = await _goCardless.StartMandateSetupAsync(
            new GoCardlessMandateSetupRequest("GBP", companyId.ToString(), _options.RedirectUri, _options.ExitUri),
            cancellationToken);

        mandate.GoCardlessBillingRequestId = setup.BillingRequestId;
        mandate.UpdatedUtc = DateTimeOffset.UtcNow;
        await _mandates.UpdateAsync(mandate, cancellationToken);

        return new MandateSetupResultDto(mandate.Id, setup.AuthorisationUrl);
    }

    /// <inheritdoc />
    public async Task<bool> CancelMandateAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var mandate = await _mandates.GetCurrentForCompanyAsync(companyId, cancellationToken);
        if (mandate is null || mandate.Status == MandateStatus.Cancelled)
        {
            return false;
        }

        // Only call the provider when the mandate actually exists there; an unauthorised (pending) mandate is
        // just cancelled locally.
        if (!string.IsNullOrEmpty(mandate.GoCardlessMandateId))
        {
            var cancelled = await _goCardless.CancelMandateAsync(mandate.GoCardlessMandateId, cancellationToken);
            mandate.Status = GoCardlessStatusMap.ToMandateStatus(cancelled.Status) ?? MandateStatus.Cancelled;
        }
        else
        {
            mandate.Status = MandateStatus.Cancelled;
        }

        mandate.UpdatedUtc = DateTimeOffset.UtcNow;
        await _mandates.UpdateAsync(mandate, cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<PaymentDto> TakePaymentAsync(Guid companyId, TakePaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.AmountPence <= 0)
        {
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(request));
        }

        var mandate = await _mandates.GetCurrentForCompanyAsync(companyId, cancellationToken);
        if (mandate is null || mandate.Status != MandateStatus.Active || string.IsNullOrEmpty(mandate.GoCardlessMandateId))
        {
            throw new InvalidOperationException("The company has no active direct-debit mandate to collect against.");
        }

        // Persist the local payment first so its id is a stable idempotency key (a retried request never
        // double-charges). On a provider failure the row is marked failed rather than left dangling.
        var payment = new Payment
        {
            CompanyId = companyId,
            MandateId = mandate.Id,
            AmountPence = request.AmountPence,
            Currency = request.Currency,
            Description = request.Description,
            Status = PaymentStatus.PendingSubmission,
        };
        await _payments.AddAsync(payment, cancellationToken);

        try
        {
            var created = await _goCardless.CreatePaymentAsync(
                new GoCardlessPaymentRequest(
                    mandate.GoCardlessMandateId, request.AmountPence, request.Currency,
                    request.Description, payment.Id.ToString(), companyId.ToString()),
                cancellationToken);

            payment.GoCardlessPaymentId = created.Id;
            payment.Status = GoCardlessStatusMap.ToPaymentStatus(created.Status) ?? PaymentStatus.PendingSubmission;
            payment.ChargeDate = created.ChargeDate;
        }
        catch (Exception ex)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = ex.Message;
            payment.UpdatedUtc = DateTimeOffset.UtcNow;
            await _payments.UpdateAsync(payment, cancellationToken);
            throw;
        }

        payment.UpdatedUtc = DateTimeOffset.UtcNow;
        await _payments.UpdateAsync(payment, cancellationToken);
        return ToDto(payment);
    }

    /// <inheritdoc />
    public async Task<PaymentDto?> RetryPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
        {
            return null;
        }

        if (!GoCardlessStatusMap.IsRetryable(payment.Status))
        {
            throw new InvalidOperationException("Only a returned or charged-back payment can be re-taken.");
        }

        if (string.IsNullOrEmpty(payment.GoCardlessPaymentId))
        {
            throw new InvalidOperationException("The payment was never submitted to GoCardless and cannot be retried.");
        }

        var retried = await _goCardless.RetryPaymentAsync(payment.GoCardlessPaymentId, cancellationToken);
        payment.Status = GoCardlessStatusMap.ToPaymentStatus(retried.Status) ?? PaymentStatus.Submitted;
        payment.ChargeDate = retried.ChargeDate;
        payment.FailureReason = null;
        payment.UpdatedUtc = DateTimeOffset.UtcNow;
        await _payments.UpdateAsync(payment, cancellationToken);
        return ToDto(payment);
    }

    /// <inheritdoc />
    public async Task<SubscriptionDto> SetSubscriptionAsync(Guid companyId, SetSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.MeterKey))
        {
            throw new ArgumentException("A meter key is required.", nameof(request));
        }

        var mandate = await _mandates.GetCurrentForCompanyAsync(companyId, cancellationToken);
        var subscription = await _subscriptions.GetForCompanyAsync(companyId, cancellationToken);

        if (subscription is null)
        {
            subscription = new BillingSubscription
            {
                CompanyId = companyId,
                MeterKey = request.MeterKey,
                BandKey = request.BandKey,
                Description = request.Description,
                MandateId = mandate?.Id,
            };
            await _subscriptions.AddAsync(subscription, cancellationToken);
        }
        else
        {
            subscription.MeterKey = request.MeterKey;
            subscription.BandKey = request.BandKey;
            subscription.Description = request.Description;
            subscription.MandateId = mandate?.Id ?? subscription.MandateId;
            subscription.UpdatedUtc = DateTimeOffset.UtcNow;
            await _subscriptions.UpdateAsync(subscription, cancellationToken);
        }

        return ToDto(subscription);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebhookEventDto>> GetWebhookEventsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        var events = await _webhookEvents.GetRecentAsync(limit, cancellationToken);
        return events.Select(e => new WebhookEventDto(
            e.Id, e.GoCardlessEventId, e.ResourceType, e.Action, e.ResourceId,
            e.Outcome.ToString(), e.Detail, e.ReceivedUtc, e.ProcessedUtc)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PayoutDto>> GetPayoutsAsync(CancellationToken cancellationToken = default)
    {
        var payouts = await _payouts.GetAllAsync(cancellationToken);
        return payouts.Select(p => new PayoutDto(
            p.Id, p.GoCardlessPayoutId, p.AmountPence, p.Currency, p.Status.ToString(),
            p.Reference, p.ArrivalDate, p.CreatedUtc)).ToList();
    }

    /// <inheritdoc />
    public Task<int> SyncPayoutsAsync(CancellationToken cancellationToken = default) =>
        _payoutSync.SyncAsync(cancellationToken);

    /// <summary>Maps a mandate entity to its DTO.</summary>
    private static MandateDto ToDto(Mandate m) => new(
        m.Id, m.CompanyId, m.GoCardlessMandateId, m.Status.ToString(),
        m.Reference, m.BankName, m.AccountNumberEnding, m.CreatedUtc);

    /// <summary>Maps a payment entity to its DTO.</summary>
    private static PaymentDto ToDto(Payment p) => new(
        p.Id, p.CompanyId, p.MandateId, p.GoCardlessPaymentId, p.AmountPence, p.Currency,
        p.Description, p.Status.ToString(), p.ChargeDate, p.FailureReason, p.CreatedUtc);

    /// <summary>Maps a subscription entity to its DTO.</summary>
    private static SubscriptionDto ToDto(BillingSubscription s) => new(
        s.Id, s.CompanyId, s.MandateId, s.MeterKey, s.BandKey, s.Description, s.Status.ToString(), s.CreatedUtc);
}
