using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Services;

/// <summary><see cref="IConnectivityService"/> backed by MAUI <see cref="Connectivity"/>.</summary>
public sealed class ConnectivityService : IConnectivityService
{
    /// <summary>Subscribes to platform connectivity changes and re-raises them as a simple connected flag.</summary>
    public ConnectivityService()
    {
        Connectivity.Current.ConnectivityChanged += (_, e) =>
            ConnectivityChanged?.Invoke(this, e.NetworkAccess == NetworkAccess.Internet);
    }

    /// <summary>True when the device currently has internet access.</summary>
    public bool IsConnected => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    /// <summary>Raised when connectivity changes; the argument is the new connected state.</summary>
    public event EventHandler<bool>? ConnectivityChanged;
}
