using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Safety;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IHazardReportService"/> backed by the Tedwren Web API (<c>/api/safety/hazards</c>). The caller's
/// company and identity are resolved server-side from the request claims (R15), so the <c>companyId</c>/
/// <c>reportedBy</c> arguments are not sent.
/// </summary>
public sealed class ApiHazardReportService : IHazardReportService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiHazardReportService(HttpClient http) => _http = http;

    /// <summary>Reports a hazard via the API and returns it with its reference.</summary>
    public async Task<HazardReportDto> ReportAsync(Guid companyId, string reportedBy, ReportHazardRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/safety/hazards", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HazardReportDto>(cancellationToken))!;
    }

    /// <summary>Gets the caller company's hazard reports, newest first.</summary>
    public async Task<IReadOnlyList<HazardReportDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<HazardReportDto>>("api/safety/hazards", cancellationToken) ?? Array.Empty<HazardReportDto>();

    /// <summary>Gets a single hazard report, or null when missing/cross-tenant.</summary>
    public async Task<HazardReportDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/safety/hazards/{id}", cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<HazardReportDto>(cancellationToken) : null;
    }

    /// <summary>Assigns a hazard via the API; returns true on success.</summary>
    public async Task<bool> AssignAsync(Guid companyId, Guid id, string assignedTo, string? category, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/safety/hazards/{id}/assign", new AssignHazardRequest(assignedTo, category), cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Closes a hazard (with a note) via the API; returns true on success.</summary>
    public async Task<bool> CloseAsync(Guid companyId, Guid id, string note, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync($"api/safety/hazards/{id}/close", new CloseHazardRequest(note), cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Gets the leading-indicator counts for the caller company's hazard register.</summary>
    public async Task<HazardStatsDto> GetStatsAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<HazardStatsDto>("api/safety/hazards/stats", cancellationToken) ?? new HazardStatsDto(0, 0, 0, 0, 0, 0, 0, 0, 0);
}
