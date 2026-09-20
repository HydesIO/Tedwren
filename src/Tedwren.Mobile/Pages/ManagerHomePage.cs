using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Dashboard;
using Tedwren.Abstractions.Contracts.Expiry;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The manager/admin home (M7): a live overview dashboard — KPI cards, a workforce-compliance bar, expiring cards
/// and recent activity, loaded from the console aggregation via <see cref="ManagerDataService"/> (cache-then-network,
/// usable offline) — above the interactive card menu that routes to the manager surfaces (muster, site entry,
/// operatives, forms, submissions, evidence, reports).
/// </summary>
public class ManagerHomePage : ContentPage
{
    private readonly ManagerDataService _data;
    private readonly IServiceProvider _services;

    private readonly TwKpiCard _operatives = new() { Label = "Operatives" };
    private readonly TwKpiCard _sites = new() { Label = "Active sites" };
    private readonly TwKpiCard _compliant = new() { Label = "Compliant" };
    private readonly TwKpiCard _expiring = new() { Label = "Expiring 30d" };
    private readonly VerticalStackLayout _complianceBody = new() { Spacing = 8 };
    private readonly VerticalStackLayout _expiringBody = new() { Spacing = 6 };
    private readonly VerticalStackLayout _activityBody = new() { Spacing = 6 };

    /// <summary>Builds the manager overview dashboard and card menu.</summary>
    public ManagerHomePage(ManagerDataService data, IServiceProvider services)
    {
        _data = data;
        _services = services;
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
                    BuildKpiGrid(),
                    Section("Workforce compliance", _complianceBody),
                    Section("Expiring soon", _expiringBody),
                    Section("Recent activity", _activityBody),
                    new Label { Text = "Menu", FontSize = 18, FontAttributes = FontAttributes.Bold },
                    BuildMenu(),
                },
            },
        };
    }

    /// <summary>Reloads the dashboard each time the page appears (fresh when online, else the cached copy).</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var summary = await _data.GetDashboardAsync();
            if (summary is not null)
            {
                _operatives.Value = summary.Kpis.Operatives.ToString();
                _sites.Value = summary.Kpis.Sites.ToString();
                _compliant.Value = summary.Kpis.CompliancePercent is { } pct ? $"{pct:0}%" : "—";
                _expiring.Value = summary.Kpis.UpcomingExpiries.ToString();
                BuildComplianceBar(summary.Compliance);
            }

            var expiries = await _data.GetUpcomingExpiriesAsync();
            _expiringBody.Children.Clear();
            if (expiries is { Count: > 0 })
            {
                foreach (var e in expiries.Take(5))
                {
                    _expiringBody.Children.Add(Row(e.QualificationName, $"{e.PersonName ?? "Operative"} · {DueLabel(e)}"));
                }
            }
            else
            {
                _expiringBody.Children.Add(Muted("Nothing expiring in the next 30 days."));
            }

            var activity = await _data.GetRecentActivityAsync();
            _activityBody.Children.Clear();
            if (activity is { Count: > 0 })
            {
                foreach (var a in activity.Take(6))
                {
                    _activityBody.Children.Add(Row($"{a.Action} · {a.Entity}", $"{a.Actor} · {UkTime.Format(a.OccurredUtc, "dd MMM HH:mm")}"));
                }
            }
            else
            {
                _activityBody.Children.Add(Muted("No recent activity."));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _complianceBody.Children.Clear();
            _complianceBody.Children.Add(Muted("Couldn't load the dashboard."));
        }
    }

    private View BuildKpiGrid()
    {
        var grid = new Grid
        {
            ColumnSpacing = 12,
            RowSpacing = 12,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
            RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto) },
        };
        grid.Add(_operatives, 0, 0);
        grid.Add(_sites, 1, 0);
        grid.Add(_compliant, 0, 1);
        grid.Add(_expiring, 1, 1);
        return grid;
    }

    /// <summary>Renders the compliance breakdown as a proportional segmented bar with a legend.</summary>
    private void BuildComplianceBar(ComplianceBreakdownDto c)
    {
        _complianceBody.Children.Clear();
        var total = c.Compliant + c.AtRisk + c.NonCompliant + c.Pending;
        if (total <= 0)
        {
            _complianceBody.Children.Add(Muted("No workforce compliance data yet."));
            return;
        }

        var bar = new Grid { HeightRequest = 14, ColumnSpacing = 0 };
        AddSegment(bar, c.Compliant, TwPalette.SuccessLight);
        AddSegment(bar, c.AtRisk, TwPalette.WarningLight);
        AddSegment(bar, c.NonCompliant, TwPalette.DangerLight);
        AddSegment(bar, c.Pending, TwPalette.BorderLight);

        var legend = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        legend.Children.Add(LegendItem($"Compliant {c.Compliant}", TwPalette.SuccessLight));
        legend.Children.Add(LegendItem($"At risk {c.AtRisk}", TwPalette.WarningLight));
        legend.Children.Add(LegendItem($"Non-compliant {c.NonCompliant}", TwPalette.DangerLight));
        legend.Children.Add(LegendItem($"Pending {c.Pending}", TwPalette.BorderLight));

        _complianceBody.Children.Add(new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(7) },
            Content = bar,
        });
        _complianceBody.Children.Add(legend);
    }

    private static void AddSegment(Grid bar, int count, Color colour)
    {
        if (count <= 0)
        {
            return;
        }

        bar.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(count, GridUnitType.Star)));
        var box = new BoxView { Color = colour };
        bar.Add(box, bar.ColumnDefinitions.Count - 1, 0);
    }

    private static View LegendItem(string text, Color colour) => new HorizontalStackLayout
    {
        Spacing = 6,
        Margin = new Thickness(0, 6, 16, 0),
        Children =
        {
            new BoxView { Color = colour, WidthRequest = 12, HeightRequest = 12, VerticalOptions = LayoutOptions.Center },
            new Label { Text = text, FontSize = 12 },
        },
    };

    private static View Section(string title, View body) => new VerticalStackLayout
    {
        Spacing = 8,
        Children =
        {
            new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold },
            new TwCard { Content = body },
        },
    };

    private static View Row(string title, string subtitle)
    {
        var sub = new Label { Text = subtitle, FontSize = 12 };
        sub.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
        return new VerticalStackLayout { Spacing = 1, Children = { new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 14 }, sub } };
    }

    private static Label Muted(string text)
    {
        var label = new Label { Text = text, FontSize = 13 };
        label.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
        return label;
    }

    private static string DueLabel(UpcomingExpiryDto e) =>
        e.DaysUntilExpiry < 0 ? "expired" : e.ExpiresOn is { } on ? $"expires {on:dd MMM}" : e.StatusLabel;

    private View BuildMenu()
    {
        var items = new (string Glyph, string Title, string Subtitle, Func<Page>? Destination)[]
        {
            ("👷", "Who's on site", "Live muster", () => _services.GetRequiredService<MusterPage>()),
            ("🚦", "Site entry", "Decisions & overrides", () => _services.GetRequiredService<SiteEntryPage>()),
            ("🧑‍🔧", "Operatives", "Register & compliance", () => _services.GetRequiredService<OperativesPage>()),
            ("📋", "Forms", "Library & assign", () => _services.GetRequiredService<FormsManagePage>()),
            ("✅", "Submissions", "Review & approve", () => _services.GetRequiredService<FormReviewListPage>()),
            ("📷", "Evidence", "Field captures", () => _services.GetRequiredService<EvidenceReviewPage>()),
            ("📊", "Reports", "Evidence pack", () => _services.GetRequiredService<ReportsPage>()),
        };
        return TileGrid.Build(this, items);
    }
}
