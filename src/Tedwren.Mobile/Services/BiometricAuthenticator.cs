using Tedwren.Mobile.Core.Platform;
#if ANDROID
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
#elif IOS
using LocalAuthentication;
#endif

namespace Tedwren.Mobile.Services;

/// <summary>
/// The real on-device biometric local unlock (M8): Android <see cref="BiometricPrompt"/> and iOS
/// <c>LAContext</c>/<c>LocalAuthentication</c>. Local unlock only — it proves the device holder can open the app,
/// never that the person is the operative (R17); no biometric data leaves the device and none is sent to a server.
/// Both device credential (PIN/pattern) and biometrics are accepted as the unlock factor. When no unlock factor is
/// enrolled it reports <see cref="BiometricResult.Unavailable"/> so callers fall back to the non-biometric path.
/// </summary>
public sealed class BiometricAuthenticator : IBiometricAuthenticator
{
#if ANDROID
    /// <summary>Whether a biometric or device-credential unlock is enrolled and usable.</summary>
    public Task<bool> IsAvailableAsync()
    {
        var manager = BiometricManager.From(Android.App.Application.Context);
        var authenticators = BiometricManager.Authenticators.BiometricWeak | BiometricManager.Authenticators.DeviceCredential;
        return Task.FromResult(manager.CanAuthenticate(authenticators) == BiometricManager.BiometricSuccess);
    }

    /// <summary>Prompts for a biometric/device-credential unlock, resolving to the outcome.</summary>
    public Task<BiometricResult> AuthenticateAsync(string reason)
    {
        if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not FragmentActivity activity)
        {
            return Task.FromResult(BiometricResult.Unavailable);
        }

        var completion = new TaskCompletionSource<BiometricResult>();
        var executor = ContextCompat.GetMainExecutor(activity);
        var prompt = new BiometricPrompt(activity, executor!, new AuthCallback(completion));
        var info = new BiometricPrompt.PromptInfo.Builder()
            .SetTitle("Tedwren")
            .SetSubtitle(reason)
            .SetAllowedAuthenticators(BiometricManager.Authenticators.BiometricWeak | BiometricManager.Authenticators.DeviceCredential)
            .Build();

        prompt.Authenticate(info);
        return completion.Task;
    }

    /// <summary>Marshals the <see cref="BiometricPrompt"/> callback into the awaited result.</summary>
    private sealed class AuthCallback : BiometricPrompt.AuthenticationCallback
    {
        private readonly TaskCompletionSource<BiometricResult> _completion;

        public AuthCallback(TaskCompletionSource<BiometricResult> completion) => _completion = completion;

        public override void OnAuthenticationSucceeded(BiometricPrompt.AuthenticationResult result) =>
            _completion.TrySetResult(BiometricResult.Success);

        public override void OnAuthenticationFailed() { } // a single bad read — the prompt stays open, so don't complete

        public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence errString) =>
            _completion.TrySetResult(BiometricResult.Failed); // cancelled / lockout / no hardware
    }
#elif IOS
    /// <summary>Whether Face/Touch ID or a device passcode is available.</summary>
    public Task<bool> IsAvailableAsync()
    {
        using var context = new LAContext();
        return Task.FromResult(context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthentication, out _));
    }

    /// <summary>Prompts for Face/Touch ID (falling back to the device passcode), resolving to the outcome.</summary>
    public async Task<BiometricResult> AuthenticateAsync(string reason)
    {
        using var context = new LAContext();
        if (!context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthentication, out _))
        {
            return BiometricResult.Unavailable;
        }

        var (ok, _) = await context.EvaluatePolicyAsync(LAPolicy.DeviceOwnerAuthentication, reason);
        return ok ? BiometricResult.Success : BiometricResult.Failed;
    }
#else
    /// <summary>No biometric platform on this target framework.</summary>
    public Task<bool> IsAvailableAsync() => Task.FromResult(false);

    /// <summary>No biometric platform on this target framework.</summary>
    public Task<BiometricResult> AuthenticateAsync(string reason) => Task.FromResult(BiometricResult.Unavailable);
#endif
}
