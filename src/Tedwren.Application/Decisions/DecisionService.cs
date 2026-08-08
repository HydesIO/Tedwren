using Tedwren.Abstractions.Contracts.Decisions;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Decisions;

/// <summary>
/// The site-entry decision record store service (R10). Reads decisions back reconstructably and records new
/// ones (the Phase 17 gate is the writer). Data-store agnostic.
/// </summary>
public sealed class DecisionService : IDecisionService
{
    private readonly IDecisionRepository _decisions;

    /// <summary>Creates the service over the decision repository.</summary>
    public DecisionService(IDecisionRepository decisions) => _decisions = decisions;

    /// <summary>Returns the decisions recorded for a worker.</summary>
    public async Task<IReadOnlyList<SiteEntryDecisionDto>> GetForPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var decisions = await _decisions.GetByPersonAsync(personId, cancellationToken);
        return decisions.Select(ToDto).ToList();
    }

    /// <summary>Returns the decisions recorded for a site.</summary>
    public async Task<IReadOnlyList<SiteEntryDecisionDto>> GetForSiteAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var decisions = await _decisions.GetBySiteAsync(siteId, cancellationToken);
        return decisions.Select(ToDto).ToList();
    }

    /// <summary>Records a site-entry decision and returns its identifier.</summary>
    public async Task<Guid> RecordAsync(RecordDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var decision = new SiteEntryDecision
        {
            PersonId = request.PersonId,
            SiteId = request.SiteId,
            Admitted = request.Admitted,
            Checks = request.Checks.Select(ToDomain).ToList(),
        };
        await _decisions.AddAsync(decision, cancellationToken);
        return decision.Id;
    }

    /// <summary>Maps a decision to its DTO.</summary>
    private static SiteEntryDecisionDto ToDto(SiteEntryDecision d) => new(
        d.Id, d.PersonId, d.SiteId, d.Admitted, d.OccurredUtc,
        d.Checks.Select(c => new DecisionCheckDto(c.Name, c.Outcome.ToString(), c.Detail)).ToList());

    /// <summary>Maps a check DTO to the domain value object, parsing the outcome (unknown → NotRun).</summary>
    private static DecisionCheck ToDomain(DecisionCheckDto c) => new(
        c.Name,
        Enum.TryParse<DecisionCheckOutcome>(c.Outcome, ignoreCase: true, out var outcome) ? outcome : DecisionCheckOutcome.NotRun,
        c.Detail);
}
