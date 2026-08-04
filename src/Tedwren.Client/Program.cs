using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Tedwren.Client;
using Tedwren.UiComponents.SampleData;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// Sample-data services (Plan & Scope §2). Swappable later for API-backed
// implementations without changing any component.
builder.Services.AddSingleton<IShellSampleDataService, ShellSampleDataService>();

await builder.Build().RunAsync();
