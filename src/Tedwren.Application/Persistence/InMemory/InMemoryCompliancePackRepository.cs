using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="ICompliancePackRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemoryCompliancePackRepository : ICompliancePackRepository
{
    private readonly InMemoryCompliancePackStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemoryCompliancePackRepository(InMemoryCompliancePackStore store) => _store = store;

    /// <summary>Persists a new pack.</summary>
    public Task AddAsync(CompliancePack pack, CancellationToken cancellationToken = default)
    {
        _store.Packs[pack.Id] = pack;
        return Task.CompletedTask;
    }

    /// <summary>Returns a pack by id, scoped to its company, or null (R15).</summary>
    public Task<CompliancePack?> GetAsync(Guid companyId, Guid packId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Packs.TryGetValue(packId, out var pack) && pack.CompanyId == companyId ? pack : null);

    /// <summary>Returns a pack by its opaque token, or null.</summary>
    public Task<CompliancePack?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Packs.Values.FirstOrDefault(p => p.Token == token));

    /// <summary>Returns a company's packs, newest first.</summary>
    public Task<IReadOnlyList<CompliancePack>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<CompliancePack> packs = _store.Packs.Values
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.CreatedUtc)
            .ToList();
        return Task.FromResult(packs);
    }

    /// <summary>Persists status changes (revoke, supersede).</summary>
    public Task UpdateStatusAsync(CompliancePack pack, CancellationToken cancellationToken = default)
    {
        _store.Packs[pack.Id] = pack;
        return Task.CompletedTask;
    }

    /// <summary>Appends a recipient access event (SUB-20).</summary>
    public Task AddAccessEventAsync(PackAccessEvent accessEvent, CancellationToken cancellationToken = default)
    {
        _store.AccessEvents[accessEvent.Id] = accessEvent;
        return Task.CompletedTask;
    }

    /// <summary>Returns the open/download tallies for a pack (SUB-20).</summary>
    public Task<(int Opened, int Downloaded)> GetAccessTallyAsync(Guid packId, CancellationToken cancellationToken = default)
    {
        var events = _store.AccessEvents.Values.Where(e => e.PackId == packId).ToList();
        var opened = events.Count(e => e.Kind == PackAccessKind.Opened);
        var downloaded = events.Count(e => e.Kind == PackAccessKind.Downloaded);
        return Task.FromResult((opened, downloaded));
    }
}
