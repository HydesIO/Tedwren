using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tedwren.Web.Partners;
using Tedwren.Web.Tests.Support;

namespace Tedwren.Web.Tests;

/// <summary>
/// W7 integration tests for the partner programme (Web Plan §7): the page states the §7.3 exclusion and
/// carries the application form, submitting creates only a pending record (no self-serve activation),
/// the dashboard is private (unknown code 404s), and a referral link attributes a later demo conversion
/// across the request boundary.
/// </summary>
public sealed class PartnerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Injects the in-process site host.</summary>
    /// <param name="factory">Test host.</param>
    public PartnerIntegrationTests(WebApplicationFactory<Program> factory) => _factory = factory;

    private HttpClient NoRedirect() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    /// <summary>The partners page states the §7.3 exclusion plainly and shows the application form.</summary>
    [Fact]
    public async Task Page_StatesExclusion_AndShowsForm()
    {
        var body = await _factory.CreateClient().GetStringAsync("/partners");

        Assert.Contains("never a route to site access", body);       // §7.3 statement from content
        Assert.Contains("__RequestVerificationToken", body);          // application form present
        Assert.Contains("ControlsSiteAccess", body);                  // the §7.3 question
    }

    /// <summary>Submitting an application creates a pending record and activates nothing.</summary>
    [Fact]
    public async Task Application_CreatesPendingRecord_NoActivation()
    {
        var client = NoRedirect();
        var company = "Advisers " + Guid.NewGuid().ToString("N")[..8];
        var token = LeadTestFactory.ExtractAntiforgeryToken(await client.GetStringAsync("/partners"));

        var response = await client.PostAsync("/partners", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Ada", ["Company"] = company, ["Email"] = "ada@example.com",
            ["HowTheyWork"] = "We advise subcontractors on compliance.",
            ["RelationshipToSites"] = "None — we don't touch site access.",
            ["RenderedAtUnixMs"] = LeadTestFactory.HumanRenderedAt().ToString(),
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/partners/thanks", response.Headers.Location!.ToString());

        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IPartnerStore>();
        var application = Assert.Single(store.Applications(), a => a.Company == company);
        Assert.Equal(PartnerApplicationStatus.Pending, application.Status); // pending, not approved
        Assert.Null(store.GetPartnerByCode(company));                        // no partner minted
    }

    /// <summary>The dashboard is private: an unknown referral code returns 404.</summary>
    [Fact]
    public async Task Dashboard_UnknownCode_ReturnsNotFound()
    {
        var response = await NoRedirect().GetAsync("/partners/dashboard/TW-NOPE99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>A referral link attributes a later demo conversion, even across separate requests.</summary>
    [Fact]
    public async Task ReferralLink_AttributesLaterDemoConversion()
    {
        // Seed an approved partner directly through the services.
        Partner partner;
        using (var scope = _factory.Services.CreateScope())
        {
            var partners = scope.ServiceProvider.GetRequiredService<PartnerService>();
            var application = partners.Submit("Ada", "Advisers", "ada@example.com", "advise", "none", false);
            partner = partners.Approve(application.Id);
        }

        var client = NoRedirect();

        // Visit the referral link → the attribution cookie is set.
        var capture = await client.GetAsync($"/r/{partner.ReferralCode}");
        Assert.Equal(HttpStatusCode.Redirect, capture.StatusCode);

        // Later, submit a demo with the same client (carrying the cookie).
        var token = LeadTestFactory.ExtractAntiforgeryToken(await client.GetStringAsync("/demo"));
        var demo = await client.PostAsync("/demo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = "Sam", ["WorkEmail"] = "sam@example.com", ["Company"] = "Sub Ltd",
            ["Role"] = "Director", ["CompanyType"] = "Subcontractor", ["HeadcountOrSites"] = "50",
            ["RenderedAtUnixMs"] = LeadTestFactory.HumanRenderedAt().ToString(),
        }));
        Assert.Equal(HttpStatusCode.Redirect, demo.StatusCode);

        using var check = _factory.Services.CreateScope();
        var store = check.ServiceProvider.GetRequiredService<IPartnerStore>();
        var referral = Assert.Single(store.ReferralsForPartner(partner.Id));
        Assert.Equal(ReferralSource.Demo, referral.Source);
        Assert.Equal("Sub Ltd", referral.SubjectCompany);
    }
}
