using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IInductionTemplateRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryInductionTemplateRepository : IInductionTemplateRepository
{
    private readonly InMemoryInductionStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryInductionTemplateRepository(InMemoryInductionStore store) => _store = store;

    /// <summary>Returns a company's templates.</summary>
    public Task<IReadOnlyList<InductionTemplate>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<InductionTemplate> templates = _store.Templates.Values
            .Where(t => t.CompanyId == companyId)
            .OrderBy(t => t.Name)
            .ToList();
        return Task.FromResult(templates);
    }

    /// <summary>Returns a template by id, or null.</summary>
    public Task<InductionTemplate?> GetByIdAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Templates.GetValueOrDefault(templateId));

    /// <summary>Persists a new template.</summary>
    public Task AddAsync(InductionTemplate template, CancellationToken cancellationToken = default)
    {
        _store.Templates[template.Id] = template;
        return Task.CompletedTask;
    }

    /// <summary>Updates an existing template.</summary>
    public Task UpdateAsync(InductionTemplate template, CancellationToken cancellationToken = default)
    {
        _store.Templates[template.Id] = template;
        return Task.CompletedTask;
    }
}
