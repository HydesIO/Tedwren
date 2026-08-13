using Tedwren.Abstractions.Contracts.Forms;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Forms;

/// <summary>
/// The single implementation of the Forms Library template rules (PRD-Phase 2 checklist/inspection engine).
/// Data-store agnostic: the same logic runs over the in-memory and Dapper repositories. Tenant-scoped — every
/// read and write is confined to the caller's company (R15). Templates are versioned and append-only
/// (R4/R10/R16): a draft is edited in place, publishing freezes a version, and editing a published version
/// creates a new draft rather than mutating the frozen one.
/// </summary>
public sealed class FormTemplateService : IFormTemplateService
{
    private readonly IFormTemplateRepository _templates;
    private readonly ICurrentUserService? _currentUser;

    /// <summary>
    /// Creates the service over its repository. <paramref name="currentUser"/> supplies the signed-in tenant so
    /// queries are scoped to the caller's company (R15); it is optional so unit tests that construct the service
    /// directly run unscoped.
    /// </summary>
    public FormTemplateService(IFormTemplateRepository templates, ICurrentUserService? currentUser = null)
    {
        _templates = templates;
        _currentUser = currentUser;
    }

    /// <summary>Resolves the signed-in caller's tenant company (R15). Null when unauthenticated or in unit tests (unscoped).</summary>
    private async Task<Guid?> ResolveTenantAsync(CancellationToken cancellationToken)
    {
        if (_currentUser is null)
        {
            return null;
        }

        var user = await _currentUser.GetCurrentAsync(cancellationToken);
        return user.CompanyId;
    }

    /// <summary>
    /// Loads a template by id and enforces tenant ownership (R15): a template outside the caller's company is
    /// treated as not found, so one company can never see or mutate another's form.
    /// </summary>
    private async Task<FormTemplate?> LoadOwnedAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await ResolveTenantAsync(cancellationToken);
        var template = await _templates.GetByIdAsync(id, cancellationToken);
        if (template is null || (tenant is not null && template.CompanyId != tenant))
        {
            return null;
        }

        return template;
    }

    /// <summary>
    /// Lists the caller's library — the latest, non-archived version of each form family (R15). An
    /// unauthenticated caller (no resolved tenant) gets an empty list rather than another company's forms;
    /// every production Forms route is authenticated, so this is the safe default.
    /// </summary>
    public async Task<IReadOnlyList<FormTemplateSummaryDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(cancellationToken);
        if (tenant is null)
        {
            return new List<FormTemplateSummaryDto>();
        }

        var all = await _templates.GetByCompanyAsync(tenant.Value, cancellationToken);

        // One row per family: the highest version, excluding families whose latest version is archived.
        return all
            .GroupBy(t => t.FamilyId)
            .Select(g => g.OrderByDescending(t => t.Version).First())
            .Where(t => t.Status != FormTemplateStatus.Archived)
            .OrderByDescending(t => t.UpdatedUtc)
            .ThenBy(t => t.Name)
            .Select(ToSummaryDto)
            .ToList();
    }

    /// <summary>Returns a template version by id (any status), scoped to the caller (R15).</summary>
    public async Task<FormTemplateDto?> GetTemplateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await LoadOwnedAsync(id, cancellationToken);
        return template is null ? null : ToDto(template);
    }

    /// <summary>Returns a published template ready to fill, scoped to the caller (R15). Null if not published.</summary>
    public async Task<FormTemplateDto?> GetTemplateForFillAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await LoadOwnedAsync(id, cancellationToken);
        return template is null || template.Status != FormTemplateStatus.Published ? null : ToDto(template);
    }

    /// <summary>Creates a new form template (version 1, Draft) owned by the caller's company (R15), and returns its id.</summary>
    public async Task<Guid> CreateAsync(CreateFormTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A form name is required.", nameof(request));
        }

        var tenant = await ResolveTenantAsync(cancellationToken);
        var companyId = tenant ?? request.CompanyId;
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(request));
        }

        var now = DateTimeOffset.UtcNow;
        var template = new FormTemplate
        {
            CompanyId = companyId,
            Name = request.Name.Trim(),
            Description = Normalise(request.Description),
            Version = 1,
            Status = FormTemplateStatus.Draft,
            Sections = ToDomainSections(request.Sections),
            CreatedUtc = now,
            UpdatedUtc = now,
        };

        await _templates.AddAsync(template, cancellationToken);
        return template.Id;
    }

    /// <summary>
    /// Updates a template. A draft is edited in place; a published version is superseded by a new draft version
    /// (R4/R10/R16). Archived versions cannot be edited. Returns the resulting template, or null if not the
    /// caller's / not found.
    /// </summary>
    public async Task<FormTemplateDto?> UpdateAsync(Guid id, UpdateFormTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await LoadOwnedAsync(id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A form name is required.", nameof(request));
        }

        if (existing.Status == FormTemplateStatus.Archived)
        {
            throw new ArgumentException("An archived form cannot be edited.", nameof(id));
        }

        var sections = ToDomainSections(request.Sections);
        var name = request.Name.Trim();
        var description = Normalise(request.Description);

        if (existing.Status == FormTemplateStatus.Draft)
        {
            existing.Name = name;
            existing.Description = description;
            existing.Sections = sections;
            existing.UpdatedUtc = DateTimeOffset.UtcNow;
            await _templates.UpdateAsync(existing, cancellationToken);
            return ToDto(existing);
        }

        // Published → create the next draft version in the same family, leaving the published version intact.
        var now = DateTimeOffset.UtcNow;
        var nextVersion = await NextVersionAsync(existing.CompanyId, existing.FamilyId, cancellationToken);
        var draft = new FormTemplate
        {
            CompanyId = existing.CompanyId,
            FamilyId = existing.FamilyId,
            Name = name,
            Description = description,
            Version = nextVersion,
            Status = FormTemplateStatus.Draft,
            Sections = sections,
            CreatedUtc = now,
            UpdatedUtc = now,
        };
        await _templates.AddAsync(draft, cancellationToken);
        return ToDto(draft);
    }

    /// <summary>Publishes a draft (freezes it). A version already published is returned unchanged. Null if not the caller's / not found.</summary>
    public async Task<FormTemplateDto?> PublishAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await LoadOwnedAsync(id, cancellationToken);
        if (template is null)
        {
            return null;
        }

        if (template.Status == FormTemplateStatus.Draft)
        {
            template.Status = FormTemplateStatus.Published;
            template.UpdatedUtc = DateTimeOffset.UtcNow;
            await _templates.UpdateAsync(template, cancellationToken);
        }

        return ToDto(template);
    }

    /// <summary>Archives a template version (retires it from the library, history retained). Null if not the caller's / not found.</summary>
    public async Task<FormTemplateDto?> ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await LoadOwnedAsync(id, cancellationToken);
        if (template is null)
        {
            return null;
        }

        template.Status = FormTemplateStatus.Archived;
        template.UpdatedUtc = DateTimeOffset.UtcNow;
        await _templates.UpdateAsync(template, cancellationToken);
        return ToDto(template);
    }

    /// <summary>
    /// Seeds the shipped starter forms for the caller's company (PRD §6 "ship a template library, not an empty
    /// builder"), publishing each so it is immediately usable. Skips any starter whose name already exists, so
    /// it is safe to run more than once. Returns how many were created.
    /// </summary>
    public async Task<int> SeedStarterTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await ResolveTenantAsync(cancellationToken);
        if (tenant is null)
        {
            return 0;
        }

        var existing = (await _templates.GetByCompanyAsync(tenant.Value, cancellationToken))
            .Select(t => t.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;
        foreach (var starter in DefaultFormTemplates.Starters)
        {
            if (existing.Contains(starter.Name))
            {
                continue;
            }

            var id = await CreateAsync(new CreateFormTemplateRequest(tenant.Value, starter.Name, starter.Description, starter.Sections), cancellationToken);
            await PublishAsync(id, cancellationToken);
            created++;
        }

        return created;
    }

    /// <summary>Computes the next version ordinal for a family (highest existing + 1).</summary>
    private async Task<int> NextVersionAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken)
    {
        var versions = await _templates.GetByFamilyAsync(companyId, familyId, cancellationToken);
        return versions.Count == 0 ? 1 : versions.Max(v => v.Version) + 1;
    }

    /// <summary>Trims a description to null when blank.</summary>
    private static string? Normalise(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Maps request sections/fields to domain records, defaulting required to true and parsing the kind (R2 field spectrum).</summary>
    private static List<FormSectionDef> ToDomainSections(IReadOnlyList<FormSectionDto> sections) =>
        (sections ?? new List<FormSectionDto>())
            .Select((s, si) => new FormSectionDef(
                string.IsNullOrWhiteSpace(s.Id) ? Guid.NewGuid().ToString("N") : s.Id,
                s.Title ?? string.Empty,
                (s.Fields ?? new List<FormFieldDto>())
                    .Select((f, fi) => new FormField(
                        string.IsNullOrWhiteSpace(f.Id) ? Guid.NewGuid().ToString("N") : f.Id,
                        Enum.TryParse<FormFieldKind>(f.Kind, ignoreCase: true, out var kind) ? kind : FormFieldKind.ShortText,
                        f.Label ?? string.Empty,
                        Normalise(f.HelpText),
                        f.Required,
                        f.ValidationJson,
                        f.OptionsJson,
                        f.Order == 0 ? fi : f.Order))
                    .ToList(),
                s.Order == 0 ? si : s.Order))
            .ToList();

    /// <summary>Maps a template to the library summary DTO (with a total field count).</summary>
    private static FormTemplateSummaryDto ToSummaryDto(FormTemplate t) => new(
        t.Id, t.FamilyId, t.Name, t.Description, t.Version, t.Status.ToString(),
        t.Sections.Sum(s => s.Fields.Count), t.UpdatedUtc);

    /// <summary>Maps a template to the full DTO (sections and fields).</summary>
    private static FormTemplateDto ToDto(FormTemplate t) => new(
        t.Id, t.FamilyId, t.Name, t.Description, t.Version, t.Status.ToString(),
        t.Sections
            .OrderBy(s => s.Order)
            .Select(s => new FormSectionDto(
                s.Id, s.Title,
                s.Fields.OrderBy(f => f.Order)
                    .Select(f => new FormFieldDto(f.Id, f.Kind.ToString(), f.Label, f.HelpText, f.Required, f.ValidationJson, f.OptionsJson, f.Order))
                    .ToList(),
                s.Order))
            .ToList(),
        t.CreatedUtc, t.UpdatedUtc);
}
