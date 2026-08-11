using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Workforce;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IWorkforceService"/> backed by the Tedwren Web API (<c>/api/workforce</c>). Serves the
/// operative register and a single operative's profile.
/// </summary>
public sealed class ApiWorkforceService : IWorkforceService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiWorkforceService(HttpClient http) => _http = http;

    /// <summary>Gets every active operative from the API.</summary>
    public async Task<IReadOnlyList<OperativeListItemDto>> ListOperativesAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<OperativeListItemDto>>("api/workforce", cancellationToken)
        ?? Array.Empty<OperativeListItemDto>();

    /// <summary>Gets an operative's profile by slug, or null when the API returns 404.</summary>
    public async Task<OperativeDetailDto?> GetOperativeBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/workforce/{slug}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OperativeDetailDto>(cancellationToken);
    }
}
