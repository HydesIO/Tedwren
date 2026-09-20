using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Contracts.Auth;
using Tedwren.Abstractions.Contracts.Users;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for the M8 console refresh flow: login returns a refresh token; <c>POST /api/auth/refresh</c>
/// rotates + re-issues; a rotated (pre-rotation) token is rejected; an unknown token is rejected; a suspended
/// account cannot refresh. Runs with the test bypass OFF so the real JWT path is exercised over a shared store.
/// </summary>
public sealed class ConsoleRefreshApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AdminEmail = "admin@tedwren.local";
    private const string AdminPassword = "ChangeMe12345";
    private static readonly Guid CompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    private readonly WebApplicationFactory<Program> _auth;

    public ConsoleRefreshApiTests(WebApplicationFactory<Program> factory) =>
        _auth = factory.WithWebHostBuilder(b => b.UseSetting("Auth:TestBypass", "false"));

    private async Task<AuthResultDto> LoginAsync(string email, string password)
    {
        var login = await _auth.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return (await login.Content.ReadFromJsonAsync<AuthResultDto>())!;
    }

    [Fact]
    public async Task Login_returns_a_refresh_token()
    {
        var auth = await LoginAsync(AdminEmail, AdminPassword);
        Assert.False(string.IsNullOrEmpty(auth.RefreshToken));
        Assert.NotNull(auth.RefreshTokenExpiresUtc);
    }

    [Fact]
    public async Task Refresh_returns_a_fresh_access_and_refresh_token()
    {
        var auth = await LoginAsync(AdminEmail, AdminPassword);

        var response = await _auth.CreateClient().PostAsJsonAsync("/api/auth/refresh", new RefreshConsoleTokenRequest(auth.RefreshToken!));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshed = (await response.Content.ReadFromJsonAsync<AuthResultDto>())!;
        Assert.False(string.IsNullOrEmpty(refreshed.Token));
        Assert.False(string.IsNullOrEmpty(refreshed.RefreshToken));
        Assert.NotEqual(auth.RefreshToken, refreshed.RefreshToken);
        Assert.Equal("Administrator", refreshed.Role);
    }

    [Fact]
    public async Task Refresh_rejects_the_pre_rotation_token()
    {
        var auth = await LoginAsync(AdminEmail, AdminPassword);
        var first = await _auth.CreateClient().PostAsJsonAsync("/api/auth/refresh", new RefreshConsoleTokenRequest(auth.RefreshToken!));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // The original token was rotated away — reusing it must be refused.
        var reused = await _auth.CreateClient().PostAsJsonAsync("/api/auth/refresh", new RefreshConsoleTokenRequest(auth.RefreshToken!));
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
    }

    [Fact]
    public async Task Refresh_rejects_an_unknown_token()
    {
        var response = await _auth.CreateClient().PostAsJsonAsync("/api/auth/refresh", new RefreshConsoleTokenRequest($"{Guid.NewGuid():N}.nope"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_rejects_a_suspended_account()
    {
        var (email, password) = await InviteAndAcceptAsync("ComplianceManager");
        var auth = await LoginAsync(email, password);

        using (var scope = _auth.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var user = (await users.GetByEmailAsync(email))!;
            user.Status = UserStatus.Suspended;
            await users.UpdateAsync(user);
        }

        var response = await _auth.CreateClient().PostAsJsonAsync("/api/auth/refresh", new RefreshConsoleTokenRequest(auth.RefreshToken!));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<(string Email, string Password)> InviteAndAcceptAsync(string role)
    {
        var admin = _auth.CreateClient();
        var adminAuth = await LoginAsync(AdminEmail, AdminPassword);
        admin.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminAuth.Token);

        var email = $"user-{Guid.NewGuid():N}@example.com";
        var invite = await admin.PostAsJsonAsync("/api/users", new InviteUserRequest(CompanyId, "Test User", email, role));
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
        var result = (await invite.Content.ReadFromJsonAsync<InviteUserResult>())!;

        const string password = "S3curePassw0rd";
        var accept = await _auth.CreateClient().PostAsJsonAsync("/api/auth/accept-invite", new AcceptInviteRequest(result.AcceptToken, password));
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        return (email, password);
    }
}
