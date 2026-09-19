using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>The operative's recorded hours for the current week (SUB-27), loaded cache-then-network (M3).</summary>
public class MyHoursPage : ContentPage
{
    private readonly OperativeDataService _data;
    private readonly VerticalStackLayout _list = new() { Spacing = 12 };
    private bool _loaded;

    /// <summary>Builds the hours page.</summary>
    public MyHoursPage(OperativeDataService data)
    {
        _data = data;
        Title = "My hours";
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children = { new Label { Text = "This week", FontSize = 22, FontAttributes = FontAttributes.Bold }, _list },
            },
        };
    }

    /// <summary>Loads the hours once when the page first appears.</summary>
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
        _list.Children.Clear();
        try
        {
            var hours = await _data.GetHoursAsync();
            if (hours is null)
            {
                _list.Children.Add(new Label { Text = "Couldn't load your hours." });
                return;
            }

            _list.Children.Add(new TwCard
            {
                Content = new Label { Text = $"Total: {hours.TotalHours:0.##} h  ·  {hours.StatusLabel}", FontAttributes = FontAttributes.Bold },
            });

            foreach (var line in hours.Lines)
            {
                var site = new Label { Text = line.SiteName, FontSize = 13 };
                site.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
                _list.Children.Add(new TwCard
                {
                    Content = new VerticalStackLayout
                    {
                        Spacing = 2,
                        Children =
                        {
                            new Label { Text = $"{line.WorkDate:ddd dd MMM} — {line.Hours:0.##} h", FontAttributes = FontAttributes.Bold },
                            site,
                        },
                    },
                });
            }

            if (hours.Lines.Count == 0)
            {
                _list.Children.Add(new Label { Text = "No hours recorded this week yet." });
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _list.Children.Add(new Label { Text = "Couldn't load your hours." });
        }
    }
}
