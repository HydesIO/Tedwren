using Tedwren.Abstractions.Contracts.Billing;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The direct-debit billing service for the platform admin area: set up and cancel GoCardless mandates, take
/// and re-take payments, and read cross-company billing state. Every operation targets an explicit company
/// (R15); the API restricts the whole surface to platform admins. Reads work from the local database even
/// when GoCardless is not configured; collection actions require a configured provider.
/// </summary>
public interface IBillingService
{
    /// <summary>Returns every mandate on the platform (most recent first).</summary>
    Task<IReadOnlyList<MandateDto>> GetMandatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns every payment on the platform (most recent first).</summary>
    Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a company's mandate, subscription and recent payments.</summary>
    Task<CompanyBillingOverviewDto> GetCompanyBillingAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts hosted direct-debit set-up for a company: creates (or reuses) a pending mandate and a GoCardless
    /// billing-request flow, returning the authorisation URL for the customer to complete.
    /// </summary>
    Task<MandateSetupResultDto> StartMandateSetupAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Cancels a company's active mandate at GoCardless and locally. Returns false when there is no mandate.</summary>
    Task<bool> CancelMandateAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Takes a one-off payment against a company's active mandate. Throws when there is no active mandate.</summary>
    Task<PaymentDto> TakePaymentAsync(Guid companyId, TakePaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-takes a returned/failed payment as a fresh collection against the same mandate ("take again after a
    /// return"). Returns null when the payment is unknown; throws when it is not in a re-takeable state.
    /// </summary>
    Task<PaymentDto?> RetryPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates a company's billing subscription (meter/band are configuration, §9).</summary>
    Task<SubscriptionDto> SetSubscriptionAsync(Guid companyId, SetSubscriptionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent inbound GoCardless webhook events and their processing outcome.</summary>
    Task<IReadOnlyList<WebhookEventDto>> GetWebhookEventsAsync(int limit = 100, CancellationToken cancellationToken = default);

    /// <summary>Returns the BACS payouts mirrored locally (most recent first).</summary>
    Task<IReadOnlyList<PayoutDto>> GetPayoutsAsync(CancellationToken cancellationToken = default);

    /// <summary>Pulls the latest payouts from GoCardless into the local store and returns the number added/updated.</summary>
    Task<int> SyncPayoutsAsync(CancellationToken cancellationToken = default);
}
