using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Safety;
using Tedwren.Application.Auth;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for safety events (<c>/api/safety</c>, PRD §8.2) — hazard / near-miss reporting and the
/// accident / incident record (with RIDDOR). The group is gated on the paid HSE module (fails closed). Each test
/// uses its own host so in-memory state does not leak between them.
/// </summary>
public sealed class SafetyApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public SafetyApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>An isolated client (own host, hence own stores) with the scheduler disabled.</summary>
    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    private static async Task EnableHseAsync(HttpClient client)
    {
        var enable = await client.PutAsJsonAsync(
            $"/api/entitlements/{AdminUserSeeder.SeedCompanyId}/hse", new { Enabled = true });
        enable.EnsureSuccessStatusCode();
    }

    [Fact] // SF-22/Q2 — safety events are a paid module and fail closed without it
    public async Task Safety_WithoutHseModule_Forbidden()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/safety/hazards");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // PRD §8.2 — report a hazard, assign it, close it out, and see the leading-indicator counts
    public async Task Hazards_Report_Assign_Close_Stats()
    {
        var client = CreateClient();
        await EnableHseAsync(client);

        var report = await client.PostAsJsonAsync("/api/safety/hazards",
            new ReportHazardRequest("NearMiss", "Scaffold tie missing", "Level 3", null, null, null, null, "High", "Working at height"));
        Assert.Equal(HttpStatusCode.Created, report.StatusCode);
        var dto = await report.Content.ReadFromJsonAsync<HazardReportDto>();
        Assert.StartsWith("HAZ-", dto!.Reference);

        Assert.Single((await client.GetFromJsonAsync<List<HazardReportDto>>("/api/safety/hazards"))!);

        var assign = await client.PostAsJsonAsync($"/api/safety/hazards/{dto.Id}/assign", new AssignHazardRequest("Foreman", "Access"));
        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

        var close = await client.PostAsJsonAsync($"/api/safety/hazards/{dto.Id}/close", new CloseHazardRequest("Tie re-fitted"));
        Assert.Equal(HttpStatusCode.NoContent, close.StatusCode);

        var stats = await client.GetFromJsonAsync<HazardStatsDto>("/api/safety/hazards/stats");
        Assert.Equal(1, stats!.Total);
        Assert.Equal(1, stats.Closed);
        Assert.Equal(1, stats.High);
    }

    [Fact] // PRD §8.2 — record an incident, run the investigation with the RIDDOR flag, then close it out
    public async Task Incidents_Report_Investigate_Riddor_Close()
    {
        var client = CreateClient();
        await EnableHseAsync(client);

        var report = await client.PostAsJsonAsync("/api/safety/incidents",
            new ReportIncidentRequest("Accident", "Slip on wet slab", "Level 1", null, "Jane Smith", "Sprained wrist", "Medium"));
        Assert.Equal(HttpStatusCode.Created, report.StatusCode);
        var dto = await report.Content.ReadFromJsonAsync<IncidentReportDto>();
        Assert.StartsWith("INC-", dto!.Reference);

        var investigate = await client.PutAsJsonAsync($"/api/safety/incidents/{dto.Id}/investigation",
            new UpdateIncidentInvestigationRequest("Wet slab unsigned", "No permit", "Permit + signage", true, "Over-7-day incapacitation", "H&S Lead"));
        Assert.Equal(HttpStatusCode.NoContent, investigate.StatusCode);

        var afterInvestigation = await client.GetFromJsonAsync<IncidentReportDto>($"/api/safety/incidents/{dto.Id}");
        Assert.Equal("UnderInvestigation", afterInvestigation!.Status);
        Assert.True(afterInvestigation.RiddorReportable);
        Assert.Equal("Over-7-day incapacitation", afterInvestigation.RiddorCategory);

        var close = await client.PostAsync($"/api/safety/incidents/{dto.Id}/close", content: null);
        Assert.Equal(HttpStatusCode.NoContent, close.StatusCode);

        var afterClose = await client.GetFromJsonAsync<IncidentReportDto>($"/api/safety/incidents/{dto.Id}");
        Assert.Equal("Closed", afterClose!.Status);
    }
}
