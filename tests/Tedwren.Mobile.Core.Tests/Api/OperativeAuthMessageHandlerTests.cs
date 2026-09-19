using System.Net;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Session;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>Verifies the operative auth handler attaches the token and refreshes-then-retries once on a 401.</summary>
public class OperativeAuthMessageHandlerTests
{
    [Fact]
    public async Task Attaches_token_and_passes_through_on_success()
    {
        var store = new AccessTokenStore { AccessToken = "good" };
        var refresher = new FakeRefresher("unused");
        var inner = new TokenGatedHandler(accept: "good");
        var client = Build(store, refresher, inner);

        var response = await client.GetAsync("api/mobile/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, refresher.Calls);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Refreshes_and_retries_once_on_401()
    {
        var store = new AccessTokenStore { AccessToken = "stale" };
        var refresher = new FakeRefresher("good");
        var inner = new TokenGatedHandler(accept: "good");
        var client = Build(store, refresher, inner);

        var response = await client.GetAsync("api/mobile/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("good", store.AccessToken);
        Assert.Equal(1, refresher.Calls);
        Assert.Equal(2, inner.Calls); // 401, then the retry
    }

    [Fact]
    public async Task Surfaces_401_when_refresh_fails()
    {
        var store = new AccessTokenStore { AccessToken = "stale" };
        var refresher = new FakeRefresher(null);
        var inner = new TokenGatedHandler(accept: "good");
        var client = Build(store, refresher, inner);

        var response = await client.GetAsync("api/mobile/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, refresher.Calls);
        Assert.Equal(1, inner.Calls); // no retry
    }

    [Fact]
    public async Task Does_not_refresh_the_auth_endpoints()
    {
        var store = new AccessTokenStore { AccessToken = "stale" };
        var refresher = new FakeRefresher("good");
        var inner = new TokenGatedHandler(accept: "never");
        var client = Build(store, refresher, inner);

        var response = await client.PostAsync("api/mobile/auth/refresh", new StringContent(""));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, refresher.Calls);
    }

    private static HttpClient Build(AccessTokenStore store, FakeRefresher refresher, TokenGatedHandler inner)
    {
        var handler = new OperativeAuthMessageHandler(store, refresher) { InnerHandler = inner };
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
    }

    private sealed class TokenGatedHandler : HttpMessageHandler
    {
        private readonly string _accept;
        public int Calls { get; private set; }

        public TokenGatedHandler(string accept) => _accept = accept;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var ok = request.Headers.Authorization?.Parameter == _accept;
            return Task.FromResult(new HttpResponseMessage(ok ? HttpStatusCode.OK : HttpStatusCode.Unauthorized));
        }
    }

    private sealed class FakeRefresher : ISessionRefresher
    {
        private readonly string? _newToken;
        public int Calls { get; private set; }

        public FakeRefresher(string? newToken) => _newToken = newToken;

        public Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_newToken);
        }
    }
}
