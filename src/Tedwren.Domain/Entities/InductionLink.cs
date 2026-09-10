using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A shareable, tokenised induction link (MC-1/MC-2, UAT-018): a main contractor sends an operative a link so
/// they can complete a specific induction on their phone <b>without a console account</b>. Reached by a token
/// (and optional passcode), it names the company and the induction template to run; a fresh induction session is
/// started from it and remembered (<see cref="SessionId"/>) so re-opening the link resumes rather than restarts.
/// Scoped to the inviting company and one of its templates (R15).
/// </summary>
public sealed class InductionLink
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The unguessable link token (R9).</summary>
    public required string Token { get; init; }

    /// <summary>Optional salted passcode hash (SUB-18 pattern). Null when no passcode is required.</summary>
    public string? PasscodeHash { get; set; }

    /// <summary>The inviting company (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The induction template the link runs (MC-3).</summary>
    public required Guid TemplateId { get; init; }

    /// <summary>Suggested name for the operative (prefilled), if the sender supplied one.</summary>
    public string? Name { get; set; }

    /// <summary>When the link expires (SUB-18 default 30 days).</summary>
    public required DateTimeOffset ExpiresUtc { get; init; }

    /// <summary>The link's state.</summary>
    public OnboardingLinkStatus Status { get; set; } = OnboardingLinkStatus.Pending;

    /// <summary>The induction session started from this link (MC-1). Null until the operative starts; reused to resume.</summary>
    public Guid? SessionId { get; set; }

    /// <summary>The console user who created the link.</summary>
    public Guid? CreatedByUserId { get; init; }

    /// <summary>When the link was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Whether the link is still usable at <paramref name="asOf"/> (not revoked and unexpired).</summary>
    public bool IsUsable(DateTimeOffset asOf) => Status != OnboardingLinkStatus.Revoked && ExpiresUtc > asOf;
}
