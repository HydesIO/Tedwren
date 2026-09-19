using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Assets;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IAssetService"/> backed by the Tedwren Web API (<c>/api/assets</c>). The caller's company is
/// resolved server-side from the request claims (R15), so the <c>companyId</c> argument is not sent.
/// </summary>
public sealed class ApiAssetService : IAssetService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiAssetService(HttpClient http) => _http = http;

    /// <summary>Gets the caller company's assets from the API, newest first.</summary>
    public async Task<IReadOnlyList<AssetDto>> ListForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<AssetDto>>("api/assets", cancellationToken) ?? Array.Empty<AssetDto>();

    /// <summary>Adds an asset via the API and returns its new identifier.</summary>
    public async Task<Guid> CreateAsync(Guid companyId, CreateAssetRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/assets", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken);
        return created?.Id ?? Guid.Empty;
    }

    /// <summary>Updates an asset via the API; returns true on success.</summary>
    public async Task<bool> UpdateAsync(Guid companyId, Guid assetId, UpdateAssetRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/assets/{assetId}", request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Retires an asset via the API; returns true on success.</summary>
    public async Task<bool> RetireAsync(Guid companyId, Guid assetId, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync($"api/assets/{assetId}/retire", content: null, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>The <c>{ id }</c> shape returned by the create endpoint.</summary>
    private sealed record CreatedResponse(Guid Id);
}
