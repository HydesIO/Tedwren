using Tedwren.Abstractions.Contracts.Assets;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Assets;

/// <summary>
/// Store-agnostic plant &amp; equipment register service (PRD §8.2): lists, adds, updates and retires a company's
/// assets, all scoped to the owning company (R15). It derives each asset's certification status from its expiry
/// date so a lapsing certificate surfaces in the list, reusing the same warning-window idea as a qualification
/// card (SF-9).
/// </summary>
public sealed class AssetService : IAssetService
{
    /// <summary>The warning window (days) before a certificate expiry within which it is flagged "expiring soon".</summary>
    private const int WarningWindowDays = 30;

    private readonly IAssetRepository _assets;

    /// <summary>Creates the service over the asset repository.</summary>
    public AssetService(IAssetRepository assets) => _assets = assets;

    /// <summary>Returns a company's assets, newest first.</summary>
    public async Task<IReadOnlyList<AssetDto>> ListForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var assets = await _assets.GetByCompanyAsync(companyId, cancellationToken);
        return assets.Select(ToDto).ToList();
    }

    /// <summary>Adds an asset to the company's register and returns its new identifier.</summary>
    public async Task<Guid> CreateAsync(Guid companyId, CreateAssetRequest request, CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("A company id is required.", nameof(companyId));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("An asset name is required.", nameof(request));
        }

        var asset = new Asset
        {
            CompanyId = companyId,
            Name = request.Name.Trim(),
            Status = AssetStatus.Active,
        };
        Apply(asset, request.AssetType, request.SerialNumber, request.Location, request.OwnerName,
            request.CertificationExpiry, request.NextInspectionDue, request.Notes);

        await _assets.AddAsync(asset, cancellationToken);
        return asset.Id;
    }

    /// <summary>Updates an asset in the company's register (R15). Returns false when it is missing or cross-tenant.</summary>
    public async Task<bool> UpdateAsync(Guid companyId, Guid assetId, UpdateAssetRequest request, CancellationToken cancellationToken = default)
    {
        var asset = await LoadScopedAsync(companyId, assetId, cancellationToken);
        if (asset is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("An asset name is required.", nameof(request));
        }

        asset.Name = request.Name.Trim();
        Apply(asset, request.AssetType, request.SerialNumber, request.Location, request.OwnerName,
            request.CertificationExpiry, request.NextInspectionDue, request.Notes);
        await _assets.UpdateAsync(asset, cancellationToken);
        return true;
    }

    /// <summary>Retires an asset (kept for history, §9). Returns false when it is missing or cross-tenant (R15).</summary>
    public async Task<bool> RetireAsync(Guid companyId, Guid assetId, CancellationToken cancellationToken = default)
    {
        var asset = await LoadScopedAsync(companyId, assetId, cancellationToken);
        if (asset is null)
        {
            return false;
        }

        asset.Status = AssetStatus.Retired;
        await _assets.UpdateAsync(asset, cancellationToken);
        return true;
    }

    /// <summary>Loads an asset only when it belongs to the given company, so a caller can never reach another tenant's asset (R15).</summary>
    private async Task<Asset?> LoadScopedAsync(Guid companyId, Guid assetId, CancellationToken cancellationToken)
    {
        var asset = await _assets.GetAsync(assetId, cancellationToken);
        return asset is not null && asset.CompanyId == companyId ? asset : null;
    }

    /// <summary>Copies the editable detail fields onto an asset (trimming blanks to null).</summary>
    private static void Apply(Asset asset, string? assetType, string? serialNumber, string? location, string? ownerName,
        DateOnly? certificationExpiry, DateOnly? nextInspectionDue, string? notes)
    {
        asset.AssetType = Clean(assetType);
        asset.SerialNumber = Clean(serialNumber);
        asset.Location = Clean(location);
        asset.OwnerName = Clean(ownerName);
        asset.CertificationExpiry = certificationExpiry;
        asset.NextInspectionDue = nextInspectionDue;
        asset.Notes = Clean(notes);
    }

    /// <summary>Trims a value, mapping blank to null.</summary>
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Maps an asset entity to its DTO, deriving the certification status from the expiry date.</summary>
    private static AssetDto ToDto(Asset a) => new(
        a.Id, a.Name, a.AssetType, a.SerialNumber, a.Location, a.OwnerName,
        a.CertificationExpiry, a.NextInspectionDue, a.Notes, a.Status.ToString(),
        CertificationStatus(a.CertificationExpiry), a.CreatedUtc);

    /// <summary>Derives the display certification status ("Valid" / "Expiring soon" / "Expired" / "No certificate").</summary>
    private static string CertificationStatus(DateOnly? expiry)
    {
        if (expiry is not { } date)
        {
            return "No certificate";
        }

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);
        if (date < today)
        {
            return "Expired";
        }

        return date <= today.AddDays(WarningWindowDays) ? "Expiring soon" : "Valid";
    }
}
