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

    /// <summary>
    /// Creates the app window with the loading page as the root; it attempts a biometric-gated session resume
    /// and routes to the operative home or the sign-in page.
    /// </summary>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var loading = _services.GetRequiredService<LoadingPage>();
        return new Window(loading) { Title = "Tedwren" };
    }
}
