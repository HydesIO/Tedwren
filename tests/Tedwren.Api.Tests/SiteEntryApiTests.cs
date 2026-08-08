using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the site-entry endpoints in the API's default (mock) mode. An unknown worker is
/// blocked (fail-closed, R2) with every check reported (R10), RAMS is recorded as not-run (module not held),
/// a manager override admits them (MC-11), and the muster returns competency cover (MC-13).
/// </summary>
public sealed class SiteEntryApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses an isolated host so decision/attendance state does not leak between tests.</summary>
    public SiteEntryApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact] // R2 + R10 — unknown worker is blocked, all checks reported, RAMS not-run
    public async Task UnknownWorker_IsBlocked_WithAllChecksAndRamsNotRun()
    {
        var client = CreateClient();
        var request = new DecideEntryRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null);

        var result = await (await client.PostAsJsonAsync("/api/site-entry/decide", request))
            .Content.ReadFromJsonAsync<EntryDecisionResultDto>();

        Assert.False(result!.Admitted);
        Assert.NotNull(result.BlockReason);
        Assert.Equal(5, result.Checks.Count);
        Assert.Contains(result.Checks, c => c.Name == "RAMS" && c.Outcome == "NotRun");
        Assert.True(result.ElapsedMs >= 0);   // instrumented against the <3s budget (R14)
    }

    [Fact] // MC-11 — a manager override admits the same blocked worker
    public async Task ManagerOverride_AdmitsBlockedWorker()
    {
        var client = CreateClient();
        var request = new DecideEntryRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            new ManagerOverrideDto("Dana Price", "Escorted for a supervised task"));

        var result = await (await client.PostAsJsonAsync("/api/site-entry/decide", request))
            .Content.ReadFromJsonAsync<EntryDecisionResultDto>();

        Assert.True(result!.Admitted);
        Assert.True(result.WasOverridden);
    }

    [Fact] // MC-12–MC-14 — the muster returns a data timestamp and competency cover
    public async Task Muster_ReturnsTimestampAndCompetencyCover()
    {
        var client = CreateClient();

        var muster = await client.GetFromJsonAsync<MusterDto>($"/api/site-entry/muster/{Guid.NewGuid()}");

        Assert.NotNull(muster);
        Assert.NotEqual(default, muster!.GeneratedUtc);
        Assert.Contains(muster.Competencies, c => c.Competency == "First Aid");
    }
}
