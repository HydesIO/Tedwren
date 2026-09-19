using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Services;

/// <summary>
/// Placeholder biometric authenticator. The real Face/Touch ID local unlock (via a biometric plugin or platform
/// APIs) is wired in M2; until then it reports unavailable so callers fall back to the non-biometric entry path.
/// Local unlock only — never identity verification (R17).
/// </summary>
public sealed class BiometricAuthenticator : IBiometricAuthenticator
{
    /// <summary>Reports no biometrics until the platform integration lands in M2.</summary>
    public Task<bool> IsAvailableAsync() => Task.FromResult(false);

    /// <summary>Returns <see cref="BiometricResult.Unavailable"/> until the platform integration lands in M2.</summary>
    public Task<BiometricResult> AuthenticateAsync(string reason) => Task.FromResult(BiometricResult.Unavailable);
}
