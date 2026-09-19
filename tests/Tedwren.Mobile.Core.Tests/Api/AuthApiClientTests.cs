using System.Net;
using System.Net.Http.Json;
using Tedwren.Abstractions.Contracts.Auth;
using Tedwren.Mobile.Core.Api;

namespace Tedwren.Mobile.Core.Tests.Api;

/// <summary>Verifies the console login client against stubbed HTTP responses.</summary>
public class AuthApiClientTests
{
    [Fact]
    public async Task LoginAsync_returns_result_on_success()
    {
        var expected = new AuthResultDto("token-123", DateTimeOffset.UtcNow.AddHours(8), "Sam Manager", "SiteManager", Guid.NewGuid());
        var http = FakeHttp.Returning(HttpStatusCode.OK, JsonContent.Create(expected));
        var client = new AuthApiClient(http);

        var result = await client.LoginAsync("sam@example.com", "pw");

        Assert.NotNull(result);
        Assert.Equal("token-123", result!.Token);
        Assert.Equal("SiteManager", result.Role);
    }

    [Fact]
    public async Task LoginAsync_returns_null_on_unauthorized()
    {
        var http = FakeHttp.Returning(HttpStatusCode.Unauthorized, new StringContent(string.Empty));
        var client = new AuthApiClient(http);

        Assert.Null(await client.LoginAsync("sam@example.com", "wrong"));
    }

    [Fact]
    public async Task LoginAsync_throws_on_unexpected_status()
    {
        var http = FakeHttp.Returning(HttpStatusCode.InternalServerError, new StringContent("boom"));
        var client = new AuthApiClient(http);

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.LoginAsync("sam@example.com", "pw"));
        Assert.Equal(500, ex.StatusCode);
    }
}
