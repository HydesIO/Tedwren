using Tedwren.Abstractions.Common;
using Tedwren.Web.App.Components;

namespace Tedwren.Web.App.Shared;

/// <summary>
/// Shared mappings + helpers for the manager surfaces — the web mirror of the native <c>ManagerUi</c>: status →
/// pill colour, a compact data-age label (MC-14), and a byte-array → data-URL helper for images served through the
/// authorised image route (R9). Keeps the manager screens consistent with the app.
/// </summary>
public static class WebManagerUi
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

    /// <summary>A compact "n unit(s) ago" label for a data-age span (MC-14).</summary>
    public static string FormatAge(TimeSpan age) => age switch
    {
        { TotalSeconds: < 60 } => $"{Math.Max(0, (int)age.TotalSeconds)} s",
        { TotalMinutes: < 60 } => $"{(int)age.TotalMinutes} min",
        { TotalHours: < 24 } => $"{(int)age.TotalHours} h",
        _ => $"{(int)age.TotalDays} d",
    };

    /// <summary>
    /// Wraps image bytes (from the authorised image route) as a data URL for an <c>&lt;img&gt;</c> src, sniffing the
    /// format from the magic bytes so PNG/GIF render correctly (default JPEG). Returns null for empty bytes.
    /// </summary>
    public static string? DataUrl(byte[]? bytes)
    {
        if (bytes is not { Length: > 3 })
        {
            return null;
        }

        var mime = bytes[0] == 0x89 && bytes[1] == 0x50 ? "image/png"
            : bytes[0] == 0x47 && bytes[1] == 0x49 ? "image/gif"
            : "image/jpeg";
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }
}
