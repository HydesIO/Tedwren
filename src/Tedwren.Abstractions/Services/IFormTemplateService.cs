using Tedwren.Abstractions.Contracts.Forms;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The Forms Library template service (PRD-Phase 2 checklist/inspection engine). Manages per-tenant form
/// templates: authoring, versioning and publication. Tenant-scoped — a caller only ever sees its own
/// company's templates (R15). Templates are versioned and append-only (R4/R10/R16).
/// </summary>
public interface IFormTemplateService
{
    /// <summary>Lists the caller's form library — the latest, non-archived version of each form family.</summary>
    Task<IReadOnlyList<FormTemplateSummaryDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a single template version by id (any status), or null when not found / not the caller's.</summary>
    Task<FormTemplateDto?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns a published template ready to fill, or null when not found / not published / not the caller's.</summary>
    Task<FormTemplateDto?> GetTemplateForFillAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates a new form template (version 1, Draft) and returns its id.</summary>
    Task<Guid> CreateAsync(CreateFormTemplateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a template. A draft is edited in place; a published version is superseded by a new draft
    /// version. Returns the resulting template, or null when not found / not the caller's.
    /// </summary>
    Task<FormTemplateDto?> UpdateAsync(Guid id, UpdateFormTemplateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Publishes a draft (freezes it). Returns the published template, or null when not found / not the caller's.</summary>
    Task<FormTemplateDto?> PublishAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Archives a template version (retires it from the library). Returns the archived template, or null when not found / not the caller's.</summary>
    Task<FormTemplateDto?> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates (and publishes) the shipped starter forms for the caller's company, skipping any whose name already exists. Returns how many were created.</summary>
    Task<int> SeedStarterTemplatesAsync(CancellationToken cancellationToken = default);
}
