using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Session;
using Tedwren.Mobile.Core.Tests.Api;

namespace Tedwren.Mobile.Core.Tests.Session;

/// <summary>Verifies operative enrolment, device-id persistence and biometric-gated resume in the session manager.</summary>
public class OperativeSessionManagerTests
{
    private const string RefreshKey = "tw.operative.refresh";

    private static MobileAuthResultDto Result(string refresh = "refresh") => new(
        "access", DateTimeOffset.UtcNow.AddMinutes(60), refresh, DateTimeOffset.UtcNow.AddDays(30),
        "Alex Operative", Guid.NewGuid(), Guid.NewGuid());

    private static OperativeSessionManager Manager(HttpClient http, FakeSecureStore store, FakeBiometrics biometrics)
        => new(new OperativeAuthApiClient(http), store, biometrics);

    [Fact]
    public async Task DeviceId_is_generated_once_and_stable()
    {
        var store = new FakeSecureStore();
        var manager = Manager(FakeHttp.Returning(HttpStatusCode.OK, new StringContent(string.Empty)), store, new FakeBiometrics(false, BiometricResult.Unavailable));

        var first = await manager.GetOrCreateDeviceIdAsync();
        var second = await manager.GetOrCreateDeviceIdAsync();

        Assert.False(string.IsNullOrEmpty(first));
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task RequestCode_rejects_an_unusable_number()
    {
        var manager = Manager(FakeHttp.Returning(HttpStatusCode.OK, new StringContent(string.Empty)), new FakeSecureStore(), new FakeBiometrics(false, BiometricResult.Unavailable));
        Assert.False(await manager.RequestCodeAsync("nonsense"));
        Assert.True(await manager.RequestCodeAsync("07700900123"));
    }

    [Fact]
    public async Task Enrol_success_stores_refresh_and_returns_operative_session()
    {
        var result = Result();
        var http = FakeHttp.Routed(req => req.RequestUri!.AbsolutePath.EndsWith("verify-otp", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(result) }
            : new HttpResponseMessage(HttpStatusCode.OK));
        var store = new FakeSecureStore();
        var manager = Manager(http, store, new FakeBiometrics(true, BiometricResult.Success));

        var enrol = await manager.EnrolAsync("07700900123", "123456", "Pixel");

        Assert.Equal(EnrolStatus.Success, enrol.Status);
        Assert.NotNull(enrol.Session);
        Assert.Equal(RoleHome.Operative, enrol.Session!.Home);
        Assert.Equal(result.PersonId, enrol.Session.PersonId);
        Assert.Equal("refresh", store.Peek(RefreshKey));
        Assert.True(await manager.IsEnrolledAsync());
    }

    [Fact]
    public async Task Enrol_invalid_code_does_not_store_refresh()
    {
        var store = new FakeSecureStore();
        var manager = Manager(FakeHttp.Returning(HttpStatusCode.Unauthorized, new StringContent(string.Empty)), store, new FakeBiometrics(false, BiometricResult.Unavailable));

        var enrol = await manager.EnrolAsync("07700900123", "000000", null);

        Assert.Equal(EnrolStatus.InvalidCode, enrol.Status);
        Assert.False(await manager.IsEnrolledAsync());
    }

    [Fact]
    public async Task Enrol_invalid_number_short_circuits()
    {
        var manager = Manager(FakeHttp.Returning(HttpStatusCode.OK, new StringContent(string.Empty)), new FakeSecureStore(), new FakeBiometrics(false, BiometricResult.Unavailable));
        var enrol = await manager.EnrolAsync("nope", "123456", null);
        Assert.Equal(EnrolStatus.InvalidNumber, enrol.Status);
    }

    [Fact]
    public async Task Resume_reports_not_enrolled_without_a_stored_token()
    {
        var manager = Manager(FakeHttp.Returning(HttpStatusCode.OK, new StringContent(string.Empty)), new FakeSecureStore(), new FakeBiometrics(true, BiometricResult.Success));
        var resume = await manager.TryResumeAsync();
        Assert.Equal(ResumeStatus.NotEnrolled, resume.Status);
    }

    [Fact]
    public async Task Resume_rotates_the_refresh_token_on_success()
    {
        var store = new FakeSecureStore();
        await store.SetAsync(RefreshKey, "old-refresh");
        var http = FakeHttp.Routed(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Result("new-refresh")) });
        var manager = Manager(http, store, new FakeBiometrics(true, BiometricResult.Success));

        var resume = await manager.TryResumeAsync();

        Assert.Equal(ResumeStatus.Resumed, resume.Status);
        Assert.Equal(RoleHome.Operative, resume.Session!.Home);
        Assert.Equal("new-refresh", store.Peek(RefreshKey));
    }

    [Fact]
    public async Task Resume_stays_locked_when_biometrics_fail()
    {
        var store = new FakeSecureStore();
        await store.SetAsync(RefreshKey, "old-refresh");
        var manager = Manager(FakeHttp.Returning(HttpStatusCode.OK, new StringContent(string.Empty)), store, new FakeBiometrics(true, BiometricResult.Failed));

        var resume = await manager.TryResumeAsync();

        Assert.Equal(ResumeStatus.Locked, resume.Status);
        Assert.Equal("old-refresh", store.Peek(RefreshKey));
    }

    [Fact]
    public async Task Resume_clears_a_rejected_refresh_token()
    {
        var store = new FakeSecureStore();
        await store.SetAsync(RefreshKey, "stale");
        var manager = Manager(FakeHttp.Returning(HttpStatusCode.Unauthorized, new StringContent(string.Empty)), store, new FakeBiometrics(false, BiometricResult.Unavailable));

        var resume = await manager.TryResumeAsync();

        Assert.Equal(ResumeStatus.Expired, resume.Status);
        Assert.Null(store.Peek(RefreshKey));
    }
}
