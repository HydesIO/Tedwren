namespace Tedwren.UiComponents.Models;

/// <summary>Direction of a KPI trend, driving colour and arrow.</summary>
public enum TrendDirection
{
    None,
    Up,
    Down,
}

/// <summary>Semantic status used by <c>StatusPill</c> and traffic-light states.</summary>
public enum StatusKind
{
    Neutral,
    Success,
    Warning,
    Danger,
    Info,
    Permit,
}

/// <summary>Severity used by <c>RiskChip</c> for the heatmap "at risk" column.</summary>
public enum RiskSeverity
{
    Low,
    Medium,
    High,
}

/// <summary>A single coloured-dot legend row with an optional value/percentage.</summary>
/// <param name="Label">Legend label.</param>
/// <param name="Value">Numeric value shown on the right.</param>
/// <param name="Colour">CSS colour (token var or literal from the palette).</param>
/// <param name="Percentage">Optional percentage of the whole.</param>
public sealed record LegendItem(string Label, double Value, string Colour, double? Percentage = null);

/// <summary>A single donut segment.</summary>
/// <param name="Label">Segment label (for the accessible name / legend).</param>
/// <param name="Value">Segment value; the donut normalises across all segments.</param>
/// <param name="Colour">CSS colour.</param>
public sealed record DonutSegment(string Label, double Value, string Colour);

/// <summary>A repeated "item / site / expiry / days remaining" row.</summary>
public sealed record ExpiryItem(
    string Title,
    string Site,
    DateOnly ExpiresOn,
    int DaysRemaining,
    StatusKind Status);

/// <summary>An "icon + primary/secondary text + relative time" activity row.</summary>
public sealed record ActivityItem(
    string Icon,
    string Primary,
    string Secondary,
    string RelativeTime,
    StatusKind Accent = StatusKind.Neutral);

/// <summary>A notification shown in the top-bar bell dropdown and the notifications page.</summary>
public sealed record NotificationEntry(
    string Icon,
    string Title,
    string Detail,
    string RelativeTime,
    StatusKind Accent = StatusKind.Neutral,
    bool Unread = false);
