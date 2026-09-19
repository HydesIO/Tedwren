using Tedwren.Mobile.Controls.Controls;

namespace Tedwren.Mobile.Pages;

/// <summary>Lays reusable <see cref="TwMenuTile"/> tiles out in a responsive two-column grid for the role card menus.</summary>
internal static class TileGrid
{
    /// <summary>
    /// Builds a two-column grid of menu tiles. Each tile shows a "coming in a later phase" prompt for now; the
    /// commands are re-pointed at real destinations as each phase (M3–M7) lands.
    /// </summary>
    public static Grid Build(Page page, IReadOnlyList<(string Glyph, string Title, string Subtitle)> items)
    {
        var grid = new Grid
        {
            ColumnSpacing = 12,
            RowSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            },
        };

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var tile = new TwMenuTile
            {
                Glyph = item.Glyph,
                Title = item.Title,
                Subtitle = item.Subtitle,
                Command = new Command(() => page.DisplayAlert(item.Title, "Coming in a later phase.", "OK")),
            };

            Place(grid, tile, i);
        }

        return grid;
    }

    /// <summary>
    /// Builds a two-column grid of menu tiles where each item may carry a destination-page factory: tiles with a
    /// destination navigate to it; tiles without one show the "coming in a later phase" prompt.
    /// </summary>
    public static Grid Build(Page page, IReadOnlyList<(string Glyph, string Title, string Subtitle, Func<Page>? Destination)> items)
    {
        var grid = new Grid
        {
            ColumnSpacing = 12,
            RowSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            },
        };

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

            Place(grid, tile, i);
        }

        return grid;
    }

    private static void Place(Grid grid, IView tile, int index)
    {
        var row = index / 2;
        var col = index % 2;
        if (grid.RowDefinitions.Count <= row)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        grid.Add(tile, col, row);
    }
}
