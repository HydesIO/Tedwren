using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Verifies the fail-closed admission rule (MC-8, R2): a worker is admitted only when no check failed; a
/// check that did not run does not block; and the block reason lists the failed checks (MC-9).
/// </summary>
public sealed class SiteEntryPolicyTests
{
    private static DecisionCheck Check(DecisionCheckOutcome outcome, string? detail = null) =>
        new("Check", outcome, detail);

    [Fact]
    public void AllPassed_IsAdmitted() =>
        Assert.True(SiteEntryPolicy.IsAdmitted(new[] { Check(DecisionCheckOutcome.Passed), Check(DecisionCheckOutcome.Passed) }));

    [Fact]
    public void NotRun_DoesNotBlock() =>
        Assert.True(SiteEntryPolicy.IsAdmitted(new[] { Check(DecisionCheckOutcome.Passed), Check(DecisionCheckOutcome.NotRun) }));

    [Fact]
    public void AnyFailed_IsNotAdmitted() =>
        Assert.False(SiteEntryPolicy.IsAdmitted(new[] { Check(DecisionCheckOutcome.Passed), Check(DecisionCheckOutcome.Failed) }));

    [Fact]
    public void BlockReason_ListsFailedDetails()
    {
        var checks = new[]
        {
            new DecisionCheck("Registered", DecisionCheckOutcome.Passed, null),
            new DecisionCheck("Cards", DecisionCheckOutcome.Failed, "A card has expired"),
            new DecisionCheck("Induction", DecisionCheckOutcome.Failed, "No induction"),
        };

        var reason = SiteEntryPolicy.BlockReason(checks);

        Assert.Contains("A card has expired", reason);
        Assert.Contains("No induction", reason);
    }

    [Fact]
    public void BlockReason_IsNull_WhenAdmitted() =>
        Assert.Null(SiteEntryPolicy.BlockReason(new[] { Check(DecisionCheckOutcome.Passed) }));
}
