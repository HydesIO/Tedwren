using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// Deprecated in-proc <see cref="IExpiryQueryService"/> for the retained (test-only) client <c>Mock</c>
/// mode. Returns empty results; the real data is served by <see cref="ApiExpiryQueryService"/> in the
/// standard <c>Api</c> mode.
/// </summary>
public sealed class ClientMockExpiryQueryService : IExpiryQueryService
{
    /// <summary>No upcoming expiries in mock mode.</summary>
    public Task<IReadOnlyList<UpcomingExpiryDto>> GetUpcomingAsync(int withinDays, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UpcomingExpiryDto>>(Array.Empty<UpcomingExpiryDto>());

    /// <summary>No job runs in mock mode.</summary>
    public Task<IReadOnlyList<JobRunDto>> GetRecentJobRunsAsync(int take, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<JobRunDto>>(Array.Empty<JobRunDto>());
}
