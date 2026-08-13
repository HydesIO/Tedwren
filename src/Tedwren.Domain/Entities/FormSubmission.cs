using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A completed form — append-only evidence (R4/R10). It captures the answers against a specific template
/// version (<see cref="FormTemplateVersion"/>) so the exact form that was completed can always be reconstructed
/// (R16), the scope it was completed at (organisation or site, requirement 7), who submitted it and when
/// (UTC, R11), and its review state. Held per company (R15).
/// </summary>
public sealed class FormSubmission
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company that owns the submission (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The template version this was completed against.</summary>
    public required Guid FormTemplateId { get; init; }

    /// <summary>The template's version ordinal at completion time (so the form can be reconstructed, R16).</summary>
    public required int FormTemplateVersion { get; init; }

    /// <summary>The template's name snapshotted at completion time (for listing without re-joining).</summary>
    public required string FormName { get; init; }

    /// <summary>Where the form was completed (organisation or site level, requirement 7).</summary>
    public FormScope Scope { get; init; } = FormScope.Organisation;

    /// <summary>The site the form was completed against, when scoped to a site.</summary>
    public Guid? SiteId { get; init; }

    /// <summary>The person the form relates to, when applicable.</summary>
    public Guid? PersonId { get; init; }

    /// <summary>The captured answers, keyed by field id.</summary>
    public required IReadOnlyList<FormAnswer> Answers { get; init; }

    /// <summary>The review state (Submitted → Approved/Rejected).</summary>
    public FormSubmissionStatus Status { get; set; } = FormSubmissionStatus.Submitted;

    /// <summary>When the form was submitted (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset SubmittedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Who submitted the form (display name).</summary>
    public required string SubmittedBy { get; init; }

    /// <summary>The reviewer's written note, recorded on approve/reject (a rejection needs a reason).</summary>
    public string? ReviewNote { get; set; }
}
