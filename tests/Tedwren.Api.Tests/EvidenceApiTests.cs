using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Evidence;
using Tedwren.Application.Auth;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for the unified compliance evidence export (<c>/api/evidence</c>, PRD §8.2). The group is gated
/// on the paid HSE module (fails closed). Each test uses its own host so in-memory state does not leak.
/// </summary>
public sealed class EvidenceApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public EvidenceApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>An isolated client (own host, hence own stores) with the scheduler disabled.</summary>
    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    private static async Task EnableHseAsync(HttpClient client)
    {
        var enable = await client.PutAsJsonAsync(
            $"/api/entitlements/{AdminUserSeeder.SeedCompanyId}/hse", new { Enabled = true });
        enable.EnsureSuccessStatusCode();
    }

    [Fact] // SF-22/Q2 — the evidence export is a paid module and fails closed without it
    public async Task Evidence_WithoutHseModule_Forbidden()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/evidence/summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // PRD §8.2 — the summary lists the evidence sections; the export returns a ZIP
    public async Task Evidence_Summary_And_Export()
    {
        var client = CreateClient();
        await EnableHseAsync(client);

        var summary = await client.GetFromJsonAsync<EvidenceSummaryDto>("/api/evidence/summary");
        Assert.NotNull(summary);
        Assert.NotEmpty(summary!.Sections);                         // every evidence section is described

        var export = await client.GetAsync("/api/evidence/export");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal("application/zip", export.Content.Headers.ContentType?.MediaType);
        var bytes = await export.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);                                     // a real ZIP payload
    }
}
