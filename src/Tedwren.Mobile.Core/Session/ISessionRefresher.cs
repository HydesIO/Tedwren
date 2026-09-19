namespace Tedwren.Mobile.Core.Session;

/// <summary>
/// Silently exchanges the stored refresh token for a fresh access token — no biometric prompt (biometrics gate
/// app launch, not mid-session token expiry). Used by the auth message handler when a call returns 401.
/// </summary>
public interface ISessionRefresher
{
    /// <summary>Returns a new access token, or null when the refresh token is missing/rejected (the app must re-enrol).</summary>
    Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default);
}
