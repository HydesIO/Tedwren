using Microsoft.Extensions.Logging;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Session;
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

        // Operative session (device id, enrolment, biometric-gated resume).
        builder.Services.AddSingleton<OperativeSessionManager>();

        // Pages.
        builder.Services.AddTransient<SignInPage>();
        builder.Services.AddTransient<OperativeEnrolPage>();
        builder.Services.AddTransient<OperativeHomePage>();
        builder.Services.AddTransient<ManagerHomePage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
