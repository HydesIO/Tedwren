namespace Tedwren.Mobile.Core.Session;

/// <summary>
/// Silently exchanges the stored console refresh token for a fresh access token (M8) — no biometric prompt
/// (biometrics gate app launch, not mid-session token expiry). Used by the manager auth message handler on a 401.
/// Distinct from the operative <see cref="ISessionRefresher"/> so the two auth planes stay independent.
/// </summary>
public interface IManagerSessionRefresher
{
    /// <summary>Returns a new access token, or null when there is no stored refresh token or it is rejected (re-login needed).</summary>
    Task<string?> RefreshAccessTokenAsync(CancellationToken cancellationToken = default);
}
