using Tedwren.Abstractions.Contracts.Mobile;
using Tedwren.Mobile.Core.Api;
using Tedwren.Mobile.Core.Identity;
using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Core.Session;

/// <summary>The outcome of an operative enrolment attempt.</summary>
public enum EnrolStatus
{
    /// <summary>Enrolled and signed in.</summary>
    Success,

    /// <summary>The one-time code was wrong or expired.</summary>
    InvalidCode,

    /// <summary>The mobile number could not be parsed to a usable form.</summary>
    InvalidNumber,
}

/// <summary>The outcome of a resume-on-launch attempt.</summary>
public enum ResumeStatus
{
    /// <summary>Resumed a session with a fresh access token.</summary>
    Resumed,

    /// <summary>No stored enrolment on this device — the operative must enrol.</summary>
    NotEnrolled,

    /// <summary>Biometric unlock was required but not satisfied — stay locked.</summary>
    Locked,

    /// <summary>The stored refresh token was rejected (revoked/expired) — the operative must enrol again.</summary>
    Expired,
}

/// <summary>An enrolment attempt result, carrying the session on success.</summary>
public sealed record EnrolResult(EnrolStatus Status, MobileSession? Session);

/// <summary>A resume attempt result, carrying the session when resumed.</summary>
public sealed record ResumeResult(ResumeStatus Status, MobileSession? Session);

/// <summary>
/// Orchestrates operative sign-in on the device (M2): a stable per-device id, mobile-number + one-time-code
/// enrolment that binds this device, and biometric-gated resume that rotates the refresh token on launch. The
/// refresh token lives only in encrypted secure storage (never in plain app state); biometrics are a local
/// unlock, not identity verification (R17).
/// </summary>
public sealed class OperativeSessionManager : ISessionRefresher
{
    private const string DeviceIdKey = "tw.device_id";
    private const string RefreshTokenKey = "tw.operative.refresh";

    private readonly OperativeAuthApiClient _auth;
    private readonly ISecureStore _store;
    private readonly IBiometricAuthenticator _biometrics;

    /// <summary>Creates the manager over the auth client, secure store and biometric authenticator.</summary>
    public OperativeSessionManager(OperativeAuthApiClient auth, ISecureStore store, IBiometricAuthenticator biometrics)
    {
        _auth = auth;
        _store = store;
        _biometrics = biometrics;
    }

    /// <summary>Returns this device's stable id, generating and persisting one on first use.</summary>
    public async Task<string> GetOrCreateDeviceIdAsync()
    {
        var existing = await _store.GetAsync(DeviceIdKey);
        if (!string.IsNullOrEmpty(existing))
        {
            return existing;
        }

        var deviceId = Guid.NewGuid().ToString("N");
        await _store.SetAsync(DeviceIdKey, deviceId);
        return deviceId;
    }

    /// <summary>Whether this device already holds an operative enrolment (a stored refresh token).</summary>
    public async Task<bool> IsEnrolledAsync() => !string.IsNullOrEmpty(await _store.GetAsync(RefreshTokenKey));

    /// <summary>Requests a one-time code for the given mobile number; returns false when the number is unusable.</summary>
    public async Task<bool> RequestCodeAsync(string mobileNumber, CancellationToken cancellationToken = default)
    {
        var normalised = MobilePhoneNumber.Normalise(mobileNumber);
        if (normalised is null)
        {
            return false;
        }

        await _auth.RequestOtpAsync(normalised, cancellationToken);
        return true;
    }

    /// <summary>
    /// Verifies the one-time code and binds this device, persisting the refresh token in secure storage on
    /// success. Throws <see cref="DeviceConflictException"/> when the operative/device is already bound elsewhere.
    /// </summary>
    public async Task<EnrolResult> EnrolAsync(string mobileNumber, string code, string? deviceName, CancellationToken cancellationToken = default)
    {
        var normalised = MobilePhoneNumber.Normalise(mobileNumber);
        if (normalised is null)
        {
            return new EnrolResult(EnrolStatus.InvalidNumber, null);
        }

        var deviceId = await GetOrCreateDeviceIdAsync();
        var result = await _auth.VerifyOtpAsync(new VerifyOtpRequest(normalised, code, deviceId, deviceName), cancellationToken);
        if (result is null)
        {
            return new EnrolResult(EnrolStatus.InvalidCode, null);
        }

        await _store.SetAsync(RefreshTokenKey, result.RefreshToken);
        return new EnrolResult(EnrolStatus.Success, ToSession(result));
    }

    /// <summary>
    /// Attempts to resume a session on launch: requires biometric unlock (when available), then rotates the
    /// stored refresh token for a fresh access token. Clears the stored token when the server rejects it.
    /// </summary>
    public async Task<ResumeResult> TryResumeAsync(CancellationToken cancellationToken = default)
    {
        var refreshToken = await _store.GetAsync(RefreshTokenKey);
        if (string.IsNullOrEmpty(refreshToken))
        {
            return new ResumeResult(ResumeStatus.NotEnrolled, null);
        }

        // Local unlock: when biometrics are enrolled, they must succeed before the stored token is used. When no
        // biometric hardware/enrolment exists we proceed (the token is still protected by the OS secure store);
        // a PIN fallback is a later enhancement.
        if (await _biometrics.IsAvailableAsync())
        {
            var unlock = await _biometrics.AuthenticateAsync("Unlock Tedwren");
            if (unlock != BiometricResult.Success)
            {
                return new ResumeResult(ResumeStatus.Locked, null);
            }
        }

        var deviceId = await GetOrCreateDeviceIdAsync();
        var result = await _auth.RefreshAsync(refreshToken, deviceId, cancellationToken);
        if (result is null)
        {
            _store.Remove(RefreshTokenKey);
            return new ResumeResult(ResumeStatus.Expired, null);
        }

        await _store.SetAsync(RefreshTokenKey, result.RefreshToken);
        return new ResumeResult(ResumeStatus.Resumed, ToSession(result));
    }

    /// <summary>
    /// Silently rotates the stored refresh token for a fresh access token — no biometric prompt (used by the
    /// auth handler when an access token expires mid-session). Clears the stored token and returns null when the
    /// server rejects it, so the app re-enrols. Distinct from <see cref="TryResumeAsync"/>, which biometric-gates
    /// on launch.
    /// </summary>
    public async Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var refreshToken = await _store.GetAsync(RefreshTokenKey);
        if (string.IsNullOrEmpty(refreshToken))
        {
            return null;
        }

        var deviceId = await GetOrCreateDeviceIdAsync();
        var result = await _auth.RefreshAsync(refreshToken, deviceId, cancellationToken);
        if (result is null)
        {
            _store.Remove(RefreshTokenKey);
            return null;
        }

        await _store.SetAsync(RefreshTokenKey, result.RefreshToken);
        return result.AccessToken;
    }

    /// <summary>Signs out on this device by discarding the stored refresh token (the device stays bound server-side until revoked).</summary>
    public void SignOut() => _store.Remove(RefreshTokenKey);

    private static MobileSession ToSession(MobileAuthResultDto result) =>
        new(result.AccessToken, result.AccessTokenExpiresUtc, result.Name, RoleHomeResolver.OperativeRole, result.CompanyId)
        {
            PersonId = result.PersonId,
        };
}
