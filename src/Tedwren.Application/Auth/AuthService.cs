using Tedwren.Abstractions.Contracts.Auth;
using Tedwren.Abstractions.Services;
using Tedwren.Application.Persistence;
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

    /// <summary>Creates the service over the user repository and token issuer.</summary>
    public AuthService(IUserRepository users, ITokenIssuer tokens)
    {
        _users = users;
        _tokens = tokens;
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
}
