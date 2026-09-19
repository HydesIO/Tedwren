using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>The operative's qualification cards + compliance (SF-8), loaded cache-then-network (M3).</summary>
public class MyCardsPage : ContentPage
{
    private readonly OperativeDataService _data;
    private readonly VerticalStackLayout _list = new() { Spacing = 12 };
    private bool _loaded;

    /// <summary>Builds the cards page.</summary>
    public MyCardsPage(OperativeDataService data)
    {
        _data = data;
        Title = "My cards";
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children = { new Label { Text = "Cards & compliance", FontSize = 22, FontAttributes = FontAttributes.Bold }, _list },
            },
        };
    }

    /// <summary>Loads the cards once when the page first appears.</summary>
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
            var profile = await _data.GetProfileAsync();
            if (profile is null)
            {
                _list.Children.Add(new Label { Text = "Couldn't load your cards." });
                return;
            }

            _list.Children.Add(new TwCard
            {
                Content = new Label { Text = profile.StatusLabel, FontAttributes = FontAttributes.Bold },
            });

            foreach (var card in profile.Qualifications)
            {
                var meta = new Label { FontSize = 13, Text = $"{card.StatusLabel}{(card.ExpiresOn is { } e ? $"  ·  expires {e:dd MMM yyyy}" : string.Empty)}" };
                meta.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
                _list.Children.Add(new TwCard
                {
                    Content = new VerticalStackLayout
                    {
                        Spacing = 2,
                        Children =
                        {
                            new Label { Text = card.Name, FontAttributes = FontAttributes.Bold },
                            meta,
                        },
                    },
                });
            }

            if (profile.Qualifications.Count == 0)
            {
                _list.Children.Add(new Label { Text = "No cards on file yet." });
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _list.Children.Add(new Label { Text = "Couldn't load your cards." });
        }
    }
}
