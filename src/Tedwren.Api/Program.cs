using Microsoft.Extensions.Options;
using Tedwren.Abstractions.Configuration;

// Composition root for the Tedwren Web API. This API is deliberately a separate deployable from
// the Blazor WebAssembly client and is CORS-enabled, so the same contracts can later serve a
// mobile application. Phase 7 wires the host, the health probe, CORS and the mock/database
// configuration switch; feature endpoints and the Dapper-backed services arrive from Phase 8.

var builder = WebApplication.CreateBuilder(args);

// Bind the mock/database switch from the "DataSource" configuration section. Defaults to mock.
builder.Services.Configure<BackendOptions>(
    builder.Configuration.GetSection(BackendOptions.SectionName));

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
