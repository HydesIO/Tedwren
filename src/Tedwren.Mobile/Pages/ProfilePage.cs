using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>The operative's own profile — identity, employer, compliance and emergency contact (M3).</summary>
public class ProfilePage : ContentPage
{
    private readonly OperativeDataService _data;
    private readonly VerticalStackLayout _body = new() { Spacing = 8 };
    private bool _loaded;

    /// <summary>Builds the profile page.</summary>
    public ProfilePage(OperativeDataService data)
    {
        _data = data;
        Title = "Profile";
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children = { new Label { Text = "Profile", FontSize = 22, FontAttributes = FontAttributes.Bold }, new TwCard { Content = _body } },
            },
        };
    }

    /// <summary>Loads the profile once when the page first appears.</summary>
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
        _body.Children.Clear();
        try
        {
            var profile = await _data.GetProfileAsync();
            if (profile is null)
            {
                _body.Children.Add(new Label { Text = "Couldn't load your profile." });
                return;
            }

            _body.Children.Add(new Label { Text = profile.Name, FontSize = 18, FontAttributes = FontAttributes.Bold });
            AddRow("Trade", profile.Trade ?? "—");
            AddRow("Employer", profile.Company);
            AddRow("Mobile", profile.Phone ?? "—");
            AddRow("Compliance", profile.StatusLabel);
            AddRow("Emergency contact", profile.EmergencyContactName is null ? "—" : $"{profile.EmergencyContactName} · {profile.EmergencyContactPhone}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _body.Children.Add(new Label { Text = "Couldn't load your profile." });
        }
    }

    private void AddRow(string label, string value)
    {
        var caption = new Label { Text = label, FontSize = 13 };
        caption.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
        _body.Children.Add(new VerticalStackLayout { Spacing = 0, Children = { caption, new Label { Text = value } } });
    }
}
