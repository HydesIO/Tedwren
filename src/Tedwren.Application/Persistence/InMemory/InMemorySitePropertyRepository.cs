using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="ISitePropertyRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemorySitePropertyRepository : ISitePropertyRepository
{
    private readonly InMemorySiteStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemorySitePropertyRepository(InMemorySiteStore store) => _store = store;

    /// <summary>Returns the properties of a site, ordered by address.</summary>
    public Task<IReadOnlyList<SiteProperty>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SiteProperty> properties = _store.Properties.Values
            .Where(p => p.SiteId == siteId)
            .OrderBy(p => p.Address)
            .ToList();
        return Task.FromResult(properties);
    }

    /// <summary>Counts the properties of a site.</summary>
    public Task<int> CountBySiteAsync(Guid siteId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Properties.Values.Count(p => p.SiteId == siteId));

    /// <summary>Adds a property to the store.</summary>
    public Task AddAsync(SiteProperty property, CancellationToken cancellationToken = default)
    {
        _store.Properties[property.Id] = property;
        return Task.CompletedTask;
    }
}
