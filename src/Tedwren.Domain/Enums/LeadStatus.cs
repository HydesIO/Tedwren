namespace Tedwren.Domain.Enums;

/// <summary>
/// The stage a lead is at in the pipeline (Web Plan §7). Progresses New → Contacted → Qualified → Proposal →
/// Converted (won, an account now exists) or Lost. The terminal states are Converted and Lost.
/// </summary>
public enum LeadStatus
{
    /// <summary>Newly captured, not yet worked.</summary>
    New = 0,

    /// <summary>First contact made.</summary>
    Contacted = 1,

    /// <summary>Qualified as a genuine opportunity.</summary>
    Qualified = 2,

    /// <summary>A proposal/quote has been put to them.</summary>
    Proposal = 3,

    /// <summary>Won — converted into an account.</summary>
    Converted = 4,

    /// <summary>Lost — no longer being pursued.</summary>
    Lost = 5,
}
