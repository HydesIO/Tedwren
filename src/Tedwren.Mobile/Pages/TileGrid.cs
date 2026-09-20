using Tedwren.Mobile.Controls.Controls;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// Lays reusable <see cref="TwMenuTile"/> tiles out in a responsive grid for the role card menus — two columns on a
/// phone, three on a tablet (M8), so the wider screen is used rather than stretching two columns across it.
/// </summary>
internal static class TileGrid
{
    /// <summary>Columns for the current idiom: three on a tablet, two on a phone (M8 tablet layout).</summary>
    private static int Columns => DeviceInfo.Idiom == DeviceIdiom.Tablet ? 3 : 2;

    /// <summary>
    /// Builds the menu grid. Each tile shows a "coming in a later phase" prompt for now; the commands are
    /// re-pointed at real destinations as each phase (M3–M7) lands.
    /// </summary>
    public static Grid Build(Page page, IReadOnlyList<(string Glyph, string Title, string Subtitle)> items) =>
        Build(page, items.Select(i => (i.Glyph, i.Title, i.Subtitle, (Func<Page>?)null)).ToList());

    /// <summary>
    /// Builds the menu grid where each item may carry a destination-page factory: tiles with a destination navigate
    /// to it; tiles without one show the "coming in a later phase" prompt.
    /// </summary>
    public static Grid Build(Page page, IReadOnlyList<(string Glyph, string Title, string Subtitle, Func<Page>? Destination)> items)
    {
        var columns = Columns;
        var grid = new Grid { ColumnSpacing = 12, RowSpacing = 12 };
        for (var c = 0; c < columns; c++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var tile = new TwMenuTile
            {
                Glyph = item.Glyph,
                Title = item.Title,
                Subtitle = item.Subtitle,
                Command = item.Destination is null
                    ? new Command(() => page.DisplayAlert(item.Title, "Coming in a later phase.", "OK"))
                    : new Command(async () => await page.Navigation.PushAsync(item.Destination())),
            };

            Place(grid, tile, i, columns);
        }

        return grid;
    }

    private static void Place(Grid grid, IView tile, int index, int columns)
    {
        var row = index / columns;
        var col = index % columns;
        if (grid.RowDefinitions.Count <= row)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        grid.Add(tile, col, row);
    }
}
