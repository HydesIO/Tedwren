using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A direct-debit payment collected (or attempted) against a company's <see cref="Mandate"/> via GoCardless.
/// Scoped to the owning company (R15). The amount is stored in minor units (pence) to avoid floating-point
/// money, matching the GoCardless API. A payment that returns (<see cref="PaymentStatus.Failed"/> /
/// <see cref="PaymentStatus.ChargedBack"/>) can be re-taken as a fresh payment.
/// </summary>
public sealed class Payment
{
    /// <summary>Stable local identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company this payment is collected from (scoped, R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The local mandate the payment is collected against.</summary>
    public required Guid MandateId { get; init; }

    /// <summary>The GoCardless payment id, once created. Null only transiently before the provider call returns.</summary>
    public string? GoCardlessPaymentId { get; set; }

    /// <summary>Amount in minor units (pence).</summary>
    public required int AmountPence { get; init; }

    /// <summary>ISO currency code (e.g. GBP).</summary>
    public string Currency { get; init; } = "GBP";

    /// <summary>Free-text description shown to the payer and on reports.</summary>
    public string? Description { get; set; }

    /// <summary>The payment lifecycle state (mirrors GoCardless).</summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.PendingSubmission;

    /// <summary>The scheduled charge date, if known.</summary>
    public DateOnly? ChargeDate { get; set; }

    /// <summary>The reason a failed/returned payment gave, if any.</summary>
    public string? FailureReason { get; set; }

    /// <summary>When the payment record was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the payment record was last updated (UTC).</summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
