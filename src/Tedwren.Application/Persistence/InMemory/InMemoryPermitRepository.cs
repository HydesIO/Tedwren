using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IPermitRepository"/>.</summary>
public sealed class InMemoryPermitRepository : IPermitRepository
{
    private readonly ConcurrentDictionary<Guid, Permit> _permits = new();

    /// <summary>Persists a new permit.</summary>
    public Task AddAsync(Permit permit, CancellationToken cancellationToken = default)
    {
        _permits[permit.Id] = permit;
        return Task.CompletedTask;
    }

    /// <summary>Returns a company's permits, newest first.</summary>
    public Task<IReadOnlyList<Permit>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Permit> rows = _permits.Values
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.CreatedUtc)
            .ToList();
        return Task.FromResult(rows);
    }
}
