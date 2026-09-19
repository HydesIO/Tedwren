using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for <see cref="Asset"/> (the plant &amp; equipment register). Reads are scoped by company (R15).</summary>
public interface IAssetRepository
{
    /// <summary>Persists a new asset.</summary>
    Task AddAsync(Asset asset, CancellationToken cancellationToken = default);

    /// <summary>Returns a company's assets, newest first.</summary>
    Task<IReadOnlyList<Asset>> GetByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Returns a single asset by id, or null if none exists.</summary>
    Task<Asset?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates an asset's mutable fields (details and status).</summary>
    Task UpdateAsync(Asset asset, CancellationToken cancellationToken = default);
}
