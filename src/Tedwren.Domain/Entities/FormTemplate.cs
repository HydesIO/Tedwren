using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A customer-built form definition — the heart of the Forms Library (the PRD-Phase 2 checklist/inspection
/// engine, PRD §8.2). Held per company so each tenant shapes its own forms (R15). Templates are versioned
/// and append-only (R4/R10/R16): every row is one version, <see cref="FamilyId"/> groups the versions of a
/// logical form, publishing freezes a version, and editing a published version creates a new draft rather
/// than mutating the frozen one — so a submission can always resolve the exact form it was completed against.
/// </summary>
public sealed class FormTemplate
{
    /// <summary>Stable identifier for this specific version row.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company that owns the template (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>Groups the versions of one logical form; shared across a family's version rows.</summary>
    public Guid FamilyId { get; init; } = Guid.NewGuid();

    /// <summary>A human name for the form.</summary>
    public required string Name { get; set; }

    /// <summary>Optional description of the form's purpose.</summary>
    public string? Description { get; set; }

    /// <summary>The version ordinal within the family (starts at 1).</summary>
    public int Version { get; init; } = 1;

    /// <summary>The version's lifecycle state (Draft while authored, Published once frozen, Archived when retired).</summary>
    public FormTemplateStatus Status { get; set; } = FormTemplateStatus.Draft;

    /// <summary>The form's sections and their fields, in order (the schema).</summary>
    public required IReadOnlyList<FormSectionDef> Sections { get; set; }

    /// <summary>When this version was created (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When this version was last modified (UTC; displayed in UK local time per R11).</summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
