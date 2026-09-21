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
/// Development/demo-only operative sign-in used by the browser emulator: exchanges a known demo email
/// (<c>operative@tedwren.com</c>) for a real operative token bound to this device, without an SMS one-time code
/// (which a browser can't receive). Only honoured when <c>Demo:Enabled</c> is true and the environment is not
/// Production — the endpoint is not even mapped otherwise, so it is fail-closed.
/// </summary>
public sealed record DemoSignInRequest(string Email, string DeviceId, string? DeviceName);

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
