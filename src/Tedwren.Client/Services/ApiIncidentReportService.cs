using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Safety;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IIncidentReportService"/> backed by the Tedwren Web API (<c>/api/safety/incidents</c>). The caller's
/// company and identity are resolved server-side from the request claims (R15), so the <c>companyId</c>/
/// <c>reportedBy</c> arguments are not sent.
/// </summary>
public sealed class ApiIncidentReportService : IIncidentReportService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiIncidentReportService(HttpClient http) => _http = http;

    /// <summary>Reports an accident/incident via the API and returns it with its reference.</summary>
    public async Task<IncidentReportDto> ReportAsync(Guid companyId, string reportedBy, ReportIncidentRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsJsonAsync("api/safety/incidents", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IncidentReportDto>(cancellationToken))!;
    }

    /// <summary>Gets the caller company's incident records, newest first.</summary>
    public async Task<IReadOnlyList<IncidentReportDto>> ListAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<IncidentReportDto>>("api/safety/incidents", cancellationToken) ?? Array.Empty<IncidentReportDto>();

    /// <summary>Gets a single incident record, or null when missing/cross-tenant.</summary>
    public async Task<IncidentReportDto?> GetAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.GetAsync($"api/safety/incidents/{id}", cancellationToken);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<IncidentReportDto>(cancellationToken) : null;
    }

    /// <summary>Records/updates the investigation via the API; returns true on success.</summary>
    public async Task<bool> UpdateInvestigationAsync(Guid companyId, Guid id, UpdateIncidentInvestigationRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PutAsJsonAsync($"api/safety/incidents/{id}/investigation", request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    /// <summary>Closes an incident via the API; returns true on success.</summary>
    public async Task<bool> CloseAsync(Guid companyId, Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await _http.PostAsync($"api/safety/incidents/{id}/close", content: null, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
