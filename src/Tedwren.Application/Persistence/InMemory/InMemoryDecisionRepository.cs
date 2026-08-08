using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IDecisionRepository"/> (API mock mode). Singleton; starts empty.</summary>
public sealed class InMemoryDecisionRepository : IDecisionRepository
{
    private readonly ConcurrentDictionary<Guid, SiteEntryDecision> _decisions = new();

    /// <summary>Returns a worker's decisions, newest first.</summary>
    public Task<IReadOnlyList<SiteEntryDecision>> GetByPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SiteEntryDecision> results = _decisions.Values
            .Where(d => d.PersonId == personId)
            .OrderByDescending(d => d.OccurredUtc)
            .ToList();
        return Task.FromResult(results);
    }

    /// <summary>Returns a site's decisions, newest first.</summary>
    public Task<IReadOnlyList<SiteEntryDecision>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<SiteEntryDecision> results = _decisions.Values
            .Where(d => d.SiteId == siteId)
            .OrderByDescending(d => d.OccurredUtc)
            .ToList();
        return Task.FromResult(results);
    }

    /// <summary>Appends a decision.</summary>
    public Task AddAsync(SiteEntryDecision decision, CancellationToken cancellationToken = default)
    {
        _decisions[decision.Id] = decision;
        return Task.CompletedTask;
    }
}
