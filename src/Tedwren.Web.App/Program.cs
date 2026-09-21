using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Web.App;
using Tedwren.Web.App.Platform;
using Tedwren.Web.App.Shell;

// The Tedwren mobile emulator: a browser host that renders the native app's screens (re-created in Blazor) over
// the SAME Tedwren.Mobile.Core logic and the SAME Web API endpoints, for device-free testing. See
// docs/web-app-emulator.md.
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The API base URL comes from wwwroot/appsettings.json ("Api:BaseUrl"), falling back to the app's own origin.
// The API's Cors:AllowedOrigins must include this emulator's origin (see docs/web-app-emulator.md).
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;

// Reuse the mobile app's own services (typed API clients, session managers, sync engine, forms engine) with
// browser implementations of the four device platform seams — so the emulator behaves like-for-like with the app.
builder.Services.AddTedwrenMobileCore(apiBaseUrl);

// Emulator chrome state: the selected device size (phone/tablet), light/dark theme and the connectivity toggle.
builder.Services.AddSingleton<EmulatorState>();

// The emulator's in-memory "who is signed in" for the UI (tokens live in the Core session managers).
builder.Services.AddSingleton<SessionContext>();

var host = builder.Build();

// Global crash hooks (mirroring the app's M8 telemetry): route unhandled exceptions to telemetry so a background
// failure is recorded, not lost. The ErrorBoundary in MainLayout catches render/UI exceptions; these catch the rest.
var telemetry = host.Services.GetRequiredService<ITelemetry>();
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    if (e.ExceptionObject is Exception ex)
    {
        telemetry.TrackError(ex, "unhandled");
    }
};
TaskScheduler.UnobservedTaskException += (_, e) =>
{
    telemetry.TrackError(e.Exception, "unobserved-task");
    e.SetObserved();
};

await host.RunAsync();
