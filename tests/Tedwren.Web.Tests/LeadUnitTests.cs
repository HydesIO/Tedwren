using Microsoft.AspNetCore.Http;
using Tedwren.Web.Attribution;
using Tedwren.Web.Consent;
using Tedwren.Web.Leads;
using Tedwren.Web.Models;

namespace Tedwren.Web.Tests;

/// <summary>W6 pure-unit tests for anti-bot, contact routing, consent parsing and UTM building.</summary>
public sealed class LeadUnitTests
{
    /// <summary>The honeypot and timing rules classify submissions correctly.</summary>
    [Fact]
    public void AntiBot_ClassifiesHumanAndBot()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var human = new ContactRequest { RenderedAtUnixMs = now - 5000 };
        var tooFast = new ContactRequest { RenderedAtUnixMs = now - 100 };
        var honeypot = new ContactRequest { RenderedAtUnixMs = now - 5000, Website = "x" };
        var stale = new ContactRequest { RenderedAtUnixMs = now - (2L * 24 * 60 * 60 * 1000) };

        Assert.True(AntiBot.IsHuman(human, now, 2));
        Assert.False(AntiBot.IsHuman(tooFast, now, 2));
        Assert.False(AntiBot.IsHuman(honeypot, now, 2));
        Assert.False(AntiBot.IsHuman(stale, now, 2));
    }

    /// <summary>Contact routing prefers the reason's inbox, then the sales inbox, then a visible fallback.</summary>
    [Fact]
    public void ContactRouting_ResolvesInbox()
    {
        var options = new LeadOptions
        {
            SalesInbox = "sales@x",
            ContactInboxes = { [ContactReason.Support] = "support@x" },
        };

        Assert.Equal("support@x", ContactRouting.ResolveInbox(ContactReason.Support, options));
        Assert.Equal("sales@x", ContactRouting.ResolveInbox(ContactReason.Press, options)); // falls back
        Assert.Equal("unrouted", ContactRouting.ResolveInbox(ContactReason.Press, new LeadOptions()));
    }

    /// <summary>Consent parses to and from its cookie value.</summary>
    [Theory]
    [InlineData("all", true, true)]
    [InlineData("analytics", true, false)]
    [InlineData("essential", false, false)]
    public void Consent_RoundTrips(string cookie, bool analytics, bool ads)
    {
        var state = ConsentState.Parse(cookie);

        Assert.True(state.HasChoice);
        Assert.Equal(analytics, state.Analytics);
        Assert.Equal(ads, state.Advertising);
        Assert.Equal(cookie, state.ToCookieValue());
    }

    /// <summary>An absent cookie means no choice has been made yet.</summary>
    [Fact]
    public void Consent_Absent_IsUnset()
    {
        var state = ConsentState.Parse(null);

        Assert.False(state.HasChoice);
        Assert.False(state.Analytics);
    }

    /// <summary>UTM parameters build a demo URL and read back from a query string.</summary>
    [Fact]
    public void Utm_BuildsDemoUrl_AndReadsQuery()
    {
        Assert.Equal("/demo?utm_source=pack&utm_medium=compliance_pack&utm_campaign=gtm_stage2", Utm.Pack.ToDemoUrl());
        Assert.Equal("/demo", new Utm(null, null, null).ToDemoUrl());

        var query = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            ["utm_source"] = "pack",
        });
        Assert.Equal("pack", Utm.FromQuery(query).Source);
    }
}
