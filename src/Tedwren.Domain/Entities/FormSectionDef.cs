namespace Tedwren.Domain.Entities;

/// <summary>
/// A titled group of fields within a form template (the PRD-Phase 2 checklist/inspection engine). Sections
/// give the form its structure and render through the existing two-column form grouping. A value record,
/// so a template's layout serialises cleanly to JSON.
/// </summary>
/// <param name="Id">Stable section identifier within the template.</param>
/// <param name="Title">The section heading.</param>
/// <param name="Fields">The section's fields, in order.</param>
/// <param name="Order">The section's position within the form.</param>
public sealed record FormSectionDef(
    string Id,
    string Title,
    IReadOnlyList<FormField> Fields,
    int Order);
