using System.Collections.Concurrent;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>
/// In-memory <see cref="IDecisionRepository"/> (API mock mode). Singleton. Seeded with one recorded site-entry
/// decision on the main contractor's site (MC-8/R10) so the decision history has data; the subcontractor
/// product never makes such a decision (R18).
/// </summary>
public sealed class InMemoryDecisionRepository : IDecisionRepository
{
    private readonly ConcurrentDictionary<Guid, SiteEntryDecision> _decisions = new();

    /// <summary>Creates the repository and loads the demo seed.</summary>
    public InMemoryDecisionRepository() : this(seed: true)
    {
    }

    /// <summary>Creates the repository, optionally loading the demo seed (tests pass false for a clean store).</summary>
    public InMemoryDecisionRepository(bool seed)
    {
        if (!seed)
        {
            return;
        }

        // A worker admitted at Meridian Tower, with the full five-check record so the decision reconstructs (R10).
        var decision = new SiteEntryDecision
        {
            PersonId = DemoSeed.FletcherPersonId,
            SiteId = DemoSeed.MeridianTowerSiteId,
            Admitted = true,
            OccurredUtc = DateTimeOffset.UtcNow.AddHours(-3),
            Checks = new List<DecisionCheck>
            {
                new("Registered", DecisionCheckOutcome.Passed, "Known operative on this site"),
                new("Not signed in elsewhere", DecisionCheckOutcome.Passed, null),
                new("Induction valid", DecisionCheckOutcome.Passed, "Valid until next year"),
                new("Cards in date and confirmed", DecisionCheckOutcome.Passed, "CSCS in date"),
                new("RAMS", DecisionCheckOutcome.NotRun, "HSC module not held"),
            },
        };
        _decisions[decision.Id] = decision;
    }

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
