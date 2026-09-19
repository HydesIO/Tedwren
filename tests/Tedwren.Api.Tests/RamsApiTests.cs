using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Rams;
using Tedwren.Application.Auth;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for RAMS review (<c>/api/rams</c>, PRD §8.2). The group is gated on the paid HSE module
/// (fails closed). Each test uses its own host so in-memory state does not leak between them.
/// </summary>
public sealed class RamsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public RamsApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>An isolated client (own host, hence own stores) with the scheduler disabled.</summary>
    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    private static async Task EnableHseAsync(HttpClient client)
    {
        var enable = await client.PutAsJsonAsync(
            $"/api/entitlements/{AdminUserSeeder.SeedCompanyId}/hse", new { Enabled = true });
        enable.EnsureSuccessStatusCode();
    }

    [Fact] // SF-22/Q2 — RAMS review is a paid module and fails closed without it
    public async Task Rams_WithoutHseModule_Forbidden()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/rams");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // PRD §8.2 — submit (with a reference), see the queue, reject-without-note is a 400, then approve
    public async Task Rams_Submit_Queue_RejectNeedsNote_Approve()
    {
        var client = CreateClient();
        await EnableHseAsync(client);

        var submit = await client.PostAsJsonAsync("/api/rams",
            new SubmitRamsRequest("Apex Groundworks", "Excavation RAMS", null, "Meridian Tower", null, null, null));
        Assert.Equal(HttpStatusCode.Created, submit.StatusCode);
        var dto = await submit.Content.ReadFromJsonAsync<RamsSubmissionDto>();
        Assert.StartsWith("RAMS-", dto!.Reference);

        Assert.Single((await client.GetFromJsonAsync<List<RamsSubmissionDto>>("/api/rams/queue"))!);

        // Rejecting requires a written note (PRD §8.2) — an empty note is a 400.
        var reject = await client.PostAsJsonAsync($"/api/rams/{dto.Id}/reject", new ReviewRamsRequest(""));
        Assert.Equal(HttpStatusCode.BadRequest, reject.StatusCode);

        var approve = await client.PostAsync($"/api/rams/{dto.Id}/approve", content: null);
        Assert.Equal(HttpStatusCode.NoContent, approve.StatusCode);

        Assert.Equal("Approved", (await client.GetFromJsonAsync<List<RamsSubmissionDto>>("/api/rams"))!.Single().Status);
    }
}
