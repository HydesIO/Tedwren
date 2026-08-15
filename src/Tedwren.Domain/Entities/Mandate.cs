using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A direct-debit mandate authorising Tedwren to collect payments from a customer company by GoCardless
/// (net-new billing; proposed PRD §9 addition). Scoped to the owning company (R15). The GoCardless-side
/// identifiers link the local record to the provider; the local <see cref="Status"/> mirrors the GoCardless
/// mandate lifecycle and is kept current by webhooks (Phase C) and reconciliation.
/// </summary>
public sealed class Mandate
{
    /// <summary>Stable local identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company this mandate authorises collection from (scoped, R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The GoCardless billing-request id created to start the hosted authorisation. Null once superseded by the mandate.</summary>
    public string? GoCardlessBillingRequestId { get; set; }

    /// <summary>The GoCardless mandate id, once the customer completes authorisation. Null while pending.</summary>
    public string? GoCardlessMandateId { get; set; }

    /// <summary>The GoCardless customer id linked to the mandate. Null while pending.</summary>
    public string? GoCardlessCustomerId { get; set; }

    /// <summary>The scheme reference shown to the payer on their bank statement, if known.</summary>
    public string? Reference { get; set; }

    /// <summary>The payer's bank name, for display, if known.</summary>
    public string? BankName { get; set; }

    /// <summary>The last two/three digits of the payer's account, for display, if known.</summary>
    public string? AccountNumberEnding { get; set; }

    /// <summary>The mandate lifecycle state (mirrors GoCardless).</summary>
    public MandateStatus Status { get; set; } = MandateStatus.PendingCustomerApproval;

    /// <summary>When the mandate record was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the mandate record was last updated (UTC).</summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
