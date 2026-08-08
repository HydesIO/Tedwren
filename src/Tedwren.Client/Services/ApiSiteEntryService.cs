using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ISiteEntryService"/> backed by the Tedwren Web API over HTTP — used when the client is configured
/// with <c>DataSource=Api</c>. The UI injects the same interface as in mock mode.
/// </summary>
public sealed class ApiSiteEntryService : ISiteEntryService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiSiteEntryService(HttpClient http) => _http = http;

    /// <summary>Runs the site-entry decision via the API (MC-8).</summary>
    public async Task<EntryDecisionResultDto> DecideAsync(DecideEntryRequest request, CancellationToken cancellationToken = default) =>
        (await (await _http.PostAsJsonAsync("api/site-entry/decide", request, cancellationToken))
            .Content.ReadFromJsonAsync<EntryDecisionResultDto>(cancellationToken))!;

    /// <summary>Gets the live muster for a site via the API (MC-12–MC-14).</summary>
    public async Task<MusterDto> GetMusterAsync(Guid siteId, CancellationToken cancellationToken = default) =>
        (await _http.GetFromJsonAsync<MusterDto>($"api/site-entry/muster/{siteId}", cancellationToken))!;
}
