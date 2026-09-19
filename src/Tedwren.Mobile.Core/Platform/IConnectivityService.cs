namespace Tedwren.Mobile.Core.Platform;

/// <summary>
/// Reports device network connectivity so the sync engine knows when it may reach the API. The site-entry
/// decision and attendance sign-in are online-only and fail closed (R2/R3); only evidence/forms capture and
/// read caches work offline. Implemented by the MAUI head over <c>Connectivity.Current</c>.
/// </summary>
public interface IConnectivityService
{
    /// <summary>True when the device currently has network access.</summary>
    bool IsConnected { get; }

    /// <summary>Raised when connectivity changes; the argument is the new connected state.</summary>
    event EventHandler<bool>? ConnectivityChanged;
}
