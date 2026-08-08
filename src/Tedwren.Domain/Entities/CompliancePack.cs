using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A shareable compliance pack (SUB-13–SUB-26): a fixed-at-send snapshot of selected operatives' compliance,
/// reachable by a recipient with <b>no account</b> (R8) via an opaque, non-guessable token plus a passcode
/// (SUB-18), valid only until its expiry (SUB-18/Q12). The pack's content is frozen when it is created (R7);
/// there are no permanent public asset URLs — access always goes through the token + passcode + expiry gate
/// (R9). It can be revoked (SUB-21) or superseded by a re-issue (R7).
/// </summary>
public sealed class CompliancePack
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The company that owns and sends the pack (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>A human title for the pack.</summary>
    public required string Title { get; init; }

    /// <summary>The site the pack is for, if scoped to one.</summary>
    public Guid? SiteId { get; init; }

    /// <summary>The site name captured for display.</summary>
    public string? SiteName { get; init; }

    /// <summary>The start of the date range the pack covers, if set.</summary>
    public DateOnly? FromDate { get; init; }

    /// <summary>The end of the date range the pack covers, if set.</summary>
    public DateOnly? ToDate { get; init; }

    /// <summary>Who created/sent the pack (a nominated role, SUB-22).</summary>
    public required string CreatedBy { get; init; }

    /// <summary>When the pack was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When access expires (SUB-18; default 30 days, Q12).</summary>
    public required DateTimeOffset ExpiresUtc { get; init; }

    /// <summary>The opaque, non-guessable access token embedded in the share link (R9).</summary>
    public required string Token { get; init; }

    /// <summary>The salted hash of the passcode — the plaintext is never stored (SUB-18).</summary>
    public required string PasscodeHash { get; init; }

    /// <summary>The pack's base status (expiry is applied on top when read).</summary>
    public PackStatus Status { get; set; } = PackStatus.Active;

    /// <summary>The pack that superseded this one, if it was re-issued (R7).</summary>
    public Guid? SupersededByPackId { get; set; }

    /// <summary>The fixed-at-send compliance snapshot (R7).</summary>
    public required IReadOnlyList<PackSubject> Subjects { get; init; }

    /// <summary>The effective status at a point in time: a live pack past its expiry reads as expired (SUB-18).</summary>
    public PackStatus EffectiveStatus(DateTimeOffset now) =>
        Status == PackStatus.Active && now > ExpiresUtc ? PackStatus.Expired : Status;

    /// <summary>Whether the pack can currently be accessed by a recipient (SUB-18/SUB-21).</summary>
    public bool IsAccessible(DateTimeOffset now) => EffectiveStatus(now) == PackStatus.Active;
}
