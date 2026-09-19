using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IAssetRepository"/>.</summary>
public sealed class InMemoryAssetRepository : IAssetRepository
{
    private readonly ConcurrentDictionary<Guid, Asset> _assets = new();

    /// <summary>Persists a new asset.</summary>
    public Task AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        _assets[asset.Id] = asset;
        return Task.CompletedTask;
    }

    /// <summary>Returns a company's assets, newest first.</summary>
    public Task<IReadOnlyList<Asset>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Asset> rows = _assets.Values
            .Where(a => a.CompanyId == companyId)
            .OrderByDescending(a => a.CreatedUtc)
            .ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns a single asset by id, or null if none exists.</summary>
    public Task<Asset?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_assets.TryGetValue(id, out var asset) ? asset : null);

    /// <summary>Updates an asset's mutable fields.</summary>
    public Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        _assets[asset.Id] = asset;
        return Task.CompletedTask;
    }
}
