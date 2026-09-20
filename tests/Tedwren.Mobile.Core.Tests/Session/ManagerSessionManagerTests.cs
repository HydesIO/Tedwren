using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Auth;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Session;
using Tedwren.Mobile.Core.Tests.Api;
using Tedwren.Mobile.Core.Tests.Sync;

namespace Tedwren.Mobile.Core.Tests.Session;

/// <summary>
/// Verifies manager (console) sign-in: login sets and persists the session, invalid credentials and transport
/// failures are distinct, a valid stored session resumes behind biometrics, an expired one is cleared (no console
/// refresh), and a 401 mid-session clears the session and raises the expiry event.
/// </summary>
public class ManagerSessionManagerTests
{
    private const string SessionKey = "tw.manager.session";

    private static AuthResultDto Auth(DateTimeOffset? expires = null) =>
        new("console-token", expires ?? DateTimeOffset.UtcNow.AddHours(8), "Morgan Manager", "SiteManager", Guid.NewGuid());

    private static AuthResultDto AuthWithRefresh(DateTimeOffset accessExpires, string refreshToken, string token = "console-token") =>
        new(token, accessExpires, "Morgan Manager", "SiteManager", Guid.NewGuid(), refreshToken, DateTimeOffset.UtcNow.AddDays(30));

    private static ManagerSessionManager Manager(HttpClient http, FakeSecureStore store, FakeBiometrics biometrics, AccessTokenStore tokens, TimeProvider? clock = null)
        => new(new AuthApiClient(http), store, biometrics, tokens, clock ?? TimeProvider.System);

    [Fact]
    public async Task Login_success_sets_token_persists_session_and_lands_on_manager_home()
    {
        var store = new FakeSecureStore();
        var tokens = new AccessTokenStore();
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(Auth()));
        var manager = Manager(http, store, new FakeBiometrics(false, BiometricResult.Unavailable), tokens);

        var result = await manager.LoginAsync("morgan@acme.test", "pw");

        Assert.Equal(ManagerLoginStatus.Success, result.Status);
        Assert.Equal(RoleHome.Manager, result.Session!.Home);
        Assert.Equal("console-token", tokens.AccessToken);
        Assert.False(string.IsNullOrEmpty(store.Peek(SessionKey)));
    }

    [Fact]
    public async Task Login_invalid_credentials_does_not_persist_or_set_a_token()
    {
        var store = new FakeSecureStore();
        var tokens = new AccessTokenStore();
        var manager = Manager(FakeHttp.Returning(HttpStatusCode.Unauthorized, new StringContent(string.Empty)), store, new FakeBiometrics(false, BiometricResult.Unavailable), tokens);

        var result = await manager.LoginAsync("morgan@acme.test", "wrong");

        Assert.Equal(ManagerLoginStatus.InvalidCredentials, result.Status);
        Assert.Null(tokens.AccessToken);
        Assert.Null(store.Peek(SessionKey));
    }

    [Fact]
    public async Task Login_transport_failure_is_reported_as_error()
    {
        var manager = Manager(FakeHttp.Returning(HttpStatusCode.InternalServerError, new StringContent(string.Empty)), new FakeSecureStore(), new FakeBiometrics(false, BiometricResult.Unavailable), new AccessTokenStore());
        var result = await manager.LoginAsync("morgan@acme.test", "pw");
        Assert.Equal(ManagerLoginStatus.Error, result.Status);
    }

    [Fact]
    public async Task Resume_reports_not_enrolled_without_a_stored_session()
    {
        var manager = Manager(FakeHttp.Returning(HttpStatusCode.OK, new StringContent(string.Empty)), new FakeSecureStore(), new FakeBiometrics(true, BiometricResult.Success), new AccessTokenStore());
        var resume = await manager.TryResumeAsync();
        Assert.Equal(ResumeStatus.NotEnrolled, resume.Status);
    }

    [Fact]
    public async Task Resume_sets_the_token_when_valid_and_biometrics_pass()
    {
        var store = new FakeSecureStore();
        var tokens = new AccessTokenStore();
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(Auth(DateTimeOffset.UtcNow.AddHours(8))));
        var manager = Manager(http, store, new FakeBiometrics(true, BiometricResult.Success), tokens);
        await manager.LoginAsync("morgan@acme.test", "pw");
        tokens.AccessToken = null; // simulate a relaunch: the in-memory token is gone, the session is persisted

        var resume = await manager.TryResumeAsync();

        Assert.Equal(ResumeStatus.Resumed, resume.Status);
        Assert.Equal(RoleHome.Manager, resume.Session!.Home);
        Assert.Equal("console-token", tokens.AccessToken);
    }

    [Fact]
    public async Task Resume_clears_an_expired_session()
    {
        var store = new FakeSecureStore();
        var tokens = new AccessTokenStore();
        var clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(Auth(DateTimeOffset.UtcNow.AddHours(1))));
        var manager = Manager(http, store, new FakeBiometrics(false, BiometricResult.Unavailable), tokens, clock);
        await manager.LoginAsync("morgan@acme.test", "pw");
        tokens.AccessToken = null;
        clock.Advance(TimeSpan.FromHours(2)); // the token has now expired

        var resume = await manager.TryResumeAsync();

        Assert.Equal(ResumeStatus.Expired, resume.Status);
        Assert.Null(store.Peek(SessionKey));
    }

    [Fact]
    public async Task Resume_stays_locked_when_biometrics_fail_and_keeps_the_session()
    {
        var store = new FakeSecureStore();
        var tokens = new AccessTokenStore();
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(Auth(DateTimeOffset.UtcNow.AddHours(8))));
        var manager = Manager(http, store, new FakeBiometrics(true, BiometricResult.Failed), tokens);
        await manager.LoginAsync("morgan@acme.test", "pw");
        tokens.AccessToken = null;

        var resume = await manager.TryResumeAsync();

        Assert.Equal(ResumeStatus.Locked, resume.Status);
        Assert.False(string.IsNullOrEmpty(store.Peek(SessionKey)));
    }

    [Fact]
    public async Task HandleUnauthorized_clears_the_session_and_raises_the_event()
    {
        var store = new FakeSecureStore();
        var tokens = new AccessTokenStore();
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(Auth()));
        var manager = Manager(http, store, new FakeBiometrics(false, BiometricResult.Unavailable), tokens);
        await manager.LoginAsync("morgan@acme.test", "pw");
        var raised = false;
        manager.SessionExpired += (_, _) => raised = true;

        manager.HandleUnauthorized();

        Assert.True(raised);
        Assert.Null(tokens.AccessToken);
        Assert.Null(store.Peek(SessionKey));
    }

    [Fact]
    public async Task Resume_silently_refreshes_an_expired_access_token()
    {
        var store = new FakeSecureStore();
        var tokens = new AccessTokenStore();
        var clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var http = FakeHttp.Routed(req => req.RequestUri!.AbsolutePath.EndsWith("refresh", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(AuthWithRefresh(clock.GetUtcNow().AddHours(8), "rt-2", "refreshed-token")) }
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(AuthWithRefresh(clock.GetUtcNow().AddHours(1), "rt-1")) });
        var manager = Manager(http, store, new FakeBiometrics(false, BiometricResult.Unavailable), tokens, clock);

        await manager.LoginAsync("morgan@acme.test", "pw");
        tokens.AccessToken = null;              // relaunch
        clock.Advance(TimeSpan.FromHours(2));   // access expired, refresh token still valid

        var resume = await manager.TryResumeAsync();

        Assert.Equal(ResumeStatus.Resumed, resume.Status);
        Assert.Equal("refreshed-token", tokens.AccessToken); // renewed without re-login
    }

    [Fact]
    public async Task Resume_reports_expired_when_the_refresh_token_is_rejected()
    {
        var store = new FakeSecureStore();
        var tokens = new AccessTokenStore();
        var clock = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var http = FakeHttp.Routed(req => req.RequestUri!.AbsolutePath.EndsWith("refresh", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(AuthWithRefresh(clock.GetUtcNow().AddHours(1), "rt-1")) });
        var manager = Manager(http, store, new FakeBiometrics(false, BiometricResult.Unavailable), tokens, clock);

        await manager.LoginAsync("morgan@acme.test", "pw");
        tokens.AccessToken = null;
        clock.Advance(TimeSpan.FromHours(2));

        var resume = await manager.TryResumeAsync();

        Assert.Equal(ResumeStatus.Expired, resume.Status);
        Assert.Null(store.Peek(SessionKey));
    }
}
