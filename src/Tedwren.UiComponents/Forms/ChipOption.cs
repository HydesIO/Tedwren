namespace Tedwren.UiComponents.Forms;

/// <summary>
/// A single editable choice in a <see cref="ChipInput"/>: a stable <see cref="Id"/> the author never types
/// (assigned on creation) and the human-readable <see cref="Text"/>. Mutable so the list can be edited in place
/// and bound two-way.
/// </summary>
public sealed class ChipOption
{
    /// <summary>Stable identifier for this option, assigned when the chip is created and preserved on edit.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The option label shown to the person filling the form / quiz.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Creates a chip with a freshly-assigned short id for the given text.</summary>
    public static ChipOption Create(string text) => new() { Id = NewId(), Text = text.Trim() };

    /// <summary>Generates a short, stable option id.</summary>
    public static string NewId() => Guid.NewGuid().ToString("N")[..8];
}
