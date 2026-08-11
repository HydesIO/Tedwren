using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Dashboard;
using Tedwren.Abstractions.Services;

namespace Tedwren.Client.Services;

/// <summary>
/// <see cref="IDashboardService"/> backed by the Tedwren Web API (<c>/api/dashboard</c>). Serves the
/// aggregated dashboard summary and the compliance breakdown.
/// </summary>
public sealed class ApiDashboardService : IDashboardService
{
    private readonly HttpClient _http;

    /// <summary>Creates the service over the configured API <see cref="HttpClient"/>.</summary>
    public ApiDashboardService(HttpClient http) => _http = http;

    /// <summary>Gets the full dashboard summary from the API.</summary>
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<DashboardSummaryDto>("api/dashboard", cancellationToken)
        ?? new DashboardSummaryDto(
            new DashboardKpisDto(0, 0, 0, null, 0),
            new ComplianceBreakdownDto(null, 0, 0, 0, 0, 0),
            Array.Empty<SiteRiskRowDto>());

    /// <summary>Gets just the workforce compliance breakdown from the API.</summary>
    public async Task<ComplianceBreakdownDto> GetComplianceAsync(CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<ComplianceBreakdownDto>("api/dashboard/compliance", cancellationToken)
        ?? new ComplianceBreakdownDto(null, 0, 0, 0, 0, 0);
}
