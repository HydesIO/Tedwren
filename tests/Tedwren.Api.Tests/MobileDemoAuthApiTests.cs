using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Abstractions.Contracts.Mobile;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end HTTP tests for the Development-only operative demo sign-in (<c>/api/mobile/auth/demo-sign-in</c>) used
/// by the browser emulator. They prove it is fail-closed (the route is not even mapped when <c>Demo:Enabled</c> is
/// false) and that, when enabled with the demo dataset seeded, it mints a real operative token.
/// </summary>
public sealed class MobileDemoAuthApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MobileDemoAuthApiTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient CreateClient(bool demoEnabled) =>
        _factory.WithWebHostBuilder(b => b
                .UseSetting("Jobs:SchedulerEnabled", "false")
                .UseSetting("Demo:Enabled", demoEnabled ? "true" : "false"))
            .CreateClient();

    [Fact]
    public async Task Demo_sign_in_mints_an_operative_token_when_enabled_and_seeded()
    {
        var client = CreateClient(demoEnabled: true);
        (await client.PostAsync("/api/admin/demo-data/seed", content: null)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync(
            "/api/mobile/auth/demo-sign-in",
            new DemoSignInRequest("operative@tedwren.com", "test-device", "Emulator"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MobileAuthResultDto>();
        Assert.NotNull(result);
        Assert.Equal("Demo Operative", result!.Name);
        Assert.NotEqual(Guid.Empty, result.PersonId);

        // The minted token is a real operative (tedwren-mobile) JWT with the Operative role + a bound device id.
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Contains("tedwren-mobile", token.Audiences);
        Assert.Contains(token.Claims, c => c.Type == "device_id" && c.Value == "test-device");
        Assert.Contains(token.Claims, c => c.Value == "Operative");
    }

    [Fact]
    public async Task Demo_sign_in_endpoint_is_absent_when_disabled()
    {
        var client = CreateClient(demoEnabled: false);

        var response = await client.PostAsJsonAsync(
            "/api/mobile/auth/demo-sign-in",
            new DemoSignInRequest("operative@tedwren.com", "test-device", null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Demo_sign_in_rejects_an_unknown_email()
    {
        var client = CreateClient(demoEnabled: true);

        var response = await client.PostAsJsonAsync(
            "/api/mobile/auth/demo-sign-in",
            new DemoSignInRequest("nobody@example.com", "test-device", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
