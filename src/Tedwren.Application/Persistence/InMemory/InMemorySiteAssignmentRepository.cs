using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="ISiteAssignmentRepository"/> over the shared site store (API mock mode).</summary>
public sealed class InMemorySiteAssignmentRepository : ISiteAssignmentRepository
{
    private readonly InMemorySiteStore _store;

    /// <summary>Creates the repository over the shared store.</summary>
    public InMemorySiteAssignmentRepository(InMemorySiteStore store) => _store = store;

    /// <summary>Returns the site ids assigned to a user.</summary>
    public Task<IReadOnlyList<Guid>> GetSiteIdsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> siteIds = _store.Assignments.Values
            .Where(a => a.UserId == userId)
            .Select(a => a.SiteId)
            .Distinct()
            .ToList();
        return Task.FromResult(siteIds);
    }

    /// <summary>Replaces a user's assignments with the given set of sites.</summary>
    public Task ReplaceForUserAsync(Guid userId, IReadOnlyList<Guid> siteIds, CancellationToken cancellationToken = default)
    {
        foreach (var existing in _store.Assignments.Where(kv => kv.Value.UserId == userId).ToList())
        {
            _store.Assignments.TryRemove(existing.Key, out _);
        }

        foreach (var siteId in siteIds.Distinct())
        {
            var assignment = new SiteAssignment { UserId = userId, SiteId = siteId };
            _store.Assignments[assignment.Id] = assignment;
        }

        return Task.CompletedTask;
    }
}
