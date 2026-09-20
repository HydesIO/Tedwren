using System.Net;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Session;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>
/// Verifies the manager auth handler attaches the console token, and on a 401 (for a non-login path) signals
/// session expiry once and surfaces the 401 without retrying — there is no console refresh.
/// </summary>
public class ManagerAuthMessageHandlerTests
{
    [Fact]
    public async Task Attaches_token_and_passes_through_on_success()
    {
        var store = new AccessTokenStore { AccessToken = "good" };
        var expired = new FakeExpired();
        var inner = new CountingHandler(HttpStatusCode.OK);
        var client = Build(store, expired, inner);

        var response = await client.GetAsync("api/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("good", inner.LastToken);
        Assert.Equal(0, expired.Calls);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task On_401_signals_expiry_once_and_does_not_retry()
    {
        var store = new AccessTokenStore { AccessToken = "stale" };
        var expired = new FakeExpired();
        var inner = new CountingHandler(HttpStatusCode.Unauthorized);
        var client = Build(store, expired, inner);

        var response = await client.GetAsync("api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, expired.Calls);
        Assert.Equal(1, inner.Calls); // no retry
    }

    [Fact]
    public async Task Does_not_signal_expiry_on_a_login_401()
    {
        var store = new AccessTokenStore();
        var expired = new FakeExpired();
        var inner = new CountingHandler(HttpStatusCode.Unauthorized);
        var client = Build(store, expired, inner);

        var response = await client.PostAsync("api/auth/login", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, expired.Calls);
    }

    private static HttpClient Build(AccessTokenStore store, FakeExpired expired, CountingHandler inner)
    {
        var handler = new ManagerAuthMessageHandler(store, expired) { InnerHandler = inner };
        return new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        public int Calls { get; private set; }
        public string? LastToken { get; private set; }

        public CountingHandler(HttpStatusCode status) => _status = status;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastToken = request.Headers.Authorization?.Parameter;
            return Task.FromResult(new HttpResponseMessage(_status));
        }
    }

    private sealed class FakeExpired : IManagerSessionExpiredHandler
    {
        public int Calls { get; private set; }

        public void HandleUnauthorized() => Calls++;
    }
}
