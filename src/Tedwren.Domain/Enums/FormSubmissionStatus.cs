namespace Tedwren.Domain.Enums;

/// <summary>
/// The review state of a completed form. Submissions are append-only evidence (R4/R10): a submitted record is
/// retained and only moves through review (approved / rejected with a written reason), never edited or deleted.
/// </summary>
public enum FormSubmissionStatus
{
    /// <summary>Started but not yet submitted.</summary>
    Draft = 0,

    /// <summary>Submitted and awaiting review.</summary>
    Submitted = 1,

    /// <summary>Reviewed and approved.</summary>
    Approved = 2,

    /// <summary>Reviewed and rejected (with a written reason).</summary>
    Rejected = 3,
}
