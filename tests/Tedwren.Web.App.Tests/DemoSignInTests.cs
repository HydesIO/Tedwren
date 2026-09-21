using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Session;
using Tedwren.Web.App.Platform;

namespace Tedwren.Web.App.Tests;

/// <summary>
/// Tests the client half of the emulator's operative demo sign-in: <see cref="OperativeSessionManager.DemoSignInAsync"/>
/// over a stubbed API. It must behave exactly like the OTP enrol path — persist the refresh token and return an
/// operative session — so the rest of the app (auth handler, resume) works unchanged.
/// </summary>
public class DemoSignInTests
{
    [Fact]
    public async Task DemoSignIn_success_stores_refresh_and_returns_operative_session()
    {
        var result = new MobileAuthResultDto(
            "access-token", DateTimeOffset.UtcNow.AddHours(1), "refresh-token", DateTimeOffset.UtcNow.AddDays(30),
            "Demo Operative", Guid.NewGuid(), Guid.NewGuid());
        var store = new InMemorySecureStore();
        var session = NewSession(new StubHandler(HttpStatusCode.OK, result), store);

        var enrol = await session.DemoSignInAsync("operative@tedwren.com", "Web emulator");

        Assert.Equal(EnrolStatus.Success, enrol.Status);
        Assert.Equal("Demo Operative", enrol.Session!.Name);
        Assert.Equal(RoleHome.Operative, enrol.Session.Home);
        Assert.Equal("refresh-token", await store.GetAsync("tw.operative.refresh"));
    }

    [Fact]
    public async Task DemoSignIn_unauthorized_returns_invalid_and_stores_nothing()
    {
        var store = new InMemorySecureStore();
        var session = NewSession(new StubHandler(HttpStatusCode.Unauthorized, null), store);

        var enrol = await session.DemoSignInAsync("operative@tedwren.com", null);

        Assert.Equal(EnrolStatus.InvalidCode, enrol.Status);
        Assert.Null(await store.GetAsync("tw.operative.refresh"));
    }

    private static OperativeSessionManager NewSession(HttpMessageHandler handler, ISecureStore store)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") };
        return new OperativeSessionManager(new OperativeAuthApiClient(http), store, new WebBiometricAuthenticator());
    }

    /// <summary>A stub handler returning a fixed status + optional JSON body for any request.</summary>
    private sealed class StubHandler(HttpStatusCode status, MobileAuthResultDto? body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(status);
            if (body is not null)
            {
                response.Content = JsonContent.Create(body);
            }

            return Task.FromResult(response);
        }
    }

    /// <summary>In-memory secure store double (localStorage stand-in) for the session manager under test.</summary>
    private sealed class InMemorySecureStore : ISecureStore
    {
        private readonly Dictionary<string, string> _values = new();

        public Task<string?> GetAsync(string key) => Task.FromResult(_values.GetValueOrDefault(key));

        public Task SetAsync(string key, string value)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public void Remove(string key) => _values.Remove(key);
    }
}
