using Microsoft.Extensions.DependencyInjection;
using Tedwren.Mobile.Controls.Resources;
using Tedwren.Mobile.Pages;

namespace Tedwren.Mobile;

/// <summary>
/// The MAUI application root. Merges the Tedwren design system (tokens + styles) into app resources so every
/// page and control resolves the same palette, then opens the sign-in entry page inside a navigation stack.
/// </summary>
public class App : Application
{
    private readonly IServiceProvider _services;

    /// <summary>Wires the design system and captures the service provider used to resolve the first page.</summary>
    public App(IServiceProvider services)
    {
        _services = services;
        Resources.MergedDictionaries.Add(new TedwrenStyles());
    }

    /// <summary>Creates the app window with the sign-in page as the navigation root.</summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var signIn = _services.GetRequiredService<SignInPage>();
        return new Window(new NavigationPage(signIn)) { Title = "Tedwren" };
    }
}
