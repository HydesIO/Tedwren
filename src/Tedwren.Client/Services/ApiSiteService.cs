using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="ISiteService"/> backed by the Tedwren Web API over HTTP — used when the client is configured
/// with <c>DataSource=Api</c>. The UI injects the same interface as in mock mode.
/// </summary>
public sealed class ApiSiteService : ISiteService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiSiteService(HttpClient http) => _http = http;

    /// <summary>Gets the site list from the API.</summary>
    public async Task<IReadOnlyList<SiteSummary>> GetSitesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<SiteSummary>>("api/sites", cancellationToken)
        ?? Array.Empty<SiteSummary>();

    /// <summary>Gets a site by slug from the API, or null on 404.</summary>
    public async Task<SiteDetailDto?> GetSiteAsync(string slug, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/sites/{slug}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SiteDetailDto>(cancellationToken);
    }

    /// <summary>Records a site via the API and returns its new id.</summary>
    public async Task<Guid> CreateSiteAsync(CreateSiteRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/sites", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken);
        return created?.Id ?? Guid.Empty;
    }

    /// <summary>Updates a site's details via the API. Returns false when the site is not found (404).</summary>
    public async Task<bool> UpdateSiteAsync(Guid siteId, UpdateSiteRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/sites/{siteId}", request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Adds a property to a scheme via the API, or null when the site is not found.</summary>
    public async Task<Guid?> AddPropertyAsync(AddSitePropertyRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync(
            $"api/sites/{request.SiteId}/properties",
            new { request.Address, request.Units, request.Boundary }, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken);
        return created?.Id;
    }

    /// <summary>Shape of the create response body.</summary>
    private sealed record CreatedResponse(Guid Id);
}
