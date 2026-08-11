using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Decisions;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IDecisionService"/> backed by the Tedwren Web API (<c>/api/decisions</c>, R10). Reads recorded
/// site-entry decisions and records new ones.
/// </summary>
public sealed class ApiDecisionService : IDecisionService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiDecisionService(HttpClient http) => _http = http;

    /// <summary>Returns the decisions recorded for a worker, newest first.</summary>
    public async Task<IReadOnlyList<SiteEntryDecisionDto>> GetForPersonAsync(Guid personId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<SiteEntryDecisionDto>>($"api/decisions/people/{personId}", cancellationToken)
        ?? Array.Empty<SiteEntryDecisionDto>();

    /// <summary>Returns the decisions recorded for a site, newest first.</summary>
    public async Task<IReadOnlyList<SiteEntryDecisionDto>> GetForSiteAsync(Guid siteId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<SiteEntryDecisionDto>>($"api/decisions/sites/{siteId}", cancellationToken)
        ?? Array.Empty<SiteEntryDecisionDto>();

    /// <summary>Records a site-entry decision and returns its identifier.</summary>
    public async Task<Guid> RecordAsync(RecordDecisionRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/decisions", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Guid>(cancellationToken);
    }
}
