using Tedwren.Mobile.Core.Session;

namespace Tedwren.Mobile.Core.Tests.Session;

/// <summary>Verifies the role → home mapping that drives the operative vs manager experience.</summary>
public class RoleHomeResolverTests
{
    [Theory]
    [InlineData("Operative", RoleHome.Operative)]
    [InlineData("operative", RoleHome.Operative)]
    [InlineData("Administrator", RoleHome.Manager)]
    [InlineData("ComplianceManager", RoleHome.Manager)]
    [InlineData("SiteManager", RoleHome.Manager)]
    [InlineData("Auditor", RoleHome.Manager)]
    [InlineData("", RoleHome.Operative)]
    [InlineData(null, RoleHome.Operative)]
    [InlineData("nonsense", RoleHome.Operative)]
    public void Resolve_maps_role_to_home(string? role, RoleHome expected)
        => Assert.Equal(expected, RoleHomeResolver.Resolve(role));

    [Fact]
    public void Session_home_and_expiry_derive_from_fields()
    {
        var expires = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var session = new MobileSession("t", expires, "Sam", "SiteManager", Guid.NewGuid());

        Assert.Equal(RoleHome.Manager, session.Home);
        Assert.False(session.IsExpired(expires.AddMinutes(-1)));
        Assert.True(session.IsExpired(expires));
    }
}
