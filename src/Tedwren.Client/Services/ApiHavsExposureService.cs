using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Havs;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IHavsExposureService"/> backed by the Tedwren Web API (<c>/api/havs</c>). The caller's company and
/// identity are resolved server-side from the request claims (R15), so the <c>companyId</c>/<c>recordedBy</c>
/// arguments are not sent.
/// </summary>
public sealed class ApiHavsExposureService : IHavsExposureService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiHavsExposureService(HttpClient http) => _http = http;

    /// <summary>Records a HAVs exposure via the API and returns it with its derived A(8)/points/band.</summary>
    public async Task<HavsExposureRecordDto> RecordAsync(Guid companyId, string recordedBy, CreateHavsExposureRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/havs", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HavsExposureRecordDto>(cancellationToken))!;
    }

    /// <summary>Gets the caller company's HAVs exposure records, newest first.</summary>
    public async Task<IReadOnlyList<HavsExposureRecordDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<HavsExposureRecordDto>>("api/havs", cancellationToken) ?? Array.Empty<HavsExposureRecordDto>();

    /// <summary>Gets a single HAVs exposure record, or null when missing/cross-tenant.</summary>
    public async Task<HavsExposureRecordDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/havs/{id}", cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<HavsExposureRecordDto>(cancellationToken) : null;
    }
}
