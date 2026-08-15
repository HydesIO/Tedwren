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

    /// <summary>Returns the company whose registration number matches (case-insensitive, ignoring spaces), or null.</summary>
    public Task<Company?> GetByRegistrationNumberAsync(string registrationNumber, CancellationToken cancellationToken = default)
    {
        var normalised = Normalise(registrationNumber);
        var match = normalised.Length == 0
            ? null
            : _store.Companies.Values.FirstOrDefault(c => Normalise(c.RegistrationNumber) == normalised);
        return Task.FromResult(match);
    }

    /// <summary>Lower-cases and strips spaces for tolerant registration-number matching.</summary>
    private static string Normalise(string? value) => (value ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();

    /// <summary>Adds a company to the store.</summary>
    public Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        _store.Companies[company.Id] = company;
        return Task.CompletedTask;
    }

    /// <summary>Updates a company in the store.</summary>
    public Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        _store.Companies[company.Id] = company;
        return Task.CompletedTask;
    }
}
