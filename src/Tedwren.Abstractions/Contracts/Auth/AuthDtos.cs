namespace Tedwren.Abstractions.Contracts.Auth;

/// <summary>Credentials for console sign-in.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Accept an invitation and set the account password.</summary>
public sealed record AcceptInviteRequest(string Token, string Password);

/// <summary>Request a password-reset link to be emailed to the given address (D1).</summary>
public sealed record ForgotPasswordRequest(string Email);

/// <summary>Exchange a console refresh token for a fresh access token (M8). No device id — managers have none.</summary>
public sealed record RefreshConsoleTokenRequest(string RefreshToken);

/// <summary>
/// The result of a successful sign-in / invite acceptance: a bearer token plus the signed-in identity. From M8 it
/// also carries a <see cref="RefreshToken"/> (and its expiry) so a console/mobile-manager client can renew the
/// 8-hour access token without re-login; both are null for callers/paths that do not issue one.
/// </summary>
public sealed record AuthResultDto(
    string Token,
    DateTimeOffset ExpiresUtc,
    string Name,
    string Role,
    Guid CompanyId,
    string? RefreshToken = null,
    DateTimeOffset? RefreshTokenExpiresUtc = null);
