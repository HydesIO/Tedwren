using Microsoft.Extensions.DependencyInjection;
using Tedwren.Mobile.Controls.Resources;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Session;
using Tedwren.Mobile.Core.Sync;
using Tedwren.Mobile.Pages;

namespace Tedwren.Mobile;

/// <summary>
/// The MAUI application root. Merges the Tedwren design system (tokens + styles) into app resources so every
/// page and control resolves the same palette, then opens the loading page inside a navigation stack. On
/// start/resume it nudges the offline sync engine (M5), and it captures unhandled exceptions to the telemetry
/// seam (M8) and re-prompts for manager sign-in when a console session can no longer be renewed.
/// </summary>
public class App : Application
{
    private readonly IServiceProvider _services;

    /// <summary>Wires the design system, crash capture, and the manager re-sign-in prompt.</summary>
    public App(IServiceProvider services)
    {
        _services = services;
        Resources.MergedDictionaries.Add(new TedwrenStyles());

        // Best-effort crash capture through the telemetry seam (M8). Platform-specific handlers
        // (AndroidEnvironment / NSSetUncaughtExceptionHandler) are a device-testing refinement.
        var telemetry = _services.GetRequiredService<ITelemetry>();
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            telemetry.TrackError(e.ExceptionObject as Exception ?? new Exception("Unhandled"), "AppDomain.UnhandledException");
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            telemetry.TrackError(e.Exception, "TaskScheduler.UnobservedTaskException");
            e.SetObserved();
        };

        // A console session is renewed silently while its refresh token is valid; when it can't be, this fires so
        // the shell routes back to manager sign-in (M7/M8).
        _services.GetRequiredService<ManagerSessionManager>().SessionExpired += OnManagerSessionExpired;
    }

    /// <summary>Swaps the window root to the manager sign-in page when the console session expires.</summary>
    private void OnManagerSessionExpired(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var window = Windows.FirstOrDefault();
            if (window is not null)
            {
                window.Page = new NavigationPage(_services.GetRequiredService<ManagerSignInPage>());
            }
        });

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
