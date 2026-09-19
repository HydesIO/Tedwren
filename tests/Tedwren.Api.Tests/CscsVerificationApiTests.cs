using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Qualifications;
using Tedwren.Application.Auth;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for the CSCS verification endpoint (<c>/api/qualifications/cscs-check</c>, PRD-Phase 1). Each
/// test uses its own host so the in-memory entitlement state does not leak between them.
/// </summary>
public sealed class CscsVerificationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public CscsVerificationApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>An isolated client (own host, hence own entitlement store) with the scheduler disabled.</summary>
    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact] // §8.1 — without the module no CSCS call is made and the card is described as customer-checked
    public async Task CscsCheck_ModuleOff_ReturnsNotEntitled()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/qualifications/cscs-check", new CscsCheckRequest("CARD-123", "CSCS"));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CscsCheckResponse>();
        Assert.Equal("NotEntitled", result!.Outcome);
        Assert.Equal("CustomerChecked", result.State);
    }

    [Fact] // §8.1 — module on but CSCS unreachable (unconfigured) → human fallback, never blocks the induction
    public async Task CscsCheck_ModuleOn_Unconfigured_FallsBackToHumanCheck()
    {
        var client = CreateClient();
        var enable = await client.PutAsJsonAsync(
            $"/api/entitlements/{AdminUserSeeder.SeedCompanyId}/cscs", new { Enabled = true });
        enable.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/qualifications/cscs-check", new CscsCheckRequest("CARD-123", "CSCS"));

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CscsCheckResponse>();
        Assert.Equal("HumanFallback", result!.Outcome);
        Assert.False(result.BlocksInduction);
    }
}
