using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="ISiteRepository"/> over the shared store (API mock mode).</summary>
public sealed class InMemorySiteRepository : ISiteRepository
{
    private readonly InMemorySiteStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemorySiteRepository(InMemorySiteStore store) => _store = store;

    /// <summary>Returns all sites ordered by name.</summary>
    public Task<IReadOnlyList<Site>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Site> sites = _store.Sites.Values.OrderBy(s => s.Name).ToList();
        return Task.FromResult(sites);
    }

    /// <summary>Returns a site by id, or null.</summary>
    public Task<Site?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Sites.GetValueOrDefault(id));

    /// <summary>Adds a site to the store.</summary>
    public Task AddAsync(Site site, CancellationToken cancellationToken = default)
    {
        _store.Sites[site.Id] = site;
        return Task.CompletedTask;
    }

    /// <summary>Updates a site in the store.</summary>
    public Task UpdateAsync(Site site, CancellationToken cancellationToken = default)
    {
        _store.Sites[site.Id] = site;
        return Task.CompletedTask;
    }
}
