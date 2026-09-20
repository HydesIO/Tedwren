using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Audit;
using Tedwren.Abstractions.Contracts.Dashboard;
using Tedwren.Abstractions.Contracts.Expiry;

namespace Tedwren.Mobile.Core.Api;

/// <summary>
/// Typed client for the manager overview surface (M7): the console dashboard aggregation, upcoming expiries and
/// recent audit activity, over the console-token auth handler. Read-only; a non-success status throws.
/// </summary>
public sealed class ManagerApiClient
{
    private readonly HttpClient _http;

    /// <summary>Creates the client over a configured <see cref="HttpClient"/> whose BaseAddress is the API root.</summary>
    public ManagerApiClient(HttpClient http) => _http = http;

    /// <summary>The organisation dashboard summary (KPIs, compliance breakdown and site-risk heatmap).</summary>
    public Task<DashboardSummaryDto?> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        _http.GetFromJsonAsync<DashboardSummaryDto>("api/dashboard", cancellationToken);

    /// <summary>Qualification cards expiring within the given window (default 30 days).</summary>
    public async Task<IReadOnlyList<UpcomingExpiryDto>> GetUpcomingExpiriesAsync(int withinDays = 30, CancellationToken cancellationToken = default) =>
        await _http.GetFromJsonAsync<IReadOnlyList<UpcomingExpiryDto>>($"api/expiry/upcoming?withinDays={withinDays}", cancellationToken)
            ?? Array.Empty<UpcomingExpiryDto>();

    /// <summary>Recent audit activity across the tenant, over the last <paramref name="days"/> days.</summary>
    public async Task<IReadOnlyList<AuditEntryDto>> GetRecentActivityAsync(int days = 7, CancellationToken cancellationToken = default)
    {
        var from = DateTimeOffset.UtcNow.AddDays(-days).ToString("o");
        return await _http.GetFromJsonAsync<IReadOnlyList<AuditEntryDto>>($"api/audit?from={Uri.EscapeDataString(from)}", cancellationToken)
            ?? Array.Empty<AuditEntryDto>();
    }
}
