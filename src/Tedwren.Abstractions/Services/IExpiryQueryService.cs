using Tedwren.Abstractions.Contracts.Expiry;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Read-side queries for the expiry engine, shared by the client and the API. Backs the upcoming-expiries
/// view and the job-runs status view (SF-21). Served identically from mock or database.
/// </summary>
public interface IExpiryQueryService
{
    /// <summary>Returns current cards expiring within <paramref name="withinDays"/> days (or already expired), soonest first.</summary>
    Task<IReadOnlyList<UpcomingExpiryDto>> GetUpcomingAsync(int withinDays, CancellationToken cancellationToken = default);

    /// <summary>Returns the most recent scheduled-job runs (SF-21 visibility).</summary>
    Task<IReadOnlyList<JobRunDto>> GetRecentJobRunsAsync(int take, CancellationToken cancellationToken = default);
}
