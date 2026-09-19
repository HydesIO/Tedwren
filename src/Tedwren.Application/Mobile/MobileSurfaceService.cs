using Tedwren.Abstractions;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Abstractions.Contracts.Workforce;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Application.Mobile;

/// <summary>
/// Composes the operative (mobile) read surface (M3). Reuses the console <see cref="IWorkforceService"/> to
/// build the operative's own detail (bridging the per-company engagement lookup), and reads the company's sites
/// with geofences directly from the site repositories. Company/person ids come from the operative token.
/// </summary>
public sealed class MobileSurfaceService : IMobileSurfaceService
{
    private readonly IEngagementRepository _engagements;
    private readonly IWorkforceService _workforce;
    private readonly ISiteRepository _sites;
    private readonly ISitePropertyRepository _properties;

    /// <summary>Creates the service over the engagement + site repositories and the workforce read model.</summary>
    public MobileSurfaceService(
        IEngagementRepository engagements,
        IWorkforceService workforce,
        ISiteRepository sites,
        ISitePropertyRepository properties)
    {
        _engagements = engagements;
        _workforce = workforce;
        _sites = sites;
        _properties = properties;
    }

    /// <summary>The operative's own profile, or null when they have no engagement in the company.</summary>
    public async Task<OperativeDetailDto?> GetProfileAsync(Guid companyId, Guid personId, CancellationToken cancellationToken = default)
    {
        var engagement = await _engagements.GetByCompanyAndPersonAsync(companyId, personId, cancellationToken);
        return engagement is null
            ? null
            : await _workforce.GetOperativeByEngagementAsync(companyId, engagement.Id, cancellationToken);
    }

    /// <summary>The company's sites, each with geofence + dispersed properties, for offline sign-in caching.</summary>
    public async Task<IReadOnlyList<MobileSiteDto>> GetSitesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var all = await _sites.GetAllAsync(cancellationToken);
        var sites = all.Where(s => s.CompanyId == companyId).ToList();

        var result = new List<MobileSiteDto>(sites.Count);
        foreach (var site in sites)
        {
            var properties = await _properties.GetBySiteAsync(site.Id, cancellationToken);
            result.Add(new MobileSiteDto(
                site.Id,
                Slug.From(site.Name),
                site.Name,
                site.Region,
                site.HasCompound,
                site.IsDispersed,
                ToDto(site.Boundary),
                properties.Select(p => new SitePropertyDto(p.Id, p.Address, p.Units, ToDto(p.Boundary)!)).ToList()));
        }

        return result;
    }

    /// <summary>Maps a domain geofence to its DTO (or null).</summary>
    private static GeofenceDto? ToDto(Geofence? boundary) =>
        boundary is null ? null : new GeofenceDto(boundary.CentreLatitude, boundary.CentreLongitude, boundary.RadiusMetres);
}
