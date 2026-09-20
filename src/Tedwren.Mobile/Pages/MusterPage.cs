using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The manager muster (M7, MC-12–MC-14): who is on a chosen site right now, with the data's age shown from the
/// muster's own <see cref="MusterDto.GeneratedUtc"/> so a cached copy is honestly stale-labelled offline (MC-14),
/// and the monitored competency cover (MC-13). Loaded via <see cref="ManagerDataService"/> (cache-then-network).
/// </summary>
public class MusterPage : ContentPage
{
    private readonly ManagerDataService _data;

    private readonly Picker _sitePicker = new() { Title = "Choose a site" };
    private readonly Label _dataAge = new() { FontSize = 12, IsVisible = false };
    private readonly VerticalStackLayout _competencyBody = new() { Spacing = 6 };
    private readonly VerticalStackLayout _peopleBody = new() { Spacing = 8 };

    private IReadOnlyList<SiteSummary> _sites = Array.Empty<SiteSummary>();

    /// <summary>Builds the muster page over the manager data service.</summary>
    public MusterPage(ManagerDataService data)
    {
        _data = data;
        Title = "Who's on site";
        _dataAge.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
        _sitePicker.SelectedIndexChanged += async (_, _) => await LoadMusterAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new Label { Text = "Who's on site", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    _sitePicker,
                    _dataAge,
                    new Label { Text = "Competency cover", FontSize = 16, FontAttributes = FontAttributes.Bold },
                    new TwCard { Content = _competencyBody },
                    new Label { Text = "On site now", FontSize = 16, FontAttributes = FontAttributes.Bold },
                    new TwCard { Content = _peopleBody },
                },
            },
        };
    }

    /// <summary>Loads the site list once when the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSitesAsync();
    }

    private async Task LoadSitesAsync()
    {
        if (_sites.Count > 0)
        {
            return;
        }

        _sites = await _data.GetSitesAsync() ?? Array.Empty<SiteSummary>();
        _sitePicker.ItemsSource = _sites.Select(s => s.Name).ToList();
        if (_sites.Count > 0)
        {
            _sitePicker.SelectedIndex = 0; // triggers the first muster load
        }
    }

    private async Task LoadMusterAsync()
    {
        if (SelectedSite() is not { } site)
        {
            return;
        }

        _competencyBody.Children.Clear();
        _peopleBody.Children.Clear();
        try
        {
            var muster = await _data.GetMusterAsync(site.Id);
            if (muster is null)
            {
                _dataAge.IsVisible = false;
                _peopleBody.Children.Add(ManagerUi.Muted("Couldn't load the muster. Connect and try again."));
                return;
            }

            _dataAge.IsVisible = true;
            _dataAge.Text = $"As at {UkTime.Format(muster.GeneratedUtc, "HH:mm")} · {ManagerUi.FormatAge(DateTimeOffset.UtcNow - muster.GeneratedUtc)} ago";

            if (muster.Competencies.Count == 0)
            {
                _competencyBody.Children.Add(ManagerUi.Muted("No monitored competencies."));
            }

            foreach (var c in muster.Competencies)
            {
                _competencyBody.Children.Add(CompetencyRow(c));
            }

            if (muster.People.Count == 0)
            {
                _peopleBody.Children.Add(ManagerUi.Muted("Nobody is signed in right now."));
            }

            foreach (var p in muster.People)
            {
                _peopleBody.Children.Add(PersonRow(p));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _dataAge.IsVisible = false;
            _peopleBody.Children.Add(ManagerUi.Muted("Couldn't load the muster."));
        }
    }

    private static View CompetencyRow(CompetencyCoverDto c)
    {
        var (kind, label) = !c.Covered
            ? (TwStatusKind.Danger, "Not covered")
            : c.HoldersOnSite <= 1
                ? (TwStatusKind.Warning, "Last holder on site")
                : (TwStatusKind.Success, $"{c.HoldersOnSite} on site");

        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
        };
        grid.Add(new Label { Text = c.Competency, VerticalOptions = LayoutOptions.Center }, 0, 0);
        grid.Add(new TwStatusPill { Text = label, Kind = kind }, 1, 0);
        return grid;
    }

    private static View PersonRow(MusterPersonDto p)
    {
        var detail = (p.PropertyName is { Length: > 0 } prop ? $"{prop} · " : string.Empty)
            + $"since {UkTime.Format(p.SinceUtc, "HH:mm")}";
        return new VerticalStackLayout
        {
            Spacing = 1,
            Children =
            {
                new Label { Text = p.Name, FontAttributes = FontAttributes.Bold, FontSize = 14 },
                ManagerUi.Muted(detail),
            },
        };
    }

    private SiteSummary? SelectedSite() =>
        _sitePicker.SelectedIndex >= 0 && _sitePicker.SelectedIndex < _sites.Count ? _sites[_sitePicker.SelectedIndex] : null;
}
