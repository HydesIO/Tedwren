namespace Tedwren.Application.Billing;

/// <summary>
/// Thin transport seam over the GoCardless HTTP API, isolating the provider's request/response shapes from the
/// billing domain (Single Responsibility) and letting tests substitute a fake. Lives in the Application layer
/// like <c>ITokenIssuer</c>; the concrete HTTP implementation is provided by the API composition root as a
/// typed <c>HttpClient</c>. All amounts are in minor units (pence).
/// </summary>
public interface IGoCardlessClient
{
    /// <summary>
    /// Starts hosted mandate set-up: creates a billing request (mandate_request) and a billing-request flow,
    /// returning the billing-request id and the payer's hosted authorisation URL.
    /// </summary>
    Task<GoCardlessMandateSetup> StartMandateSetupAsync(GoCardlessMandateSetupRequest request, CancellationToken cancellationToken = default);

    /// <summary>Fetches a mandate's current state, or null when it does not exist.</summary>
    Task<GoCardlessMandate?> GetMandateAsync(string mandateId, CancellationToken cancellationToken = default);

    /// <summary>Cancels a mandate and returns its updated state.</summary>
    Task<GoCardlessMandate> CancelMandateAsync(string mandateId, CancellationToken cancellationToken = default);

    /// <summary>Creates a one-off payment against a mandate and returns its created state.</summary>
    Task<GoCardlessPayment> CreatePaymentAsync(GoCardlessPaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Fetches a payment's current state, or null when it does not exist.</summary>
    Task<GoCardlessPayment?> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default);

    /// <summary>Retries a failed payment and returns its updated state.</summary>
    Task<GoCardlessPayment> RetryPaymentAsync(string paymentId, CancellationToken cancellationToken = default);
}

/// <summary>Request to start hosted mandate set-up.</summary>
/// <param name="Currency">ISO currency code (e.g. GBP).</param>
/// <param name="CompanyId">The local company id, stored as GoCardless metadata for correlation.</param>
/// <param name="RedirectUri">Where the payer returns after completing authorisation.</param>
/// <param name="ExitUri">Where the payer returns if they exit without completing.</param>
public sealed record GoCardlessMandateSetupRequest(string Currency, string CompanyId, string RedirectUri, string ExitUri);

/// <summary>Outcome of starting mandate set-up: the billing-request id and the hosted authorisation URL.</summary>
public sealed record GoCardlessMandateSetup(string BillingRequestId, string AuthorisationUrl);

/// <summary>A GoCardless mandate in transport-neutral form.</summary>
public sealed record GoCardlessMandate(string Id, string Status, string? Reference, string? CustomerId);

/// <summary>Request to create a one-off payment against a mandate. Amount is in minor units (pence).</summary>
/// <param name="MandateId">The GoCardless mandate id to collect against.</param>
/// <param name="Amount">Amount in minor units (pence).</param>
/// <param name="Currency">ISO currency code (e.g. GBP).</param>
/// <param name="Description">Optional description shown to the payer.</param>
/// <param name="IdempotencyKey">Client-supplied key so a retried request never double-charges.</param>
/// <param name="CompanyId">The local company id, stored as GoCardless metadata for correlation.</param>
public sealed record GoCardlessPaymentRequest(
    string MandateId, int Amount, string Currency, string? Description, string IdempotencyKey, string CompanyId);

/// <summary>A GoCardless payment in transport-neutral form. Amount is in minor units (pence).</summary>
public sealed record GoCardlessPayment(string Id, string Status, int Amount, string Currency, DateOnly? ChargeDate, string? Description);
