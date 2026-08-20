using Tedwren.Abstractions.Common;
using Tedwren.Abstractions.Contracts.SiteEntry;
using Tedwren.Client.Pages.SiteGate;
using Tedwren.UiComponents.Models;
using Xunit;

namespace Tedwren.Client.Tests;

/// <summary>
/// Locks the R18 rule at the site-entry result surface: the subcontractor product never presents a site-access
/// decision — it only ever "records" — while the main contractor product presents the actual entry decision
/// (MC-8/9) with a manager override (MC-11).
/// </summary>
public sealed class SiteGateResultPresenterTests
{
    private static EntryDecisionResultDto Result(bool admitted) => new(
        Admitted: admitted,
        BlockReason: admitted ? null : "Card expired",
        WasOverridden: false,
        DecisionId: System.Guid.NewGuid(),
        ElapsedMs: 12,
        Checks: System.Array.Empty<DecisionCheckResultDto>());

    [Theory] // R18: the subcontractor result must never read as an access verdict.
    [InlineData(true)]
    [InlineData(false)]
    public void Subcontractor_NeverPresentsAnAccessDecision(bool admitted)
    {
        var view = SiteGateResultPresenter.For(OrgType.Subcontractor, Result(admitted));

        var text = (view.Title + " " + view.Message).ToLowerInvariant();
        Assert.DoesNotContain("permitted", text);
        Assert.DoesNotContain("denied", text);
        Assert.DoesNotContain("blocked", text);
        Assert.Contains("recorded", text);
        Assert.False(view.OffersOverride);                       // nothing to override — it never blocks
        Assert.NotEqual(StatusKind.Danger, view.Severity);       // outstanding items are a warning, not a block
    }

    [Fact] // MC-8/9/11: the main contractor result is the actual decision, with an override on a block.
    public void MainContractor_Blocked_PresentsDecisionAndOffersOverride()
    {
        var view = SiteGateResultPresenter.For(OrgType.MainContractor, Result(admitted: false));

        Assert.Equal("Entry blocked", view.Title);
        Assert.Equal(StatusKind.Danger, view.Severity);
        Assert.True(view.OffersOverride);
    }

    [Fact] // An unknown product keeps the main-contractor decision presentation (safe default).
    public void NullProduct_KeepsTheDecisionPresentation()
    {
        var view = SiteGateResultPresenter.For(null, Result(admitted: false));

        Assert.Equal("Entry blocked", view.Title);
        Assert.True(view.OffersOverride);
    }
}
