using Tedwren.Abstractions.Contracts.SiteEntry;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The site-entry decision and muster service (MC-8–MC-14, MC-28, R2, R3, R10, R14). It runs the five-check
/// decision against current data (R3), <b>fails closed</b> so any error means no entry (R2), gives a specific
/// actionable block reason (MC-9), and writes a self-reconstructing record including checks that did not run
/// and why (R10) — all within the &lt;3s budget (R14). It also serves the live, offline-aware muster with
/// competency cover (MC-12–MC-14). Reachable on no-compound schemes (MC-28).
/// </summary>
public interface ISiteEntryService
{
    /// <summary>Decides whether a worker may enter, records the decision, and returns the result (MC-8/R10).</summary>
    Task<EntryDecisionResultDto> DecideAsync(DecideEntryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the live muster for a site, with data-age and competency cover (MC-12–MC-14).</summary>
    Task<MusterDto> GetMusterAsync(Guid siteId, CancellationToken cancellationToken = default);
}
