using Tedwren.Abstractions.Contracts.Dashboard;
using Tedwren.UiComponents.Models;

namespace Tedwren.Client.Services;

/// <summary>Donut + legend + headline view model for the compliance overview card (client presentation shape).</summary>
public sealed record ComplianceOverviewVm(
    string HeadlinePercent,
    string HeadlineLabel,
    IReadOnlyList<DonutSegment> Segments,
    IReadOnlyList<LegendItem> Legend);

/// <summary>
/// Maps the API <see cref="ComplianceBreakdownDto"/> onto the <see cref="ComplianceOverviewVm"/> (donut
/// segments + legend) that the Dashboard and Compliance pages render. Colours use the theme tokens so there
/// are no colour literals in the pages.
/// </summary>
public static class ComplianceOverviewView
{
    private const string SuccessColour = "var(--color-success)";
    private const string WarningColour = "var(--color-warning)";
    private const string DangerColour = "var(--color-danger)";
    private const string NeutralColour = "var(--color-text-muted)";

    /// <summary>Builds the donut + legend view model from a compliance breakdown.</summary>
    public static ComplianceOverviewVm From(ComplianceBreakdownDto b)
    {
        double Pct(int n) => b.Total == 0 ? 0 : Math.Round(100.0 * n / b.Total, 1);

        var segments = new List<DonutSegment>
        {
            new("Compliant", b.Compliant, SuccessColour),
            new("At risk", b.AtRisk, WarningColour),
            new("Non-compliant", b.NonCompliant, DangerColour),
            new("Pending", b.Pending, NeutralColour),
        };

        var legend = new List<LegendItem>
        {
            new("Compliant", b.Compliant, SuccessColour, Pct(b.Compliant)),
            new("At risk", b.AtRisk, WarningColour, Pct(b.AtRisk)),
            new("Non-compliant", b.NonCompliant, DangerColour, Pct(b.NonCompliant)),
            new("Pending", b.Pending, NeutralColour, Pct(b.Pending)),
        };

        return new ComplianceOverviewVm(
            HeadlinePercent: b.CompliancePercent is null ? "—" : $"{b.CompliancePercent.Value:0}%",
            HeadlineLabel: "Compliant",
            Segments: segments,
            Legend: legend);
    }
}
