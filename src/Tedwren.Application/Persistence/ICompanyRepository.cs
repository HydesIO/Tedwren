using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for <see cref="Company"/>. Implemented in-memory (mock) and with Dapper.</summary>
public interface ICompanyRepository
{
    /// <summary>Returns all companies.</summary>
    Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a company by id, or null.</summary>
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a new company.</summary>
    Task AddAsync(Company company, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing company's editable fields.</summary>
    Task UpdateAsync(Company company, CancellationToken cancellationToken = default);
}
