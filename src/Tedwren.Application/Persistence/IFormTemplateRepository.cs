using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for <see cref="FormTemplate"/> (the PRD-Phase 2 Forms Library). Tenant-agnostic — the service scopes by company (R15).</summary>
public interface IFormTemplateRepository
{
    /// <summary>Returns every form template version a company owns.</summary>
    Task<IReadOnlyList<FormTemplate>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single template version by id, or null.</summary>
    Task<FormTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all versions of one logical form (a family) owned by a company, newest version first.</summary>
    Task<IReadOnlyList<FormTemplate>> GetByFamilyAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new template version.</summary>
    Task AddAsync(FormTemplate template, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing template version (draft edits, status transitions).</summary>
    Task UpdateAsync(FormTemplate template, CancellationToken cancellationToken = default);
}
