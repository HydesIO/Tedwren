using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IFormTemplateRepository"/> over the shared store (test-only mock mode).</summary>
public sealed class InMemoryFormTemplateRepository : IFormTemplateRepository
{
    private readonly InMemoryFormStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryFormTemplateRepository(InMemoryFormStore store) => _store = store;

    /// <summary>Returns a company's template versions, newest change first.</summary>
    public Task<IReadOnlyList<FormTemplate>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FormTemplate> templates = _store.Templates.Values
            .Where(t => t.CompanyId == companyId)
            .OrderByDescending(t => t.UpdatedUtc)
            .ThenBy(t => t.Name)
            .ToList();
        return Task.FromResult(templates);
    }

    /// <summary>Returns a template version by id, or null.</summary>
    public Task<FormTemplate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Templates.GetValueOrDefault(id));

    /// <summary>Returns all versions of a family owned by a company, highest version first.</summary>
    public Task<IReadOnlyList<FormTemplate>> GetByFamilyAsync(Guid companyId, Guid familyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<FormTemplate> versions = _store.Templates.Values
            .Where(t => t.CompanyId == companyId && t.FamilyId == familyId)
            .OrderByDescending(t => t.Version)
            .ToList();
        return Task.FromResult(versions);
    }

    /// <summary>Adds a template version to the store.</summary>
    public Task AddAsync(FormTemplate template, CancellationToken cancellationToken = default)
    {
        _store.Templates[template.Id] = template;
        return Task.CompletedTask;
    }

    /// <summary>Updates a template version in the store.</summary>
    public Task UpdateAsync(FormTemplate template, CancellationToken cancellationToken = default)
    {
        _store.Templates[template.Id] = template;
        return Task.CompletedTask;
    }
}
