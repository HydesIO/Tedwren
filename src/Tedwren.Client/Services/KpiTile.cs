using Tedwren.UiComponents.Models;

namespace Tedwren.Client.Services;

/// <summary>A dashboard KPI tile view model bound by the Dashboard page's <c>KpiCard</c>s.</summary>
public sealed record KpiTile(
    string Title,
    string Value,
    string Icon,
    string? TrendValue,
    TrendDirection TrendDirection,
    IReadOnlyList<double> Sparkline,
    string AccentColour);
