using Tedwren.Abstractions.Common;
using Tedwren.Application.Organisation;
using Tedwren.Domain.Entities;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Verifies the compliance roll-up (SF-8): the state is derived from card statuses (never invented), reports
/// Pending when there is nothing to assess, and downgrades to at-risk / non-compliant as cards expire.
/// </summary>
public sealed class ComplianceRollupTests
{
    private static readonly DateOnly Today = new(2026, 8, 10);

    private static QualificationCard Card(DateOnly? expiresOn) =>
        new() { PersonId = Guid.NewGuid(), QualificationTypeId = Guid.NewGuid(), ExpiresOn = expiresOn };

    [Fact]
    public void NoCards_IsPending()
    {
        var (state, percent) = ComplianceRollup.FromCards(Array.Empty<QualificationCard>(), Today);

        Assert.Equal(ComplianceState.Pending, state);
        Assert.Null(percent);
    }

    [Fact]
    public void AllInDate_IsCompliant_At100Percent()
    {
        var cards = new[] { Card(Today.AddYears(1)), Card(null) };   // one dated, one no-expiry

        var (state, percent) = ComplianceRollup.FromCards(cards, Today);

        Assert.Equal(ComplianceState.Compliant, state);
        Assert.Equal(100, percent);
    }

    [Fact]
    public void SomethingExpiringSoon_IsAtRisk()
    {
        var cards = new[] { Card(Today.AddYears(1)), Card(Today.AddDays(10)) };

        var (state, percent) = ComplianceRollup.FromCards(cards, Today);

        Assert.Equal(ComplianceState.AtRisk, state);
        Assert.Equal(50, percent);   // one of two counts as fully valid
    }

    [Fact]
    public void SomethingExpired_IsNonCompliant()
    {
        var cards = new[] { Card(Today.AddYears(1)), Card(Today.AddDays(-1)) };

        var (state, _) = ComplianceRollup.FromCards(cards, Today);

        Assert.Equal(ComplianceState.NonCompliant, state);
    }
}
