using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for <see cref="Site"/> (SF-6/SF-14/SF-25/SF-26).</summary>
public interface ISiteRepository
{
    /// <summary>Returns all sites, ordered by name.</summary>
    Task<IReadOnlyList<Site>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a site by id, or null.</summary>
    Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a new site.</summary>
    Task AddAsync(Site site, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing site (e.g. marking it dispersed once it gains a property).</summary>
    Task UpdateAsync(Site site, CancellationToken cancellationToken = default);

    /// <summary>Permanently removes a site by id (used only by the demo-data teardown).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
