using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A RAMS (risk assessment / method statement) submission from a contractor, for a main contractor to review
/// (PRD §8.2). Each submission carries an immediate reference (proof of when it was submitted), the site and
/// contractor it applies to, and its version. A resubmission is a new record in the same <see cref="FamilyId"/>
/// with the next version — earlier versions remain intact (append-only, R4/R16). Scoped to the reviewing company
/// (R15). Turning the HSE module off stops enforcement but never deletes RAMS history (§9).
/// </summary>
public sealed class RamsSubmission
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The main contractor company that reviews this RAMS (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>Groups all versions of the same RAMS; a resubmission reuses this id with the next version.</summary>
    public Guid FamilyId { get; init; } = Guid.NewGuid();

    /// <summary>The version number within the family (1, 2, 3…).</summary>
    public int Version { get; init; } = 1;

    /// <summary>Human-readable reference given at submission (proof of when it was submitted).</summary>
    public required string Reference { get; init; }

    /// <summary>The contractor/subcontractor the RAMS is for.</summary>
    public required string ContractorName { get; init; }

    /// <summary>The RAMS document title/description.</summary>
    public required string Title { get; init; }

    /// <summary>The site the RAMS applies to.</summary>
    public Guid? SiteId { get; init; }

    /// <summary>The site name (denormalised for display).</summary>
    public string? SiteName { get; init; }

    /// <summary>Reference to the uploaded RAMS document (via the image/file store; no permanent public URL, R9).</summary>
    public string? FileReference { get; init; }

    /// <summary>The review state.</summary>
    public RamsStatus Status { get; set; } = RamsStatus.Submitted;

    /// <summary>The reviewer's written note (required to reject or return).</summary>
    public string? ReviewNote { get; set; }

    /// <summary>Who reviewed it.</summary>
    public string? ReviewedBy { get; set; }

    /// <summary>When it was reviewed (UTC; displayed in UK local time, R11).</summary>
    public DateTimeOffset? ReviewedUtc { get; set; }

    /// <summary>When it was submitted (UTC).</summary>
    public DateTimeOffset SubmittedUtc { get; init; } = DateTimeOffset.UtcNow;
}
