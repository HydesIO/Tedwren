using Tedwren.Domain.Entities;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Verifies the operative RAMS acknowledgement validity rule (Subcontractor Onboarding spec Gate 5): a signature is
/// valid until its re-sign deadline (the MC's review cycle), and forever when no deadline is set (version-pinned).
/// </summary>
public class RamsAcknowledgementTests
{
    private static RamsAcknowledgement Ack(DateTimeOffset? expires) => new()
    {
        CompanyId = Guid.NewGuid(),
        PersonId = Guid.NewGuid(),
        FamilyId = Guid.NewGuid(),
        Version = 1,
        SignatureName = "Alex Operative",
        ExpiresUtc = expires,
    };

    [Fact] // No review cycle set → the signature never lapses (pinned to the version).
    public void IsValid_IsTrue_WhenNoExpiry()
    {
        Assert.True(Ack(expires: null).IsValid(DateTimeOffset.UtcNow));
    }

    [Fact] // Before the re-sign deadline → still valid.
    public void IsValid_IsTrue_BeforeExpiry()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.True(Ack(now.AddDays(1)).IsValid(now));
    }

    [Fact] // After the re-sign deadline → lapsed (must re-sign).
    public void IsValid_IsFalse_AfterExpiry()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.False(Ack(now.AddDays(-1)).IsValid(now));
    }
}
