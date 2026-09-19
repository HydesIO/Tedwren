namespace Tedwren.Mobile.Pages;

/// <summary>
/// Shared device-location capture for the field pages (M4 attendance, M5 capture). Location is optional (SF-15):
/// when the sensor is unavailable or permission is denied it returns null, and the caller proceeds without it.
/// </summary>
internal static class DeviceLocation
{
    /// <summary>Captures the current location, or null when it is unavailable/denied.</summary>
    public static async Task<Location?> CaptureAsync()
    {
        try
        {
            return await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
        }
        catch (Exception ex) when (ex is FeatureNotSupportedException or FeatureNotEnabledException or PermissionException or OperationCanceledException)
        {
            return null;
        }
    }
}
