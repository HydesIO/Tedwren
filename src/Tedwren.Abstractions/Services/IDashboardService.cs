using Tedwren.Abstractions.Contracts.Dashboard;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Aggregates the dashboard read model from the organisation, qualification, site and expiry data: KPI
/// counts, the workforce compliance breakdown (SF-8) and the site-risk heatmap. Compliance is derived from
/// current cards, never invented.
/// </summary>
public interface IDashboardService
{
    /// <summary>Returns the full dashboard summary (KPIs, compliance breakdown, site risk).</summary>
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the workforce compliance breakdown for the Compliance page. With no <paramref name="siteSlug"/>
    /// it covers the caller's whole tenant; given one it is scoped to that site's operatives (UAT-016/MC-21) —
    /// resolved through the same role-aware site lookup, so a Site Manager only ever sees their assigned sites.
    /// </summary>
    Task<ComplianceBreakdownDto> GetComplianceAsync(string? siteSlug = null, CancellationToken cancellationToken = default);
}
