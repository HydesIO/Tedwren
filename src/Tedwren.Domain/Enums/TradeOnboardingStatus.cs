namespace Tedwren.Domain.Enums;

/// <summary>
/// The review lifecycle of a trade's self-service onboarding submission (UAT-023, SUB-4/MC-27). The vocabulary
/// uses <b>Returned</b> for "sent back for changes", never "denied" (R18).
/// </summary>
public enum TradeOnboardingStatus
{
    /// <summary>Invited: the trade has a link but has not yet submitted their documents for review.</summary>
    Invited = 0,

    /// <summary>Submitted: the trade has sent their documents in and a manager must review them.</summary>
    Submitted = 1,

    /// <summary>Approved: the manager accepted the submission; the trade is onboarded.</summary>
    Approved = 2,

    /// <summary>Rejected: the manager declined the submission (a reason is recorded).</summary>
    Rejected = 3,

    /// <summary>Returned: sent back to the trade with comments to correct and resubmit (a reason is recorded).</summary>
    Returned = 4,
}
