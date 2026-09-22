using System.Text.Json;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Sync;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// Add an accreditation / qualification card offline (Gate 3): pick the type, photograph the card, add its dates, and
/// queue it to the encrypted outbox to sync when back online. Stored for a manager to confirm (SF-6), never
/// auto-accepted. Mirrors the emulator's AddAccreditation page — same outbox enqueue, so the sync path is shared.
/// The capture is stamped in UTC (R11); the outbox id doubles as the idempotency key (R4/R16).
/// </summary>
public class AddCardPage : ContentPage
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly CaptureApiClient _capture;
    private readonly IOutboxStore _outbox;
    private readonly SyncEngine _sync;
    private readonly IConnectivityService _connectivity;

    private readonly Picker _typePicker = new() { Title = "Select a card type" };
    private readonly Entry _cardNumber = new() { Placeholder = "Card number (optional)" };
    private readonly Entry _holderName = new() { Placeholder = "Name on card (optional)" };
    private readonly Switch _hasIssued = new();
    private readonly DatePicker _issued = new() { IsEnabled = false };
    private readonly Switch _hasExpiry = new();
    private readonly DatePicker _expiry = new() { IsEnabled = false };
    private readonly Image _preview = new() { IsVisible = false, HeightRequest = 160, Aspect = Aspect.AspectFit };
    private readonly Button _photoButton;
    private readonly Button _save;
    private readonly Label _status = new() { IsVisible = false, Padding = new Thickness(12) };

    private IReadOnlyList<QualificationTypeDto> _types = Array.Empty<QualificationTypeDto>();
    private byte[]? _photoBytes;
    private string? _photoContentType;
    private bool _loaded;
    private bool _busy;

    /// <summary>Builds the add-accreditation page over the capture client, outbox, sync engine and connectivity.</summary>
    public AddCardPage(CaptureApiClient capture, IOutboxStore outbox, SyncEngine sync, IConnectivityService connectivity)
    {
        _capture = capture;
        _outbox = outbox;
        _sync = sync;
        _connectivity = connectivity;
        Title = "Add accreditation";

        _hasIssued.Toggled += (_, e) => _issued.IsEnabled = e.Value;
        _hasExpiry.Toggled += (_, e) => _expiry.IsEnabled = e.Value;

        _photoButton = new Button { Text = "Take photo" };
        _photoButton.SetDynamicResource(VisualElement.StyleProperty, "TwSecondaryButton");
        _photoButton.Clicked += OnTakePhotoAsync;

        _save = new Button { Text = "Save accreditation" };
        _save.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        _save.Clicked += OnSaveAsync;

        var guidance = new TwCard
        {
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = "Add an accreditation", FontAttributes = FontAttributes.Bold },
                    new Label { Text = "Pick the card type, take a clear photo of the card and add its dates. It's saved on your device and uploads when you're online. Your manager confirms it before it counts.", FontSize = 13 },
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
                    new Label { Text = "Accreditation", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    guidance,
                    new Label { Text = "Card type", FontAttributes = FontAttributes.Bold },
                    _typePicker,
                    _photoButton,
                    _preview,
                    _cardNumber,
                    _holderName,
                    new HorizontalStackLayout { Spacing = 8, Children = { _hasIssued, new Label { Text = "Issued date", VerticalOptions = LayoutOptions.Center } } },
                    _issued,
                    new HorizontalStackLayout { Spacing = 8, Children = { _hasExpiry, new Label { Text = "Expiry date", VerticalOptions = LayoutOptions.Center } } },
                    _expiry,
                    _status,
                    _save,
                },
            },
        };
    }

    /// <summary>Loads the qualification-type library into the picker once when the page first appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        try
        {
            _types = await _capture.GetCardTypesAsync();
            _typePicker.ItemsSource = _types.Select(t => t.Name).ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _status.IsVisible = true;
            _status.Text = "Couldn't load the card types. Check your connection and try again.";
        }
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

        if (_typePicker.SelectedIndex < 0 || _typePicker.SelectedIndex >= _types.Count)
        {
            await DisplayAlert("Pick a card type", "Choose the accreditation type first.", "OK");
            return;
        }

        if (_photoBytes is null)
        {
            await DisplayAlert("Take a photo", "Take a photo of the card first.", "OK");
            return;
        }

        _busy = true;
        _save.IsEnabled = false;
        try
        {
            var clientId = Guid.NewGuid();
            var capturedUtc = DateTimeOffset.UtcNow;
            DateOnly? issuedOn = _hasIssued.IsToggled ? DateOnly.FromDateTime(_issued.Date) : null;
            DateOnly? expiresOn = _hasExpiry.IsToggled ? DateOnly.FromDateTime(_expiry.Date) : null;
            var request = new MobileCaptureCardRequest(
                clientId,
                _types[_typePicker.SelectedIndex].Id,
                CaptureForm.Clean(_cardNumber.Text),
                CaptureForm.Clean(_holderName.Text),
                issuedOn,
                expiresOn,
                null,
                capturedUtc);

            await _outbox.EnqueueAsync(new OutboxItem
            {
                Id = clientId,
                Kind = CardOutboxHandler.ItemKind,
                PayloadJson = JsonSerializer.Serialize(request, Json),
                PhotoBytes = _photoBytes,
                PhotoContentType = _photoContentType,
                CreatedUtc = capturedUtc,
            });
            _sync.RequestSync();

            CaptureForm.ShowSaved(_status, _connectivity.IsConnected);
            _typePicker.SelectedIndex = -1;
            _cardNumber.Text = string.Empty;
            _holderName.Text = string.Empty;
            _hasIssued.IsToggled = false;
            _hasExpiry.IsToggled = false;
            _photoBytes = null;
            _photoContentType = null;
            _preview.IsVisible = false;
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
