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

    /// <summary>
    /// Whether this accreditation is <b>legally required</b> for the trade (e.g. Gas Safe for gas work). A missing
    /// or expired legal-mandatory accreditation blocks the operative at Gate 3; other requirements are advisory.
    /// </summary>
    public bool LegalMandatory { get; set; }

    /// <summary>Whether the (main) contractor/client requires this accreditation over and above the legal minimum. Advisory at Gate 3.</summary>
    public bool ClientRequired { get; set; }

    /// <summary>The owning company for an org-custom requirement, or null for a global (platform-default) row the customer may adjust (Q21, R15).</summary>
    public Guid? CompanyId { get; init; }
}
