using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IExpiryQueryService"/> backed by the Tedwren Web API over HTTP (SF-9/SF-21) — used in the
/// standard <c>DataSource=Api</c> mode. Backs the notifications feed and job-run visibility.
/// </summary>
public sealed class ApiExpiryQueryService : IExpiryQueryService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiExpiryQueryService(HttpClient http) => _http = http;

    /// <summary>Gets the cards expiring within the given window (or already expired), soonest first.</summary>
    public async Task<IReadOnlyList<UpcomingExpiryDto>> GetUpcomingAsync(int withinDays, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<UpcomingExpiryDto>>($"api/expiry/upcoming?withinDays={withinDays}", cancellationToken)
        ?? Array.Empty<UpcomingExpiryDto>();

    /// <summary>Gets the most recent scheduled-job runs (SF-21 visibility).</summary>
    public async Task<IReadOnlyList<JobRunDto>> GetRecentJobRunsAsync(int take, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<JobRunDto>>("api/jobs/runs", cancellationToken)
        ?? Array.Empty<JobRunDto>();
}
