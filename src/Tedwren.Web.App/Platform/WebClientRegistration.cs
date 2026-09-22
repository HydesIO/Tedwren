using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Caching;
using Tedwren.Mobile.Core.Configuration;
using Tedwren.Mobile.Core.Forms;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Session;
using Tedwren.Mobile.Core.Sync;

namespace Tedwren.Web.App.Platform;

/// <summary>
/// Wires the emulator's dependency graph. It registers the exact same <c>Tedwren.Mobile.Core</c> services the
/// MAUI head does (<c>MauiProgram.cs</c>) — typed API clients, both auth message handlers, the operative and
/// manager session managers, the sync engine and forms engine — but over browser implementations of the device
/// platform seams and WITHOUT TLS certificate pinning (the browser owns TLS). Keeping this a faithful translation
/// of <c>MauiProgram</c> is what makes the emulator behave like the app.
/// </summary>
public static class WebClientRegistration
{
    /// <summary>Registers the mobile-core services + browser platform seams against the given API base URL.</summary>
    public static IServiceCollection AddTedwrenMobileCore(this IServiceCollection services, string apiBaseUrl)
    {
        // The API root as a single injectable source. A trailing slash is required so relative request URIs
        // ("api/auth/login") resolve against the HttpClient base address.
        var baseUrl = apiBaseUrl.TrimEnd('/') + "/";
        services.AddSingleton(new TedwrenApiOptions { BaseUrl = baseUrl });

        // Browser implementations of the four Core platform seams (replacing the MAUI head's device services),
        // plus the browser read cache / outbox / draft store (the MAUI head's SQLCipher store is device-only).
        services.AddSingleton<ITelemetry, WebTelemetry>();
        services.AddSingleton<ISecureStore, WebSecureStore>();
        // The connectivity service is registered concretely too, so the emulator chrome's offline toggle can drive it.
        services.AddSingleton<WebConnectivityService>();
        services.AddSingleton<IConnectivityService>(sp => sp.GetRequiredService<WebConnectivityService>());
        services.AddSingleton<IBiometricAuthenticator, WebBiometricAuthenticator>();
        services.AddSingleton<IReadCache, WebReadCache>();
        services.AddSingleton<IOutboxStore, WebOutboxStore>();
        services.AddSingleton<IFormDraftStore, WebFormDraftStore>();
        services.AddSingleton<WebGeolocation>();

        // Login clients (no auth handler; browser handles TLS).
        services.AddTedwrenWebClient<AuthApiClient>();
        services.AddTedwrenWebClient<OperativeAuthApiClient>();

        // Operative session (device id, enrolment/demo sign-in, resume) + silent refresh for the auth handler.
        services.AddSingleton<OperativeSessionManager>();
        services.AddSingleton<ISessionRefresher>(sp => sp.GetRequiredService<OperativeSessionManager>());

        // Authenticated operative read surface: token store + auth handler + typed client + cache-then-network reader.
        services.AddSingleton<AccessTokenStore>();
        services.AddTransient<OperativeAuthMessageHandler>();
        services.AddTedwrenWebClient<OperativeApiClient>().AddHttpMessageHandler<OperativeAuthMessageHandler>();
        services.AddSingleton<OperativeDataService>();

        // Attendance (online-only), offline capture + hazard, and the connectivity-driven sync engine.
        services.AddTedwrenWebClient<AttendanceApiClient>().AddHttpMessageHandler<OperativeAuthMessageHandler>();
        services.AddTedwrenWebClient<CaptureApiClient>().AddHttpMessageHandler<OperativeAuthMessageHandler>();
        services.AddSingleton<IOutboxItemHandler, EvidenceOutboxHandler>();
        services.AddSingleton<IOutboxItemHandler, HazardOutboxHandler>();
        services.AddSingleton<SyncEngine>();

        // Forms & inspection engine: the forms client, its outbox handler (auto-discovered by SyncEngine) and drafts.
        services.AddTedwrenWebClient<FormsApiClient>().AddHttpMessageHandler<OperativeAuthMessageHandler>();
        services.AddSingleton<IOutboxItemHandler, FormsOutboxHandler>();

        // Operative induction take-flow (Gate 4): resolve-or-start, steps, server-scored quiz, finalise.
        services.AddTedwrenWebClient<InductionApiClient>().AddHttpMessageHandler<OperativeAuthMessageHandler>();

        // Manager / admin surface: the console-token session (silent refresh + expiry sink) and its typed clients.
        services.AddSingleton<ManagerSessionManager>();
        services.AddSingleton<IManagerSessionExpiredHandler>(sp => sp.GetRequiredService<ManagerSessionManager>());
        services.AddSingleton<IManagerSessionRefresher>(sp => sp.GetRequiredService<ManagerSessionManager>());
        services.AddTransient<ManagerAuthMessageHandler>();
        services.AddTedwrenWebClient<ManagerApiClient>().AddHttpMessageHandler<ManagerAuthMessageHandler>();
        services.AddTedwrenWebClient<ManagerSiteEntryApiClient>().AddHttpMessageHandler<ManagerAuthMessageHandler>();
        services.AddTedwrenWebClient<ManagerWorkforceApiClient>().AddHttpMessageHandler<ManagerAuthMessageHandler>();
        services.AddTedwrenWebClient<ManagerFormsApiClient>().AddHttpMessageHandler<ManagerAuthMessageHandler>();
        services.AddTedwrenWebClient<ManagerEvidenceApiClient>().AddHttpMessageHandler<ManagerAuthMessageHandler>();
        services.AddTedwrenWebClient<ManagerImageApiClient>().AddHttpMessageHandler<ManagerAuthMessageHandler>();
        services.AddSingleton<ManagerDataService>();

        return services;
    }

    /// <summary>
    /// Registers a typed API client bound to the configured base URL — the browser-side equivalent of the MAUI
    /// head's <c>AddTedwrenClient&lt;T&gt;</c>, but WITHOUT a TLS-pinning primary handler (the browser owns TLS).
    /// Callers chain the relevant auth message handler with <c>.AddHttpMessageHandler&lt;T&gt;()</c>.
    /// </summary>
    private static IHttpClientBuilder AddTedwrenWebClient<TClient>(this IServiceCollection services)
        where TClient : class =>
        services.AddHttpClient<TClient>((sp, client) =>
            client.BaseAddress = new Uri(sp.GetRequiredService<TedwrenApiOptions>().BaseUrl));
}
