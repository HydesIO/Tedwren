using MudBlazor;
using Tedwren.UiComponents.Models;

namespace Tedwren.UiComponents.SampleData;

/// <summary>
/// In-memory dashboard data. Numbers are illustrative only (Plan &amp; Scope §12 Q4) and
/// mirror the approved dashboard rather than being cross-page consistent.
/// </summary>
public sealed class DashboardSampleDataService : IDashboardSampleDataService
{
    public IReadOnlyList<KpiTile> GetKpis() => new List<KpiTile>
    {
        new("Active operatives", "1,284", Icons.Material.Outlined.Engineering,
            "+4.2%", TrendDirection.Up, new double[] { 20, 22, 21, 25, 24, 28, 30 }, "var(--color-brand)"),
        new("Compliant workforce", "92%", Icons.Material.Outlined.VerifiedUser,
            "+1.8%", TrendDirection.Up, new double[] { 84, 85, 86, 88, 89, 91, 92 }, "var(--color-success)"),
        new("Expiring in 30 days", "37", Icons.Material.Outlined.Schedule,
            "-6", TrendDirection.Down, new double[] { 52, 49, 47, 44, 43, 40, 37 }, "var(--color-warning)"),
        new("Active sites", "18", Icons.Material.Outlined.Apartment,
            "+2", TrendDirection.Up, new double[] { 14, 15, 15, 16, 16, 17, 18 }, "var(--color-info)"),
        new("Open permits", "26", Icons.Material.Outlined.Assignment,
            "+3", TrendDirection.Up, new double[] { 18, 20, 19, 22, 23, 24, 26 }, "var(--color-permit)"),
    };

    public ComplianceOverview GetComplianceOverview() => new(
        HeadlinePercent: "92%",
        HeadlineLabel: "Compliant",
        Segments: new List<DonutSegment>
        {
            new("Compliant", 1181, "var(--color-success)"),
            new("Expiring", 66, "var(--color-warning)"),
            new("Non-compliant", 37, "var(--color-danger)"),
        },
        Legend: new List<LegendItem>
        {
            new("Compliant", 1181, "var(--color-success)", 92.0),
            new("Expiring soon", 66, "var(--color-warning)", 5.1),
            new("Non-compliant", 37, "var(--color-danger)", 2.9),
        });

    public IReadOnlyList<ExpiryItem> GetUpcomingExpiries() => new List<ExpiryItem>
    {
        new("CSCS Card — J. Fletcher", "Meridian Tower", new DateOnly(2026, 8, 9), 5, StatusKind.Danger),
        new("First Aid — S. Okoro", "Kingsway Retail", new DateOnly(2026, 8, 16), 12, StatusKind.Warning),
        new("Asbestos Awareness — L. Reid", "Northgate Depot", new DateOnly(2026, 8, 21), 17, StatusKind.Warning),
        new("SSSTS — D. Marsh", "Meridian Tower", new DateOnly(2026, 8, 28), 24, StatusKind.Info),
        new("Working at Height — P. Nowak", "Harbour Point", new DateOnly(2026, 9, 3), 30, StatusKind.Info),
    };

    public IReadOnlyList<ActivityItem> GetRecentActivity() => new List<ActivityItem>
    {
        new(Icons.Material.Outlined.PersonAddAlt, "New operative onboarded", "M. Adeyemi joined via self-service link", "12m ago", StatusKind.Success),
        new(Icons.Material.Outlined.UploadFile, "Document uploaded", "RAMS submitted for Harbour Point", "48m ago", StatusKind.Info),
        new(Icons.Material.Outlined.WarningAmber, "Qualification expiring", "CSCS card for J. Fletcher expires in 5 days", "2h ago", StatusKind.Warning),
        new(Icons.Material.Outlined.Assignment, "Permit issued", "Hot works permit #HW-2043 at Meridian Tower", "3h ago", StatusKind.Permit),
        new(Icons.Material.Outlined.Block, "Site entry blocked", "Non-compliant operative at Kingsway Retail", "5h ago", StatusKind.Danger),
    };

    public IReadOnlyList<SiteRisk> GetSiteRiskHeatmap() => new List<SiteRisk>
    {
        new("Meridian Tower", 214, 201, 9, 4, RiskSeverity.Medium),
        new("Kingsway Retail", 176, 158, 12, 6, RiskSeverity.High),
        new("Northgate Depot", 98, 95, 2, 1, RiskSeverity.Low),
        new("Harbour Point", 143, 137, 5, 1, RiskSeverity.Low),
        new("Riverside Phase 2", 121, 110, 8, 3, RiskSeverity.Medium),
    };
}
