using Tedwren.Abstractions.Contracts.Mobile;

namespace Tedwren.Abstractions.Services;

/// <summary>The outcome kind of an operative verify / refresh attempt, mapped by the API to an HTTP status.</summary>
public enum OperativeAuthStatus
{
    /// <summary>Authenticated; a token result is present.</summary>
    Success,

    /// <summary>The one-time code (or refresh token) was wrong, expired or used up → 401.</summary>
    Invalid,

    /// <summary>The operative is already bound to another device, or this device to another operative → 409; an administrator must move them.</summary>
    DeviceConflict,
}

/// <summary>The result of an operative verify / refresh attempt.</summary>
public sealed record OperativeAuthOutcome(OperativeAuthStatus Status, MobileAuthResultDto? Result, string? Message)
{
    /// <summary>A successful authentication carrying the token result.</summary>
    public static OperativeAuthOutcome Ok(MobileAuthResultDto result) => new(OperativeAuthStatus.Success, result, null);

    /// <summary>A rejected attempt (bad/expired code or refresh token).</summary>
    public static OperativeAuthOutcome Invalid(string message) => new(OperativeAuthStatus.Invalid, null, message);

    /// <summary>A device-binding conflict that needs an administrator to resolve.</summary>
    public static OperativeAuthOutcome Conflict(string message) => new(OperativeAuthStatus.DeviceConflict, null, message);
}

/// <summary>
/// Operative (mobile) authentication: mobile-number + one-time-code enrolment, device binding and token
/// refresh. Operatives are <c>Person</c>/<c>Engagement</c> records (SF-1), not console <c>User</c> accounts, so
/// this is a separate surface from <see cref="IAuthService"/>. Biometric unlock is entirely on-device (R17).
/// </summary>
public interface IOperativeAuthService
{
    /// <summary>
    /// Sends a one-time code by SMS to an engaged operative with this mobile number. Always completes without
    /// revealing whether the number is known (no enumeration); the SMS is only sent when a match exists.
    /// </summary>
    Task RequestOtpAsync(RequestOtpRequest request, CancellationToken cancellationToken = default);

    /// <summary>Verifies the one-time code, binds this device to the operative, and issues access + refresh tokens.</summary>
    Task<OperativeAuthOutcome> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default);

    /// <summary>Rotates a valid, device-bound refresh token for a fresh access token (and a new refresh token).</summary>
    Task<OperativeAuthOutcome> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
