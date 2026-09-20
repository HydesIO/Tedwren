using System.Net;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Session;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>
/// Verifies the manager auth handler (M8): attaches the console token, silently refreshes-then-retries once on a
/// 401, and only signals expiry when the refresh fails; login-path 401s are left alone.
/// </summary>
public class ManagerAuthMessageHandlerTests
{
    [Fact]
    public async Task Attaches_token_and_passes_through_on_success()
    {
        var store = new AccessTokenStore { AccessToken = "good" };
        var refresher = new FakeRefresher("unused");
        var expired = new FakeExpired();
        var inner = new TokenGatedHandler(accept: "good");
        var client = Build(store, refresher, expired, inner);

        var response = await client.GetAsync("api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, refresher.Calls);
        Assert.Equal(0, expired.Calls);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Refreshes_and_retries_once_on_401()
    {
        var store = new AccessTokenStore { AccessToken = "stale" };
        var refresher = new FakeRefresher("good");
        var expired = new FakeExpired();
        var inner = new TokenGatedHandler(accept: "good");
        var client = Build(store, refresher, expired, inner);

        var response = await client.GetAsync("api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("good", store.AccessToken);
        Assert.Equal(1, refresher.Calls);
        Assert.Equal(0, expired.Calls);
        Assert.Equal(2, inner.Calls); // 401, then the retry
    }

    [Fact]
    public async Task Signals_expiry_when_refresh_fails()
    {
        var store = new AccessTokenStore { AccessToken = "stale" };
        var refresher = new FakeRefresher(null);
        var expired = new FakeExpired();
        var inner = new TokenGatedHandler(accept: "good");
        var client = Build(store, refresher, expired, inner);

        var response = await client.GetAsync("api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, refresher.Calls);
        Assert.Equal(1, expired.Calls);
        Assert.Equal(1, inner.Calls); // no retry
    }

    [Fact]
    public async Task Does_not_refresh_or_signal_on_a_login_401()
    {
        var store = new AccessTokenStore();
        var refresher = new FakeRefresher("good");
        var expired = new FakeExpired();
        var inner = new TokenGatedHandler(accept: "never");
        var client = Build(store, refresher, expired, inner);

        var response = await client.PostAsync("api/auth/login", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, refresher.Calls);
        Assert.Equal(0, expired.Calls);
    }

    private static HttpClient Build(AccessTokenStore store, FakeRefresher refresher, FakeExpired expired, TokenGatedHandler inner)
    {
        var handler = new ManagerAuthMessageHandler(store, refresher, expired) { InnerHandler = inner };
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

    private sealed class FakeRefresher : IManagerSessionRefresher
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

    private sealed class FakeExpired : IManagerSessionExpiredHandler
    {
        public int Calls { get; private set; }

        public void HandleUnauthorized() => Calls++;
    }
}
