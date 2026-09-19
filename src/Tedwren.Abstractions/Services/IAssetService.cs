using Tedwren.Abstractions.Contracts.Assets;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The plant &amp; equipment register (PRD §8.2): list, add, update and retire a company's assets. Scoped to the
/// owning company (R15); part of the paid Health, Safety &amp; Compliance module.
/// </summary>
public interface IAssetService
{
    /// <summary>Returns a company's assets, newest first.</summary>
    Task<IReadOnlyList<AssetDto>> ListForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Adds an asset to the company's register and returns its new identifier.</summary>
    Task<Guid> CreateAsync(Guid companyId, CreateAssetRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates an asset in the company's register. Returns false when no such asset exists for the company.</summary>
    Task<bool> UpdateAsync(Guid companyId, Guid assetId, UpdateAssetRequest request, CancellationToken cancellationToken = default);

    /// <summary>Retires an asset (kept for history, §9). Returns false when no such asset exists for the company.</summary>
    Task<bool> RetireAsync(Guid companyId, Guid assetId, CancellationToken cancellationToken = default);
}
