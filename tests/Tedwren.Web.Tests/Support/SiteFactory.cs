using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tedwren.Web.Tests.Support;

/// <summary>
/// Test host for the launched (non-landing) marketing site: forces <c>Site:IsLanding=false</c> so the real
/// pages, routes and SEO documents are served rather than the pre-launch landing gate. The committed
/// <c>appsettings.json</c> ships with the gate on for the pre-launch deployment, so content/SEO/routing
/// tests must opt the gate off explicitly to exercise the actual site (the gate-on behaviour is covered
/// separately by <c>LandingGateTests</c>).
/// </summary>
public sealed class SiteFactory : WebApplicationFactory<Program>
{
    /// <summary>Turns the pre-launch landing gate off for the host under test.</summary>
    /// <param name="builder">The web host builder.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("Site:IsLanding", "false");
}
