using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A company's ongoing agreement to be charged the metered SaaS fee (PRD §9), collected by direct debit.
/// Scoped to the owning company (R15). Per §9 the <b>meter</b> (which thing is counted — sites for a main
/// contractor, engaged operatives for a subcontractor) and the <b>band</b> are configuration, not hard-coded
/// numbers: they are held here as keys so pricing can move without a code change. The prices themselves are
/// deliberately not stored on the entity.
/// </summary>
public sealed class BillingSubscription
{
    /// <summary>Stable local identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company this subscription bills (scoped, R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The local mandate collections are made against. Null until a mandate is set up.</summary>
    public Guid? MandateId { get; set; }

    /// <summary>The meter key that decides what is counted (e.g. "sites", "operatives"); configuration, not a number (§9).</summary>
    public required string MeterKey { get; set; }

    /// <summary>The pricing band key, if the plan is banded; configuration, not a number (§9). Null when not banded.</summary>
    public string? BandKey { get; set; }

    /// <summary>Human-readable plan description for display.</summary>
    public string? Description { get; set; }

    /// <summary>The subscription lifecycle state.</summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    /// <summary>When the subscription record was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the subscription record was last updated (UTC).</summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
