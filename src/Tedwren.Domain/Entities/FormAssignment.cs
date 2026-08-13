using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// Assigns a form (by its family, so it always resolves to the latest published version) to where it should be
/// completed — the organisation, a site, a site operator, or the induction (requirement 5). Carries the expected
/// cadence and an optional address for the failed-check alert (PRD §8 "a failed check auto-alerts by email with
/// the failed report attached"). Held per company (R15).
/// </summary>
public sealed class FormAssignment
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company that owns the assignment (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The form family assigned (resolves to its latest published version).</summary>
    public required Guid FormTemplateFamilyId { get; init; }

    /// <summary>A snapshot of the form's name at assignment time (for listing without re-joining).</summary>
    public required string FormName { get; init; }

    /// <summary>Where the form is assigned (organisation, site, operator or induction, requirement 5).</summary>
    public FormScope Scope { get; init; } = FormScope.Organisation;

    /// <summary>The site the form is assigned to, when site-scoped.</summary>
    public Guid? SiteId { get; init; }

    /// <summary>The operator (person) the form is assigned to, when operator-scoped.</summary>
    public Guid? PersonId { get; init; }

    /// <summary>The induction template the form is attached to, when induction-scoped.</summary>
    public Guid? InductionTemplateId { get; init; }

    /// <summary>How often the form is expected to be completed.</summary>
    public FormSchedule Schedule { get; init; } = FormSchedule.AdHoc;

    /// <summary>Where to email the failed-check alert (with the completed report attached). Null = no alert.</summary>
    public string? FailureAlertEmail { get; set; }

    /// <summary>When the assignment was created (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
