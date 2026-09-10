using Tedwren.Web.Attribution;
using Tedwren.Web.Tests.Support;

namespace Tedwren.Web.Tests;

/// <summary>
/// W8 launch guardrails (Web Plan §4.1, §14) for the compliance-pack viewer chrome — the distribution
/// mechanism that must reuse this site's brand while never gating the recipient. These enforce the two
/// hard rules: the pack demo link is UTM-tagged so pack-driven bookings are measurable, and the pack
/// chrome contains no sign-up wall of any kind.
/// </summary>
public sealed class LaunchGuardrailTests
{
    /// <summary>Markup that would constitute a sign-up/login wall — none may appear in the pack chrome.</summary>
    private static readonly string[] SignUpWallMarkers =
    {
        "<form", "type=\"password\"", "sign up", "signup", "sign in", "log in", "login", "register", "create account",
    };

    /// <summary>The pack chrome's "Book a demo" link carries utm_source=pack (Plan §4.1, GTM Stage 2).</summary>
    [Fact]
    public void PackDemoLink_IsUtmTagged()
    {
        var url = Utm.Pack.ToDemoUrl();

        Assert.StartsWith("/demo?", url);
        Assert.Contains("utm_source=pack", url);
    }

    /// <summary>The pack chrome view has a demo CTA and no sign-up wall of any kind (Plan §4.1, §14).</summary>
    [Fact]
    public void PackChrome_HasDemoCta_AndNoSignUpWall()
    {
        var view = Path.Combine(
            RepoPaths.WebProject, "Views", "Shared", "Components", "PackChrome", "Default.cshtml");
        var markup = File.ReadAllText(view);

        Assert.Contains("Book a demo", markup);
        foreach (var marker in SignUpWallMarkers)
        {
            Assert.DoesNotContain(marker, markup, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>The pack chrome reuses this site's shared brand mark (Plan §4.1).</summary>
    [Fact]
    public void PackChrome_ShowsSharedBrandLogo()
    {
        var view = Path.Combine(
            RepoPaths.WebProject, "Views", "Shared", "Components", "PackChrome", "Default.cshtml");
        var markup = File.ReadAllText(view);

        Assert.Contains("images/logo-icon.svg", markup);
    }
}
