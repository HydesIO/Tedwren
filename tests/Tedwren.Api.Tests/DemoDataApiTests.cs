using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.DemoData;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the demo-data admin surface (<c>/api/admin/demo-data</c>). The test host
/// authenticates as a platform admin, so seed/status/delete all succeed; the round-trip proves the dataset is
/// created and then fully removed.
/// </summary>
public sealed class DemoDataApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses the shared test host (in-memory data double, platform-admin identity).</summary>
    public DemoDataApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact]
    public async Task Seed_then_delete_round_trips_the_demo_dataset()
    {
        var client = CreateClient();

        var seedResponse = await client.PostAsync("/api/admin/demo-data/seed", content: null);
        Assert.Equal(HttpStatusCode.OK, seedResponse.StatusCode);
        var seeded = await seedResponse.Content.ReadFromJsonAsync<DemoDataStatus>();
        Assert.NotNull(seeded);
        Assert.True(seeded!.Present);
        Assert.Equal(2, seeded.Companies);
        Assert.Equal(16, seeded.Sites);
        Assert.Equal(30, seeded.Operatives);
        Assert.Equal(26, seeded.Payments);

        var status = await client.GetFromJsonAsync<DemoDataStatus>("/api/admin/demo-data/status");
        Assert.NotNull(status);
        Assert.True(status!.Present);

        var deleteResponse = await client.PostAsync("/api/admin/demo-data/delete", content: null);
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        var afterDelete = await deleteResponse.Content.ReadFromJsonAsync<DemoDataStatus>();
        Assert.NotNull(afterDelete);
        Assert.False(afterDelete!.Present);
        Assert.Equal(0, afterDelete.Companies);
        Assert.Equal(0, afterDelete.Payments);
    }
}
