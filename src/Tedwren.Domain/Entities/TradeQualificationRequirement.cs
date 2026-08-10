namespace Tedwren.Domain.Entities;

/// <summary>
/// A rule that a given trade must hold a given qualification type (SF-11). The set of these for a trade
/// is what lets the system say who is short of what, rather than relying on someone remembering. The
/// default set ships as a starting point and is the customer's to adjust (PRD Q21).
/// </summary>
public sealed class TradeQualificationRequirement
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The trade the requirement applies to (matched against the engagement's trade).</summary>
    public required string Trade { get; init; }

    /// <summary>The qualification type the trade must hold.</summary>
    public required Guid QualificationTypeId { get; init; }
}
