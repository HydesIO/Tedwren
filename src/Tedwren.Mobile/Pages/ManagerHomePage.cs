using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The manager/admin home: an overview dashboard (KPI cards) above the interactive card menu. The dashboard is
/// populated from the console's existing IDashboardService aggregation in M7; this M1 build establishes the
/// structure and the manager card menu.
/// </summary>
public class ManagerHomePage : ContentPage
{
    /// <summary>Builds the manager overview dashboard and card menu.</summary>
    public ManagerHomePage()
    {
        Title = "Overview";

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new Label { Text = "Site overview", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    BuildKpis(),
                    new Label { Text = "Menu", FontSize = 18, FontAttributes = FontAttributes.Bold },
                    BuildMenu(),
                },
            },
        };
    }

    /// <summary>Placeholder KPI cards; M7 replaces the values with live IDashboardService data.</summary>
    private static View BuildKpis()
    {
        var kpis = new (string Value, string Label)[]
        {
            ("—", "On site now"),
            ("—", "Active sites"),
            ("—", "Compliant"),
            ("—", "Expiring 30d"),
        };

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

        for (var i = 0; i < kpis.Length; i++)
        {
            var kpi = kpis[i];
            var card = new TwCard
            {
                Content = new VerticalStackLayout
                {
                    Spacing = 2,
                    Children =
                    {
                        new Label { Text = kpi.Value, FontSize = 26, FontAttributes = FontAttributes.Bold, TextColor = TwPalette.Brand },
                        new Label { Text = kpi.Label, FontSize = 13, TextColor = TwPalette.TextSecondaryLight },
                    },
                },
            };

            var row = i / 2;
            var col = i % 2;
            if (grid.RowDefinitions.Count <= row)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }

            grid.Add(card, col, row);
        }

        return grid;
    }

    /// <summary>Builds the manager card menu.</summary>
    private View BuildMenu()
    {
        var items = new (string Glyph, string Title, string Subtitle)[]
        {
            ("👷", "Who's on site", "Live muster"),
            ("🚦", "Site entry", "Decisions & overrides"),
            ("📋", "Forms", "Assign & review"),
            ("🧑‍🔧", "Operatives", "Register & compliance"),
            ("🏗", "Sites", "Boundaries & properties"),
            ("📷", "Evidence", "Review submissions"),
            ("📊", "Reports", "Exports & packs"),
        };
        return TileGrid.Build(this, items);
    }
}
