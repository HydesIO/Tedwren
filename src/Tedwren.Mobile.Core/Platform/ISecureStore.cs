namespace Tedwren.Mobile.Core.Platform;

/// <summary>
/// Encrypted, per-device key/value storage for secrets — the JWT refresh token and the SQLCipher database key.
/// Backed by the platform secure enclave (iOS Keychain / Android Keystore) and, in the app, gated behind
/// biometric unlock. Implemented by the MAUI head over <c>SecureStorage</c>.
/// </summary>
public interface ISecureStore
{
    /// <summary>Returns a stored secret by key, or null when absent.</summary>
    Task<string?> GetAsync(string key);

    /// <summary>Stores (or replaces) a secret.</summary>
    Task SetAsync(string key, string value);

    /// <summary>Removes a secret (used on sign-out / device unbind).</summary>
    void Remove(string key);
}
