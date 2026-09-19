using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Services;

/// <summary><see cref="ISecureStore"/> backed by MAUI <see cref="SecureStorage"/> (iOS Keychain / Android Keystore).</summary>
public sealed class SecureStore : ISecureStore
{
    /// <summary>Returns a stored secret by key, or null when absent.</summary>
    public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);

    /// <summary>Stores (or replaces) a secret.</summary>
    public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);

    /// <summary>Removes a secret.</summary>
    public void Remove(string key) => SecureStorage.Default.Remove(key);
}
