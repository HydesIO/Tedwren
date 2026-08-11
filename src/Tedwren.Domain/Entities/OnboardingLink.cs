using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A self-service onboarding link (SF-4, SUB-2): an operative completes their own details and uploads their
/// card photographs from a link, without an account. Reached by a token (and optional passcode); the details
/// they submit create a person/engagement (SF-1/SF-2) and unconfirmed cards an administrator later confirms
/// (SF-6). Scoped to the inviting company (R15).
/// </summary>
public sealed class OnboardingLink
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The unguessable link token.</summary>
    public required string Token { get; init; }

    /// <summary>Optional salted passcode hash (SUB-18 pattern). Null when no passcode is required.</summary>
    public string? PasscodeHash { get; set; }

    /// <summary>The inviting company.</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>Suggested name for the operative (prefilled), if the sender supplied one.</summary>
    public string? Name { get; set; }

    /// <summary>Suggested trade, if the sender supplied one.</summary>
    public string? Trade { get; set; }

    /// <summary>When the link expires (SUB-18 default 30 days).</summary>
    public required DateTimeOffset ExpiresUtc { get; init; }

    /// <summary>The link's state.</summary>
    public OnboardingLinkStatus Status { get; set; } = OnboardingLinkStatus.Pending;

    /// <summary>The person created/resolved from the submitted details (SF-1). Null until details are submitted.</summary>
    public Guid? PersonId { get; set; }

    /// <summary>The engagement created from the submitted details (SF-2). Null until details are submitted.</summary>
    public Guid? EngagementId { get; set; }

    /// <summary>The console user who created the link.</summary>
    public Guid? CreatedByUserId { get; init; }

    /// <summary>When the link was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Whether the link is still usable at <paramref name="asOf"/> (pending and unexpired).</summary>
    public bool IsUsable(DateTimeOffset asOf) => Status != OnboardingLinkStatus.Revoked && ExpiresUtc > asOf;
}

/// <summary>
/// A stored binary asset (e.g. a card photograph, SF-5). Held privately and served only through an
/// authorised endpoint — never a permanent public URL (R9).
/// </summary>
public sealed class StoredImage
{
    /// <summary>Stable identifier (used as the image reference on a card).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>MIME content type.</summary>
    public required string ContentType { get; init; }

    /// <summary>The raw bytes.</summary>
    public required byte[] Bytes { get; init; }

    /// <summary>When the image was stored (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
