using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The operative home: an overview dashboard (today's site, hours this week, forms due) above the interactive
/// card menu. Live data is wired from <c>GET /api/mobile/dashboard</c> and the offline cache in M3; this M1
/// build establishes the structure and the card menu.
/// </summary>
public class OperativeHomePage : ContentPage
{
    /// <summary>Builds the operative overview dashboard and card menu.</summary>
    public OperativeHomePage()
    {
        Title = "My work";

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new Label { Text = "Today", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    BuildDashboard(),
                    new Label { Text = "Menu", FontSize = 18, FontAttributes = FontAttributes.Bold },
                    BuildMenu(),
                },
            },
        };
    }

    /// <summary>Placeholder overview card; M3 replaces the values with live dashboard data.</summary>
    private static View BuildDashboard()
    {
        var card = new TwCard
        {
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    new Label { Text = "Not signed in", FontAttributes = FontAttributes.Bold },
                    new Label { Text = "No site selected", FontSize = 13, TextColor = TwPalette.TextSecondaryLight },
                    new Label { Text = "Hours this week: —    ·    Forms due: —" },
                },
            },
        };
        return card;
    }

    /// <summary>Builds the operative card menu.</summary>
    private View BuildMenu()
    {
        var items = new (string Glyph, string Title, string Subtitle)[]
        {
            ("🕒", "Sign in / out", "Record arrival & departure"),
            ("📋", "Forms due", "Checklists & inspections"),
            ("📷", "Capture evidence", "Photos with location"),
            ("⏱", "My hours", "This week's timesheet"),
            ("🪪", "My cards", "Compliance & expiries"),
            ("📄", "Site documents", "Rules, plans, welfare"),
            ("👤", "Profile", "Your details"),
        };
        return TileGrid.Build(this, items);
    }
}
