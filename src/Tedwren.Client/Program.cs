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

// The mock/API data-source switch (wwwroot/appsettings.json "DataSource:Mode"). The UI injects the
// same IOrganisationService in both modes — switching requires no component or page changes.
Enum.TryParse<ClientDataSourceMode>(builder.Configuration["DataSource:Mode"], ignoreCase: true, out var dataSourceMode);
if (dataSourceMode == ClientDataSourceMode.Api)
{
    var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? builder.HostEnvironment.BaseAddress;
    builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
    builder.Services.AddScoped<IOrganisationService, ApiOrganisationService>();
    builder.Services.AddScoped<IQualificationService, ApiQualificationService>();
    builder.Services.AddScoped<IUserService, ApiUserService>();
    builder.Services.AddScoped<ISiteService, ApiSiteService>();
    builder.Services.AddScoped<IAttendanceService, ApiAttendanceService>();
    builder.Services.AddScoped<IAuditService, ApiAuditService>();
    builder.Services.AddScoped<ITimesheetService, ApiTimesheetService>();
    builder.Services.AddScoped<ICompliancePackService, ApiCompliancePackService>();
    builder.Services.AddScoped<IInductionService, ApiInductionService>();
    builder.Services.AddScoped<ISiteEntryService, ApiSiteEntryService>();
}
else
{
    builder.Services.AddScoped<IOrganisationService, ClientMockOrganisationService>();
    builder.Services.AddScoped<IQualificationService, ClientMockQualificationService>();
    builder.Services.AddScoped<IUserService, ClientMockUserService>();
    builder.Services.AddScoped<ISiteService, ClientMockSiteService>();
    builder.Services.AddScoped<IAttendanceService, ClientMockAttendanceService>();
    builder.Services.AddScoped<IAuditService, ClientMockAuditService>();
    builder.Services.AddScoped<ITimesheetService, ClientMockTimesheetService>();
    builder.Services.AddScoped<ICompliancePackService, ClientMockCompliancePackService>();
    builder.Services.AddScoped<IInductionService, ClientMockInductionService>();
    builder.Services.AddScoped<ISiteEntryService, ClientMockSiteEntryService>();
}

await builder.Build().RunAsync();
