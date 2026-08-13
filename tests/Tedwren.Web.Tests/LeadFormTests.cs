using System.Net;
using Tedwren.Web.Leads;
using Tedwren.Web.Tests.Support;

namespace Tedwren.Web.Tests;

/// <summary>
/// W6 integration tests for the lead forms (Web Plan §7): valid submissions route (and demo leads carry
/// UTM attribution), invalid submissions redisplay without routing, the honeypot and too-fast timing
/// drop bots, contact messages route by reason, and antiforgery is enforced.
/// </summary>
public sealed class LeadFormTests : IClassFixture<LeadTestFactory>
{
    private readonly LeadTestFactory _factory;

    /// <summary>Injects the lead test host and resets captured leads per test.</summary>
    /// <param name="factory">Lead test host.</param>
    public LeadFormTests(LeadTestFactory factory)
    {
        _factory = factory;
        _factory.Router.Reset();
    }

    /// <summary>A valid demo submission routes a lead carrying the captured UTM source.</summary>
    [Fact]
    public async Task Demo_ValidSubmission_RoutesLeadWithUtm()
    {
        var client = _factory.CreateNoRedirectClient();
        var token = LeadTestFactory.ExtractAntiforgeryToken(await client.GetStringAsync("/demo?utm_source=pack"));

        var response = await client.PostAsync("/demo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Sam Trades",
            ["WorkEmail"] = "sam@example.com",
            ["Company"] = "Trades Ltd",
            ["Role"] = "Director",
            ["CompanyType"] = "Subcontractor",
            ["HeadcountOrSites"] = "50 operatives",
            ["UtmSource"] = "pack",
            ["RenderedAtUnixMs"] = LeadTestFactory.HumanRenderedAt().ToString(),
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/demo/thanks", response.Headers.Location!.ToString());
        var lead = Assert.Single(_factory.Router.Demos);
        Assert.Equal("Trades Ltd", lead.Company);
        Assert.Equal("pack", lead.UtmSource);
    }

    /// <summary>A submission missing required fields redisplays the form and routes nothing.</summary>
    [Fact]
    public async Task Demo_MissingFields_RedisplaysWithoutRouting()
    {
        var client = _factory.CreateNoRedirectClient();
        var token = LeadTestFactory.ExtractAntiforgeryToken(await client.GetStringAsync("/demo"));

        var response = await client.PostAsync("/demo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "", // required
            ["RenderedAtUnixMs"] = LeadTestFactory.HumanRenderedAt().ToString(),
        }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // re-rendered form, not a redirect
        Assert.Empty(_factory.Router.Demos);
    }

    /// <summary>A filled honeypot is treated as a bot: pretend success, route nothing.</summary>
    [Fact]
    public async Task Demo_HoneypotFilled_DropsSilently()
    {
        var client = _factory.CreateNoRedirectClient();
        var token = LeadTestFactory.ExtractAntiforgeryToken(await client.GetStringAsync("/demo"));

        var response = await client.PostAsync("/demo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Bot", ["WorkEmail"] = "bot@example.com", ["Company"] = "Bot Co",
            ["Role"] = "Bot", ["CompanyType"] = "Other", ["HeadcountOrSites"] = "1",
            ["Website"] = "http://spam.example", // honeypot filled
            ["RenderedAtUnixMs"] = LeadTestFactory.HumanRenderedAt().ToString(),
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); // reveals nothing to the bot
        Assert.Empty(_factory.Router.Demos);
    }

    /// <summary>A submission that arrives too fast to be human is dropped.</summary>
    [Fact]
    public async Task Demo_TooFast_DropsSilently()
    {
        var client = _factory.CreateNoRedirectClient();
        var token = LeadTestFactory.ExtractAntiforgeryToken(await client.GetStringAsync("/demo"));

        var response = await client.PostAsync("/demo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Fast", ["WorkEmail"] = "fast@example.com", ["Company"] = "Fast Co",
            ["Role"] = "Dir", ["CompanyType"] = "Other", ["HeadcountOrSites"] = "1",
            // rendered "now" → zero elapsed → below the minimum fill time
            ["RenderedAtUnixMs"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Empty(_factory.Router.Demos);
    }

    /// <summary>Contact messages route to the inbox their reason maps to.</summary>
    [Fact]
    public async Task Contact_RoutesByReason()
    {
        var client = _factory.CreateNoRedirectClient();
        var token = LeadTestFactory.ExtractAntiforgeryToken(await client.GetStringAsync("/contact"));

        var response = await client.PostAsync("/contact", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Pat Press",
            ["Email"] = "pat@news.example",
            ["Reason"] = nameof(ContactReason.Press),
            ["Message"] = "Requesting a press pack for a feature.",
            ["RenderedAtUnixMs"] = LeadTestFactory.HumanRenderedAt().ToString(),
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var lead = Assert.Single(_factory.Router.Contacts);
        Assert.Equal(ContactReason.Press, lead.Reason);
        Assert.Equal("press@tedwren.example", lead.Destination);
    }

    /// <summary>A POST without an antiforgery token is rejected and routes nothing.</summary>
    [Fact]
    public async Task Demo_WithoutAntiforgeryToken_IsRejected()
    {
        var client = _factory.CreateNoRedirectClient();

        var response = await client.PostAsync("/demo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = "No Token", ["WorkEmail"] = "n@example.com", ["Company"] = "X",
            ["Role"] = "Y", ["CompanyType"] = "Other", ["HeadcountOrSites"] = "1",
            ["RenderedAtUnixMs"] = LeadTestFactory.HumanRenderedAt().ToString(),
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(_factory.Router.Demos);
    }
}
