namespace Tedwren.Mobile.Core.Platform;

/// <summary>The outcome of a biometric unlock attempt.</summary>
public enum BiometricResult
{
    /// <summary>The user authenticated successfully.</summary>
    Success,

    /// <summary>The user failed or cancelled the biometric prompt.</summary>
    Failed,

    /// <summary>No biometric hardware is enrolled/available on the device.</summary>
    Unavailable,
}

/// <summary>
/// Gates the local app session behind device biometrics (Face/Touch ID). This is <b>local unlock only</b> — it
/// proves the device holder can open the app, not that the person is the operative (R17). True identity
/// verification (face-match with liveness at sign-in) is deferred to PRD Phase 5 and needs a DPIA. Implemented
/// by the MAUI head.
/// </summary>
public interface IBiometricAuthenticator
{
    /// <summary>Whether the device has biometrics enrolled and available to prompt.</summary>
    Task<bool> IsAvailableAsync();

    /// <summary>Prompts for biometric unlock, showing <paramref name="reason"/> to the user.</summary>
    Task<BiometricResult> AuthenticateAsync(string reason);
}
