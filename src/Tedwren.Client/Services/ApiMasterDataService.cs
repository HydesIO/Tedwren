using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.MasterData;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IMasterDataService"/> backed by the Tedwren Web API (<c>/api/master-data</c>). Serves and
/// maintains the compliance master lists (SSIP schemes, configurable document headings and per-operative
/// "other requirements"; spec §5–§8) the subcontractor-onboarding wizard draws on.
/// </summary>
public sealed class ApiMasterDataService : IMasterDataService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiMasterDataService(HttpClient http) => _http = http;

    /// <summary>Gets the active values for a list visible to the caller (global + own-org), in display order.</summary>
    public async Task<IReadOnlyList<MasterListItemDto>> GetListAsync(string listKey, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<MasterListItemDto>>($"api/master-data/{listKey}", cancellationToken)
        ?? Array.Empty<MasterListItemDto>();

    /// <summary>Gets the values for a list including soft-deleted rows, for management screens.</summary>
    public async Task<IReadOnlyList<MasterListItemDto>> GetForManagementAsync(string listKey, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<MasterListItemDto>>($"api/master-data/{listKey}/manage", cancellationToken)
        ?? Array.Empty<MasterListItemDto>();

    /// <summary>Adds a value (global for a platform admin, else an org-scoped custom entry) and returns its id.</summary>
    public async Task<Guid> CreateAsync(CreateMasterListItemRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/master-data", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedResponse>(cancellationToken);
        return created?.Id ?? Guid.Empty;
    }

    /// <summary>Updates a value (text/order/active).</summary>
    public async Task UpdateAsync(Guid id, UpdateMasterListItemRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/master-data/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Soft-deletes a value (clears IsActive).</summary>
    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.DeleteAsync($"api/master-data/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>The created-id envelope the API returns (<c>{ id }</c>).</summary>
    private sealed record CreatedResponse(Guid Id);
}
