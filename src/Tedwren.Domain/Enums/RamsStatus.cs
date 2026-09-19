namespace Tedwren.Domain.Enums;

/// <summary>The review state of a RAMS (risk assessment / method statement) submission (PRD §8.2).</summary>
public enum RamsStatus
{
    /// <summary>Submitted and awaiting the site manager's review.</summary>
    Submitted = 0,

    /// <summary>Approved — the contractor's workers may start on the site.</summary>
    Approved = 1,

    /// <summary>Rejected with a written reason; a corrected resubmission is a new version.</summary>
    Rejected = 2,

    /// <summary>Returned for changes with a written note; a corrected resubmission is a new version (R18 — returned, not denied).</summary>
    Returned = 3,
}
