namespace Tedwren.Domain.Enums;

/// <summary>Lifecycle of an affiliate (Web Plan §7). Approval-gated: an affiliate starts Pending until set up.</summary>
public enum AffiliateStatus
{
    /// <summary>Set up but not yet activated.</summary>
    Pending = 0,

    /// <summary>Active — earning commission on associated accounts.</summary>
    Active = 1,

    /// <summary>No longer active.</summary>
    Inactive = 2,
}

/// <summary>Status of a commission payout to an affiliate (Web Plan §7): earned then paid.</summary>
public enum AffiliatePayoutStatus
{
    /// <summary>Earned but not yet paid.</summary>
    Pending = 0,

    /// <summary>Paid to the affiliate (after the customer's funds cleared).</summary>
    Paid = 1,
}

/// <summary>Lifecycle of an affiliate agreement (Web Plan §7): drafted, sent for signing, then signed.</summary>
public enum AffiliateAgreementStatus
{
    /// <summary>Created but not yet issued.</summary>
    Draft = 0,

    /// <summary>Issued to the affiliate to sign.</summary>
    Sent = 1,

    /// <summary>Signed by the affiliate (both parties' signatures captured).</summary>
    Signed = 2,
}
