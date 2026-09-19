namespace Tedwren.Abstractions.Contracts.Mobile;

/// <summary>
/// Step 1 of operative sign-in: request a one-time code by mobile number (SF-1). The server always responds
/// the same way whether or not the number matches an engaged operative (no account enumeration); the SMS is
/// only sent when it does.
/// </summary>
public sealed record RequestOtpRequest(string MobileNumber);

/// <summary>
/// Step 2 of operative sign-in: verify the one-time code and bind this device to the operative (one operative
/// per device, one active device per operative — a deterrent against buddy-punching). Returns access + refresh
/// tokens on success.
/// </summary>
public sealed record VerifyOtpRequest(string MobileNumber, string Code, string DeviceId, string? DeviceName);

/// <summary>Exchanges a still-valid refresh token (bound to this device) for a fresh access token, rotating the refresh token.</summary>
public sealed record RefreshTokenRequest(string RefreshToken, string DeviceId);

/// <summary>
/// A successful operative authentication: a short-lived bearer access token plus a long-lived, device-bound,
/// rotating refresh token, and the resolved identity. The refresh token is stored on the device in encrypted
/// secure storage (gated by biometric unlock); the access token is held in memory.
/// </summary>
public sealed record MobileAuthResultDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresUtc,
    string Name,
    Guid PersonId,
    Guid CompanyId);
