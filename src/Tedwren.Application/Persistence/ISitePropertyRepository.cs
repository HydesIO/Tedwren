using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for the properties of a dispersed scheme (SF-26).</summary>
public interface ISitePropertyRepository
{
    /// <summary>Returns the properties of a site, ordered by address.</summary>
    Task<IReadOnlyList<SiteProperty>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default);

    /// <summary>Counts the properties of a site.</summary>
    Task<int> CountBySiteAsync(Guid siteId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new property.</summary>
    Task AddAsync(SiteProperty property, CancellationToken cancellationToken = default);
}
