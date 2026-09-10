using Tedwren.Domain.Enums;

namespace Tedwren.Domain.Entities;

/// <summary>
/// A trade self-service onboarding invitation (UAT-023, SUB-4/MC-27). A main contractor invites a trade
/// (subcontractor) company to upload its own registration, RAMS, insurance and accreditations from a tokenised
/// link, without a console account; the submission is then reviewed and approved / rejected / returned by a
/// manager. Reached by a token (and optional passcode), scoped to the inviting-created trade company (R15). The
/// token/passcode/expiry machinery mirrors <see cref="OnboardingLink"/>; the review state mirrors the timesheet
/// and form-submission workflows.
/// </summary>
public sealed class TradeInvite
{
    /// <summary>Stable identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The unguessable link token (R9).</summary>
    public required string Token { get; init; }

    /// <summary>Optional salted passcode hash (SUB-18 pattern). Null when no passcode is required.</summary>
    public string? PasscodeHash { get; set; }

    /// <summary>The trade company created for this invitation — where the uploaded documents attach (R15).</summary>
    public required Guid CompanyId { get; init; }

    /// <summary>The inviting main contractor's company — the tenant that owns and reviews this invitation (R15).</summary>
    public required Guid InviterCompanyId { get; init; }

    /// <summary>The trade's primary contact name, if supplied by the inviter.</summary>
    public string? ContactName { get; set; }

    /// <summary>The trade's contact email the invite is sent to, if supplied.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>The review state (UAT-023).</summary>
    public TradeOnboardingStatus Status { get; set; } = TradeOnboardingStatus.Invited;

    /// <summary>When the link expires (SUB-18 default 30 days).</summary>
    public required DateTimeOffset ExpiresUtc { get; init; }

    /// <summary>When the trade submitted their documents for review, if they have.</summary>
    public DateTimeOffset? SubmittedUtc { get; set; }

    /// <summary>The manager who approved/rejected/returned the submission, if decided.</summary>
    public string? DecidedBy { get; set; }

    /// <summary>When the submission was decided, if decided.</summary>
    public DateTimeOffset? DecidedUtc { get; set; }

    /// <summary>The manager's note — required when rejecting or returning (UAT-023).</summary>
    public string? ReviewNote { get; set; }

    /// <summary>The console user who created the invite.</summary>
    public Guid? CreatedByUserId { get; init; }

    /// <summary>When the invite was created (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Whether the link is still usable at <paramref name="asOf"/> (not decided-and-closed, and unexpired).</summary>
    public bool IsUsable(DateTimeOffset asOf) =>
        Status is TradeOnboardingStatus.Invited or TradeOnboardingStatus.Submitted or TradeOnboardingStatus.Returned
        && ExpiresUtc > asOf;
}
