using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Abstractions.Contracts.Sites;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Typed client for the manager site-entry surface (M7): the company's sites (for the picker), the live muster
/// (MC-12–MC-14) and an authenticated entry decision + day-only override (MC-8/MC-11), over the console-token auth
/// handler. Reads throw on a non-success status; <see cref="DecideAsync"/> surfaces a failed decision as an
/// <see cref="ApiException"/> so the caller never mistakes an error for an admission (fail-closed, R2).
/// </summary>
public sealed class ManagerSiteEntryApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over a configured <see cref="HttpClient"/> whose BaseAddress is the API root.</summary>
    public ManagerSiteEntryApiClient(HttpClient http) => _http = http;

    /// <summary>The company's sites (for the muster / decide site picker).</summary>
    public async Task<IReadOnlyList<SiteSummary>> GetSitesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<SiteSummary>>("api/sites", cancellationToken) ?? Array.Empty<SiteSummary>();

    /// <summary>The live muster for one of the manager's own sites (data-age via <see cref="MusterDto.GeneratedUtc"/>).</summary>
    public Task<MusterDto?> GetMusterAsync(Guid siteId, CancellationToken cancellationToken = default) =>
        _http.GetFromJsonAsync<MusterDto>($"api/manager/muster/{siteId}", cancellationToken);

    /// <summary>Runs an authenticated entry decision (with an optional day-only override) and returns the result.</summary>
    public async Task<EntryDecisionResultDto?> DecideAsync(ManagerDecideRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/manager/entry/decide", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, $"Entry decision failed ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<EntryDecisionResultDto>(cancellationToken);
    }
}
