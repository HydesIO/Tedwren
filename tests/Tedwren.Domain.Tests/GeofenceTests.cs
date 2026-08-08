using Tedwren.Domain.ValueObjects;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Verifies the geofence boundary test (SF-14): a point is inside when within the radius, outside beyond it;
/// and that invalid coordinates or radius are rejected.
/// </summary>
public sealed class GeofenceTests
{
    private static readonly Geofence Fence = new(51.5074, -0.1278, 150);

    [Fact]
    public void CentrePoint_IsInside() => Assert.True(Fence.Contains(51.5074, -0.1278));

    [Fact]
    public void PointJustInsideRadius_IsInside() =>
        Assert.True(Fence.Contains(51.5075, -0.1278));   // ~11 m north of centre

    [Fact]
    public void PointBeyondRadius_IsOutside() =>
        Assert.False(Fence.Contains(51.5100, -0.1278));  // ~289 m north of centre

    [Theory]
    [InlineData(91, 0, 100)]
    [InlineData(-91, 0, 100)]
    [InlineData(0, 181, 100)]
    [InlineData(0, 0, 0)]
    [InlineData(0, 0, -5)]
    public void InvalidValues_AreRejected(double lat, double lng, double radius) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Geofence(lat, lng, radius));
}
