namespace Tedwren.Web.App.Shell;

/// <summary>The device form factor the emulator is currently rendering — phone or tablet only (no desktop).</summary>
public enum DevicePreset
{
    /// <summary>A phone-sized frame (~390px wide).</summary>
    Phone,

    /// <summary>A tablet-sized frame (~834px wide).</summary>
    Tablet,
}

/// <summary>
/// Holds the emulator "chrome" state that sits around the rendered app: the selected device size, the light/dark
/// theme applied inside the frame, and (from WA6) the manual offline toggle. It is a tiny observable so the shell
/// and the device frame re-render together when the tester changes a control. It is emulator-only state — nothing
/// here reaches the app logic or the API.
/// </summary>
public sealed class EmulatorState
{
    /// <summary>The current device form factor (defaults to phone).</summary>
    public DevicePreset Device { get; private set; } = DevicePreset.Phone;

    /// <summary>Whether the app inside the frame renders in dark theme.</summary>
    public bool IsDarkMode { get; private set; }

    /// <summary>Raised whenever any emulator-chrome value changes, so subscribers re-render.</summary>
    public event Action? Changed;

    /// <summary>Switches the device form factor and notifies subscribers.</summary>
    public void SetDevice(DevicePreset device)
    {
        if (Device == device)
        {
            return;
        }

        Device = device;
        Changed?.Invoke();
    }

    /// <summary>Toggles the in-frame light/dark theme and notifies subscribers.</summary>
    public void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        Changed?.Invoke();
    }
}
