using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Web.App.Platform;

/// <summary>
/// Browser <see cref="IConnectivityService"/> for the emulator. It reflects a cached connected flag so the sync
/// engine's synchronous checks stay cheap; the flag is driven by <c>navigator.onLine</c> and the window
/// online/offline events (wired via JS in WA6) and by the emulator's manual offline toggle. It defaults to
/// connected so the online-first surface works from first paint.
/// </summary>
public sealed class WebConnectivityService : IConnectivityService
{
    private bool _isConnected = true;

    /// <summary>True when the emulator currently considers itself online.</summary>
    public bool IsConnected => _isConnected;

    /// <summary>Raised when connectivity changes; the argument is the new connected state.</summary>
    public event EventHandler<bool>? ConnectivityChanged;

    /// <summary>
    /// Sets the connected state (from <c>navigator.onLine</c>, the window events, or the emulator's offline
    /// toggle) and raises <see cref="ConnectivityChanged"/> when it actually changes — which is what triggers the
    /// sync engine to flush on reconnect.
    /// </summary>
    public void SetConnected(bool connected)
    {
        if (_isConnected == connected)
        {
            return;
        }

        _isConnected = connected;
        ConnectivityChanged?.Invoke(this, connected);
    }
}
