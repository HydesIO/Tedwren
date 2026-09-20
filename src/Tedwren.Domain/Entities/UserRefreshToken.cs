namespace Tedwren.Domain.Entities;

/// <summary>
/// A console user's refresh token (M8), so a signed-in manager/admin (including the mobile manager mode) can renew
/// an expired 8-hour access token without re-entering their password. One row per issued token → a user may hold
/// several concurrent sessions, each independently revocable. Only the PBKDF2 hash of the token's secret is stored;
/// the row id is the token's public selector. Rotated on every use; scoped to the owning company (R15).
/// </summary>
public sealed class UserRefreshToken
{
    /// <summary>Stable identifier — also the public selector embedded in the opaque token (<c>{Id}.{secret}</c>).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The console user this token belongs to.</summary>
    public required Guid UserId { get; init; }

    /// <summary>The user's tenant company (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>PBKDF2 hash of the current (rotating) token secret.</summary>
    public required string TokenHash { get; set; }

    /// <summary>When the current token expires (UTC).</summary>
    public DateTimeOffset ExpiresUtc { get; set; }

    /// <summary>When the token was first issued (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When the token was last rotated/used (UTC).</summary>
    public DateTimeOffset LastUsedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the token was revoked (UTC); null while live.</summary>
    public DateTimeOffset? RevokedUtc { get; set; }

    /// <summary>Whether the token can currently be used: not revoked and unexpired at <paramref name="asOf"/>.</summary>
    public bool CanUse(DateTimeOffset asOf) => RevokedUtc is null && ExpiresUtc > asOf;
}
