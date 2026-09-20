using Microsoft.Extensions.DependencyInjection;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Session;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The launch page (M3, extended in M7): attempts a biometric-gated resume of a stored session — the operative
/// session first, then the manager (console) session — and routes to the matching role home, otherwise to sign-in.
/// Wires the resume-on-launch deferred from M2.
/// </summary>
public class LoadingPage : ContentPage
{
    private readonly OperativeSessionManager _session;
    private readonly ManagerSessionManager _manager;
    private readonly AccessTokenStore _tokens;
    private readonly IServiceProvider _services;
    private bool _routed;

    /// <summary>Builds the branded loading screen.</summary>
    public LoadingPage(OperativeSessionManager session, ManagerSessionManager manager, AccessTokenStore tokens, IServiceProvider services)
    {
        _session = session;
        _manager = manager;
        _tokens = tokens;
        _services = services;
        BackgroundColor = TwPalette.Brand;

        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 16,
            Children =
            {
                new Label { Text = "Tedwren", FontSize = 34, FontAttributes = FontAttributes.Bold, TextColor = TwPalette.OnBrand, HorizontalOptions = LayoutOptions.Center },
                new ActivityIndicator { IsRunning = true, Color = TwPalette.OnBrand },
            },
        };
    }

    /// <summary>Resolves the session once and swaps the window root to the resulting home / sign-in page.</summary>
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_routed)
        {
            return;
        }

        _routed = true;

        Page next;
        try
        {
            var operative = await _session.TryResumeAsync();
            if (operative.Status == ResumeStatus.Resumed && operative.Session is not null)
            {
                _tokens.AccessToken = operative.Session.Token;
                next = new NavigationPage(_services.GetRequiredService<OperativeHomePage>());
            }
            else if ((await _manager.TryResumeAsync()).Status == ResumeStatus.Resumed)
            {
                // The manager resume sets the access token itself (no operative-style token hand-off).
                next = new NavigationPage(_services.GetRequiredService<ManagerHomePage>());
            }
            else
            {
                next = new NavigationPage(_services.GetRequiredService<SignInPage>());
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Launch must never crash on a transient failure — fall back to sign-in.
            next = new NavigationPage(_services.GetRequiredService<SignInPage>());
        }

        if (Window is not null)
        {
            Window.Page = next;
        }
    }
}
