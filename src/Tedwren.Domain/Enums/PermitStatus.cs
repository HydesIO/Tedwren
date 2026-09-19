namespace Tedwren.Domain.Enums;

/// <summary>The lifecycle state of a permit to work (PRD §8.2: issue, approve, time-bound, close).</summary>
public enum PermitStatus
{
    /// <summary>Saved but not yet issued.</summary>
    Draft = 0,

    /// <summary>Issued and in force for its valid period.</summary>
    Issued = 1,

    /// <summary>Reviewed and approved (signed off) — in force with authorisation.</summary>
    Approved = 2,

    /// <summary>Work complete and the permit signed off closed; no longer in force.</summary>
    Closed = 3,

    /// <summary>Past its valid-to date and never closed. Derived at read time from the valid period, not stored.</summary>
    Expired = 4,
}
