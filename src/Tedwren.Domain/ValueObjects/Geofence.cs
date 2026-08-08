namespace Tedwren.Domain.ValueObjects;

/// <summary>
/// A circular site (or property) boundary: a centre point and a radius in metres (SF-14, SF-26). A sign-in
/// is only accepted as verified when it happens inside the boundary — this value object owns the
/// containment test so that rule lives in one place and is unit-tested. Equality is by value.
/// </summary>
public sealed record Geofence
{
    private const double EarthRadiusMetres = 6_371_000;

    /// <summary>Centre latitude in decimal degrees (−90..90).</summary>
    public double CentreLatitude { get; }

    /// <summary>Centre longitude in decimal degrees (−180..180).</summary>
    public double CentreLongitude { get; }

    /// <summary>Boundary radius in metres (positive).</summary>
    public double RadiusMetres { get; }

    /// <summary>Creates and validates a geofence, throwing when the coordinates or radius are out of range.</summary>
    public Geofence(double centreLatitude, double centreLongitude, double radiusMetres)
    {
        if (centreLatitude is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(centreLatitude), centreLatitude, "Latitude must be between −90 and 90.");
        }

        if (centreLongitude is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(centreLongitude), centreLongitude, "Longitude must be between −180 and 180.");
        }

        if (radiusMetres <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusMetres), radiusMetres, "Radius must be positive.");
        }

        CentreLatitude = centreLatitude;
        CentreLongitude = centreLongitude;
        RadiusMetres = radiusMetres;
    }

    /// <summary>Whether the given point lies within the boundary, using great-circle (haversine) distance.</summary>
    public bool Contains(double latitude, double longitude) =>
        DistanceMetresTo(latitude, longitude) <= RadiusMetres;

    /// <summary>Great-circle distance in metres from the centre to the given point.</summary>
    public double DistanceMetresTo(double latitude, double longitude)
    {
        var dLat = ToRadians(latitude - CentreLatitude);
        var dLng = ToRadians(longitude - CentreLongitude);
        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2))
                + (Math.Cos(ToRadians(CentreLatitude)) * Math.Cos(ToRadians(latitude))
                   * Math.Sin(dLng / 2) * Math.Sin(dLng / 2));
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMetres * c;
    }

    /// <summary>Converts decimal degrees to radians.</summary>
    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
