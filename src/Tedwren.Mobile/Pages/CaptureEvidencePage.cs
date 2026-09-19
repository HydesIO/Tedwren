using System.Text.Json;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Sync;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// Capture field evidence offline (M5): a camera photo + note + location, queued to the encrypted outbox and synced
/// when back online. Available to every operative (ungated). Offline-first — the save always succeeds locally
/// (R2/R3 permit evidence capture offline, unlike attendance); the capture is stamped in UTC (R11).
/// </summary>
public class CaptureEvidencePage : ContentPage
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IOutboxStore _outbox;
    private readonly SyncEngine _sync;
    private readonly IConnectivityService _connectivity;

    private readonly Editor _note = new() { Placeholder = "What did you see? (optional)", HeightRequest = 100 };
    private readonly Image _preview = new() { IsVisible = false, HeightRequest = 160, Aspect = Aspect.AspectFit };
    private readonly Button _photoButton;
    private readonly Button _save;
    private readonly Label _status = new() { IsVisible = false, Padding = new Thickness(12) };

    private byte[]? _photoBytes;
    private string? _photoContentType;
    private bool _busy;

    /// <summary>Builds the evidence-capture page over the outbox, sync engine and connectivity.</summary>
    public CaptureEvidencePage(IOutboxStore outbox, SyncEngine sync, IConnectivityService connectivity)
    {
        _outbox = outbox;
        _sync = sync;
        _connectivity = connectivity;
        Title = "Capture evidence";

        _photoButton = new Button { Text = "Take photo" };
        _photoButton.SetDynamicResource(VisualElement.StyleProperty, "TwSecondaryButton");
        _photoButton.Clicked += OnTakePhotoAsync;

        _save = new Button { Text = "Save evidence" };
        _save.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        _save.Clicked += OnSaveAsync;

        var guidance = new TwCard
        {
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = "Capture evidence on site", FontAttributes = FontAttributes.Bold },
                    new Label { Text = "Take a photo and add a note. It's saved on your device and uploads automatically when you're online.", FontSize = 13 },
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
                    new Label { Text = "Evidence", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    guidance,
                    _photoButton,
                    _preview,
                    new Label { Text = "Note", FontAttributes = FontAttributes.Bold },
                    _note,
                    _status,
                    _save,
                },
            },
        };
    }

    private async void OnTakePhotoAsync(object? sender, EventArgs e)
    {
        var captured = await CaptureForm.TakePhotoAsync(this);
        if (captured is null)
        {
            return;
        }

        (_photoBytes, _photoContentType) = captured.Value;
        _preview.Source = ImageSource.FromStream(() => new MemoryStream(_photoBytes!));
        _preview.IsVisible = true;
    }

    private async void OnSaveAsync(object? sender, EventArgs e)
    {
        if (_busy)
        {
            return;
        }

        if (_photoBytes is null && string.IsNullOrWhiteSpace(_note.Text))
        {
            await DisplayAlert("Nothing to save", "Add a photo or a note first.", "OK");
            return;
        }

        _busy = true;
        _save.IsEnabled = false;
        try
        {
            var clientId = Guid.NewGuid();
            var capturedUtc = DateTimeOffset.UtcNow;
            var location = await DeviceLocation.CaptureAsync();
            var request = new MobileReportEvidenceRequest(
                clientId, CaptureForm.Clean(_note.Text), location?.Latitude, location?.Longitude, null, capturedUtc);

            await _outbox.EnqueueAsync(new OutboxItem
            {
                Id = clientId,
                Kind = EvidenceOutboxHandler.ItemKind,
                PayloadJson = JsonSerializer.Serialize(request, Json),
                PhotoBytes = _photoBytes,
                PhotoContentType = _photoContentType,
                CreatedUtc = capturedUtc,
            });
            _sync.RequestSync();

            CaptureForm.ShowSaved(_status, _connectivity.IsConnected);
            _photoBytes = null;
            _photoContentType = null;
            _preview.IsVisible = false;
            _note.Text = string.Empty;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CaptureForm.ShowError(_status);
        }
        finally
        {
            _busy = false;
            _save.IsEnabled = true;
        }
    }
}
