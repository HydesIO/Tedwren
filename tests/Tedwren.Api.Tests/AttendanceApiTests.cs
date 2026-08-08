using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Abstractions.Contracts.Sites;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the attendance endpoints (SF-13–SF-18) in the API's default (mock) mode. Uses
/// the seeded boundaried site so a sign-in at its centre is accepted.
/// </summary>
public sealed class AttendanceApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Uses an isolated host so attendance state does not leak between tests.</summary>
    public AttendanceApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.WithWebHostBuilder(b => b.UseSetting("Jobs:SchedulerEnabled", "false")).CreateClient();

    [Fact] // SF-14 + SF-17 + SF-18
    public async Task SignIn_OnSite_DoubleSiteRefused_ThenSignOut()
    {
        var client = CreateClient();
        var person = Guid.NewGuid();
        var sites = await client.GetFromJsonAsync<List<SiteSummary>>("/api/sites");
        var meridian = sites!.Single(s => s.Name == "Meridian Tower");   // boundary (51.5074, -0.1278, 150)
        var dispersed = sites!.First(s => s.IsDispersed);

        // Sign in at the boundary centre → accepted, and present.
        var signIn = await (await client.PostAsJsonAsync("/api/attendance/sign-in",
            new SignInRequest(person, meridian.Id, null, 51.5074, -0.1278, SignInMethod.QrScan)))
            .Content.ReadFromJsonAsync<SignInResult>();
        Assert.True(signIn!.SignedIn);
        Assert.Equal("Accepted", signIn.Outcome);

        var onSite = await client.GetFromJsonAsync<List<OnSiteWorkerDto>>($"/api/attendance/on-site/{meridian.Id}");
        Assert.Contains(onSite!, w => w.PersonId == person);

        // SF-18: signing in at another site while present is refused, naming the first.
        var second = await (await client.PostAsJsonAsync("/api/attendance/sign-in",
            new SignInRequest(person, dispersed.Id, null, null, null, SignInMethod.AssignmentLink)))
            .Content.ReadFromJsonAsync<SignInResult>();
        Assert.False(second!.SignedIn);
        Assert.Equal("Meridian Tower", second.SignedInElsewhere);

        // Sign out → no longer present.
        var signOut = await (await client.PostAsJsonAsync("/api/attendance/sign-out",
            new SignOutRequest(person, meridian.Id, null, null, SignInMethod.QrScan)))
            .Content.ReadFromJsonAsync<SignOutResult>();
        Assert.True(signOut!.SignedOut);

        var onSiteAfter = await client.GetFromJsonAsync<List<OnSiteWorkerDto>>($"/api/attendance/on-site/{meridian.Id}");
        Assert.DoesNotContain(onSiteAfter!, w => w.PersonId == person);
    }
}
