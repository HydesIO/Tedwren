using Microsoft.Extensions.Logging;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Caching;
using Tedwren.Mobile.Core.Configuration;
using Tedwren.Mobile.Core.Forms;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Session;
using Tedwren.Mobile.Core.Sync;
using Tedwren.Mobile.Pages;
using Tedwren.Mobile.Services;

namespace Tedwren.Mobile;

/// <summary>Configures and builds the MAUI app: fonts, DI registrations, HTTP clients and platform services.</summary>
public static class MauiProgram
{
    // The Tedwren Web API root. Replace per environment (dev/staging/prod); a physical device cannot reach
    // "localhost", so this is wired from configuration before device testing (see docs/mobile-app-build.md).
    private const string ApiBaseUrl = "https://localhost:7296/";

    /// <summary>Builds the configured MAUI application.</summary>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                // Inter is the brand typeface; register once the OFL .ttf files are added (Resources/Fonts/README.md):
                // fonts.AddFont("Inter-Regular.ttf", "InterRegular");
                // fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                // fonts.AddFont("Inter-Bold.ttf", "InterBold");
            });

        // Cross-cutting (M8): the API base URL as a single injectable source, and the telemetry/crash seam.
        builder.Services.AddSingleton(new TedwrenApiOptions { BaseUrl = ApiBaseUrl });
        builder.Services.AddSingleton<ITelemetry, LoggingTelemetry>();

        // Platform services implementing the Core abstractions.
        builder.Services.AddSingleton<ISecureStore, SecureStore>();
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<IBiometricAuthenticator, BiometricAuthenticator>();

        // Login clients (no auth handler; TLS-pinned like every client, M8).
        builder.Services.AddTedwrenClient<AuthApiClient>();
        builder.Services.AddTedwrenClient<OperativeAuthApiClient>();

        // Operative session (device id, enrolment, biometric-gated resume) + silent refresh for the auth handler.
        builder.Services.AddSingleton<OperativeSessionManager>();
        builder.Services.AddSingleton<ISessionRefresher>(sp => sp.GetRequiredService<OperativeSessionManager>());

        // Authenticated operative read surface (M3): token store + auth handler + typed client + read cache.
        builder.Services.AddSingleton<AccessTokenStore>();
        builder.Services.AddTransient<OperativeAuthMessageHandler>();
        builder.Services.AddTedwrenClient<OperativeApiClient>().AddHttpMessageHandler<OperativeAuthMessageHandler>();

        // Encrypted local store (M5): SQLCipher-backed read cache + append-only outbox; from M8 its key is gated
        // behind a biometric unlock (in addition to the OS secure enclave).
        builder.Services.AddSingleton<EncryptedStore>(sp =>
            new EncryptedStore(
                sp.GetRequiredService<ISecureStore>(),
                sp.GetRequiredService<IBiometricAuthenticator>(),
                Path.Combine(FileSystem.AppDataDirectory, "tedwren.db")));
        builder.Services.AddSingleton<IReadCache>(sp => sp.GetRequiredService<EncryptedStore>());
        builder.Services.AddSingleton<IOutboxStore>(sp => sp.GetRequiredService<EncryptedStore>());
        builder.Services.AddSingleton<OperativeDataService>();

        // Attendance actions (M4): online-only sign-in/out over the same auth handler (never cached, R2/R3).
        builder.Services.AddTedwrenClient<AttendanceApiClient>().AddHttpMessageHandler<OperativeAuthMessageHandler>();

        // Offline capture & sync (M5): the capture client, the item handlers and the connectivity-driven sync engine.
        builder.Services.AddTedwrenClient<CaptureApiClient>().AddHttpMessageHandler<OperativeAuthMessageHandler>();
        builder.Services.AddSingleton<IOutboxItemHandler, EvidenceOutboxHandler>();
        builder.Services.AddSingleton<IOutboxItemHandler, HazardOutboxHandler>();
        builder.Services.AddSingleton<SyncEngine>();

        // Forms & inspection engine (M6): the forms client, its outbox handler (auto-discovered by SyncEngine) and
        // the offline draft store (in the same encrypted database).
        builder.Services.AddTedwrenClient<FormsApiClient>().AddHttpMessageHandler<OperativeAuthMessageHandler>();
        builder.Services.AddSingleton<IOutboxItemHandler, FormsOutboxHandler>();
        builder.Services.AddSingleton<IFormDraftStore>(sp => sp.GetRequiredService<EncryptedStore>());

        // Manager / admin surface (M7; M8 adds silent refresh). The console-token session is now the manager auth
        // handler's refresher (renew the 8h token) as well as its expiry sink (re-login when the refresh is gone).
        builder.Services.AddSingleton<ManagerSessionManager>();
        builder.Services.AddSingleton<IManagerSessionExpiredHandler>(sp => sp.GetRequiredService<ManagerSessionManager>());
        builder.Services.AddSingleton<IManagerSessionRefresher>(sp => sp.GetRequiredService<ManagerSessionManager>());
        builder.Services.AddTransient<ManagerAuthMessageHandler>();
        builder.Services.AddTedwrenClient<ManagerApiClient>().AddHttpMessageHandler<ManagerAuthMessageHandler>();
        builder.Services.AddTedwrenClient<ManagerSiteEntryApiClient>().AddHttpMessageHandler<ManagerAuthMessageHandler>();
        builder.Services.AddTedwrenClient<ManagerWorkforceApiClient>().AddHttpMessageHandler<ManagerAuthMessageHandler>();
        builder.Services.AddTedwrenClient<ManagerFormsApiClient>().AddHttpMessageHandler<ManagerAuthMessageHandler>();
        builder.Services.AddTedwrenClient<ManagerEvidenceApiClient>().AddHttpMessageHandler<ManagerAuthMessageHandler>();
        builder.Services.AddTedwrenClient<ManagerImageApiClient>().AddHttpMessageHandler<ManagerAuthMessageHandler>();
        builder.Services.AddSingleton<ManagerDataService>();

        // Pages.
        builder.Services.AddTransient<LoadingPage>();
        builder.Services.AddTransient<SignInPage>();
        builder.Services.AddTransient<OperativeEnrolPage>();
        builder.Services.AddTransient<OperativeHomePage>();
        builder.Services.AddTransient<SignInOutPage>();
        builder.Services.AddTransient<CaptureEvidencePage>();
        builder.Services.AddTransient<ReportHazardPage>();
        builder.Services.AddTransient<FormsInboxPage>();
        builder.Services.AddTransient<FormFillPage>();
        builder.Services.AddTransient<MyHoursPage>();
        builder.Services.AddTransient<MyCardsPage>();
        builder.Services.AddTransient<ProfilePage>();

        // Manager / admin pages (M7).
        builder.Services.AddTransient<ManagerSignInPage>();
        builder.Services.AddTransient<ManagerHomePage>();
        builder.Services.AddTransient<MusterPage>();
        builder.Services.AddTransient<SiteEntryPage>();
        builder.Services.AddTransient<OperativesPage>();
        builder.Services.AddTransient<OperativeDetailPage>();
        builder.Services.AddTransient<FormsManagePage>();
        builder.Services.AddTransient<FormAssignPage>();
        builder.Services.AddTransient<FormReviewListPage>();
        builder.Services.AddTransient<FormReviewPage>();
        builder.Services.AddTransient<EvidenceReviewPage>();
        builder.Services.AddTransient<EvidenceDetailPage>();
        builder.Services.AddTransient<ReportsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    /// <summary>
    /// Registers a typed API client bound to the configured base URL with the shared TLS-pinning primary handler
    /// (M8), so every client trusts only Tedwren's certificate. Callers chain the relevant auth message handler.
    /// </summary>
    private static IHttpClientBuilder AddTedwrenClient<TClient>(this IServiceCollection services)
        where TClient : class =>
        services.AddHttpClient<TClient>((sp, client) => client.BaseAddress = new Uri(sp.GetRequiredService<TedwrenApiOptions>().BaseUrl))
            .ConfigurePrimaryHttpMessageHandler(TlsPinning.CreateHandler);
}
