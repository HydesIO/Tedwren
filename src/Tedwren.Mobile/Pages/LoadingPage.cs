using Microsoft.Extensions.DependencyInjection;
using Tedwren.Mobile.Controls.Theme;
using Tedwren.Mobile.Core.Session;

namespace Tedwren.Mobile.Pages;

/// <summary>
/// The launch page (M3): attempts a biometric-gated resume of the operative session and routes to the operative
/// home when resumed, otherwise to sign-in. Wires the resume-on-launch deferred from M2.
/// </summary>
public class LoadingPage : ContentPage
{
    private readonly OperativeSessionManager _session;
    private readonly AccessTokenStore _tokens;
    private readonly IServiceProvider _services;
    private bool _routed;

    /// <summary>Builds the branded loading screen.</summary>
    public LoadingPage(OperativeSessionManager session, AccessTokenStore tokens, IServiceProvider services)
    {
        _session = session;
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
            var resume = await _session.TryResumeAsync();
            if (resume.Status == ResumeStatus.Resumed && resume.Session is not null)
            {
                _tokens.AccessToken = resume.Session.Token;
                next = new NavigationPage(_services.GetRequiredService<OperativeHomePage>());
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
