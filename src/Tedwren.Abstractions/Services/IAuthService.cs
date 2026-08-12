using Tedwren.Abstractions.Contracts.Auth;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// Console authentication: sign a user in with email + password, and let an invited user accept their
/// invitation by setting a password. Both return a bearer token the client uses for subsequent calls.
/// </summary>
public interface IAuthService
{
    /// <summary>Signs a user in. Returns null when the credentials are invalid or the account is not active.</summary>
    Task<AuthResultDto?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>Accepts an invitation (sets the password, activates the account). Null when the token is invalid/expired.</summary>
    Task<AuthResultDto?> AcceptInviteAsync(AcceptInviteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a password reset: emails a one-time reset link to the address when it belongs to an active
    /// account. Returns unconditionally and reveals nothing about whether the account exists, so the endpoint
    /// cannot be used to enumerate registered emails.
    /// </summary>
    Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
}
