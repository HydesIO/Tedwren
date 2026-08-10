using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A specific qualification/card held by a <see cref="Person"/> (a CSCS card, a First Aid certificate, …).
/// Captured by photograph or upload (SF-5); an uncertain reading is flagged for review, never accepted
/// silently (<see cref="NeedsReview"/>). A named person confirms it (SF-6). Its currency is always
/// computed from the expiry date via <see cref="GetStatus"/> (SF-8). A renewal creates a new card that
/// supersedes this one; the superseded record is retained, never overwritten (SF-10).
/// </summary>
public sealed class QualificationCard
{
    /// <summary>Default window (days) before expiry within which a card is "expiring soon" (SF-8/SF-9).</summary>
    public const int DefaultExpiringSoonWindowDays = 60;

    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The person who holds the card (identity is the person, not the engagement; SF-1).</summary>
    public required Guid PersonId { get; init; }

    /// <summary>The qualification type this card is an instance of.</summary>
    public required Guid QualificationTypeId { get; init; }

    /// <summary>The card number as read/entered.</summary>
    public string? CardNumber { get; set; }

    /// <summary>The holder name as read from the card (SF-5); not reconciled with the engagement name.</summary>
    public string? HolderName { get; set; }

    /// <summary>Issue date, if known.</summary>
    public DateOnly? IssuedOn { get; set; }

    /// <summary>Expiry date, if any. Null means the qualification does not expire.</summary>
    public DateOnly? ExpiresOn { get; set; }

    /// <summary>How the card was captured (SF-5).</summary>
    public CardCaptureSource CaptureSource { get; set; } = CardCaptureSource.Photo;

    /// <summary>An optional handle to the stored card image (never a permanent public URL, R9).</summary>
    public string? ImageReference { get; set; }

    /// <summary>Assurance state (SF-7). New captures start <see cref="CardVerificationState.ReadUnchecked"/>.</summary>
    public CardVerificationState VerificationState { get; set; } = CardVerificationState.ReadUnchecked;

    /// <summary>True when an uncertain reading needs a person to confirm the details (SF-5).</summary>
    public bool NeedsReview { get; set; }

    /// <summary>Name of the person who confirmed the card, once confirmed (SF-6).</summary>
    public string? ConfirmedBy { get; set; }

    /// <summary>When the card was confirmed, once confirmed (SF-6; UTC).</summary>
    public DateTimeOffset? ConfirmedUtc { get; set; }

    /// <summary>The card this one renews/replaces, if it is a renewal (SF-10).</summary>
    public Guid? SupersedesCardId { get; init; }

    /// <summary>The later card that superseded this one, if it has been renewed (SF-10).</summary>
    public Guid? SupersededByCardId { get; set; }

    /// <summary>When the card record was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>True once a later card has superseded this one (SF-10). Superseded cards are retained.</summary>
    public bool IsSuperseded => SupersededByCardId is not null;

    /// <summary>
    /// Computes the card's currency from its expiry date (SF-8) — never stored, always derived. A card
    /// with no expiry is <see cref="CardStatus.Valid"/>; one past expiry is <see cref="CardStatus.Expired"/>;
    /// one within <paramref name="expiringSoonWindowDays"/> of expiry is <see cref="CardStatus.ExpiringSoon"/>.
    /// </summary>
    public CardStatus GetStatus(DateOnly asOf, int expiringSoonWindowDays = DefaultExpiringSoonWindowDays)
    {
        if (ExpiresOn is not { } expiry)
        {
            return CardStatus.Valid;
        }

        if (expiry < asOf)
        {
            return CardStatus.Expired;
        }

        return expiry <= asOf.AddDays(expiringSoonWindowDays) ? CardStatus.ExpiringSoon : CardStatus.Valid;
    }
}
