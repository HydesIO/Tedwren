using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Domain.ValueObjects;

namespace Tedwren.Mobile.Core.Geofencing;

/// <summary>
/// A pre-submit, on-device hint about whether the operative's captured location is inside a site's boundary
/// (SF-14), and how far off it is. Purely advisory — it lets the app warn "you look ~40 m outside the boundary"
/// before an online sign-in; the <b>server stays authoritative</b> for the actual decision (R2/R3). Reuses the
/// domain <see cref="Geofence"/> so the containment/distance maths lives in exactly one place.
/// </summary>
public readonly record struct GeofenceHint(bool HasBoundary, bool Inside, double? DistanceMetres, double? RadiusMetres)
{
    /// <summary>
    /// Evaluates a captured location against a site boundary. When the site has no site-level boundary (a
    /// dispersed scheme whose boundaries live on its properties, SF-26, or an unbounded site), returns a
    /// no-boundary hint so the UI simply lets the server decide rather than blocking the operative.
    /// </summary>
    public static GeofenceHint Evaluate(GeofenceDto? boundary, double latitude, double longitude)
    {
        if (boundary is null)
        {
            return new GeofenceHint(false, false, null, null);
        }

        var fence = new Geofence(boundary.CentreLatitude, boundary.CentreLongitude, boundary.RadiusMetres);
        var distance = fence.DistanceMetresTo(latitude, longitude);
        return new GeofenceHint(true, distance <= boundary.RadiusMetres, distance, boundary.RadiusMetres);
    }
}
