using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A commission payout owed to (or paid to) an affiliate (Web Plan §7). Records the amount and status only —
/// there are no raw bank details; payment itself is made out of band against the affiliate's payee reference.
/// Optionally tied to the specific account/deal (a lead) the commission was earned on.
/// </summary>
public sealed class AffiliatePayout
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The affiliate this payout belongs to.</summary>
    public required Guid AffiliateId { get; init; }

    /// <summary>The account/deal (lead) the commission was earned on, if tied to one.</summary>
    public Guid? LeadId { get; init; }

    /// <summary>The payout amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Currency code (ISO 4217).</summary>
    public string Currency { get; set; } = "GBP";

    /// <summary>Payout status.</summary>
    public AffiliatePayoutStatus Status { get; set; } = AffiliatePayoutStatus.Pending;

    /// <summary>A free-text reference/description for the payout.</summary>
    public string? Reference { get; set; }

    /// <summary>When the payout was raised (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the payout was marked paid, if it has been.</summary>
    public DateTimeOffset? PaidUtc { get; set; }
}
