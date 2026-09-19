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
    public async Task<EntryDecisionResultDto> DecideAsync(DecideEntryRequest request, CancellationToken cancellationToken = default)
    {
        // Fail safe on the entry-gate path: check the status before reading, so an error response is never
        // deserialized into a default (all-false) decision that could read as a spurious allow/deny (MC-8).
        using var response = await _http.PostAsJsonAsync("api/site-entry/decide", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<EntryDecisionResultDto>(cancellationToken)
            ?? throw new InvalidOperationException("The site-entry decision could not be read from the server.");
    }

    /// <summary>Gets the live muster for a site via the API (MC-12–MC-14).</summary>
    public async Task<MusterDto> GetMusterAsync(Guid siteId, CancellationToken cancellationToken = default)
    {
        var muster = await _http.GetFromJsonAsync<MusterDto>($"api/site-entry/muster/{siteId}", cancellationToken);
        if (muster is null)
        {
            throw new InvalidOperationException("The site muster could not be loaded.");
        }

        // Coalesce the nested collections so the muster table and competency-cover row cannot NRE on a null/absent
        // list from the API (the DTO declares them non-null but System.Text.Json does not enforce that) (F19).
        return muster with
        {
            People = muster.People ?? Array.Empty<MusterPersonDto>(),
            Competencies = muster.Competencies ?? Array.Empty<CompetencyCoverDto>(),
        };
    }
}
