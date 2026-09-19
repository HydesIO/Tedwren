namespace Tedwren.Domain.Entities;

/// <summary>
/// A pending one-time-code challenge for operative sign-in (M2), keyed by the operative's canonical mobile
/// number (SF-1). Only the hash of the code is stored; a short expiry and a capped attempt count limit
/// guessing. A challenge exists only when the number matched an engaged operative, so its presence is never
/// revealed to the caller (no enumeration).
/// </summary>
public sealed class OtpChallenge
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The canonical E.164 mobile number the code was issued for.</summary>
    public required string PhoneNumber { get; init; }

    /// <summary>PBKDF2 hash of the one-time code.</summary>
    public required string CodeHash { get; init; }

    /// <summary>When the code expires (UTC).</summary>
    public required DateTimeOffset ExpiresUtc { get; init; }

    /// <summary>How many failed verification attempts have been made against this challenge.</summary>
    public int Attempts { get; set; }

    /// <summary>When the challenge was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Whether the challenge has expired as of <paramref name="asOf"/>.</summary>
    public bool IsExpired(DateTimeOffset asOf) => asOf >= ExpiresUtc;
}
