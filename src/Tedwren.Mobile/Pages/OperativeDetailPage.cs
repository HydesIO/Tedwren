using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Workforce;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The manager operative profile (M7): overview, qualification cards (with the captured card photo loaded through
/// the authorised image route, R9) and recent history — mirroring the console operative detail. Fetched live from
/// the console workforce endpoint; shows a friendly message when it can't be loaded.
/// </summary>
public class OperativeDetailPage : ContentPage
{
    private readonly ManagerWorkforceApiClient _workforce;
    private readonly ManagerImageApiClient _images;

    private readonly VerticalStackLayout _body = new() { Padding = new Thickness(16), Spacing = 16 };

    private string? _slug;
    private bool _loaded;

    /// <summary>Builds the operative detail page over the workforce + image clients.</summary>
    public OperativeDetailPage(ManagerWorkforceApiClient workforce, ManagerImageApiClient images)
    {
        _workforce = workforce;
        _images = images;
        Title = "Operative";
        Content = new ScrollView { Content = _body };
    }

    /// <summary>Sets the operative to show (called before navigation).</summary>
    public void Load(string slug, string name)
    {
        _slug = slug;
        Title = name;
        _loaded = false;
    }

    /// <summary>Loads the profile once when the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded || _slug is null)
        {
            return;
        }

        _loaded = true;
        _body.Children.Clear();
        try
        {
            var op = await _workforce.GetOperativeAsync(_slug);
            if (op is null)
            {
                _body.Children.Add(new TwEmptyState { Glyph = "🧑‍🔧", Message = "Couldn't load this operative. Connect and try again." });
                return;
            }

            Render(op);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _body.Children.Add(new TwEmptyState { Glyph = "🧑‍🔧", Message = "Couldn't load this operative." });
        }
    }

    private void Render(OperativeDetailDto op)
    {
        var header = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = op.Name, FontSize = 22, FontAttributes = FontAttributes.Bold },
                ManagerUi.Muted($"{op.Trade ?? "—"} · {op.Company}"),
                new TwStatusPill { Text = op.StatusLabel, Kind = ManagerUi.ForCompliance(op.State) },
            },
        };
        _body.Children.Add(header);

        if (op.InductionApplies && !op.InductionValid)
        {
            var banner = new Label { Text = "Not site-ready — induction outstanding", Padding = new Thickness(12), FontAttributes = FontAttributes.Bold };
            banner.SetAppThemeColor(VisualElement.BackgroundColorProperty, TwPalette.WarningPaleLight, TwPalette.WarningPaleDark);
            banner.SetAppThemeColor(Label.TextColorProperty, TwPalette.WarningLight, TwPalette.WarningDark);
            _body.Children.Add(banner);
        }

        var overview = new VerticalStackLayout { Spacing = 6 };
        overview.Children.Add(KeyValue("Phone", op.Phone ?? "—"));
        if (op.InductionApplies)
        {
            overview.Children.Add(KeyValue("Induction", op.InductionStatusLabel));
        }

        overview.Children.Add(KeyValue("Emergency contact", op.EmergencyContactName ?? "—"));
        overview.Children.Add(KeyValue("Emergency phone", op.EmergencyContactPhone ?? "—"));
        _body.Children.Add(Section("Overview", overview));

        var quals = new VerticalStackLayout { Spacing = 10 };
        if (op.Qualifications.Count == 0)
        {
            quals.Children.Add(ManagerUi.Muted("No qualification cards on record."));
        }

        foreach (var q in op.Qualifications)
        {
            quals.Children.Add(QualificationRow(q));
        }

        _body.Children.Add(Section($"Qualifications ({op.Qualifications.Count})", quals));

        var history = new VerticalStackLayout { Spacing = 8 };
        if (op.History.Count == 0)
        {
            history.Children.Add(ManagerUi.Muted("No recent history."));
        }

        foreach (var h in op.History)
        {
            var row = new VerticalStackLayout
            {
                Spacing = 1,
                Children = { new Label { Text = h.Title, FontAttributes = FontAttributes.Bold, FontSize = 14 } },
            };
            row.Children.Add(ManagerUi.Muted((h.Detail is { Length: > 0 } d ? d + " · " : string.Empty) + UkTime.Format(h.OccurredUtc, "dd MMM yyyy HH:mm")));
            history.Children.Add(row);
        }

        _body.Children.Add(Section("History", history));
    }

    private View QualificationRow(OperativeQualificationDto q)
    {
        var text = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = q.Name, FontAttributes = FontAttributes.Bold, FontSize = 15 },
                ManagerUi.Muted($"{q.Issuer ?? "—"}{(q.ExpiresOn is { } on ? $" · expires {on:dd MMM yyyy}" : string.Empty)}"),
                new TwStatusPill { Text = q.StatusLabel, Kind = ManagerUi.ForCompliance(q.State) },
            },
        };

        if (q.ImageReference is { Length: > 0 } reference)
        {
            var image = new Image { HeightRequest = 160, Aspect = Aspect.AspectFit, IsVisible = false, Margin = new Thickness(0, 6, 0, 0) };
            text.Children.Add(image);
            _ = LoadImageAsync(image, reference);
        }

        return new TwCard { Content = text };
    }

    private async Task LoadImageAsync(Image image, string reference)
    {
        try
        {
            var bytes = await _images.GetImageAsync(reference);
            if (bytes is { Length: > 0 })
            {
                image.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
                image.IsVisible = true;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A card photo that can't be loaded simply stays hidden — never break the profile.
        }
    }

    private static View KeyValue(string key, string value)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(130)), new ColumnDefinition(GridLength.Star) } };
        grid.Add(ManagerUi.Muted(key), 0, 0);
        grid.Add(new Label { Text = value, FontSize = 14 }, 1, 0);
        return grid;
    }

    private static View Section(string title, View body) => new VerticalStackLayout
    {
        Spacing = 8,
        Children =
        {
            new Label { Text = title, FontSize = 16, FontAttributes = FontAttributes.Bold },
            new TwCard { Content = body },
        },
    };
}
