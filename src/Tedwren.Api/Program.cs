using Microsoft.Extensions.Options;
using Tedwren.Abstractions.Configuration;
using Tedwren.Api.Endpoints;
using Tedwren.Api.Hosting;
using Tedwren.Application;
using Tedwren.DataAccess;

// Composition root for the Tedwren Web API. This API is deliberately a separate deployable from
// the Blazor WebAssembly client and is CORS-enabled, so the same contracts can later serve a
// mobile application. It runs against the database; the in-memory repositories are a test-only double.

var builder = WebApplication.CreateBuilder(args);

// Bind the "DataSource" section (defaults to Database; InMemory is selected only by the API test host).
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
builder.Services.AddFormCore();
builder.Services.AddSiteEntryCore();
builder.Services.AddConsoleFoundationCore();
builder.Services.AddBillingCore();
builder.Services.AddWorkforceCore();
builder.Services.AddDashboardCore();
builder.Services.AddReferenceDataCore();
builder.Services.AddSettingsCore();
builder.Services.AddPermitCore();
builder.Services.AddOnboardingCore();
builder.Services.AddAuthCore();

// Email delivery (PRD-Phase 7): bind the "Email" section and register the branded HTML template renderer.
// The provider defaults to Outbox (nothing sends); when configured for Resend with an API key, a typed
// HttpClient sender overrides the Application-layer OutboxEmailSender — mirroring how the JWT ITokenIssuer
// is provided by the API composition root rather than the Application layer.
var emailOptions = builder.Configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
builder.Services.AddSingleton(emailOptions);
builder.Services.AddSingleton<Tedwren.Abstractions.Notifications.IEmailTemplateRenderer,
    Tedwren.Application.Notifications.Email.EmailTemplateRenderer>();
if (emailOptions.Provider == EmailProvider.Resend && !string.IsNullOrWhiteSpace(emailOptions.ApiKey))
{
    builder.Services.AddHttpClient<Tedwren.Abstractions.Notifications.IEmailSender,
        Tedwren.Application.Notifications.ResendEmailSender>(client =>
    {
        client.BaseAddress = new Uri(emailOptions.ApiBaseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", emailOptions.ApiKey);
    });
}

// Direct-debit billing (GoCardless; admin area). Bind the "GoCardless" section and, when an access token is
// configured, register the real transport as a typed HttpClient (base address + Bearer token + version
// header). With no token the Application-layer UnconfiguredGoCardlessClient default stands, so read surfaces
// still work and collection actions fail with a clear "not configured" message — mirroring the Resend override.
var goCardlessOptions = builder.Configuration.GetSection(GoCardlessOptions.SectionName).Get<GoCardlessOptions>() ?? new GoCardlessOptions();
builder.Services.AddSingleton(goCardlessOptions);
if (!string.IsNullOrWhiteSpace(goCardlessOptions.AccessToken))
{
    builder.Services.AddHttpClient<Tedwren.Application.Billing.IGoCardlessClient,
        Tedwren.Application.Billing.GoCardlessClient>(client =>
    {
        client.BaseAddress = new Uri(goCardlessOptions.ApiBaseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", goCardlessOptions.AccessToken);
        client.DefaultRequestHeaders.Add("GoCardless-Version", goCardlessOptions.ApiVersion);
    });
}

// Bootstrap admin so a fresh install can be signed into (idempotent). Credentials from the "Seed" section.
var seedAdminOptions = builder.Configuration.GetSection(Tedwren.Application.Auth.SeedAdminOptions.SectionName)
    .Get<Tedwren.Application.Auth.SeedAdminOptions>() ?? new Tedwren.Application.Auth.SeedAdminOptions();
builder.Services.AddSingleton(seedAdminOptions);
builder.Services.AddScoped<Tedwren.Application.Auth.AdminUserSeeder>();

// Authentication (D1): JWT bearer. The current operator is resolved from the request claims — the real
// signed-in user with a tenant company id (R15), replacing the former config stub.
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<Tedwren.Application.Auth.ITokenIssuer, Tedwren.Api.Auth.JwtTokenIssuer>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Tedwren.Abstractions.Services.ICurrentUserService, Tedwren.Api.Auth.ClaimsCurrentUserService>();

// The API test host authenticates every request as an Administrator via a bypass scheme (Auth:TestBypass),
// so the end-to-end tests exercise authorised endpoints without minting JWTs. Real deployments use JWT.
var testBypass = builder.Configuration.GetValue<bool>("Auth:TestBypass");
var authBuilder = builder.Services.AddAuthentication(testBypass
    ? Tedwren.Api.Auth.TestBypassAuthenticationHandler.SchemeName
    : Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme);
if (testBypass)
{
    authBuilder.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Tedwren.Api.Auth.TestBypassAuthenticationHandler>(
        Tedwren.Api.Auth.TestBypassAuthenticationHandler.SchemeName, _ => { });
}
else
{
    authBuilder.AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
}

// Secure by default: every endpoint requires an authenticated user unless it opts out with AllowAnonymous
// (auth, health and the recipient/kiosk flows). Named policies gate writes (SF-23) and admin-only surfaces.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().Build();
    options.AddPolicy("RequireWrite", p => p.RequireRole(
        Tedwren.Domain.Enums.AccessRole.Administrator.ToString(),
        Tedwren.Domain.Enums.AccessRole.ComplianceManager.ToString(),
        Tedwren.Domain.Enums.AccessRole.SiteManager.ToString()));
    options.AddPolicy("AdminOnly", p => p.RequireRole(Tedwren.Domain.Enums.AccessRole.Administrator.ToString()));
    // Platform-admin surfaces (the admin area: all companies/users/billing). Stricter than AdminOnly — a
    // customer's own Administrator must NOT pass. Requires Administrator in the fixed Tedwren tenant.
    options.AddPolicy("PlatformAdmin", p => p.RequireAssertion(ctx =>
        ctx.User.IsInRole(Tedwren.Domain.Enums.AccessRole.Administrator.ToString()) &&
        Guid.TryParse(ctx.User.FindFirst(Tedwren.Api.Auth.JwtTokenIssuer.CompanyClaim)?.Value, out var cid) &&
        cid == Tedwren.Application.Auth.AdminUserSeeder.SeedCompanyId));
});
// Database is the only supported runtime backend. The in-memory stores are a test-only double, selected
// exclusively by the API test host (DataSource:Mode=InMemory) so the end-to-end tests run without SQL Server.
if (backend.Mode == DataSourceMode.InMemory)
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
    builder.Services.AddInMemoryFormStore();
    builder.Services.AddInMemoryConsoleFoundationStore();
    builder.Services.AddInMemoryBillingStore();
    builder.Services.AddInMemoryReferenceDataStore();
    builder.Services.AddInMemorySettingsStore();
    builder.Services.AddInMemoryPermitStore();
    builder.Services.AddInMemoryOnboardingStore();
}
else
{
    var connectionStringName = backend.Provider == DatabaseProvider.PostgreSql ? "PostgreSql" : "SqlServer";
    var connectionString = builder.Configuration.GetConnectionString(connectionStringName) ?? string.Empty;
    builder.Services.AddSqlDataAccess(backend.Provider, connectionString);
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
app.UseAuthentication();
app.UseAuthorization();

// SF-23: the read-only Auditor role may view but never change data. Enforced server-side for every
// authenticated write (non-GET) request, so the read-only guarantee does not rely on the UI hiding buttons.
app.Use(async (context, next) =>
{
    var user = context.User;
    if (user.Identity?.IsAuthenticated == true &&
        user.IsInRole(Tedwren.Domain.Enums.AccessRole.Auditor.ToString()) &&
        !HttpMethods.IsGet(context.Request.Method) &&
        !HttpMethods.IsHead(context.Request.Method) &&
        !HttpMethods.IsOptions(context.Request.Method))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }

    await next();
});

// Apply database migrations at startup when running against a real database (skipped for the in-memory test host).
if (backend.Mode != DataSourceMode.InMemory)
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

// Seed the bootstrap admin for both modes (idempotent) so the console can be signed into (D1).
using (var adminScope = app.Services.CreateScope())
{
    var adminSeeder = adminScope.ServiceProvider.GetRequiredService<Tedwren.Application.Auth.AdminUserSeeder>();
    await adminSeeder.RunAsync();
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
app.MapFormEndpoints();
app.MapSiteEntryEndpoints();
app.MapEntitlementEndpoints();
app.MapAuditEndpoints();
app.MapDecisionEndpoints();
app.MapReferenceDataEndpoints();
app.MapCurrentUserEndpoints();
app.MapWorkforceEndpoints();
app.MapDashboardEndpoints();
app.MapSettingsEndpoints();
app.MapPermitEndpoints();
app.MapOnboardingEndpoints();
app.MapImageEndpoints();
app.MapEmailTemplateEndpoints();
app.MapAdminEndpoints();
app.MapBillingEndpoints();
app.MapAuthEndpoints();

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
    .WithName("HealthCheck")
    .AllowAnonymous();

app.Run();

/// <summary>
/// Marker for the API host's entry point. Declared so integration tests can drive the host with
/// <c>WebApplicationFactory&lt;Program&gt;</c> in later phases.
/// </summary>
public partial class Program;
