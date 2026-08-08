using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Verifies the pack readiness check (SUB-14): expired cards are blocking problems, expiring-soon cards are
/// warnings, and valid cards raise nothing — so not-site-ready operatives are surfaced before send.
/// </summary>
public sealed class PackReadinessTests
{
    private static PackSubject Subject(string name, CardStatus status) =>
        new(Guid.NewGuid(), name, new[] { new PackCard("CSCS", status.ToString(), status, null, "Read") });

    [Fact]
    public void ExpiredCard_IsBlocking()
    {
        var issues = PackReadiness.Evaluate(new[] { Subject("J. Fletcher", CardStatus.Expired) });

        var issue = Assert.Single(issues);
        Assert.True(issue.Blocking);
        Assert.True(PackReadiness.HasBlocking(issues));
    }

    [Fact]
    public void ExpiringSoonCard_WarnsButDoesNotBlock()
    {
        var issues = PackReadiness.Evaluate(new[] { Subject("M. Adeyemi", CardStatus.ExpiringSoon) });

        var issue = Assert.Single(issues);
        Assert.False(issue.Blocking);
        Assert.False(PackReadiness.HasBlocking(issues));
    }

    [Fact]
    public void ValidCards_RaiseNothing() =>
        Assert.Empty(PackReadiness.Evaluate(new[] { Subject("R. Okafor", CardStatus.Valid) }));
}
