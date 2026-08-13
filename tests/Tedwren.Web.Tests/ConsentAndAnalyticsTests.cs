using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Tedwren.Web.Tests.Support;

namespace Tedwren.Web.Tests;

/// <summary>
/// W6 integration tests for consent and analytics gating (Web Plan §8): the banner shows until a choice
/// is made and offers a one-click reject, no analytics script fires before consent, and — even after
/// consent — nothing loads unless a measurement id is configured (none is, by default).
/// </summary>
public sealed class ConsentAndAnalyticsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <summary>Injects the in-process site host.</summary>
    /// <param name="factory">Test host.</param>
    public ConsentAndAnalyticsTests(WebApplicationFactory<Program> factory) => _factory = factory;

    /// <summary>Before any choice, the banner shows with a one-click reject and no analytics script fires.</summary>
    [Fact]
    public async Task BeforeConsent_BannerShows_NoAnalyticsScript()
    {
        var body = await _factory.CreateClient().GetStringAsync("/");

        Assert.Contains("class=\"consent\"", body);
        Assert.Contains("value=\"essential\"", body);            // one-click reject-non-essential
        Assert.DoesNotContain("googletagmanager", body);         // no analytics tag pre-consent
        Assert.DoesNotContain("gtag(", body);
    }

    /// <summary>Rejecting hides the banner on subsequent pages and still fires no analytics.</summary>
    [Fact]
    public async Task AfterReject_BannerGone_StillNoAnalytics()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var token = LeadTestFactory.ExtractAntiforgeryToken(await client.GetStringAsync("/"));

        var post = await client.PostAsync("/consent", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["choice"] = "essential",
            ["returnUrl"] = "/",
        }));
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);

        var body = await client.GetStringAsync("/");
        Assert.DoesNotContain("class=\"consent\"", body); // banner gone
        Assert.DoesNotContain("googletagmanager", body);  // still nothing fires
    }

    /// <summary>Accepting all still loads no analytics when no measurement id is configured (the default).</summary>
    [Fact]
    public async Task AfterAcceptAll_NoIdConfigured_StillNoAnalytics()
    {
        var client = _factory.CreateClient();
        var token = LeadTestFactory.ExtractAntiforgeryToken(await client.GetStringAsync("/"));

        await client.PostAsync("/consent", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["choice"] = "all",
            ["returnUrl"] = "/",
        }));

        var body = await client.GetStringAsync("/");
        Assert.DoesNotContain("class=\"consent\"", body);  // choice made, banner gone
        Assert.DoesNotContain("googletagmanager", body);   // no id → nothing loads even with consent
    }
}
