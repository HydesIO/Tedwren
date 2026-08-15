using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.LaunchList;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for the launch list (Web Content Spec §6.9): the public signup is anonymous and
/// deduplicated, and the admin surface lists subscribers and sends the announcement. The in-memory test host
/// uses the outbox email sender, so notify succeeds without dispatching a real email.
/// </summary>
public sealed class LaunchListApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Receives the shared in-memory test host.</summary>
    public LaunchListApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Signup_IsAnonymous_Deduplicated_ThenListedAndNotified()
    {
        var client = _factory.CreateClient();
        var email = $"launch-{Guid.NewGuid():N}@example.com";

        // Anonymous signup succeeds.
        var first = await client.PostAsJsonAsync("/api/launch-signups", new CreateLaunchSignupRequest(email, "landing"));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstResult = (await first.Content.ReadFromJsonAsync<LaunchSignupResultDto>())!;
        Assert.True(firstResult.Accepted);
        Assert.False(firstResult.AlreadyOnList);

        // A repeat of the same address is accepted but flagged as already on the list.
        var second = await client.PostAsJsonAsync("/api/launch-signups", new CreateLaunchSignupRequest(email.ToUpperInvariant()));
        var secondResult = (await second.Content.ReadFromJsonAsync<LaunchSignupResultDto>())!;
        Assert.True(secondResult.AlreadyOnList);

        // Admin list includes the subscriber exactly once, not yet notified.
        var list = await client.GetFromJsonAsync<List<LaunchSignupDto>>("/api/launch-signups");
        Assert.NotNull(list);
        var mine = list!.Where(s => s.Email == email).ToList();
        Assert.Single(mine);
        Assert.False(mine[0].Notified);

        // Notify sends to the un-notified subscribers.
        var notify = await client.PostAsJsonAsync("/api/launch-signups/notify", new NotifyLaunchRequest(OnlyUnnotified: true));
        Assert.Equal(HttpStatusCode.OK, notify.StatusCode);
        var outcome = (await notify.Content.ReadFromJsonAsync<NotifyLaunchResultDto>())!;
        Assert.True(outcome.Sent >= 1);

        // The subscriber is now marked notified.
        var after = await client.GetFromJsonAsync<List<LaunchSignupDto>>("/api/launch-signups");
        Assert.True(after!.Single(s => s.Email == email).Notified);
    }

    [Fact]
    public async Task Signup_RejectsInvalidEmail_WithBadRequest()
    {
        var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/launch-signups", new CreateLaunchSignupRequest("   "))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/launch-signups", new CreateLaunchSignupRequest("not-an-email"))).StatusCode);
    }

    [Fact]
    public async Task Unsubscribe_IsAnonymous_AndReturnsHtml()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/launch-signups/unsubscribe?token={Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Export_ReturnsCsv_AndDelete_RemovesSubscriber()
    {
        var client = _factory.CreateClient();
        var email = $"del-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/launch-signups", new CreateLaunchSignupRequest(email));

        // Export returns CSV containing the email.
        var export = await client.GetAsync("/api/launch-signups/export");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal("text/csv", export.Content.Headers.ContentType?.MediaType);
        Assert.Contains(email, await export.Content.ReadAsStringAsync());

        // Find the id and delete it.
        var list = await client.GetFromJsonAsync<List<LaunchSignupDto>>("/api/launch-signups");
        var id = list!.Single(s => s.Email == email).Id;
        var delete = await client.DeleteAsync($"/api/launch-signups/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var after = await client.GetFromJsonAsync<List<LaunchSignupDto>>("/api/launch-signups");
        Assert.DoesNotContain(after!, s => s.Id == id);
    }
}
