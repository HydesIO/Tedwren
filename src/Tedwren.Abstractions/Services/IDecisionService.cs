using Tedwren.Abstractions.Contracts.Decisions;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The site-entry decision record store (R10). Decisions are reconstructable later. This service is
/// introduced now (read + record); the main-contractor gate (Phase 17) records real decisions through it.
/// </summary>
public interface IDecisionService
{
    /// <summary>Returns the decisions recorded for a worker, newest first.</summary>
    Task<IReadOnlyList<SiteEntryDecisionDto>> GetForPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    /// <summary>Returns the decisions recorded for a site, newest first.</summary>
    Task<IReadOnlyList<SiteEntryDecisionDto>> GetForSiteAsync(Guid siteId, CancellationToken cancellationToken = default);

    /// <summary>Records a site-entry decision and returns its identifier.</summary>
    Task<Guid> RecordAsync(RecordDecisionRequest request, CancellationToken cancellationToken = default);
}
