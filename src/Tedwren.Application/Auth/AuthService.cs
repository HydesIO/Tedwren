using System.Security.Cryptography;
using Tedwren.Abstractions.Configuration;
using Tedwren.Abstractions.Contracts.Auth;
using Tedwren.Abstractions.Notifications;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Notifications.Email;
using Tedwren.Application.Persistence;
using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;

namespace Tedwren.Application.Auth;

/// <summary>
/// Console authentication (D1). Verifies email + password for sign-in, and accepts an invitation by setting
/// the password and activating the account. Suspended/invited accounts cannot sign in. Successful auth
/// yields a bearer token from the injected <see cref="ITokenIssuer"/>.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly ITokenIssuer _tokens;
    private readonly IEmailSender _email;
    private readonly EmailOptions _emailOptions;
    private readonly IUserRefreshTokenRepository _refreshTokens;
    private readonly JwtOptions _jwt;

    /// <summary>How long a password-reset link stays valid. Deliberately short — the link grants a password change.</summary>
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    /// <summary>Creates the service over the user + refresh-token repositories, token issuer, email sender and JWT options.</summary>
    public AuthService(
        IUserRepository users,
        ITokenIssuer tokens,
        IEmailSender email,
        EmailOptions emailOptions,
        IUserRefreshTokenRepository refreshTokens,
        JwtOptions jwt)
    {
        _users = users;
        _tokens = tokens;
        _email = email;
        _emailOptions = emailOptions;
        _refreshTokens = refreshTokens;
        _jwt = jwt;
    }

    /// <summary>Signs a user in. Null when credentials are invalid or the account is not active.</summary>
    public async Task<AuthResultDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = (request.Email ?? string.Empty).Trim();
        if (email.Length == 0 || string.IsNullOrEmpty(request.Password))
        {
            return null;
        }

        var user = await _users.GetByEmailAsync(email, cancellationToken);
        if (user is null || user.Status != UserStatus.Active || !PasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        user.LastActiveUtc = DateTimeOffset.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        return await IssueAsync(user, cancellationToken);
    }

    /// <summary>Accepts an invitation: sets the password, activates the account, clears the token. Null when invalid/expired.</summary>
    public async Task<AuthResultDto?> AcceptInviteAsync(AcceptInviteRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrEmpty(request.Password) || request.Password.Length < 8)
        {
            return null;
        }

        var user = await _users.GetByInviteTokenAsync(request.Token.Trim(), cancellationToken);
        if (user is null || user.InviteTokenExpiresUtc is null || user.InviteTokenExpiresUtc < DateTimeOffset.UtcNow)
        {
            return null;
        }

        user.PasswordHash = PasswordHasher.Hash(request.Password);
        user.PasswordSetUtc = DateTimeOffset.UtcNow;
        user.Status = UserStatus.Active;
        user.InviteToken = null;
        user.InviteTokenExpiresUtc = null;
        user.LastActiveUtc = DateTimeOffset.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);

        return await IssueAsync(user, cancellationToken);
    }

    /// <summary>Exchanges a valid console refresh token for a fresh access token, rotating the refresh token (M8).</summary>
    public async Task<AuthResultDto?> RefreshAsync(RefreshConsoleTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (!TrySplitToken(request.RefreshToken, out var id, out var secret))
        {
            return null;
        }

        var row = await _refreshTokens.GetByIdAsync(id, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        if (row is null || !row.CanUse(now) || !PasswordHasher.Verify(secret, row.TokenHash))
        {
            return null;
        }

        // The account must still be active (a suspended/removed user's live sessions are refused).
        var user = await _users.GetByIdAsync(row.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            return null;
        }

        // Rotate the secret in place (the id/selector is stable) so a stolen pre-rotation token is refused next time.
        var newSecret = NewSecret();
        row.TokenHash = PasswordHasher.Hash(newSecret);
        row.ExpiresUtc = now.AddDays(_jwt.RefreshLifetimeDays);
        row.LastUsedUtc = now;
        await _refreshTokens.UpdateAsync(row, cancellationToken);

        var token = _tokens.Issue(user);
        return new AuthResultDto(
            token.Token, token.ExpiresUtc, user.Name, user.Role.ToString(), user.CompanyId, $"{id:N}.{newSecret}", row.ExpiresUtc);
    }

    /// <summary>Issues an access token plus a freshly-stored refresh token for a signed-in/activated user (M8).</summary>
    private async Task<AuthResultDto> IssueAsync(User user, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();
        var secret = NewSecret();
        var expiresUtc = now.AddDays(_jwt.RefreshLifetimeDays);
        await _refreshTokens.AddAsync(
            new UserRefreshToken
            {
                Id = id,
                UserId = user.Id,
                CompanyId = user.CompanyId,
                TokenHash = PasswordHasher.Hash(secret),
                ExpiresUtc = expiresUtc,
                CreatedUtc = now,
                LastUsedUtc = now,
            },
            cancellationToken);

        var token = _tokens.Issue(user);
        return new AuthResultDto(
            token.Token, token.ExpiresUtc, user.Name, user.Role.ToString(), user.CompanyId, $"{id:N}.{secret}", expiresUtc);
    }

    /// <summary>Generates a new opaque token secret (32 random bytes, base64).</summary>
    private static string NewSecret() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>Splits an opaque refresh token <c>{id:N}.{secret}</c> into its selector id and secret; false when malformed.</summary>
    private static bool TrySplitToken(string? token, out Guid id, out string secret)
    {
        id = Guid.Empty;
        secret = string.Empty;
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        var dot = token.IndexOf('.');
        if (dot <= 0 || dot == token.Length - 1 || !Guid.TryParseExact(token[..dot], "N", out id))
        {
            return false;
        }

        secret = token[(dot + 1)..];
        return true;
    }
    public async Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = (request.Email ?? string.Empty).Trim();
        if (email.Length == 0)
        {
            return;
        }

        var user = await _users.GetByEmailAsync(email, cancellationToken);
        // Silently no-op for unknown or non-active accounts: never reveal whether the email exists, and never
        // hand a reset link to an invited (not-yet-activated) or suspended account.
        if (user is null || user.Status != UserStatus.Active)
        {
            return;
        }

        var resetToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        user.InviteToken = resetToken;
        user.InviteTokenExpiresUtc = DateTimeOffset.UtcNow.Add(ResetTokenLifetime);
        await _users.UpdateAsync(user, cancellationToken);

        await SendResetEmailAsync(user, resetToken, cancellationToken);
    }

    /// <summary>
    /// Emails the branded password-reset link (best-effort). Any delivery failure is swallowed so a transient
    /// mail fault never surfaces to the caller (which would also leak that the account exists).
    /// </summary>
    private async Task SendResetEmailAsync(User user, string resetToken, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = (_emailOptions.ConsoleBaseUrl ?? string.Empty).TrimEnd('/');
            var resetUrl = $"{baseUrl}/reset-password?token={resetToken}";
            var content = PasswordResetEmail.BuildContent(user.Name, resetUrl, user.InviteTokenExpiresUtc);
            await _email.SendHtmlAsync(user.Email, PasswordResetEmail.Subject, content, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Best-effort delivery: the reset token is already persisted; a mail failure must not be observable.
        }
    }
}
