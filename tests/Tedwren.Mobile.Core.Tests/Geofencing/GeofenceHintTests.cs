using Tedwren.Abstractions.Contracts.Sites;
using Tedwren.Mobile.Core.Geofencing;

namespace Tedwren.Mobile.Core.Tests.Geofencing;

/// <summary>Verifies the pre-submit geofence hint (M4): inside/outside detection and the no-boundary case (advisory only).</summary>
public class GeofenceHintTests
{
    private static readonly GeofenceDto London = new(51.5074, -0.1278, 150);

    [Fact]
    public void Inside_the_boundary_reports_inside_with_a_small_distance()
    {
        var hint = GeofenceHint.Evaluate(London, 51.5074, -0.1278);

        Assert.True(hint.HasBoundary);
        Assert.True(hint.Inside);
        Assert.True(hint.DistanceMetres < London.RadiusMetres);
    }

    [Fact]
    public void Outside_the_boundary_reports_outside_with_the_distance()
    {
        var hint = GeofenceHint.Evaluate(London, 51.6000, -0.2000);

        Assert.True(hint.HasBoundary);
        Assert.False(hint.Inside);
        Assert.True(hint.DistanceMetres > London.RadiusMetres);
    }

    [Fact]
    public void No_boundary_reports_no_boundary_so_the_server_decides()
    {
        var hint = GeofenceHint.Evaluate(null, 51.5074, -0.1278);

        Assert.False(hint.HasBoundary);
        Assert.False(hint.Inside);
        Assert.Null(hint.DistanceMetres);
    }
}
