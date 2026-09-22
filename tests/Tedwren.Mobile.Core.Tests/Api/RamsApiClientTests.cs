using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Rams;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>
/// Verifies the operative RAMS client (Gate 5): it maps the live RAMS, treats "nothing to sign" (204) as null, posts
/// the signature and maps "the version moved on" (409) to null so the caller re-reads the current RAMS.
/// </summary>
public class RamsApiClientTests
{
    private static LiveRamsDto Live() => new(
        Guid.NewGuid(), Guid.NewGuid(), 2, "Groundworks RAMS", "RAMS-1", "Groundworks Co", HasFile: true, "Approved",
        DateTimeOffset.UtcNow);

    [Fact]
    public async Task GetLive_returns_null_when_none_to_sign()
    {
        var http = FakeHttp.Returning(HttpStatusCode.NoContent, new StringContent(string.Empty));
        var client = new RamsApiClient(http);

        Assert.Null(await client.GetLiveAsync());
    }

    [Fact]
    public async Task GetLive_maps_the_live_rams()
    {
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(Live()));
        var client = new RamsApiClient(http);

        var live = await client.GetLiveAsync();

        Assert.NotNull(live);
        Assert.Equal("Groundworks RAMS", live!.Title);
        Assert.Equal(2, live.Version);
    }

    [Fact]
    public async Task Sign_maps_the_acknowledgement()
    {
        var ack = new RamsAcknowledgementDto(Guid.NewGuid(), Guid.NewGuid(), 2, DateTimeOffset.UtcNow, null);
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(ack));
        var client = new RamsApiClient(http);

        var result = await client.SignAsync(new SignRamsRequest(Guid.NewGuid(), "Alex Operative"));

        Assert.NotNull(result);
        Assert.Equal(2, result!.Version);
    }

    [Fact]
    public async Task Sign_returns_null_when_version_is_stale()
    {
        // The server answers 409 when the submission is no longer the current live version.
        var http = FakeHttp.Returning(HttpStatusCode.Conflict, new StringContent(string.Empty));
        var client = new RamsApiClient(http);

        Assert.Null(await client.SignAsync(new SignRamsRequest(Guid.NewGuid(), "Alex Operative")));
    }

    [Fact]
    public async Task Sign_posts_to_the_sign_route()
    {
        HttpRequestMessage? seen = null;
        var http = FakeHttp.Routed(request =>
        {
            seen = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new RamsAcknowledgementDto(Guid.NewGuid(), Guid.NewGuid(), 1, DateTimeOffset.UtcNow, null)),
            };
        });
        var client = new RamsApiClient(http);

        await client.SignAsync(new SignRamsRequest(Guid.NewGuid(), "Alex Operative"));

        Assert.NotNull(seen);
        Assert.Equal(HttpMethod.Post, seen!.Method);
        Assert.EndsWith("api/mobile/rams/sign", seen.RequestUri!.ToString());
    }
}
