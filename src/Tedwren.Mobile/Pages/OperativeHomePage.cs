using Microsoft.Extensions.DependencyInjection;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Sync;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The operative home (M3): an overview dashboard (today's site + sign-in state, hours this week, compliance +
/// next expiry, and the M5 pending-sync badge) loaded from <see cref="OperativeDataService"/> (cache-then-network),
/// above the interactive card menu. Sign in/out (M4) and evidence/hazard capture (M5) are live; forms are M6.
/// </summary>
public class OperativeHomePage : ContentPage
{
    private readonly OperativeDataService _data;
    private readonly SyncEngine _sync;
    private readonly IServiceProvider _services;

    private readonly Label _statusLine = new() { FontAttributes = FontAttributes.Bold, IsVisible = false };
    private readonly Label _complianceLine = new() { FontSize = 13 };
    private readonly Label _metaLine = new() { Text = "Hours this week: —    ·    Forms due: —" };
    private readonly Label _syncLine = new() { FontSize = 13 };
    private readonly Button _syncNow;
    private readonly TwSkeleton _loadingSkeleton = new() { HeightRequest = 18, WidthRequest = 180, HorizontalOptions = LayoutOptions.Start };

    /// <summary>Builds the operative overview dashboard and card menu.</summary>
    public OperativeHomePage(OperativeDataService data, SyncEngine sync, IServiceProvider services)
    {
        _data = data;
        _sync = sync;
        _services = services;
        Title = "My work";
        _complianceLine.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
        _syncLine.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);

        _syncNow = new Button { Text = "Sync now", IsVisible = false, Padding = new Thickness(8, 0) };
        _syncNow.SetDynamicResource(VisualElement.StyleProperty, "TwSecondaryButton");
        _syncNow.Clicked += async (_, _) => await _sync.DrainAsync();

        var syncRow = new HorizontalStackLayout
        {
            Spacing = 12,
            VerticalOptions = LayoutOptions.Center,
            Children = { _syncLine, _syncNow },
        };

        var dashboard = new TwCard
        {
            Content = new VerticalStackLayout { Spacing = 8, Children = { _loadingSkeleton, _statusLine, _complianceLine, _metaLine, syncRow } },
        };

        _sync.StateChanged += OnSyncStateChanged;

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

    /// <summary>Reloads the dashboard and refreshes the sync badge each time the page appears (also nudges a sync).</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
        UpdateSyncLine();
        _sync.RequestSync();
    }

    private void OnSyncStateChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(UpdateSyncLine);

    private void UpdateSyncLine()
    {
        var state = _sync.State;
        _syncNow.IsVisible = state.PendingCount > 0;
        _syncLine.Text = state switch
        {
            { NeedsAttentionCount: > 0 } => $"{state.NeedsAttentionCount} item(s) need attention",
            { IsSyncing: true } => "Syncing…",
            { PendingCount: > 0 } => $"{state.PendingCount} to sync",
            _ => "All synced",
        };
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
        finally
        {
            // Swap the loading skeleton for the resolved status line (M8: skeletons, not spinners).
            _loadingSkeleton.IsVisible = false;
            _statusLine.IsVisible = true;
        }
    }

    private View BuildMenu()
    {
        var items = new (string Glyph, string Title, string Subtitle, Func<Page>? Destination)[]
        {
            ("🕒", "Sign in / out", "Record arrival & departure", () => _services.GetRequiredService<SignInOutPage>()),
            ("📷", "Capture evidence", "Photos with location", () => _services.GetRequiredService<CaptureEvidencePage>()),
            ("⚠️", "Report hazard", "Near-miss & observations", () => _services.GetRequiredService<ReportHazardPage>()),
            ("📋", "Forms due", "Checklists & inspections", () => _services.GetRequiredService<FormsInboxPage>()),
            ("🎓", "Induction", "Complete your site induction", () => _services.GetRequiredService<InductionPage>()),
            ("📝", "Site RAMS", "Read & sign the method statement", () => _services.GetRequiredService<RamsSignPage>()),
            ("⏱", "My hours", "This week's timesheet", () => _services.GetRequiredService<MyHoursPage>()),
            ("🪪", "My cards", "Compliance & expiries", () => _services.GetRequiredService<MyCardsPage>()),
            ("📄", "Site documents", "Rules, plans, welfare", null),
            ("👤", "Profile", "Your details", () => _services.GetRequiredService<ProfilePage>()),
        };
        return TileGrid.Build(this, items);
    }
}
