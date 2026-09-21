using Microsoft.JSInterop;

namespace Tedwren.Web.App.Platform;

/// <summary>A captured geolocation fix (WGS84), or null when unavailable/denied.</summary>
public sealed record GeoFix(double Latitude, double Longitude, double? AccuracyMetres);

/// <summary>
/// Browser geolocation helper for the emulator, mirroring the device's location capture (SF-14/SF-15). It uses
/// the <c>navigator.geolocation</c> API via JS interop; on denial or absence it returns null (the
/// location-optional path), letting the calling screen fall back to a demo site-centre. Consumed by the WA3
/// attendance/hazard screens.
/// </summary>
public sealed class WebGeolocation
{
    private readonly IJSRuntime _js;

    /// <summary>Creates the helper over the JS runtime used to reach <c>navigator.geolocation</c>.</summary>
    public WebGeolocation(IJSRuntime js) => _js = js;

    /// <summary>Requests the current position; returns null when the user denies it or the browser can't provide one.</summary>
    public async Task<GeoFix?> TryGetAsync()
    {
        try
        {
            return await _js.InvokeAsync<GeoFix?>("webapp.getPosition");
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            return null;
        }
    }
}
