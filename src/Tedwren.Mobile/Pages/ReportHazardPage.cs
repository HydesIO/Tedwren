using System.Text.Json;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Sync;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// Report a hazard / near-miss offline (M5), reusing the HSE hazard domain (PRD §8.2). Gated by the company's
/// <c>hse</c> module: when the cached dashboard says it is off, the form is hidden and a short note is shown instead
/// (generic evidence capture stays available on its own page). Offline-first — the save is queued to the encrypted
/// outbox and synced when online; the capture is stamped in UTC (R11).
/// </summary>
public class ReportHazardPage : ContentPage
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly (string Label, string Value)[] Kinds =
    {
        ("Near miss", "NearMiss"), ("Hazard", "Hazard"), ("Unsafe act", "UnsafeAct"), ("Unsafe condition", "UnsafeCondition"),
    };

    private static readonly string[] Severities = { "Low", "Medium", "High" };

    private readonly IOutboxStore _outbox;
    private readonly SyncEngine _sync;
    private readonly IConnectivityService _connectivity;
    private readonly OperativeDataService _data;

    private readonly Picker _kind = new() { Title = "Type" };
    private readonly Picker _severity = new() { Title = "Severity" };
    private readonly Editor _description = new() { Placeholder = "Describe what you saw", HeightRequest = 100 };
    private readonly Entry _location = new() { Placeholder = "Location (e.g. Level 3, east core)" };
    private readonly Image _preview = new() { IsVisible = false, HeightRequest = 160, Aspect = Aspect.AspectFit };
    private readonly Button _photoButton;
    private readonly Button _save;
    private readonly Label _status = new() { IsVisible = false, Padding = new Thickness(12) };
    private readonly VerticalStackLayout _form;
    private readonly Label _unavailable = new()
    {
        IsVisible = false,
        Padding = new Thickness(12),
        Text = "Hazard reporting isn't enabled for your organisation. You can still capture evidence.",
    };

    private byte[]? _photoBytes;
    private string? _photoContentType;
    private bool _busy;
    private bool _checked;

    /// <summary>Builds the hazard-report page over the outbox, sync engine, connectivity and cached dashboard.</summary>
    public ReportHazardPage(IOutboxStore outbox, SyncEngine sync, IConnectivityService connectivity, OperativeDataService data)
    {
        _outbox = outbox;
        _sync = sync;
        _connectivity = connectivity;
        _data = data;
        Title = "Report hazard";

        _kind.ItemsSource = Kinds.Select(k => k.Label).ToList();
        _kind.SelectedIndex = 0;
        _severity.ItemsSource = Severities;
        _severity.SelectedIndex = 1;

        _photoButton = new Button { Text = "Take photo" };
        _photoButton.SetDynamicResource(VisualElement.StyleProperty, "TwSecondaryButton");
        _photoButton.Clicked += OnTakePhotoAsync;

        _save = new Button { Text = "Report hazard" };
        _save.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        _save.Clicked += OnSaveAsync;

        var guidance = new TwCard
        {
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = "Report a hazard or near-miss", FontAttributes = FontAttributes.Bold },
                    new Label { Text = "Add a photo, type and description. It's saved on your device, uploads when you're online, and your manager reviews it.", FontSize = 13 },
                },
            },
        };

        _form = new VerticalStackLayout
        {
            Spacing = 16,
            Children =
            {
                guidance,
                new Label { Text = "Type", FontAttributes = FontAttributes.Bold }, _kind,
                new Label { Text = "Severity", FontAttributes = FontAttributes.Bold }, _severity,
                _photoButton, _preview,
                new Label { Text = "Description", FontAttributes = FontAttributes.Bold }, _description,
                _location,
                _status,
                _save,
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
                    new Label { Text = "Hazard", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    _unavailable,
                    _form,
                },
            },
        };
    }

    /// <summary>Checks the module entitlement once and shows/hides the form accordingly.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_checked)
        {
            return;
        }

        _checked = true;
        var dashboard = await _data.GetDashboardAsync();
        var canReport = dashboard?.CanReportHazards ?? false;
        _form.IsVisible = canReport;
        _unavailable.IsVisible = !canReport;
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

        if (string.IsNullOrWhiteSpace(_description.Text))
        {
            await DisplayAlert("Description needed", "Describe what you saw.", "OK");
            return;
        }

        _busy = true;
        _save.IsEnabled = false;
        try
        {
            var clientId = Guid.NewGuid();
            var capturedUtc = DateTimeOffset.UtcNow;
            var location = await DeviceLocation.CaptureAsync();
            var request = new MobileReportHazardRequest(
                clientId,
                Kinds[Math.Max(_kind.SelectedIndex, 0)].Value,
                _description.Text!.Trim(),
                CaptureForm.Clean(_location.Text),
                location?.Latitude,
                location?.Longitude,
                null,
                Severities[Math.Max(_severity.SelectedIndex, 0)],
                null,
                capturedUtc);

            await _outbox.EnqueueAsync(new OutboxItem
            {
                Id = clientId,
                Kind = HazardOutboxHandler.ItemKind,
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
            _description.Text = string.Empty;
            _location.Text = string.Empty;
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
