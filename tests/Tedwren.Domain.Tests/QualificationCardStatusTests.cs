using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Verifies that a card's status is computed from its expiry date (SF-8): valid when in date or
/// non-expiring, expiring-soon within the window, expired past the date. Never typed in.
/// </summary>
public sealed class QualificationCardStatusTests
{
    private static readonly DateOnly Today = new(2026, 8, 6);

    private static QualificationCard Card(DateOnly? expiry) =>
        new() { PersonId = Guid.NewGuid(), QualificationTypeId = Guid.NewGuid(), ExpiresOn = expiry };

    [Fact]
    public void NoExpiry_IsValid() =>
        Assert.Equal(CardStatus.Valid, Card(null).GetStatus(Today));

    [Fact]
    public void PastExpiry_IsExpired() =>
        Assert.Equal(CardStatus.Expired, Card(Today.AddDays(-1)).GetStatus(Today));

    [Fact]
    public void OnExpiryDate_IsExpiringSoon_NotExpired() =>
        Assert.Equal(CardStatus.ExpiringSoon, Card(Today).GetStatus(Today, expiringSoonWindowDays: 60));

    [Fact]
    public void WithinWindow_IsExpiringSoon() =>
        Assert.Equal(CardStatus.ExpiringSoon, Card(Today.AddDays(30)).GetStatus(Today, expiringSoonWindowDays: 60));

    [Fact]
    public void OnWindowBoundary_IsExpiringSoon() =>
        Assert.Equal(CardStatus.ExpiringSoon, Card(Today.AddDays(60)).GetStatus(Today, expiringSoonWindowDays: 60));

    [Fact]
    public void BeyondWindow_IsValid() =>
        Assert.Equal(CardStatus.Valid, Card(Today.AddDays(90)).GetStatus(Today, expiringSoonWindowDays: 60));
}
