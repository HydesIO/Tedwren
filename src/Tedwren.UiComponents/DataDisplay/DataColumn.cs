using Microsoft.AspNetCore.Components;

namespace Tedwren.UiComponents.DataDisplay;

/// <summary>
/// Declarative column definition for <see cref="DataTable{TItem}"/>. A column projects a
/// value for sorting / search / filtering and optionally supplies a custom cell template.
/// </summary>
/// <typeparam name="TItem">Row item type.</typeparam>
public sealed class DataColumn<TItem>
{
    /// <summary>Column header text.</summary>
    public required string Title { get; init; }

    /// <summary>
    /// Value projection used for sorting and, when <see cref="Text"/> is not set, as the
    /// default cell content and the string used for search / filtering.
    /// </summary>
    public Func<TItem, object?>? Value { get; init; }

    /// <summary>Optional string projection for search / filter / default cell rendering.</summary>
    public Func<TItem, string>? Text { get; init; }

    /// <summary>Optional custom cell template (takes precedence over <see cref="Text"/>/<see cref="Value"/>).</summary>
    public RenderFragment<TItem>? CellTemplate { get; init; }

    /// <summary>Whether the column is sortable (default true).</summary>
    public bool Sortable { get; init; } = true;

    /// <summary>Whether the column participates in the free-text search (default true).</summary>
    public bool Searchable { get; init; } = true;

    /// <summary>Whether the column exposes a distinct-value dropdown filter (default false).</summary>
    public bool Filterable { get; init; }

    /// <summary>Right-align the column (for numeric columns).</summary>
    public bool AlignRight { get; init; }

    /// <summary>Resolves the string representation used for search / filter / default cell.</summary>
    public string TextOf(TItem item)
        => Text?.Invoke(item) ?? Value?.Invoke(item)?.ToString() ?? string.Empty;
}
