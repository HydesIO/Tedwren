using Tedwren.Abstractions.Contracts.Sites;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The sites service contract (SF-6, SF-14, SF-25, SF-26), shared by the client and the API so the UI and
/// business logic are identical whether data is served from mock or from the database. Sites carry a
/// boundary, may have no fixed point of presence, and may be dispersed schemes of geofenced properties.
/// </summary>
public interface ISiteService
{
    /// <summary>Returns every site as a list-row summary.</summary>
    Task<IReadOnlyList<SiteSummary>> GetSitesAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the full site for a slug, or null when no site matches.</summary>
    Task<SiteDetailDto?> GetSiteAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Records a site and returns its new identifier (SF-6; unlimited and never billed).</summary>
    Task<Guid> CreateSiteAsync(CreateSiteRequest request, CancellationToken cancellationToken = default);

    /// <summary>Adds a property to a dispersed scheme and returns its new identifier (SF-26). Null if the site is not found.</summary>
    Task<Guid?> AddPropertyAsync(AddSitePropertyRequest request, CancellationToken cancellationToken = default);
}
