using Microsoft.Extensions.DependencyInjection;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The operative home (M3): an overview dashboard (today's site + sign-in state, hours this week, compliance +
/// next expiry) loaded from <see cref="OperativeDataService"/> (cache-then-network), above the interactive card
/// menu. Sign-in/evidence/forms tiles are placeholders until M4/M5/M6.
/// </summary>
public class OperativeHomePage : ContentPage
{
    private readonly OperativeDataService _data;
    private readonly IServiceProvider _services;

    private readonly Label _statusLine = new() { FontAttributes = FontAttributes.Bold, Text = "Loading…" };
    private readonly Label _complianceLine = new() { FontSize = 13 };
    private readonly Label _metaLine = new() { Text = "Hours this week: —    ·    Forms due: —" };
    private bool _loaded;

    /// <summary>Builds the operative overview dashboard and card menu.</summary>
    public OperativeHomePage(OperativeDataService data, IServiceProvider services)
    {
        _data = data;
        _services = services;
        Title = "My work";
        _complianceLine.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);

        var dashboard = new TwCard
        {
            Content = new VerticalStackLayout { Spacing = 8, Children = { _statusLine, _complianceLine, _metaLine } },
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new Label { Text = "Today", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    dashboard,
                    new Label { Text = "Menu", FontSize = 18, FontAttributes = FontAttributes.Bold },
                    BuildMenu(),
                },
            },
        };
    }

    /// <summary>Loads the dashboard once when the page first appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var dashboard = await _data.GetDashboardAsync();
            if (dashboard is null)
            {
                _statusLine.Text = "Couldn't load your dashboard";
                return;
            }

            _statusLine.Text = dashboard is { SignedIn: true, CurrentSiteName: { } site } ? $"On site: {site}" : "Not signed in";
            _complianceLine.Text = dashboard.ComplianceLabel + (dashboard.NextExpiry is { } expiry ? $"  ·  next expiry {expiry:dd MMM yyyy}" : string.Empty);
            _metaLine.Text = $"Hours this week: {dashboard.HoursThisWeek:0.##}    ·    Forms due: {dashboard.FormsDue}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _statusLine.Text = "Couldn't load your dashboard";
        }
    }

    private View BuildMenu()
    {
        var items = new (string Glyph, string Title, string Subtitle, Func<Page>? Destination)[]
        {
            ("🕒", "Sign in / out", "Record arrival & departure", null),
            ("📋", "Forms due", "Checklists & inspections", null),
            ("📷", "Capture evidence", "Photos with location", null),
            ("⏱", "My hours", "This week's timesheet", () => _services.GetRequiredService<MyHoursPage>()),
            ("🪪", "My cards", "Compliance & expiries", () => _services.GetRequiredService<MyCardsPage>()),
            ("📄", "Site documents", "Rules, plans, welfare", null),
            ("👤", "Profile", "Your details", () => _services.GetRequiredService<ProfilePage>()),
        };
        return TileGrid.Build(this, items);
    }
}
