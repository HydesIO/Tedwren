using Microsoft.Extensions.Options;
using Tedwren.Web.Partners;

namespace Tedwren.Web.Tests;

/// <summary>
/// W7 unit tests for the partner programme (Web Plan §7): submitting creates only a pending record
/// (no self-serve activation), approval refuses anyone who controls site access (§7.3), and a
/// commission is reversible against its specific referral within the clawback window.
/// </summary>
public sealed class PartnerProgrammeUnitTests
{
    private readonly InMemoryPartnerStore _store = new();
    private readonly FixedClock _clock = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly PartnerOptions _options = new();

    private PartnerService Partners() => new(_store, _clock);
    private ReferralService Referrals() => new(_store, Options.Create(_options), _clock);

    /// <summary>Submitting only creates a pending record — no partner, no referral code, no dashboard.</summary>
    [Fact]
    public void Submit_CreatesPendingRecord_NoActivation()
    {
        var service = Partners();

        var application = service.Submit("Ada", "Advisers Ltd", "ada@x", "we advise subbies", "no site ties", false);

        Assert.Equal(PartnerApplicationStatus.Pending, application.Status);
        Assert.Null(_store.GetPartnerByCode(application.Id.ToString())); // no partner minted
        Assert.Null(service.GetDashboard("anything"));                   // nothing to access
    }

    /// <summary>Approving a clean application activates a partner with a unique referral code.</summary>
    [Fact]
    public void Approve_CleanApplication_ActivatesPartner()
    {
        var service = Partners();
        var application = service.Submit("Ada", "Advisers Ltd", "ada@x", "we advise subbies", "no site ties", false);

        var partner = service.Approve(application.Id);

        Assert.True(partner.Active);
        Assert.StartsWith("TW-", partner.ReferralCode);
        Assert.Equal(PartnerApplicationStatus.Approved, _store.GetApplication(application.Id)!.Status);
        Assert.NotNull(service.GetDashboard(partner.ReferralCode));
    }

    /// <summary>An applicant who controls site access can never be approved (§7.3).</summary>
    [Fact]
    public void Approve_SiteAccessController_IsRefused()
    {
        var service = Partners();
        var application = service.Submit("Gate Co", "Gate Co", "g@x", "we run inductions", "we manage the gate", controlsSiteAccess: true);

        Assert.True(application.HasAccessConflict);
        var ex = Assert.Throws<InvalidOperationException>(() => service.Approve(application.Id));
        Assert.Contains("site access", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(service.GetDashboard("anything")); // no partner exists
    }

    /// <summary>A referral is captured for an active partner, and ignored for an unknown code.</summary>
    [Fact]
    public void Referral_CapturedForActivePartner_IgnoredOtherwise()
    {
        var partners = Partners();
        var referrals = Referrals();
        var application = partners.Submit("Ada", "Advisers Ltd", "ada@x", "advise", "none", false);
        var partner = partners.Approve(application.Id);

        Assert.NotNull(referrals.Capture(partner.ReferralCode, ReferralSource.Demo, "Sub Ltd"));
        Assert.Null(referrals.Capture("TW-NOPE99", ReferralSource.Demo, "Sub Ltd"));
    }

    /// <summary>A commission is 20% of first-year revenue and reversible against its referral.</summary>
    [Fact]
    public void Commission_RaisedAtRate_AndClawbackReversesAgainstReferral()
    {
        var partners = Partners();
        var referrals = Referrals();
        var partner = partners.Approve(partners.Submit("Ada", "Advisers", "a@x", "advise", "none", false).Id);
        var referral = referrals.Capture(partner.ReferralCode, ReferralSource.Demo, "Sub Ltd")!;

        var commission = referrals.RaiseCommission(referral.Id, 1000m);
        Assert.Equal(200m, commission.Amount);                       // 20% of 1000
        Assert.Equal(referral.Id, commission.ReferralId);            // tied to the specific referral
        Assert.Equal(_clock.GetUtcNow().AddDays(90), commission.ClawbackDeadlineUtc);

        var reversed = referrals.Clawback(commission.Id);
        Assert.Equal(CommissionStatus.Reversed, reversed.Status);
        Assert.Equal(referral.Id, reversed.ReferralId);             // reversal is against that referral
        Assert.NotNull(reversed.ReversedUtc);
    }

    /// <summary>Clawback is refused once the 90-day window has passed.</summary>
    [Fact]
    public void Clawback_AfterWindow_IsRefused()
    {
        var partners = Partners();
        var referrals = Referrals();
        var partner = partners.Approve(partners.Submit("Ada", "Advisers", "a@x", "advise", "none", false).Id);
        var referral = referrals.Capture(partner.ReferralCode, ReferralSource.Demo, "Sub Ltd")!;
        var commission = referrals.RaiseCommission(referral.Id, 1000m);

        _clock.Advance(TimeSpan.FromDays(91));

        Assert.Throws<InvalidOperationException>(() => referrals.Clawback(commission.Id));
    }

    /// <summary>Dashboard totals reflect pending, paid and reversed commissions.</summary>
    [Fact]
    public void Dashboard_TotalsByStatus()
    {
        var partners = Partners();
        var referrals = Referrals();
        var partner = partners.Approve(partners.Submit("Ada", "Advisers", "a@x", "advise", "none", false).Id);
        var referral = referrals.Capture(partner.ReferralCode, ReferralSource.Demo, "Sub Ltd")!;
        var paid = referrals.RaiseCommission(referral.Id, 500m);   // 100
        referrals.MarkPaid(paid.Id);
        var clawed = referrals.RaiseCommission(referral.Id, 500m);  // 100
        referrals.Clawback(clawed.Id);
        referrals.RaiseCommission(referral.Id, 1000m);              // 200 pending

        var dashboard = partners.GetDashboard(partner.ReferralCode)!;

        Assert.Equal(100m, dashboard.PaidTotal);
        Assert.Equal(200m, dashboard.PendingTotal);
        Assert.Equal(100m, dashboard.ReversedTotal);
    }

    /// <summary>A fixed, advanceable clock for deterministic time-based rules.</summary>
    private sealed class FixedClock : TimeProvider
    {
        private DateTimeOffset _now;
        public FixedClock(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }
}
