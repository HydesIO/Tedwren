using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Havs;
using Tedwren.Application.Auth;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for HAVs exposure monitoring (<c>/api/havs</c>, PRD §8.2). The group is gated on the paid HSE
/// module (fails closed). Each test uses its own host so in-memory state does not leak between them.
/// </summary>
public sealed class HavsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public HavsApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>An isolated client (own host, hence own stores) with the scheduler disabled.</summary>
    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    private static async Task EnableHseAsync(HttpClient client)
    {
        var enable = await client.PutAsJsonAsync(
            $"/api/entitlements/{AdminUserSeeder.SeedCompanyId}/hse", new { Enabled = true });
        enable.EnsureSuccessStatusCode();
    }

    [Fact] // SF-22/Q2 — HAVs monitoring is a paid module and fails closed without it
    public async Task Havs_WithoutHseModule_Forbidden()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/havs");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // PRD §8.2 — record a day's tool usages and read back the derived A(8)/points/band
    public async Task Havs_Record_DerivesExposure_And_ListsAndGets()
    {
        var client = CreateClient();
        await EnableHseAsync(client);

        var request = new CreateHavsExposureRequest("Sam Mason", DateOnly.FromDateTime(DateTime.UtcNow),
            new[] { new HavsToolUsageDto("Breaker", 5.0, 480) });   // 5.0 m/s² for 8h = the ELV
        var record = await client.PostAsJsonAsync("/api/havs", request);
        Assert.Equal(HttpStatusCode.Created, record.StatusCode);
        var dto = await record.Content.ReadFromJsonAsync<HavsExposureRecordDto>();
        Assert.Equal(5.0, dto!.DailyExposureA8);
        Assert.Equal(400, dto.ExposurePoints);
        Assert.Equal("AboveLimitValue", dto.Band);

        Assert.Single((await client.GetFromJsonAsync<List<HavsExposureRecordDto>>("/api/havs"))!);

        var fetched = await client.GetFromJsonAsync<HavsExposureRecordDto>($"/api/havs/{dto.Id}");
        Assert.Equal("Sam Mason", fetched!.PersonName);
        Assert.Single(fetched.ToolUsages);
    }
}
