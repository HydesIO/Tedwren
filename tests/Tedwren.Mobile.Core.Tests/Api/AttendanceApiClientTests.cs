using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Attendance;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>Verifies the attendance client (M4): it shapes the sign-in/out/current JSON and surfaces a non-success status.</summary>
public class AttendanceApiClientTests
{
    [Fact]
    public async Task SignIn_maps_the_recorded_outcome()
    {
        var http = FakeHttp.Returning(HttpStatusCode.OK,
            JsonContent.Create(new SignInResult(true, "Accepted", null, Guid.NewGuid(), null)));
        var client = new AttendanceApiClient(http, new NoOpTelemetry());

        var result = await client.SignInAsync(new MobileSignInRequest(Guid.NewGuid(), null, 51.5, -0.1));

        Assert.True(result.SignedIn);
        Assert.Equal("Accepted", result.Outcome);
    }

    [Fact]
    public async Task SignIn_maps_a_recorded_refusal_naming_the_other_site()
    {
        var http = FakeHttp.Returning(HttpStatusCode.OK,
            JsonContent.Create(new SignInResult(false, "Refused", "Already signed in at Site Alpha.", Guid.NewGuid(), "Site Alpha")));
        var client = new AttendanceApiClient(http, new NoOpTelemetry());

        var result = await client.SignInAsync(new MobileSignInRequest(Guid.NewGuid(), null, 51.5, -0.1));

        Assert.False(result.SignedIn);
        Assert.Equal("Site Alpha", result.SignedInElsewhere);
    }

    [Fact]
    public async Task SignIn_throws_on_a_non_success_status()
    {
        var http = FakeHttp.Returning(HttpStatusCode.Forbidden, new StringContent(string.Empty));
        var client = new AttendanceApiClient(http, new NoOpTelemetry());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.SignInAsync(new MobileSignInRequest(Guid.NewGuid(), null, 51.5, -0.1)));
    }

    [Fact]
    public async Task SignOut_maps_the_duration()
    {
        var http = FakeHttp.Returning(HttpStatusCode.OK,
            JsonContent.Create(new SignOutResult(true, "Accepted", null, 2.5, Guid.NewGuid())));
        var client = new AttendanceApiClient(http, new NoOpTelemetry());

        var result = await client.SignOutAsync(new MobileSignOutRequest(Guid.NewGuid(), 51.5, -0.1));

        Assert.True(result.SignedOut);
        Assert.Equal(2.5, result.DurationHours);
    }

    [Fact]
    public async Task GetCurrent_returns_null_when_not_signed_in()
    {
        // The server answers 204 No Content when there is no open sign-in (SF-18).
        var http = FakeHttp.Returning(HttpStatusCode.NoContent, new StringContent(string.Empty));
        var client = new AttendanceApiClient(http, new NoOpTelemetry());

        Assert.Null(await client.GetCurrentAsync());
    }

    [Fact]
    public async Task GetCurrent_maps_the_current_site()
    {
        var siteId = Guid.NewGuid();
        var http = FakeHttp.Returning(HttpStatusCode.OK,
            JsonContent.Create(new CurrentAttendanceDto(siteId, "Riverside Works", null, DateTimeOffset.UtcNow)));
        var client = new AttendanceApiClient(http, new NoOpTelemetry());

        var current = await client.GetCurrentAsync();

        Assert.Equal(siteId, current!.SiteId);
        Assert.Equal("Riverside Works", current.SiteName);
    }
}
