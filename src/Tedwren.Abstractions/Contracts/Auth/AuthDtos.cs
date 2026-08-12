namespace Tedwren.Abstractions.Contracts.Auth;

/// <summary>Credentials for console sign-in.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Accept an invitation and set the account password.</summary>
public sealed record AcceptInviteRequest(string Token, string Password);

/// <summary>Request a password-reset link to be emailed to the given address (D1).</summary>
public sealed record ForgotPasswordRequest(string Email);

/// <summary>The result of a successful sign-in / invite acceptance: a bearer token plus the signed-in identity.</summary>
public sealed record AuthResultDto(
    string Token,
    DateTimeOffset ExpiresUtc,
    string Name,
    string Role,
    Guid CompanyId);
