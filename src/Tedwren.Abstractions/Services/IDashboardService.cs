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

    /// <summary>Returns just the workforce compliance breakdown (for the Compliance page).</summary>
    Task<ComplianceBreakdownDto> GetComplianceAsync(CancellationToken cancellationToken = default);
}
