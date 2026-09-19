using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>Verifies the operative auth client's status handling against stubbed HTTP responses.</summary>
public class OperativeAuthApiClientTests
{
    private static MobileAuthResultDto SampleResult() => new(
        "access", DateTimeOffset.UtcNow.AddMinutes(60), "refresh", DateTimeOffset.UtcNow.AddDays(30),
        "Alex Operative", Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public async Task VerifyOtp_returns_result_on_success()
    {
        var client = new OperativeAuthApiClient(FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(SampleResult())));
        var result = await client.VerifyOtpAsync(new VerifyOtpRequest("+447700900123", "123456", "dev1", "Pixel"));
        Assert.NotNull(result);
        Assert.Equal("access", result!.AccessToken);
    }

    [Fact]
    public async Task VerifyOtp_returns_null_on_invalid_code()
    {
        var client = new OperativeAuthApiClient(FakeHttp.Returning(HttpStatusCode.Unauthorized, new StringContent(string.Empty)));
        Assert.Null(await client.VerifyOtpAsync(new VerifyOtpRequest("+447700900123", "000000", "dev1", null)));
    }

    [Fact]
    public async Task VerifyOtp_throws_on_device_conflict()
    {
        var client = new OperativeAuthApiClient(FakeHttp.Returning(HttpStatusCode.Conflict, new StringContent("Already on another device")));
        await Assert.ThrowsAsync<DeviceConflictException>(() =>
            client.VerifyOtpAsync(new VerifyOtpRequest("+447700900123", "123456", "dev1", null)));
    }

    [Fact]
    public async Task Refresh_returns_result_on_success()
    {
        var client = new OperativeAuthApiClient(FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(SampleResult())));
        Assert.NotNull(await client.RefreshAsync("refresh", "dev1"));
    }

    [Fact]
    public async Task Refresh_returns_null_when_rejected()
    {
        var client = new OperativeAuthApiClient(FakeHttp.Returning(HttpStatusCode.Unauthorized, new StringContent(string.Empty)));
        Assert.Null(await client.RefreshAsync("stale", "dev1"));
    }
}
