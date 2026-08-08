using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="ICompanyRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryCompanyRepository : ICompanyRepository
{
    private readonly InMemoryOrganisationStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryCompanyRepository(InMemoryOrganisationStore store) => _store = store;

    /// <summary>Returns all companies ordered by name.</summary>
    public Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Company> companies = _store.Companies.Values.OrderBy(c => c.Name).ToList();
        return Task.FromResult(companies);
    }

    /// <summary>Returns a company by id, or null.</summary>
    public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Companies.GetValueOrDefault(id));

    /// <summary>Adds a company to the store.</summary>
    public Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        _store.Companies[company.Id] = company;
        return Task.CompletedTask;
    }
}
