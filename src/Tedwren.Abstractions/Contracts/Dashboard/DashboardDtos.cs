using Tedwren.Abstractions.Common;

namespace Tedwren.Abstractions.Contracts.Dashboard;

/// <summary>Headline counts for the dashboard KPI row, aggregated across the organisation.</summary>
public sealed record DashboardKpisDto(
    int Companies,
    int Operatives,
    int Sites,
    double? CompliancePercent,
    int UpcomingExpiries);

/// <summary>
/// The workforce compliance breakdown (SF-8): how many operatives fall in each compliance state, with the
/// headline compliant percentage. Derived from current cards via the shared roll-up, never invented.
/// </summary>
public sealed record ComplianceBreakdownDto(
    double? CompliancePercent,
    int Compliant,
    int AtRisk,
    int NonCompliant,
    int Pending,
    int Total);

/// <summary>One site row of the dashboard risk heatmap, from the site summary (SF-6/SF-14).</summary>
public sealed record SiteRiskRowDto(
    string Site,
    string Slug,
    int Operatives,
    double? CompliancePercent,
    ComplianceState State,
    string StatusLabel,
    RiskState Risk);

/// <summary>The aggregated dashboard: KPI counts, the compliance breakdown and the site-risk heatmap.</summary>
public sealed record DashboardSummaryDto(
    DashboardKpisDto Kpis,
    ComplianceBreakdownDto Compliance,
    IReadOnlyList<SiteRiskRowDto> SiteRisk);
