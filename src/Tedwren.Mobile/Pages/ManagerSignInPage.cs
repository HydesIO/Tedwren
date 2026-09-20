using Microsoft.Extensions.DependencyInjection;
using Tedwren.Mobile.Controls.Controls;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Session;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// Manager / admin sign-in (M7): console email + password against the existing <c>/api/auth/login</c> surface. On
/// success the console session is set live and persisted (biometric-gated resume next launch) and the manager lands
/// on the manager home, replacing the navigation root so they cannot go "back" to sign-in. Operatives use the
/// separate mobile-number + one-time-code flow (<see cref="OperativeEnrolPage"/>).
/// </summary>
public class ManagerSignInPage : ContentPage
{
    private readonly ManagerSessionManager _session;
    private readonly IServiceProvider _services;

    private readonly Entry _email = new() { Keyboard = Keyboard.Email, Placeholder = "Work email" };
    private readonly Entry _password = new() { IsPassword = true, Placeholder = "Password" };
    private readonly Label _status = new() { IsVisible = false, Padding = new Thickness(12) };
    private readonly Button _signIn;

    /// <summary>Builds the manager sign-in page over the manager session manager.</summary>
    public ManagerSignInPage(ManagerSessionManager session, IServiceProvider services)
    {
        _session = session;
        _services = services;
        Title = "Manager sign in";

        _signIn = new Button { Text = "Sign in" };
        _signIn.SetDynamicResource(VisualElement.StyleProperty, "TwPrimaryButton");
        _signIn.Clicked += OnSignInAsync;

        var guidance = new TwCard
        {
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = "Sign in with your console account", FontAttributes = FontAttributes.Bold },
                    new Label
                    {
                        Text = "Use the same email and password as the Tedwren web console. Operatives sign in with their mobile number instead.",
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
                    new Label { Text = "Manager / admin", FontSize = 22, FontAttributes = FontAttributes.Bold },
                    guidance,
                    _email,
                    _password,
                    _status,
                    _signIn,
                },
            },
        };
    }

    private async void OnSignInAsync(object? sender, EventArgs e)
    {
        _status.IsVisible = false;
        _signIn.IsEnabled = false;
        try
        {
            var result = await _session.LoginAsync(_email.Text?.Trim() ?? string.Empty, _password.Text ?? string.Empty);
            switch (result.Status)
            {
                case ManagerLoginStatus.Success:
                    if (Window is not null)
                    {
                        Window.Page = new NavigationPage(_services.GetRequiredService<ManagerHomePage>());
                    }

                    break;
                case ManagerLoginStatus.InvalidCredentials:
                    ShowError("That email or password wasn't recognised.");
                    break;
                default:
                    ShowError("Couldn't sign in. Check your connection and try again.");
                    break;
            }
        }
        finally
        {
            _signIn.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        _status.Text = message;
        _status.IsVisible = true;
        _status.SetAppThemeColor(Label.TextColorProperty, TwPalette.DangerLight, TwPalette.DangerDark);
    }
}
