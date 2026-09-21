namespace Tedwren.Web.App.Components;

/// <summary>
/// The semantic colour of a status pill / RAG state — the web mirror of the native
/// <c>Tedwren.Mobile.Controls.Controls.TwStatusKind</c>, so the emulator renders compliance/risk/form states in
/// the same palette as the app.
/// </summary>
public enum TwStatusKind
{
    /// <summary>Subtle neutral (unknown / informational).</summary>
    Neutral,

    /// <summary>Green (compliant / approved / covered).</summary>
    Success,

    /// <summary>Amber (at risk / awaiting review / last holder).</summary>
    Warning,

    /// <summary>Red (non-compliant / rejected / not covered).</summary>
    Danger,
}
