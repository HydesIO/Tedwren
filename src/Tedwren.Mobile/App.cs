using Microsoft.Extensions.DependencyInjection;
using Tedwren.Mobile.Controls.Resources;
using Tedwren.Mobile.Core.Session;
using Tedwren.Mobile.Core.Sync;
using Tedwren.Mobile.Pages;

namespace Tedwren.Mobile;

/// <summary>
/// The MAUI application root. Merges the Tedwren design system (tokens + styles) into app resources so every
/// page and control resolves the same palette, then opens the sign-in entry page inside a navigation stack.
/// On start/resume it nudges the offline sync engine (M5) — but only once a session is established, so queued
/// captures are never drained (and prematurely parked) before the operative is resumed.
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

    /// <summary>Nudges a sync when the app starts (a session may already be resumable).</summary>
    protected override void OnStart() => TryRequestSync();

    /// <summary>Nudges a sync when the app returns to the foreground.</summary>
    protected override void OnResume() => TryRequestSync();

    /// <summary>Requests a background sync, but only when an operative session is active (else the outbox waits).</summary>
    private void TryRequestSync()
    {
        if (_services.GetRequiredService<AccessTokenStore>().AccessToken is null)
        {
            return;
        }

        _services.GetRequiredService<SyncEngine>().RequestSync();
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
