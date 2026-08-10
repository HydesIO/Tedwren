using Microsoft.Extensions.Options;
using Tedwren.Abstractions.Configuration;
using Tedwren.Api.Endpoints;
using Tedwren.Api.Hosting;
using Tedwren.Application;
using Tedwren.DataAccess;

// Composition root for the Tedwren Web API. This API is deliberately a separate deployable from
// the Blazor WebAssembly client and is CORS-enabled, so the same contracts can later serve a
// mobile application. The mock/database switch chooses which repositories back the (single)
// organisation business service; the service itself is identical in both modes.

var builder = WebApplication.CreateBuilder(args);

// Bind and read the mock/database switch from the "DataSource" configuration section (defaults to mock).
var dataSourceSection = builder.Configuration.GetSection(BackendOptions.SectionName);
builder.Services.Configure<BackendOptions>(dataSourceSection);
var backend = dataSourceSection.Get<BackendOptions>() ?? new BackendOptions();

// Business services; the data-source switch only changes which repositories are registered.
builder.Services.AddOrganisationCore();
builder.Services.AddQualificationCore();
builder.Services.AddExpiryCore();
builder.Services.AddUserCore();
builder.Services.AddSiteCore();
builder.Services.AddAttendanceCore();
builder.Services.AddTimesheetCore();
builder.Services.AddCompliancePackCore();
builder.Services.AddInductionCore();
builder.Services.AddSiteEntryCore();
builder.Services.AddConsoleFoundationCore();
if (backend.Mode == DataSourceMode.Database)
{
    var connectionStringName = backend.Provider == DatabaseProvider.PostgreSql ? "PostgreSql" : "SqlServer";
    var connectionString = builder.Configuration.GetConnectionString(connectionStringName) ?? string.Empty;
    builder.Services.AddSqlDataAccess(backend.Provider, connectionString);
}
else
{
    builder.Services.AddInMemoryOrganisationStore();
    builder.Services.AddInMemoryQualificationStore();
    builder.Services.AddInMemoryExpiryStore();
    builder.Services.AddInMemoryUserStore();
    builder.Services.AddInMemorySiteStore();
    builder.Services.AddInMemoryAttendanceStore();
    builder.Services.AddInMemoryTimesheetStore();
    builder.Services.AddInMemoryCompliancePackStore();
    builder.Services.AddInMemoryInductionStore();
    builder.Services.AddInMemoryConsoleFoundationStore();
}

// Runs the expiry engine on a schedule in a real deployment (gated by Jobs:SchedulerEnabled).
builder.Services.AddHostedService<ExpirySchedulerHostedService>();

builder.Services.AddOpenApi();

// CORS: permit the Blazor WASM client origin(s) declared in configuration to call the API.
const string clientCorsPolicy = "TedwrenClient";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? Array.Empty<string>();
builder.Services.AddCors(options =>
    options.AddPolicy(clientCorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    }));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(clientCorsPolicy);

// Apply database migrations at startup when running against a real database.
if (backend.Mode == DataSourceMode.Database)
{
    using var scope = app.Services.CreateScope();
    var migrationRunner = scope.ServiceProvider.GetRequiredService<Tedwren.DataAccess.Migrations.MigrationRunner>();
    await migrationRunner.RunAsync();

    // Seed the default qualification library (SF-12) and trade requirements (SF-11) — idempotent.
    var librarySeeder = scope.ServiceProvider.GetRequiredService<Tedwren.DataAccess.Qualifications.QualificationLibrarySeeder>();
    await librarySeeder.RunAsync();

    // Seed the default induction template (MC-3) — idempotent.
    var inductionSeeder = scope.ServiceProvider.GetRequiredService<Tedwren.DataAccess.Inductions.InductionTemplateSeeder>();
    await inductionSeeder.RunAsync();
}

app.MapOrganisationEndpoints();
app.MapQualificationEndpoints();
app.MapJobEndpoints();
app.MapUserEndpoints();
app.MapSiteEndpoints();
app.MapAttendanceEndpoints();
app.MapTimesheetEndpoints();
app.MapCompliancePackEndpoints();
app.MapInductionEndpoints();
app.MapSiteEntryEndpoints();
app.MapEntitlementEndpoints();
app.MapAuditEndpoints();
app.MapDecisionEndpoints();

// Liveness probe. Reports the resolved data-source mode and provider so the active configuration
// is observable at a glance, without exposing any application data.
app.MapGet("/health", (IOptions<BackendOptions> options) =>
        Results.Ok(new
        {
            status = "healthy",
            dataSource = options.Value.Mode.ToString(),
            provider = options.Value.Provider.ToString(),
            utc = DateTimeOffset.UtcNow,
        }))
    .WithName("HealthCheck");

app.Run();

/// <summary>
/// Marker for the API host's entry point. Declared so integration tests can drive the host with
/// <c>WebApplicationFactory&lt;Program&gt;</c> in later phases.
/// </summary>
public partial class Program;
