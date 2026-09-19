using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Geofencing;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// Operative attendance sign-in / sign-out (M4). Online-only and never queued (R2/R3): when offline it blocks
/// with a reminder that the browser link still works (R1). Location is captured (SF-15) and checked against the
/// cached site boundary as an advisory hint (SF-14) — the server stays authoritative. Outcomes use
/// subcontractor-safe wording ("recorded"/"site-ready", never "permitted"/"denied", R18/SUB-12) and surface
/// being already signed in elsewhere (SF-18). Times are UTC on the wire and shown in UK-local (R11).
/// </summary>
public class SignInOutPage : ContentPage
{
    private readonly OperativeDataService _data;
    private readonly AttendanceApiClient _attendance;
    private readonly IConnectivityService _connectivity;

    private readonly Picker _sitePicker = new() { Title = "Choose a site" };
    private readonly Label _statusLine = new() { FontAttributes = FontAttributes.Bold, Text = "Loading…" };
    private readonly Label _hintLine = new() { FontSize = 13, IsVisible = false };
    private readonly Label _resultBanner = new() { IsVisible = false, Padding = new Thickness(12) };
    private readonly Button _signIn;
    private readonly Button _signOut;

    private IReadOnlyList<MobileSiteDto> _sites = Array.Empty<MobileSiteDto>();
    private Guid? _currentSiteId;
    private bool _busy;

    /// <summary>Builds the attendance page over the cached site list, the attendance client and connectivity.</summary>
    public SignInOutPage(OperativeDataService data, AttendanceApiClient attendance, IConnectivityService connectivity)
    {
        _data = data;
        _attendance = attendance;
        _connectivity = connectivity;
        Title = "Sign in / out";

        _hintLine.SetAppThemeColor(Label.TextColorProperty, TwPalette.TextSecondaryLight, TwPalette.TextSecondaryDark);

        _signIn = new Button { Text = "Sign in" };
        _signIn.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        _signIn.Clicked += OnSignInAsync;

        _signOut = new Button { Text = "Sign out" };
        _signOut.SetDynamicResource(VisualElement.StyleProperty, "TwSecondaryButton");
        _signOut.Clicked += OnSignOutAsync;

        var guidance = new TwCard
        {
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = "Sign in when you arrive on site", FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "You must be online to sign in or out — it keeps site records accurate. If you can't get online, the browser link still works.",
                        FontSize = 13,
                    },
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
                    new Label { Text = "Attendance", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    guidance,
                    new TwCard { Content = _statusLine },
                    new Label { Text = "Site", FontAttributes = FontAttributes.Bold },
                    _sitePicker,
                    _hintLine,
                    _resultBanner,
                    _signIn,
                    _signOut,
                },
            },
        };
    }

    /// <summary>Loads the cached site list and refreshes the live status each time the page appears.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadSitesAsync();
        await RefreshStatusAsync();
    }

    /// <summary>Populates the site picker from the offline-cached site list (loaded once).</summary>
    private async Task LoadSitesAsync()
    {
        if (_sites.Count > 0)
        {
            return;
        }

        var sites = await _data.GetSitesAsync();
        _sites = sites ?? Array.Empty<MobileSiteDto>();
        _sitePicker.ItemsSource = _sites.Select(s => s.Name).ToList();
    }

    /// <summary>Reads the operative's current on-site state (online-only) into the status line (SF-18).</summary>
    private async Task RefreshStatusAsync()
    {
        if (!_connectivity.IsConnected)
        {
            _currentSiteId = null;
            _statusLine.Text = "Offline — connect to sign in or out";
            return;
        }

        try
        {
            var current = await _attendance.GetCurrentAsync();
            _currentSiteId = current?.SiteId;
            _statusLine.Text = current is null
                ? "Not signed in"
                : $"On site: {current.SiteName} — since {UkTime.Format(current.SinceUtc, "ddd dd MMM HH:mm")}";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _statusLine.Text = "Couldn't check your current status";
        }
    }

    /// <summary>Handles a sign-in: online guard, location capture + geofence hint, then the recorded outcome.</summary>
    private async void OnSignInAsync(object? sender, EventArgs e)
    {
        if (_busy || !EnsureOnline())
        {
            return;
        }

        if (SelectedSite() is not { } site)
        {
            await DisplayAlert("Choose a site", "Pick the site you're signing in at.", "OK");
            return;
        }

        await RunAsync(async () =>
        {
            var location = await CaptureLocationAsync();
            ShowGeofenceHint(site, location);

            var result = await _attendance.SignInAsync(
                new MobileSignInRequest(site.Id, null, location?.Latitude, location?.Longitude));

            if (result.SignedIn && string.Equals(result.Outcome, "Flagged", StringComparison.OrdinalIgnoreCase))
            {
                ShowResult($"Recorded (flagged for review): {result.Reason}", Severity.Warning);
            }
            else if (result.SignedIn)
            {
                ShowResult("Recorded — you're signed in. Site-ready.", Severity.Success);
            }
            else if (result.SignedInElsewhere is { } elsewhere)
            {
                ShowResult($"You're already signed in at {elsewhere}. Sign out there first.", Severity.Warning);
            }
            else
            {
                ShowResult($"Not recorded as on site: {result.Reason}", Severity.Danger);
            }

            await RefreshStatusAsync();
        });
    }

    /// <summary>Handles a sign-out from the site the operative is currently signed in at (or the picked site).</summary>
    private async void OnSignOutAsync(object? sender, EventArgs e)
    {
        if (_busy || !EnsureOnline())
        {
            return;
        }

        await RunAsync(async () =>
        {
            // Sign out of the site the operative is actually on; fall back to the picker when the status is unknown.
            var current = await _attendance.GetCurrentAsync();
            var siteId = current?.SiteId ?? _currentSiteId ?? SelectedSite()?.Id;
            if (siteId is not { } id)
            {
                ShowResult("You're not signed in anywhere.", Severity.Warning);
                await RefreshStatusAsync();
                return;
            }

            var location = await CaptureLocationAsync();
            var result = await _attendance.SignOutAsync(new MobileSignOutRequest(id, location?.Latitude, location?.Longitude));

            ShowResult(
                result.SignedOut
                    ? $"Signed out. Time on site: {result.DurationHours:0.##} h."
                    : $"Couldn't sign out: {result.Reason}",
                result.SignedOut ? Severity.Success : Severity.Warning);

            await RefreshStatusAsync();
        });
    }

    /// <summary>The site currently chosen in the picker, or null when none is selected.</summary>
    private MobileSiteDto? SelectedSite() =>
        _sitePicker.SelectedIndex >= 0 && _sitePicker.SelectedIndex < _sites.Count ? _sites[_sitePicker.SelectedIndex] : null;

    /// <summary>Blocks the action when offline (R2/R3), pointing the operative at the always-available browser path (R1).</summary>
    private bool EnsureOnline()
    {
        if (_connectivity.IsConnected)
        {
            return true;
        }

        ShowResult("You must be online to sign in — the browser link still works.", Severity.Warning);
        return false;
    }

    /// <summary>Captures the device location for the attempt, or null when it is unavailable/denied (SF-15).</summary>
    private static async Task<Location?> CaptureLocationAsync()
    {
        try
        {
            return await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or FeatureNotEnabledException or PermissionException or OperationCanceledException)
        {
            return null;
        }
    }

    /// <summary>Shows an advisory geofence hint (SF-14) before submit; the server remains authoritative (R2/R3).</summary>
    private void ShowGeofenceHint(MobileSiteDto site, Location? location)
    {
        if (location is null)
        {
            _hintLine.IsVisible = false;
            return;
        }

        var hint = GeofenceHint.Evaluate(site.Boundary, location.Latitude, location.Longitude);
        if (!hint.HasBoundary)
        {
            _hintLine.IsVisible = false;
            return;
        }

        _hintLine.IsVisible = true;
        _hintLine.Text = hint.Inside
            ? "You're inside the site boundary."
            : $"You look about {hint.DistanceMetres:0} m outside the boundary — the site may still record it.";
    }

    /// <summary>Runs a busy-guarded action, disabling the buttons and surfacing an error banner on failure.</summary>
    private async Task RunAsync(Func<Task> action)
    {
        _busy = true;
        _signIn.IsEnabled = false;
        _signOut.IsEnabled = false;
        try
        {
            await action();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ShowResult("Something went wrong. Check your connection and try again.", Severity.Danger);
        }
        finally
        {
            _busy = false;
            _signIn.IsEnabled = true;
            _signOut.IsEnabled = true;
        }
    }

    /// <summary>Shows the outcome banner in the token colour for its severity, readable in both themes.</summary>
    private void ShowResult(string text, Severity severity)
    {
        _resultBanner.Text = text;
        _resultBanner.IsVisible = true;
        var (paleLight, paleDark, solidLight, solidDark) = severity switch
        {
            Severity.Success => (TwPalette.SuccessPaleLight, TwPalette.SuccessPaleDark, TwPalette.SuccessLight, TwPalette.SuccessDark),
            Severity.Danger => (TwPalette.DangerPaleLight, TwPalette.DangerPaleDark, TwPalette.DangerLight, TwPalette.DangerDark),
            _ => (TwPalette.WarningPaleLight, TwPalette.WarningPaleDark, TwPalette.WarningLight, TwPalette.WarningDark),
        };
        _resultBanner.SetAppThemeColor(VisualElement.BackgroundColorProperty, paleLight, paleDark);
        _resultBanner.SetAppThemeColor(Label.TextColorProperty, solidLight, solidDark);
    }

    /// <summary>Outcome severity for the result banner colour.</summary>
    private enum Severity
    {
        Success,
        Warning,
        Danger,
    }
}
