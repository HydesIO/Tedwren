using Tedwren.Domain.Entities;
using Tedwren.Domain.Enums;
using Xunit;

namespace Tedwren.Domain.Tests;

/// <summary>
/// Verifies the pack's effective-status and accessibility rules (SUB-18/SUB-21): a live pack past its expiry
/// reads as expired, and a revoked or expired pack is not accessible.
/// </summary>
public sealed class CompliancePackTests
{
    private static CompliancePack Pack(PackStatus status, DateTimeOffset expiresUtc) => new()
    {
        CompanyId = Guid.NewGuid(),
        Title = "Pack",
        CreatedBy = "Alex",
        ExpiresUtc = expiresUtc,
        Token = "tok",
        PasscodeHash = "hash",
        Status = status,
        Subjects = Array.Empty<PackSubject>(),
    };

    private static readonly DateTimeOffset Now = new(2026, 8, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ActivePack_BeforeExpiry_IsAccessible()
    {
        var pack = Pack(PackStatus.Active, Now.AddDays(10));
        Assert.Equal(PackStatus.Active, pack.EffectiveStatus(Now));
        Assert.True(pack.IsAccessible(Now));
    }

    [Fact]
    public void ActivePack_PastExpiry_ReadsAsExpired_AndIsNotAccessible()
    {
        var pack = Pack(PackStatus.Active, Now.AddDays(-1));
        Assert.Equal(PackStatus.Expired, pack.EffectiveStatus(Now));
        Assert.False(pack.IsAccessible(Now));
    }

    [Fact]
    public void RevokedPack_IsNotAccessible()
    {
        var pack = Pack(PackStatus.Revoked, Now.AddDays(10));
        Assert.False(pack.IsAccessible(Now));
    }
}
