using System.Net.Http.Json;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IReferenceDataService"/> backed by the Tedwren Web API (<c>/api/reference</c>). Serves the
/// fixed form option lists (trades, company types, permit types, regions) from the database.
/// </summary>
public sealed class ApiReferenceDataService : IReferenceDataService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiReferenceDataService(HttpClient http) => _http = http;

    /// <summary>Gets the ordered values for a reference list, or an empty list.</summary>
    public async Task<IReadOnlyList<string>> GetListAsync(string listKey, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<string>>($"api/reference/{listKey}", cancellationToken)
        ?? Array.Empty<string>();
}
