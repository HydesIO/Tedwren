using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for the append-only site-entry decision store (R10).</summary>
public interface IDecisionRepository
{
    /// <summary>Returns a worker's decisions, newest first.</summary>
    Task<IReadOnlyList<SiteEntryDecision>> GetByPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Returns a site's decisions, newest first.</summary>
    Task<IReadOnlyList<SiteEntryDecision>> GetBySiteAsync(Guid siteId, CancellationToken cancellationToken = default);

    /// <summary>Appends a decision.</summary>
    Task AddAsync(SiteEntryDecision decision, CancellationToken cancellationToken = default);
}
