using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Assets;
using Tedwren.Application.Auth;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for the plant &amp; equipment register (<c>/api/assets</c>, PRD §8.2). The group is gated on
/// the paid HSE module (fails closed), so tests enable it for the caller's company first. Each test uses its own
/// host so the in-memory entitlement/asset state does not leak between them.
/// </summary>
public sealed class AssetApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public AssetApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>An isolated client (own host, hence own stores) with the scheduler disabled.</summary>
    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    private static async Task EnableHseAsync(HttpClient client)
    {
        var enable = await client.PutAsJsonAsync(
            $"/api/entitlements/{AdminUserSeeder.SeedCompanyId}/hse", new { Enabled = true });
        enable.EnsureSuccessStatusCode();
    }

    [Fact] // SF-22/Q2 — the register is a paid module and fails closed without it
    public async Task Assets_WithoutHseModule_Forbidden()
    {
        var client = CreateClient();

        var response = await client.GetAsync("/api/assets");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact] // PRD §8.2 — add, list (with derived certification status), update and retire
    public async Task Assets_CreateListUpdateRetire_RoundTrip()
    {
        var client = CreateClient();
        await EnableHseAsync(client);

        var create = await client.PostAsJsonAsync("/api/assets", new CreateAssetRequest(
            "Tower Crane TC-1", "Crane", "SN-100", "Meridian Tower", "Ops",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), null, null));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (await create.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;

        var asset = Assert.Single((await client.GetFromJsonAsync<List<AssetDto>>("/api/assets"))!);
        Assert.Equal("Tower Crane TC-1", asset.Name);
        Assert.Equal("Expiring soon", asset.CertificationStatus);

        var update = await client.PutAsJsonAsync($"/api/assets/{id}", new UpdateAssetRequest(
            "Tower Crane TC-1", "Crane", "SN-100", "Meridian Tower", "Ops", null, null, "serviced"));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var retire = await client.PostAsync($"/api/assets/{id}/retire", content: null);
        Assert.Equal(HttpStatusCode.NoContent, retire.StatusCode);

        var retired = Assert.Single((await client.GetFromJsonAsync<List<AssetDto>>("/api/assets"))!);
        Assert.Equal("Retired", retired.Status);
    }

    /// <summary>The <c>{ id }</c> shape returned by the create endpoint.</summary>
    private sealed record CreatedResponse(Guid Id);
}
