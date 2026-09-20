using Microsoft.Extensions.Logging;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Caching;
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

        // Platform services implementing the Core abstractions.
        builder.Services.AddSingleton<ISecureStore, SecureStore>();
        builder.Services.AddSingleton<IConnectivityService, ConnectivityService>();
        builder.Services.AddSingleton<IBiometricAuthenticator, BiometricAuthenticator>();

        // API clients (typed HttpClient bound to the API root).
        builder.Services.AddHttpClient<AuthApiClient>(client => client.BaseAddress = new Uri(ApiBaseUrl));
        builder.Services.AddHttpClient<OperativeAuthApiClient>(client => client.BaseAddress = new Uri(ApiBaseUrl));

        // Operative session (device id, enrolment, biometric-gated resume) + silent refresh for the auth handler.
        builder.Services.AddSingleton<OperativeSessionManager>();
        builder.Services.AddSingleton<ISessionRefresher>(sp => sp.GetRequiredService<OperativeSessionManager>());

        // Authenticated operative read surface (M3): token store + auth handler + typed client + read cache.
        builder.Services.AddSingleton<AccessTokenStore>();
        builder.Services.AddTransient<OperativeAuthMessageHandler>();
        builder.Services.AddHttpClient<OperativeApiClient>(client => client.BaseAddress = new Uri(ApiBaseUrl))
            .AddHttpMessageHandler<OperativeAuthMessageHandler>();

        // Encrypted local store (M5): SQLCipher-backed, serving BOTH the read cache and the append-only outbox
        // (replaces the M3 unencrypted JsonFileReadCache — encryption at rest, R13-adjacent).
        builder.Services.AddSingleton<EncryptedStore>(sp =>
            new EncryptedStore(sp.GetRequiredService<ISecureStore>(), Path.Combine(FileSystem.AppDataDirectory, "tedwren.db")));
        builder.Services.AddSingleton<IReadCache>(sp => sp.GetRequiredService<EncryptedStore>());
        builder.Services.AddSingleton<IOutboxStore>(sp => sp.GetRequiredService<EncryptedStore>());
        builder.Services.AddSingleton<OperativeDataService>();

        // Attendance actions (M4): online-only sign-in/out over the same auth handler (never cached, R2/R3).
        builder.Services.AddHttpClient<AttendanceApiClient>(client => client.BaseAddress = new Uri(ApiBaseUrl))
            .AddHttpMessageHandler<OperativeAuthMessageHandler>();

        // Offline capture & sync (M5): the capture client, the item handlers and the connectivity-driven sync engine.
        builder.Services.AddHttpClient<CaptureApiClient>(client => client.BaseAddress = new Uri(ApiBaseUrl))
            .AddHttpMessageHandler<OperativeAuthMessageHandler>();
        builder.Services.AddSingleton<IOutboxItemHandler, EvidenceOutboxHandler>();
        builder.Services.AddSingleton<IOutboxItemHandler, HazardOutboxHandler>();
        builder.Services.AddSingleton<SyncEngine>();

        // Forms & inspection engine (M6): the forms client, its outbox handler (auto-discovered by SyncEngine) and
        // the offline draft store (in the same encrypted database).
        builder.Services.AddHttpClient<FormsApiClient>(client => client.BaseAddress = new Uri(ApiBaseUrl))
            .AddHttpMessageHandler<OperativeAuthMessageHandler>();
        builder.Services.AddSingleton<IOutboxItemHandler, FormsOutboxHandler>();
        builder.Services.AddSingleton<IFormDraftStore>(sp => sp.GetRequiredService<EncryptedStore>());

        // Manager / admin surface (M7): console-token session + its auth handler (no silent refresh — re-login on
        // expiry), the manager API clients over that handler, and the cache-then-network reader. Managers call the
        // existing console endpoints; the shared AccessTokenStore holds the console token (one role at a time).
        builder.Services.AddSingleton<ManagerSessionManager>();
        builder.Services.AddSingleton<IManagerSessionExpiredHandler>(sp => sp.GetRequiredService<ManagerSessionManager>());
        builder.Services.AddTransient<ManagerAuthMessageHandler>();
        builder.Services.AddHttpClient<ManagerApiClient>(client => client.BaseAddress = new Uri(ApiBaseUrl)).AddHttpMessageHandler<ManagerAuthMessageHandler>();
        builder.Services.AddHttpClient<ManagerSiteEntryApiClient>(client => client.BaseAddress = new Uri(ApiBaseUrl)).AddHttpMessageHandler<ManagerAuthMessageHandler>();
        builder.Services.AddHttpClient<ManagerWorkforceApiClient>(client => client.BaseAddress = new Uri(ApiBaseUrl)).AddHttpMessageHandler<ManagerAuthMessageHandler>();
        builder.Services.AddHttpClient<ManagerFormsApiClient>(client => client.BaseAddress = new Uri(ApiBaseUrl)).AddHttpMessageHandler<ManagerAuthMessageHandler>();
        builder.Services.AddHttpClient<ManagerEvidenceApiClient>(client => client.BaseAddress = new Uri(ApiBaseUrl)).AddHttpMessageHandler<ManagerAuthMessageHandler>();
        builder.Services.AddHttpClient<ManagerImageApiClient>(client => client.BaseAddress = new Uri(ApiBaseUrl)).AddHttpMessageHandler<ManagerAuthMessageHandler>();
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
}
