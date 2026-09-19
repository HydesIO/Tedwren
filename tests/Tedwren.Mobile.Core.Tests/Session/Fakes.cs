using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Core.Tests.Session;

/// <summary>In-memory <see cref="ISecureStore"/> double for tests.</summary>
internal sealed class FakeSecureStore : ISecureStore
{
    private readonly Dictionary<string, string> _values = new();

    public Task<string?> GetAsync(string key) => Task.FromResult(_values.TryGetValue(key, out var v) ? v : null);

    public Task SetAsync(string key, string value)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }

    public void Remove(string key) => _values.Remove(key);

    /// <summary>Reads a stored value without the async ceremony, for assertions.</summary>
    public string? Peek(string key) => _values.TryGetValue(key, out var v) ? v : null;
}

/// <summary>Configurable <see cref="IBiometricAuthenticator"/> double for tests.</summary>
internal sealed class FakeBiometrics : IBiometricAuthenticator
{
    private readonly bool _available;
    private readonly BiometricResult _result;

    public FakeBiometrics(bool available, BiometricResult result)
    {
        _available = available;
        _result = result;
    }

    public Task<bool> IsAvailableAsync() => Task.FromResult(_available);

    public Task<BiometricResult> AuthenticateAsync(string reason) => Task.FromResult(_result);
}
