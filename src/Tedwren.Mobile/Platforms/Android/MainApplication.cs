using Android.App;
using Android.Runtime;

namespace Tedwren.Mobile;

/// <summary>The Android application object that boots the shared MAUI app.</summary>
[Application]
public class MainApplication : MauiApplication
{
    /// <summary>Standard Android runtime constructor.</summary>
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    /// <summary>Builds the shared MAUI app.</summary>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
