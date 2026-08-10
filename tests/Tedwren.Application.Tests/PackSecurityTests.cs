using System.Security.Cryptography;
using System.Text;
using Tedwren.Application.CompliancePacks;
using Xunit;

namespace Tedwren.Application.Tests;

/// <summary>
/// Covers the compliance-pack link hardening from the security review: PBKDF2 passcode hashing (with legacy
/// verification kept working), and per-token passcode rate limiting.
/// </summary>
public sealed class PackSecurityTests
{
    [Fact]
    public void Passcode_Hash_IsPbkdf2_AndRoundTrips()
    {
        var hash = PackPasscode.Hash("ABCD2345");

        Assert.StartsWith("pbkdf2:", hash);
        Assert.True(PackPasscode.Verify("ABCD2345", hash));
        Assert.False(PackPasscode.Verify("WRONG123", hash));
    }

    [Fact]
    public void Passcode_Verify_StillAcceptsLegacySha256()
    {
        // Reproduce the old salt:hash (salted SHA-256) format and confirm Verify still accepts it.
        var salt = RandomNumberGenerator.GetBytes(16);
        var input = new byte[salt.Length + Encoding.UTF8.GetByteCount("LEGACY12")];
        salt.CopyTo(input, 0);
        Encoding.UTF8.GetBytes("LEGACY12", 0, 8, input, salt.Length);
        var legacy = Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(SHA256.HashData(input));

        Assert.True(PackPasscode.Verify("LEGACY12", legacy));
        Assert.False(PackPasscode.Verify("nope", legacy));
    }

    [Fact]
    public void Throttle_LocksOut_AfterMaxFailures()
    {
        var throttle = new InMemoryPackAccessThrottle();
        const string token = "tok-123";

        Assert.False(throttle.IsLockedOut(token));
        for (var i = 0; i < InMemoryPackAccessThrottle.MaxFailures; i++)
        {
            Assert.False(throttle.IsLockedOut(token));   // not yet locked while registering
            throttle.RegisterFailure(token);
        }

        Assert.True(throttle.IsLockedOut(token));

        throttle.Reset(token);                            // a success clears it
        Assert.False(throttle.IsLockedOut(token));
    }

    [Fact]
    public void Throttle_Lockout_ExpiresAfterWindow()
    {
        var now = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var throttle = new InMemoryPackAccessThrottle(() => now);
        const string token = "tok-xyz";

        for (var i = 0; i < InMemoryPackAccessThrottle.MaxFailures; i++)
        {
            throttle.RegisterFailure(token);
        }

        Assert.True(throttle.IsLockedOut(token));

        now = now.AddMinutes(InMemoryPackAccessThrottle.LockoutMinutes + 1);
        Assert.False(throttle.IsLockedOut(token));
    }
}
