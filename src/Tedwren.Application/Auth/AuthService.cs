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

    /// <summary>How long a password-reset link stays valid. Deliberately short — the link grants a password change.</summary>
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    /// <summary>Creates the service over the user repository, token issuer and email sender.</summary>
    public AuthService(IUserRepository users, ITokenIssuer tokens, IEmailSender email, EmailOptions emailOptions)
    {
        _users = users;
        _tokens = tokens;
        _email = email;
        _emailOptions = emailOptions;
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

        var token = _tokens.Issue(user);
        return new AuthResultDto(token.Token, token.ExpiresUtc, user.Name, user.Role.ToString(), user.CompanyId);
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

        var token = _tokens.Issue(user);
        return new AuthResultDto(token.Token, token.ExpiresUtc, user.Name, user.Role.ToString(), user.CompanyId);
    }

    /// <summary>
    /// Starts a password reset (D1): mints a one-time reset link and emails it to the address, but only when
    /// it belongs to an active account. Returns unconditionally so a caller cannot tell whether the email is
    /// registered (no account enumeration). The reset link reuses the one-time token the accept-invite flow
    /// consumes, so the set-new-password step is shared with invitation acceptance.
    /// </summary>
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
