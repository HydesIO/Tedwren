using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Abstractions.Contracts.Auth;
using Tedwren.Abstractions.Contracts.Identity;
using Tedwren.Abstractions.Contracts.Users;
using Tedwren.Application.Persistence;
using Xunit;

namespace Tedwren.Api.Tests;

/// <summary>
/// End-to-end tests for authentication (D1): sign in as the seeded admin, invite → accept (sets password) →
/// login, JWT-protected endpoints (401 without a token), the read-only Auditor guard (SF-23), and anonymous
/// recipient access. All requests run against ONE host with the test bypass OFF so the real JWT path is
/// exercised and the in-memory store is shared.
/// </summary>
public sealed class AuthApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string AdminEmail = "admin@tedwren.local";
    private const string AdminPassword = "ChangeMe12345";
    private static readonly Guid CompanyId = Guid.Parse("22222222-2222-4222-8222-000000000001");

    private readonly WebApplicationFactory<Program> _auth;

    /// <summary>Builds a single host with real auth (bypass off) so all clients share one in-memory store.</summary>
    public AuthApiTests(WebApplicationFactory<Program> factory) =>
        _auth = factory.WithWebHostBuilder(b => b.UseSetting("Auth:TestBypass", "false"));

    private async Task<HttpClient> SignedInAsync(string email, string password)
    {
        var client = _auth.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = (await login.Content.ReadFromJsonAsync<AuthResultDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    private async Task<(string Email, string Password)> InviteAndAcceptAsync(string role)
    {
        var admin = await SignedInAsync(AdminEmail, AdminPassword);
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var invite = await admin.PostAsJsonAsync("/api/users", new InviteUserRequest(CompanyId, "Test User", email, role));
        Assert.Equal(HttpStatusCode.Created, invite.StatusCode);
        var result = (await invite.Content.ReadFromJsonAsync<InviteUserResult>())!;

        const string password = "S3curePassw0rd";
        var accept = await _auth.CreateClient().PostAsJsonAsync("/api/auth/accept-invite", new AcceptInviteRequest(result.AcceptToken, password));
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        return (email, password);
    }

    [Fact]
    public async Task Invite_Accept_Login_Flow()
    {
        var (email, password) = await InviteAndAcceptAsync("ComplianceManager");

        var bad = await _auth.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "wrong"));
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);

        var good = await _auth.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        var auth = (await good.Content.ReadFromJsonAsync<AuthResultDto>())!;
        Assert.Equal("ComplianceManager", auth.Role);
        Assert.Equal(CompanyId, auth.CompanyId);
    }

    [Fact]
    public async Task ProtectedEndpoint_Requires_Token()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _auth.CreateClient().GetAsync("/api/users")).StatusCode);

        var client = await SignedInAsync(AdminEmail, AdminPassword);
        var me = await client.GetFromJsonAsync<CurrentUserDto>("/api/me");
        Assert.Equal("Administrator", me!.Role);
        Assert.Equal(CompanyId, me.CompanyId);
    }

    [Fact]
    public async Task Auditor_CanRead_ButCannotWrite()
    {
        var (email, password) = await InviteAndAcceptAsync("Auditor");
        var client = await SignedInAsync(email, password);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/users")).StatusCode);

        var write = await client.PostAsJsonAsync("/api/users",
            new InviteUserRequest(CompanyId, "Nope", $"nope-{Guid.NewGuid():N}@example.com", "Auditor"));
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }

    [Fact]
    public async Task RecipientEndpoint_StaysAnonymous()
    {
        var response = await _auth.CreateClient().GetAsync($"/api/site-entry/muster/{Guid.NewGuid()}");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_IsAnonymous_AndDoesNotEnumerate()
    {
        // Unknown address: still 200, so a caller can't tell a registered email from an unregistered one (D1).
        var unknown = await _auth.CreateClient()
            .PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest($"nobody-{Guid.NewGuid():N}@example.com"));
        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_Then_Reset_Then_Login()
    {
        var (email, oldPassword) = await InviteAndAcceptAsync("ComplianceManager");

        // Request a reset; the endpoint reveals nothing, so read the minted token from the shared store.
        var forgot = await _auth.CreateClient().PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest(email));
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);

        using var scope = _auth.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await users.GetByEmailAsync(email);
        Assert.NotNull(user!.InviteToken);

        // Set a new password with the reset token (same endpoint the reset page posts to).
        const string newPassword = "N3wSecurePass!";
        var reset = await _auth.CreateClient()
            .PostAsJsonAsync("/api/auth/accept-invite", new AcceptInviteRequest(user.InviteToken!, newPassword));
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        // The old password no longer works; the new one signs in.
        var withOld = await _auth.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, oldPassword));
        Assert.Equal(HttpStatusCode.Unauthorized, withOld.StatusCode);

        var withNew = await _auth.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, newPassword));
        Assert.Equal(HttpStatusCode.OK, withNew.StatusCode);
    }
}
