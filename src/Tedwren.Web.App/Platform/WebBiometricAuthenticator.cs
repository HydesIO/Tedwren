using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Web.App.Platform;

/// <summary>
/// Browser <see cref="IBiometricAuthenticator"/> for the emulator. Biometrics don't exist in a browser, so it
/// reports "unavailable" — the operative/manager session managers then resume without a prompt, exactly as the
/// MAUI head does on a device with no enrolled biometrics (R17 local-unlock, not identity). This keeps the
/// like-for-like resume path intact.
/// </summary>
public sealed class WebBiometricAuthenticator : IBiometricAuthenticator
{
    /// <summary>Always false in a browser — no biometric hardware to prompt.</summary>
    public Task<bool> IsAvailableAsync() => Task.FromResult(false);

    /// <summary>Never invoked (availability is false); returns <see cref="BiometricResult.Unavailable"/> for safety.</summary>
    public Task<BiometricResult> AuthenticateAsync(string reason) => Task.FromResult(BiometricResult.Unavailable);
}
