namespace Tedwren.Abstractions.Contracts.Billing;

/// <summary>A direct-debit mandate row for the admin billing views. Status is the string form of the domain state.</summary>
public sealed record MandateDto(
    Guid Id,
    Guid CompanyId,
    string? GoCardlessMandateId,
    string Status,
    string? Reference,
    string? BankName,
    string? AccountNumberEnding,
    DateTimeOffset CreatedUtc);

/// <summary>A direct-debit payment row for the admin billing views. Amount is in minor units (pence).</summary>
public sealed record PaymentDto(
    Guid Id,
    Guid CompanyId,
    Guid MandateId,
    string? GoCardlessPaymentId,
    int AmountPence,
    string Currency,
    string? Description,
    string Status,
    DateOnly? ChargeDate,
    string? FailureReason,
    DateTimeOffset CreatedUtc);

/// <summary>A company's billing subscription (meter/band are configuration keys, not prices — PRD §9).</summary>
public sealed record SubscriptionDto(
    Guid Id,
    Guid CompanyId,
    Guid? MandateId,
    string MeterKey,
    string? BandKey,
    string? Description,
    string Status,
    DateTimeOffset CreatedUtc);

/// <summary>Everything the admin billing page needs for one company: its mandate, subscription and recent payments.</summary>
public sealed record CompanyBillingOverviewDto(
    Guid CompanyId,
    MandateDto? Mandate,
    SubscriptionDto? Subscription,
    IReadOnlyList<PaymentDto> RecentPayments);

/// <summary>
/// The outcome of starting mandate set-up: the local mandate id and the GoCardless hosted authorisation URL
/// the payer must visit to authorise the direct debit. The admin hands this link to the customer.
/// </summary>
public sealed record MandateSetupResultDto(Guid MandateId, string AuthorisationUrl);

/// <summary>Request to take a one-off payment against a company's active mandate. Amount is in minor units (pence).</summary>
public sealed record TakePaymentRequest(int AmountPence, string? Description, string Currency = "GBP");

/// <summary>Request to set (create or update) a company's billing subscription. Meter/band are configuration keys (§9).</summary>
public sealed record SetSubscriptionRequest(string MeterKey, string? BandKey, string? Description);
