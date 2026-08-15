using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A BACS payout: GoCardless settling collected funds into the Tedwren creditor bank account. Unlike mandates
/// and payments, a payout is <b>not</b> tenant-scoped — it is Tedwren's own settlement across all collections,
/// not a single company's. Mirrored locally (read-only; GoCardless creates payouts) so the admin area can
/// reconcile what has actually settled. The amount is in minor units (pence).
/// </summary>
public sealed class Payout
{
    /// <summary>Stable local identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The GoCardless payout id — unique; the key used to keep the local copy in step.</summary>
    public required string GoCardlessPayoutId { get; init; }

    /// <summary>Amount in minor units (pence).</summary>
    public int AmountPence { get; set; }

    /// <summary>ISO currency code (e.g. GBP).</summary>
    public string Currency { get; set; } = "GBP";

    /// <summary>The payout lifecycle state (mirrors GoCardless).</summary>
    public PayoutStatus Status { get; set; } = PayoutStatus.Pending;

    /// <summary>The payout reference shown on the bank statement, if any.</summary>
    public string? Reference { get; set; }

    /// <summary>The date the funds arrive in the creditor account, if known.</summary>
    public DateOnly? ArrivalDate { get; set; }

    /// <summary>When the local payout record was first created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the local payout record was last updated (UTC).</summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
