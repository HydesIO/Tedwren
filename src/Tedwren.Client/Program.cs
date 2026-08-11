using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Services;
using Tedwren.Client;
using Tedwren.Client.Services;
using Tedwren.UiComponents.SampleData;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// Sample-data services. Pages not yet migrated to the API consume these directly; the client mock
// organisation service also wraps them so mock mode looks identical to before.
builder.Services.AddSingleton<IShellSampleDataService, ShellSampleDataService>();
builder.Services.AddSingleton<IDashboardSampleDataService, DashboardSampleDataService>();
builder.Services.AddSingleton<IListSampleDataService, ListSampleDataService>();
builder.Services.AddSingleton<IFormSampleDataService, FormSampleDataService>();
builder.Services.AddSingleton<IDetailSampleDataService, DetailSampleDataService>();

// The active customer (company). Scoped so it initialises once per app load; onboarding sets it after
// creating the first company so the whole app scopes to the new organisation (R15).
builder.Services.AddScoped<ITenantState, TenantState>();

// Data services are served by the Tedwren Web API over HTTP. The API base URL comes from
// wwwroot/appsettings.json ("Api:BaseUrl"), falling back to the app's own origin.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<IOrganisationService, ApiOrganisationService>();
builder.Services.AddScoped<IQualificationService, ApiQualificationService>();
builder.Services.AddScoped<IUserService, ApiUserService>();
builder.Services.AddScoped<ISiteService, ApiSiteService>();
builder.Services.AddScoped<IAttendanceService, ApiAttendanceService>();
builder.Services.AddScoped<IExpiryQueryService, ApiExpiryQueryService>();
builder.Services.AddScoped<IEntitlementService, ApiEntitlementService>();
builder.Services.AddScoped<IAuditService, ApiAuditService>();
builder.Services.AddScoped<ITimesheetService, ApiTimesheetService>();
builder.Services.AddScoped<ICompliancePackService, ApiCompliancePackService>();
builder.Services.AddScoped<IInductionService, ApiInductionService>();
builder.Services.AddScoped<ISiteEntryService, ApiSiteEntryService>();

await builder.Build().RunAsync();
