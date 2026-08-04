using Tedwren.UiComponents.Models;

namespace Tedwren.UiComponents.SampleData;

/// <summary>
/// Supplies the presentation-layer data the dashboard renders. Interface-first so a real
/// API-backed implementation can replace it later without touching any component.
/// </summary>
public interface IDashboardSampleDataService
{
    IReadOnlyList<KpiTile> GetKpis();
    ComplianceOverview GetComplianceOverview();
    IReadOnlyList<ExpiryItem> GetUpcomingExpiries();
    IReadOnlyList<ActivityItem> GetRecentActivity();
    IReadOnlyList<SiteRisk> GetSiteRiskHeatmap();
}

/// <summary>A KPI tile view model.</summary>
public sealed record KpiTile(
    string Title,
    string Value,
    string Icon,
    string? TrendValue,
    TrendDirection TrendDirection,
    IReadOnlyList<double> Sparkline,
    string AccentColour);

/// <summary>Compliance donut + legend + headline percentage.</summary>
public sealed record ComplianceOverview(
    string HeadlinePercent,
    string HeadlineLabel,
    IReadOnlyList<DonutSegment> Segments,
    IReadOnlyList<LegendItem> Legend);

/// <summary>One row of the site risk heatmap.</summary>
public sealed record SiteRisk(
    string Site,
    int Operatives,
    int Compliant,
    int Expiring,
    int AtRisk,
    RiskSeverity Severity);
