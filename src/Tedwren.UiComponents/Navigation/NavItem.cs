namespace Tedwren.UiComponents.Navigation;

/// <summary>
/// A single sidebar navigation entry. Passed in from the host app so the
/// component library never hard-codes the product's route list.
/// </summary>
/// <param name="Label">Visible nav label.</param>
/// <param name="Icon">MudBlazor icon string (outline style).</param>
/// <param name="Href">Route the item navigates to.</param>
/// <param name="Children">Optional nested items for expandable groups.</param>
public sealed record NavItem(
    string Label,
    string Icon,
    string Href,
    IReadOnlyList<NavItem>? Children = null)
{
    public bool HasChildren => Children is { Count: > 0 };

    /// <summary>
    /// The navigable leaves this item contributes: its children when it is a group header (which is not
    /// itself navigable), otherwise the item itself. Lets callers walk a nested nav list as a flat set of
    /// destinations for title matching and the command palette.
    /// </summary>
    public IEnumerable<NavItem> Leaves() => HasChildren ? Children! : new[] { this };
}
