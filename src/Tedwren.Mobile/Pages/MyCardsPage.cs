using Microsoft.Extensions.DependencyInjection;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>The operative's qualification cards + compliance (SF-8), loaded cache-then-network (M3), with the
/// trade's outstanding accreditations (SF-11 / Gate 3) and an entry to add one.</summary>
public class MyCardsPage : ContentPage
{
    private readonly OperativeDataService _data;
    private readonly IServiceProvider _services;
    private readonly VerticalStackLayout _list = new() { Spacing = 12 };
    private readonly Button _addButton;
    private bool _loaded;

    /// <summary>Builds the cards page. <paramref name="services"/> resolves the add-accreditation page for navigation.</summary>
    public MyCardsPage(OperativeDataService data, IServiceProvider services)
    {
        _data = data;
        _services = services;
        Title = "My cards";

        _addButton = new Button { Text = "Add accreditation" };
        _addButton.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        _addButton.Clicked += async (_, _) => await Navigation.PushAsync(_services.GetRequiredService<AddCardPage>());

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children = { new Label { Text = "Cards & compliance", FontSize = 22, FontAttributes = FontAttributes.Bold }, _list, _addButton },
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

            // The trade's required-but-missing accreditations (SF-11 / Gate 3) — surfaced so the operative knows what
            // to add. The gate itself is enforced at site entry (Phase 7); this is the "still needed" prompt only.
            if (profile.MissingQualifications is { Count: > 0 } missing)
            {
                var stillNeeded = new VerticalStackLayout { Spacing = 4 };
                stillNeeded.Children.Add(new Label { Text = "Still needed", FontAttributes = FontAttributes.Bold });
                var note = new Label { Text = "Your trade requires these accreditations. Add them so you can be cleared for site.", FontSize = 13 };
                note.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);
                stillNeeded.Children.Add(note);
                foreach (var name in missing)
                {
                    stillNeeded.Children.Add(new Label { Text = $"•  {name}" });
                }

                _list.Children.Add(new TwCard { Content = stillNeeded });
            }

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
