using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// Guards the site-entry decision latency budget (R14): a gate decision must return well within three
/// seconds. Asserts both the server-reported <see cref="EntryDecisionResultDto.ElapsedMs"/> and the
/// end-to-end round trip stay under budget, so a latency regression fails the build. A representative check,
/// not a load test — sustained-load/soak testing is an infrastructure exercise done outside the unit suite.
/// </summary>
public sealed class SiteEntryLatencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    // R14: a site-entry decision must be near-instant. Three seconds is the hard budget.
    private const long BudgetMs = 3000;

    private readonly WebApplicationFactory<Program> _factory;

    public SiteEntryLatencyTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact]
    public async Task Decision_CompletesWithinR14Budget()
    {
        var client = CreateClient();
        var request = new DecideEntryRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null);

        var stopwatch = Stopwatch.StartNew();
        var result = await (await client.PostAsJsonAsync("/api/site-entry/decide", request))
            .Content.ReadFromJsonAsync<EntryDecisionResultDto>();
        stopwatch.Stop();

        Assert.NotNull(result);
        Assert.True(result!.ElapsedMs < BudgetMs, $"Server decision took {result.ElapsedMs}ms (budget {BudgetMs}ms, R14).");
        Assert.True(stopwatch.ElapsedMilliseconds < BudgetMs, $"Round trip took {stopwatch.ElapsedMilliseconds}ms (budget {BudgetMs}ms, R14).");
    }
}
