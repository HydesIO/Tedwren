using MudBlazor;

namespace Tedwren.UiComponents.Dialogs;

/// <summary>
/// Standard <see cref="DialogOptions"/> presets for every MudBlazor dialog in the app, so a dialog's
/// width is chosen deliberately (Small / Medium / Large) rather than being dictated by its content.
/// See the "MudDialog design standard" in CLAUDE.md. All presets are full-width (fill the chosen max
/// width, responsive down to tablet/mobile) with a header close button; the backdrop is not click-to-close
/// so in-progress edits are not lost by a stray click.
/// </summary>
public static class TedwrenDialog
{
    /// <summary>Small dialog: confirmations, warnings, simple choices and short messages.</summary>
    public static DialogOptions Small() => Build(MaxWidth.Small);

    /// <summary>Medium dialog: normal forms, editing screens and moderate interaction.</summary>
    public static DialogOptions Medium() => Build(MaxWidth.Medium);

    /// <summary>Large dialog: complex/multi-section forms, detailed viewers and workflows.</summary>
    public static DialogOptions Large() => Build(MaxWidth.Large);

    /// <summary>Builds the shared option profile for the given standard width.</summary>
    private static DialogOptions Build(MaxWidth maxWidth) => new()
    {
        MaxWidth = maxWidth,
        FullWidth = true,
        CloseButton = true,
        BackdropClick = false,
    };
}
