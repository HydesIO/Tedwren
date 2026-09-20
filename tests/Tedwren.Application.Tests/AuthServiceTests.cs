using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Auth;
using Tedwren.Abstractions.Notifications;
using Tedwren.Application.Auth;
using Tedwren.Application.Persistence.InMemory;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies console auth refresh (M8): login issues an access + refresh token; refresh rotates and re-issues; a
/// pre-rotation, expired, revoked, unknown or malformed token is refused; and a suspended account cannot refresh.
/// </summary>
public sealed class AuthServiceTests
{
    private const string Email = "morgan@acme.test";
    private const string Password = "correct horse battery";

    private static Sut Create()
    {
        var userStore = new InMemoryUserStore();
        var users = new InMemoryUserRepository(userStore);
        var refresh = new InMemoryUserRefreshTokenRepository();
        var user = new User
        {
            CompanyId = Guid.NewGuid(),
            Name = "Morgan Manager",
            Email = Email,
            Role = AccessRole.SiteManager,
            Status = UserStatus.Active,
            PasswordHash = PasswordHasher.Hash(Password),
        };
        users.AddAsync(user).GetAwaiter().GetResult();

        var service = new AuthService(users, new FakeTokenIssuer(), new NoOpEmailSender(), new EmailOptions(), refresh, new JwtOptions());
        return new Sut(service, users, refresh, user);
    }

    [Fact]
    public async Task Login_issues_an_access_and_refresh_token()
    {
        var sut = Create();

        var result = await sut.Service.LoginAsync(new LoginRequest(Email, Password));

        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result!.Token));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.NotNull(result.RefreshTokenExpiresUtc);
    }

    [Fact]
    public async Task Refresh_rotates_and_reissues()
    {
        var sut = Create();
        var login = await sut.Service.LoginAsync(new LoginRequest(Email, Password));

        var refreshed = await sut.Service.RefreshAsync(new RefreshConsoleTokenRequest(login!.RefreshToken!));

        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrEmpty(refreshed!.RefreshToken));
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken); // rotated
    }

    [Fact]
    public async Task Refresh_rejects_the_pre_rotation_token()
    {
        var sut = Create();
        var login = await sut.Service.LoginAsync(new LoginRequest(Email, Password));
        await sut.Service.RefreshAsync(new RefreshConsoleTokenRequest(login!.RefreshToken!)); // rotates

        // The original token was rotated away — reusing it must be refused.
        var reused = await sut.Service.RefreshAsync(new RefreshConsoleTokenRequest(login.RefreshToken!));

        Assert.Null(reused);
    }

    [Fact]
    public async Task Refresh_rejects_unknown_and_malformed_tokens()
    {
        var sut = Create();

        Assert.Null(await sut.Service.RefreshAsync(new RefreshConsoleTokenRequest("not-a-token")));
        Assert.Null(await sut.Service.RefreshAsync(new RefreshConsoleTokenRequest($"{Guid.NewGuid():N}.deadbeef")));
        Assert.Null(await sut.Service.RefreshAsync(new RefreshConsoleTokenRequest(string.Empty)));
    }

    [Fact]
    public async Task Refresh_rejects_an_expired_token()
    {
        var sut = Create();
        // Seed a token whose row is already expired, then present the matching opaque token.
        var id = Guid.NewGuid();
        const string secret = "c2VjcmV0LXNlY3JldC1zZWNyZXQtc2VjcmV0LTAxMjM=";
        await sut.RefreshTokens.AddAsync(new UserRefreshToken
        {
            Id = id,
            UserId = sut.User.Id,
            CompanyId = sut.User.CompanyId,
            TokenHash = PasswordHasher.Hash(secret),
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(-1),
            CreatedUtc = DateTimeOffset.UtcNow.AddDays(-31),
            LastUsedUtc = DateTimeOffset.UtcNow.AddDays(-31),
        });

        Assert.Null(await sut.Service.RefreshAsync(new RefreshConsoleTokenRequest($"{id:N}.{secret}")));
    }

    [Fact]
    public async Task Refresh_rejects_a_suspended_account()
    {
        var sut = Create();
        var login = await sut.Service.LoginAsync(new LoginRequest(Email, Password));
        sut.User.Status = UserStatus.Suspended;
        await sut.Users.UpdateAsync(sut.User);

        Assert.Null(await sut.Service.RefreshAsync(new RefreshConsoleTokenRequest(login!.RefreshToken!)));
    }

    [Fact]
    public async Task Refresh_rejects_a_revoked_token()
    {
        var sut = Create();
        var login = await sut.Service.LoginAsync(new LoginRequest(Email, Password));
        await sut.RefreshTokens.RevokeAllForUserAsync(sut.User.Id, DateTimeOffset.UtcNow);

        Assert.Null(await sut.Service.RefreshAsync(new RefreshConsoleTokenRequest(login!.RefreshToken!)));
    }

    private sealed record Sut(AuthService Service, InMemoryUserRepository Users, InMemoryUserRefreshTokenRepository RefreshTokens, User User);

    private sealed class FakeTokenIssuer : ITokenIssuer
    {
        public IssuedToken Issue(User user) => new($"access-{user.Id:N}", DateTimeOffset.UtcNow.AddMinutes(480));
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendHtmlAsync(string toEmail, string subject, string contentHtml, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
