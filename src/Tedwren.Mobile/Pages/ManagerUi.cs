using Tedwren.Abstractions.Common;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Pages;

/// <summary>Small shared mappings + helpers for the manager surfaces (M7): status → pill colour, and muted text.</summary>
internal static class ManagerUi
{
    /// <summary>Maps a compliance state to a status-pill colour.</summary>
    public static TwStatusKind ForCompliance(ComplianceState state) => state switch
    {
        ComplianceState.Compliant => TwStatusKind.Success,
        ComplianceState.AtRisk => TwStatusKind.Warning,
        ComplianceState.NonCompliant => TwStatusKind.Danger,
        _ => TwStatusKind.Neutral,
    };

    /// <summary>Maps a form-submission status name to a status-pill colour.</summary>
    public static TwStatusKind ForFormStatus(string status) => status switch
    {
        "Approved" => TwStatusKind.Success,
        "Rejected" => TwStatusKind.Danger,
        "Submitted" => TwStatusKind.Warning,
        _ => TwStatusKind.Neutral,
    };

    /// <summary>Maps a RAG answer value to a status-pill colour (Red/Amber/Green).</summary>
    public static TwStatusKind ForRag(string value) => value switch
    {
        "Green" => TwStatusKind.Success,
        "Amber" => TwStatusKind.Warning,
        "Red" => TwStatusKind.Danger,
        _ => TwStatusKind.Neutral,
    };

    /// <summary>Wraps content in a surface card that runs <paramref name="onTapped"/> when tapped (a list row that drills in).</summary>
    public static TwCard TappableCard(View content, Func<Task> onTapped)
    {
        var card = new TwCard { Content = content };
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await onTapped();
        card.GestureRecognizers.Add(tap);
        return card;
    }

    /// <summary>A muted secondary-text label for hints and empty rows.</summary>
    public static Label Muted(string text)
    {
        var label = new Label { Text = text, FontSize = 13 };
        label.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
        return label;
    }

    /// <summary>A compact "n unit(s) ago" label for a data-age span (MC-14).</summary>
    public static string FormatAge(TimeSpan age) => age switch
    {
        { TotalSeconds: < 60 } => $"{Math.Max(0, (int)age.TotalSeconds)} s",
        { TotalMinutes: < 60 } => $"{(int)age.TotalMinutes} min",
        { TotalHours: < 24 } => $"{(int)age.TotalHours} h",
        _ => $"{(int)age.TotalDays} d",
    };
}
