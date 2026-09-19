using Microsoft.Extensions.DependencyInjection;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Session;
using Tedwren.Mobile.Controls.Theme;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// Operative enrolment (M2): a two-step mobile-number + one-time-code flow that binds this device to the
/// operative (SF-1; buddy-punching deterrent). Real auth runs through <see cref="OperativeSessionManager"/> and
/// the API; biometric unlock takes over on subsequent launches.
/// </summary>
public class OperativeEnrolPage : ContentPage
{
    private readonly OperativeSessionManager _session;
    private readonly AccessTokenStore _tokens;
    private readonly IServiceProvider _services;

    private readonly Entry _mobileEntry;
    private readonly Entry _codeEntry;
    private readonly VerticalStackLayout _codeSection;
    private readonly Label _status;
    private readonly Button _primary;
    private string? _mobile;

    /// <summary>Builds the enrolment page over the session manager.</summary>
    public OperativeEnrolPage(OperativeSessionManager session, AccessTokenStore tokens, IServiceProvider services)
    {
        _session = session;
        _tokens = tokens;
        _services = services;
        Title = "Operative sign in";

        _mobileEntry = new Entry { Keyboard = Keyboard.Telephone, Placeholder = "Mobile number" };
        _codeEntry = new Entry { Keyboard = Keyboard.Numeric, Placeholder = "6-digit code", MaxLength = 6 };
        _codeSection = new VerticalStackLayout
        {
            IsVisible = false,
            Spacing = 8,
            Children = { new Label { Text = "Enter the code we texted you." }, _codeEntry },
        };

        _status = new Label { IsVisible = false, TextColor = Colors.White, Padding = new Thickness(12), BackgroundColor = TwPalette.Brand };

        _primary = new Button { Text = "Send code" };
        _primary.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        _primary.Clicked += OnPrimaryAsync;

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new Label { Text = "Sign in with your mobile number", FontSize = 20, FontAttributes = FontAttributes.Bold },
                    new Label { Text = "We'll text you a one-time code. Your account is tied to this one device to keep site records accurate." },
                    _mobileEntry,
                    _codeSection,
                    _status,
                    _primary,
                },
            },
        };
    }

    private async void OnPrimaryAsync(object? sender, EventArgs e)
    {
        _status.IsVisible = false;
        _primary.IsEnabled = false;
        try
        {
            if (_mobile is null)
            {
                await SendCodeAsync();
            }
            else
            {
                await VerifyAsync();
            }
        }
        catch (DeviceConflictException ex)
        {
            await DisplayAlert("Already set up", ex.Message, "OK");
        }
        catch (ApiException)
        {
            ShowError("Something went wrong. Please check your connection and try again.");
        }
        finally
        {
            _primary.IsEnabled = true;
        }
    }

    private async Task SendCodeAsync()
    {
        if (!await _session.RequestCodeAsync(_mobileEntry.Text ?? string.Empty))
        {
            ShowError("Enter a valid mobile number.");
            return;
        }

        _mobile = _mobileEntry.Text;
        _mobileEntry.IsEnabled = false;
        _codeSection.IsVisible = true;
        _primary.Text = "Verify & sign in";
    }

    private async Task VerifyAsync()
    {
        var result = await _session.EnrolAsync(_mobile!, _codeEntry.Text ?? string.Empty, DeviceInfo.Current.Name);
        switch (result.Status)
        {
            case EnrolStatus.Success:
                // Make the access token available to the authenticated API handler, then replace the navigation
                // root so the operative lands on their home and cannot navigate "back" to sign-in.
                _tokens.AccessToken = result.Session!.Token;
                if (Window is not null)
                {
                    Window.Page = new NavigationPage(_services.GetRequiredService<OperativeHomePage>());
                }

                break;
            case EnrolStatus.InvalidCode:
                ShowError("That code was wrong or has expired.");
                break;
            case EnrolStatus.InvalidNumber:
                ShowError("Enter a valid mobile number.");
                break;
        }
    }

    private void ShowError(string message)
    {
        _status.Text = message;
        _status.IsVisible = true;
    }
}
