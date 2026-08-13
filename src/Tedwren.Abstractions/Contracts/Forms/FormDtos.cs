namespace Tedwren.Abstractions.Contracts.Forms;

/// <summary>A single field within a form template (the PRD-Phase 2 Forms Library). <see cref="Kind"/> is the <c>FormFieldKind</c> name.</summary>
public sealed record FormFieldDto(
    string Id,
    string Kind,
    string Label,
    string? HelpText,
    bool Required,
    string? ValidationJson,
    string? OptionsJson,
    int Order);

/// <summary>A titled group of fields within a form template.</summary>
public sealed record FormSectionDto(
    string Id,
    string Title,
    IReadOnlyList<FormFieldDto> Fields,
    int Order);

/// <summary>A form template row for the library list — identity, version/status and a field count.</summary>
public sealed record FormTemplateSummaryDto(
    Guid Id,
    Guid FamilyId,
    string Name,
    string? Description,
    int Version,
    string Status,
    int FieldCount,
    DateTimeOffset UpdatedUtc);

/// <summary>The full form template — its sections and fields. Also the shape served to fill a published form.</summary>
public sealed record FormTemplateDto(
    Guid Id,
    Guid FamilyId,
    string Name,
    string? Description,
    int Version,
    string Status,
    IReadOnlyList<FormSectionDto> Sections,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

/// <summary>Request to create a new form template (starts as version 1, Draft) for a company.</summary>
public sealed record CreateFormTemplateRequest(
    Guid CompanyId,
    string Name,
    string? Description,
    IReadOnlyList<FormSectionDto> Sections);

/// <summary>
/// Request to update a form template's content. Editing a draft mutates it in place; editing a published
/// version creates a new draft version (R4/R10/R16), leaving the published one intact.
/// </summary>
public sealed record UpdateFormTemplateRequest(
    string Name,
    string? Description,
    IReadOnlyList<FormSectionDto> Sections);
