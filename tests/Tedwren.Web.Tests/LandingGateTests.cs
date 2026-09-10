using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tedwren.Web.Leads;
using Tedwren.Web.Tests.Support;

namespace Tedwren.Web.Tests;

/// <summary>
/// Tests the pre-launch landing gate (Site:IsLanding). With the flag on, the whole site funnels to the
/// landing page, static assets still load, and the email-capture form routes a sign-up. With the flag
/// off the normal site is served (covered by <see cref="RoutingTests"/>).
/// </summary>
public sealed class LandingGateTests : IClassFixture<LandingGateTests.LandingFactory>
{
    private readonly LandingFactory _factory;

    /// <summary>Injects the landing-mode host and resets captured leads per test.</summary>
    /// <param name="factory">Landing-mode test host.</param>
    public LandingGateTests(LandingFactory factory)
    {
        _factory = factory;
        _factory.Router.Reset();
    }

    /// <summary>With the gate on, the home route renders the landing page, not the full site.</summary>
    [Fact]
    public async Task Gate_On_HomeRendersLanding()
    {
        var body = await _factory.CreateClient().GetStringAsync("/");

        Assert.Contains("Launching soon", body);
        Assert.Contains("landing__form", body);
        Assert.DoesNotContain("data-audience-panel", body); // the full home's tabbed products are hidden
    }

    /// <summary>With the gate on, a deep marketing route also funnels to the landing page (whole-site gate).</summary>
    [Fact]
    public async Task Gate_On_DeepRouteFunnelsToLanding()
    {
        var response = await _factory.CreateClient().GetAsync("/pricing");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Launching soon", body);
    }

    /// <summary>Static assets are served ahead of the gate, so the page's styles still load.</summary>
    [Fact]
    public async Task Gate_On_StaticAssetsStillServed()
    {
        var response = await _factory.CreateClient().GetAsync("/css/site.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>The landing email-capture form routes a launch-notify sign-up through the lead pipeline.</summary>
    [Fact]
    public async Task Gate_On_NotifyForm_RoutesSignup()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = LeadTestFactory.ExtractAntiforgeryToken(await client.GetStringAsync("/landing"));

        var response = await client.PostAsync("/landing/notify", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Email"] = "early@builder.example",
            ["RenderedAtUnixMs"] = LeadTestFactory.HumanRenderedAt().ToString(),
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("notified=", response.Headers.Location!.ToString());
        var lead = Assert.Single(_factory.Router.Contacts);
        Assert.Equal("early@builder.example", lead.Email);
    }

    /// <summary>Landing-mode host: sets Site:IsLanding=true and captures routed leads in memory.</summary>
    public sealed class LandingFactory : WebApplicationFactory<Program>
    {
        /// <summary>The in-memory router the app uses under test.</summary>
        public FakeLeadRouter Router { get; } = new();

        /// <inheritdoc />
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Site:IsLanding", "true");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILeadRouter>();
                services.AddSingleton<ILeadRouter>(Router);
            });
        }
    }
}
