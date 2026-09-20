using System.Diagnostics;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Typed client for the manager site-entry surface (M7): the company's sites (for the picker), the live muster
/// (MC-12–MC-14) and an authenticated entry decision + day-only override (MC-8/MC-11), over the console-token auth
/// handler. Reads throw on a non-success status; <see cref="DecideAsync"/> surfaces a failed decision as an
/// <see cref="ApiException"/> so the caller never mistakes an error for an admission (fail-closed, R2). The decision
/// round-trip is timed for the R14 &lt;3s budget (M8).
/// </summary>
public sealed class ManagerSiteEntryApiClient
{
    private readonly HttpClient _http;
    private readonly ITelemetry _telemetry;

    /// <summary>Creates the client over a configured <see cref="HttpClient"/> (BaseAddress = API root) and telemetry.</summary>
    public ManagerSiteEntryApiClient(HttpClient http, ITelemetry telemetry)
    {
        _http = http;
        _telemetry = telemetry;
    }

    /// <summary>The company's sites (for the muster / decide site picker).</summary>
    public async Task<IReadOnlyList<SiteSummary>> GetSitesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<SiteSummary>>("api/sites", cancellationToken) ?? Array.Empty<SiteSummary>();

    /// <summary>The live muster for one of the manager's own sites (data-age via <see cref="MusterDto.GeneratedUtc"/>).</summary>
    public Task<MusterDto?> GetMusterAsync(Guid siteId, CancellationToken cancellationToken = default) =>
        _http.GetFromJsonAsync<MusterDto>($"api/manager/muster/{siteId}", cancellationToken);

    /// <summary>Runs an authenticated entry decision (with an optional day-only override) and returns the result.</summary>
    public async Task<EntryDecisionResultDto?> DecideAsync(ManagerDecideRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        using var response = await _http.PostAsJsonAsync("api/manager/entry/decide", request, cancellationToken);
        _telemetry.TrackTiming("site-entry.decide.roundtrip", stopwatch.Elapsed); // R14 client-side round-trip
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException((int)response.StatusCode, $"Entry decision failed ({(int)response.StatusCode}).");
        }

        return await response.Content.ReadFromJsonAsync<EntryDecisionResultDto>(cancellationToken);
    }
}
