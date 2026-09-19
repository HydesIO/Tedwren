using Foundation;

namespace Tedwren.Mobile;

/// <summary>The iOS application delegate that boots the shared MAUI app.</summary>
[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    /// <summary>Builds the shared MAUI app.</summary>
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
