using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Evidence;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// A single field-evidence capture (M7): the note, who captured it and when, the location, and the photo loaded
/// through the authorised image route (R9). Fetched live from <c>/api/evidence-captures/{id}</c>.
/// </summary>
public class EvidenceDetailPage : ContentPage
{
    private readonly ManagerEvidenceApiClient _evidence;
    private readonly ManagerImageApiClient _images;

    private readonly VerticalStackLayout _body = new() { Padding = new Thickness(16), Spacing = 16 };

    private Guid _id;
    private bool _loaded;

    /// <summary>Builds the evidence detail page over the evidence + image clients.</summary>
    public EvidenceDetailPage(ManagerEvidenceApiClient evidence, ManagerImageApiClient images)
    {
        _evidence = evidence;
        _images = images;
        Title = "Evidence";
        Content = new ScrollView { Content = _body };
    }

    /// <summary>Sets the capture to show (called before navigation).</summary>
    public void Load(Guid id, string title)
    {
        _id = id;
        Title = title;
        _loaded = false;
    }

    /// <summary>Loads the capture once when the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded || _id == Guid.Empty)
        {
            return;
        }

        _loaded = true;
        _body.Children.Clear();
        EvidenceCaptureDto? capture;
        try
        {
            capture = await _evidence.GetCaptureAsync(_id);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            capture = null;
        }

        if (capture is null)
        {
            _body.Children.Add(new TwEmptyState { Glyph = "📷", Message = "Couldn't load this capture. Connect and try again." });
            return;
        }

        Render(capture);
    }

    private void Render(EvidenceCaptureDto c)
    {
        var details = new VerticalStackLayout { Spacing = 6 };
        details.Children.Add(KeyValue("Captured by", c.PersonName));
        details.Children.Add(KeyValue("Captured", UkTime.Format(c.CapturedUtc, "dd MMM yyyy HH:mm")));
        details.Children.Add(KeyValue("Note", string.IsNullOrWhiteSpace(c.Note) ? "—" : c.Note!));
        if (c is { Latitude: { } lat, Longitude: { } lng })
        {
            details.Children.Add(KeyValue("Location", $"{lat:0.00000}, {lng:0.00000}"));
        }

        _body.Children.Add(new TwCard { Content = details });

        if (c.PhotoReference is { Length: > 0 } reference)
        {
            var image = new Image { Aspect = Aspect.AspectFit, IsVisible = false };
            _body.Children.Add(image);
            _ = LoadPhotoAsync(image, reference);
        }
    }

    private async Task LoadPhotoAsync(Image image, string reference)
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
            // A photo that can't be loaded simply stays hidden.
        }
    }

    private static View KeyValue(string key, string value)
    {
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(120)), new ColumnDefinition(GridLength.Star) } };
        grid.Add(ManagerUi.Muted(key), 0, 0);
        grid.Add(new Label { Text = value, FontSize = 14 }, 1, 0);
        return grid;
    }
}
