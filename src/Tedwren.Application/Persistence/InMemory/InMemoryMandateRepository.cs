using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// In-memory <see cref="IMandateRepository"/> (test-only double). Registered as a singleton so mandates persist
/// across requests within the host, mirroring <see cref="InMemoryEntitlementRepository"/>.
/// </summary>
public sealed class InMemoryMandateRepository : IMandateRepository
{
    private readonly ConcurrentDictionary<Guid, Mandate> _mandates = new();

    /// <summary>Returns every mandate, most recent first.</summary>
    public Task<IReadOnlyList<Mandate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Mandate> rows = _mandates.Values.OrderByDescending(m => m.CreatedUtc).ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns a mandate by local id, or null.</summary>
    public Task<Mandate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_mandates.GetValueOrDefault(id));

    /// <summary>Returns the company's most recent mandate, or null.</summary>
    public Task<Mandate?> GetCurrentForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_mandates.Values
            .Where(m => m.CompanyId == companyId)
            .OrderByDescending(m => m.CreatedUtc)
            .FirstOrDefault());

    /// <summary>Returns the mandate carrying the given GoCardless mandate id, or null.</summary>
    public Task<Mandate?> GetByGoCardlessMandateIdAsync(string goCardlessMandateId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_mandates.Values
            .FirstOrDefault(m => m.GoCardlessMandateId == goCardlessMandateId));

    /// <summary>Adds a mandate.</summary>
    public Task AddAsync(Mandate mandate, CancellationToken cancellationToken = default)
    {
        _mandates[mandate.Id] = mandate;
        return Task.CompletedTask;
    }

    /// <summary>Updates a mandate.</summary>
    public Task UpdateAsync(Mandate mandate, CancellationToken cancellationToken = default)
    {
        _mandates[mandate.Id] = mandate;
        return Task.CompletedTask;
    }
}
