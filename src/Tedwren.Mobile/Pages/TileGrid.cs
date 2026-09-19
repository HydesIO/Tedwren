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

            var row = i / 2;
            var col = i % 2;
            if (grid.RowDefinitions.Count <= row)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            grid.Add(tile, col, row);
        }

        return grid;
    }
}
