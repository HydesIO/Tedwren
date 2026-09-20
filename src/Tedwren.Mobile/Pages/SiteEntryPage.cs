using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Abstractions.Contracts.Workforce;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.SiteEntry;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The manager site-entry check + day-only override (M7, MC-8/MC-11). A manager picks a site and operative and
/// runs the five-check decision against current data — <b>online-only and never queued</b> (R2/R3), fail-closed
/// (R2). The result and its checks are shown with product-correct wording via the ported presenter (R18); when a
/// decision is blocked the manager may override it for the day with a reason, attributed server-side to the
/// signed-in manager (MC-11).
/// </summary>
public class SiteEntryPage : ContentPage
{
    private readonly ManagerDataService _data;
    private readonly ManagerSiteEntryApiClient _siteEntry;
    private readonly IConnectivityService _connectivity;

    private readonly Picker _sitePicker = new() { Title = "Choose a site" };
    private readonly Picker _operativePicker = new() { Title = "Choose an operative" };
    private readonly Label _resultBanner = new() { IsVisible = false, Padding = new Thickness(12), FontAttributes = FontAttributes.Bold };
    private readonly VerticalStackLayout _checksBody = new() { Spacing = 6, IsVisible = false };
    private readonly Button _check;
    private readonly Button _override;

    private IReadOnlyList<SiteSummary> _sites = Array.Empty<SiteSummary>();
    private IReadOnlyList<OperativeListItemDto> _operatives = Array.Empty<OperativeListItemDto>();
    private bool _busy;

    /// <summary>Builds the site-entry page over the manager data service, the site-entry client and connectivity.</summary>
    public SiteEntryPage(ManagerDataService data, ManagerSiteEntryApiClient siteEntry, IConnectivityService connectivity)
    {
        _data = data;
        _siteEntry = siteEntry;
        _connectivity = connectivity;
        Title = "Site entry";

        _check = new Button { Text = "Check entry" };
        _check.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        _check.Clicked += OnCheckAsync;

        _override = new Button { Text = "Override for today", IsVisible = false };
        _override.SetDynamicResource(VisualElement.StyleProperty, "TwSecondaryButton");
        _override.Clicked += OnOverrideAsync;

        var guidance = new TwCard
        {
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = "Check whether a worker can enter", FontAttributes = FontAttributes.Bold },
                    new Label { Text = "The decision uses live data and needs a connection. A blocked worker can be admitted with a recorded manager override.", FontSize = 13 },
                },
            },
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new Label { Text = "Site entry", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    guidance,
                    new Label { Text = "Site", FontAttributes = FontAttributes.Bold },
                    _sitePicker,
                    new Label { Text = "Operative", FontAttributes = FontAttributes.Bold },
                    _operativePicker,
                    _check,
                    _resultBanner,
                    new TwCard { Content = _checksBody },
                    _override,
                },
            },
        };
    }

    /// <summary>Loads the cached site and operative lists once when the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_sites.Count == 0)
        {
            _sites = await _data.GetSitesAsync() ?? Array.Empty<SiteSummary>();
            _sitePicker.ItemsSource = _sites.Select(s => s.Name).ToList();
        }

        if (_operatives.Count == 0)
        {
            _operatives = await _data.GetOperativesAsync() ?? Array.Empty<OperativeListItemDto>();
            _operativePicker.ItemsSource = _operatives.Select(o => o.Name).ToList();
        }
    }

    private async void OnCheckAsync(object? sender, EventArgs e) => await DecideAsync(null);

    private async void OnOverrideAsync(object? sender, EventArgs e)
    {
        var reason = await DisplayPromptAsync("Manager override", "Reason for admitting despite the block:", accept: "Admit", cancel: "Cancel");
        if (!string.IsNullOrWhiteSpace(reason))
        {
            await DecideAsync(reason.Trim());
        }
    }

    private async Task DecideAsync(string? overrideReason)
    {
        if (_busy)
        {
            return;
        }

        if (!_connectivity.IsConnected)
        {
            ShowBanner("You must be online to check entry — the browser link still works.", GateResultSeverity.Warning);
            return;
        }

        if (SelectedSite() is not { } site || SelectedOperative() is not { } operative)
        {
            await DisplayAlert("Choose both", "Pick a site and an operative first.", "OK");
            return;
        }

        _busy = true;
        _check.IsEnabled = false;
        _override.IsEnabled = false;
        try
        {
            var result = await _siteEntry.DecideAsync(new ManagerDecideRequest(site.Id, operative.PersonId, null, overrideReason));
            if (result is null)
            {
                ShowBanner("Couldn't reach the site gate. Try again.", GateResultSeverity.Danger);
                return;
            }

            // Product is unknown to the app; the presenter's null default keeps the fail-closed main-contractor framing (R2/R18).
            var view = SiteGateResultPresenter.For(null, result);
            ShowBanner($"{view.Title} — {view.Message}", view.Severity);
            RenderChecks(result.Checks);
            _override.IsVisible = view.OffersOverride;
        }
        catch (ApiException)
        {
            ShowBanner("Couldn't complete the check. You may not have permission, or the connection failed.", GateResultSeverity.Danger);
        }
        finally
        {
            _busy = false;
            _check.IsEnabled = true;
            _override.IsEnabled = true;
        }
    }

    private void RenderChecks(IReadOnlyList<DecisionCheckResultDto> checks)
    {
        _checksBody.Children.Clear();
        _checksBody.IsVisible = checks.Count > 0;
        foreach (var check in checks)
        {
            var kind = check.Outcome switch
            {
                "Passed" => TwStatusKind.Success,
                "Failed" => TwStatusKind.Danger,
                _ => TwStatusKind.Neutral,
            };
            var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
            var text = new VerticalStackLayout
            {
                Spacing = 1,
                Children = { new Label { Text = check.Name, FontSize = 14, FontAttributes = FontAttributes.Bold } },
            };
            if (!string.IsNullOrWhiteSpace(check.Detail))
            {
                text.Children.Add(ManagerUi.Muted(check.Detail!));
            }

            grid.Add(text, 0, 0);
            grid.Add(new TwStatusPill { Text = check.Outcome, Kind = kind }, 1, 0);
            _checksBody.Children.Add(grid);
        }
    }

    private void ShowBanner(string text, GateResultSeverity severity)
    {
        _resultBanner.Text = text;
        _resultBanner.IsVisible = true;
        var (paleLight, paleDark, solidLight, solidDark) = severity switch
        {
            GateResultSeverity.Success => (TwPalette.SuccessPaleLight, TwPalette.SuccessPaleDark, TwPalette.SuccessLight, TwPalette.SuccessDark),
            GateResultSeverity.Danger => (TwPalette.DangerPaleLight, TwPalette.DangerPaleDark, TwPalette.DangerLight, TwPalette.DangerDark),
            _ => (TwPalette.WarningPaleLight, TwPalette.WarningPaleDark, TwPalette.WarningLight, TwPalette.WarningDark),
        };
        _resultBanner.SetAppThemeColor(VisualElement.BackgroundColorProperty, paleLight, paleDark);
        _resultBanner.SetAppThemeColor(Label.TextColorProperty, solidLight, solidDark);
    }

    private SiteSummary? SelectedSite() =>
        _sitePicker.SelectedIndex >= 0 && _sitePicker.SelectedIndex < _sites.Count ? _sites[_sitePicker.SelectedIndex] : null;

    private OperativeListItemDto? SelectedOperative() =>
        _operativePicker.SelectedIndex >= 0 && _operativePicker.SelectedIndex < _operatives.Count ? _operatives[_operativePicker.SelectedIndex] : null;
}
