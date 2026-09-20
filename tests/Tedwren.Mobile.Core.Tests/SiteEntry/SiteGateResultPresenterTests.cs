using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Mobile.Core.SiteEntry;

namespace Tedwren.Mobile.Core.Tests.SiteEntry;

/// <summary>
/// Verifies the ported R18 gate presenter: subcontractors never see an access decision (recorded/site-ready, no
/// override), main contractors see the admit/block decision with a manager override, and a null product keeps the
/// fail-closed main-contractor framing (R2).
/// </summary>
public class SiteGateResultPresenterTests
{
    private static EntryDecisionResultDto Result(bool admitted, bool overridden = false, string? blockReason = null) =>
        new(admitted, blockReason, overridden, Guid.NewGuid(), 42, Array.Empty<DecisionCheckResultDto>());

    [Fact]
    public void Subcontractor_admitted_is_recorded_site_ready_without_override()
    {
        var view = SiteGateResultPresenter.For(OrgType.Subcontractor, Result(admitted: true));

        Assert.Equal(GateResultSeverity.Success, view.Severity);
        Assert.Equal("Recorded — site-ready", view.Title);
        Assert.False(view.OffersOverride);
    }

    [Fact]
    public void Subcontractor_not_admitted_never_says_blocked_and_offers_no_override() // R18
    {
        var view = SiteGateResultPresenter.For(OrgType.Subcontractor, Result(admitted: false, blockReason: "Card expired"));

        Assert.Equal(GateResultSeverity.Warning, view.Severity);
        Assert.Equal("Recorded — action needed", view.Title);
        Assert.DoesNotContain("block", view.Title, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("denied", view.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(view.OffersOverride);
    }

    [Fact]
    public void MainContractor_admitted_reads_admitted()
    {
        var view = SiteGateResultPresenter.For(OrgType.MainContractor, Result(admitted: true));

        Assert.Equal(GateResultSeverity.Success, view.Severity);
        Assert.Equal("Admitted", view.Title);
        Assert.False(view.OffersOverride);
    }

    [Fact]
    public void MainContractor_blocked_offers_an_override()
    {
        var view = SiteGateResultPresenter.For(OrgType.MainContractor, Result(admitted: false, blockReason: "Induction expired"));

        Assert.Equal(GateResultSeverity.Danger, view.Severity);
        Assert.Equal("Entry blocked", view.Title);
        Assert.Equal("Induction expired", view.Message);
        Assert.True(view.OffersOverride);
    }

    [Fact]
    public void MainContractor_admitted_by_override_says_so()
    {
        var view = SiteGateResultPresenter.For(OrgType.MainContractor, Result(admitted: true, overridden: true));

        Assert.Equal("Admitted (manager override)", view.Title);
    }

    [Fact]
    public void Null_product_keeps_the_fail_closed_main_contractor_framing()
    {
        var view = SiteGateResultPresenter.For(null, Result(admitted: false, blockReason: "Not registered"));

        Assert.Equal(GateResultSeverity.Danger, view.Severity);
        Assert.Equal("Entry blocked", view.Title);
        Assert.True(view.OffersOverride);
    }
}
