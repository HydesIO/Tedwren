using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Evidence;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The manager evidence review list (M7): field-evidence captures operatives recorded on the app (M5), newest first,
/// each tapping through to the full capture. Loaded via <see cref="ManagerDataService"/> (cache-then-network).
/// </summary>
public class EvidenceReviewPage : ContentPage
{
    private readonly ManagerDataService _data;
    private readonly IServiceProvider _services;

    private readonly VerticalStackLayout _listBody = new() { Spacing = 8 };

    /// <summary>Builds the evidence review list over the manager data service.</summary>
    public EvidenceReviewPage(ManagerDataService data, IServiceProvider services)
    {
        _data = data;
        _services = services;
        Title = "Evidence";

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new Label { Text = "Field evidence", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    _listBody,
                },
            },
        };
    }

    /// <summary>Reloads captures each time the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _listBody.Children.Clear();
        var captures = await _data.GetEvidenceCapturesAsync();
        if (captures is not { Count: > 0 })
        {
            _listBody.Children.Add(new TwEmptyState { Glyph = "📷", Message = "No evidence captured yet." });
            return;
        }

        foreach (var c in captures)
        {
            _listBody.Children.Add(Row(c));
        }
    }

    private View Row(EvidenceCaptureDto c)
    {
        var title = string.IsNullOrWhiteSpace(c.Note) ? "(no note)" : c.Note!;
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        grid.Add(new VerticalStackLayout
        {
            Spacing = 1,
            Children =
            {
                new Label { Text = title, FontAttributes = FontAttributes.Bold, FontSize = 14, LineBreakMode = LineBreakMode.TailTruncation },
                ManagerUi.Muted($"{c.PersonName} · {UkTime.Format(c.CapturedUtc, "dd MMM HH:mm")}"),
            },
        }, 0, 0);
        if (c.PhotoReference is { Length: > 0 })
        {
            grid.Add(new Label { Text = "📷", FontSize = 18, VerticalOptions = LayoutOptions.Center }, 1, 0);
        }

        return ManagerUi.TappableCard(grid, async () =>
        {
            var page = _services.GetRequiredService<EvidenceDetailPage>();
            page.Load(c.Id, c.PersonName);
            await Navigation.PushAsync(page);
        });
    }
}
