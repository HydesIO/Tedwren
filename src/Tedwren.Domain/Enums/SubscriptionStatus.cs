namespace Tedwren.Domain.Enums;

/// <summary>
/// State of a company's billing subscription — its ongoing agreement to be charged the metered SaaS fee
/// (PRD §9). Pausing a subscription stops billing without deleting history (§9 "off hides but never deletes").
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Active — the company is billed on the agreed meter.</summary>
    Active = 0,

    /// <summary>Paused — billing suspended, history retained.</summary>
    Paused = 1,

    /// <summary>Cancelled — the agreement has ended.</summary>
    Cancelled = 2,
}
