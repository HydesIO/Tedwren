namespace Tedwren.Web.Partners;

// Domain model for the partner programme (Web Plan §7). The programme is approval-gated and is never a
// site-access channel (§7.3): an application is only ever a pending record until a human approves it,
// and an applicant who controls or influences site access can never be approved. Commission is tied to
// a specific referral from the start so a 90-day clawback is reversible against that exact record.

/// <summary>Lifecycle of a partner application (Web Plan §7.2).</summary>
public enum PartnerApplicationStatus
{
    /// <summary>Awaiting a human approval decision — the only state a self-serve submission reaches.</summary>
    Pending,

    /// <summary>Approved by a human; a <see cref="Partner"/> with a referral code now exists.</summary>
    Approved,

    /// <summary>Declined (including anyone caught by the §7.3 site-access exclusion).</summary>
    Rejected,
}

/// <summary>Lifecycle of a commission (Web Plan §7.2): payable, paid, or clawed back.</summary>
public enum CommissionStatus
{
    /// <summary>Earned but not yet paid.</summary>
    Pending,

    /// <summary>Paid to the partner (after funds cleared).</summary>
    Paid,

    /// <summary>Reversed within the clawback window, against its referral.</summary>
    Reversed,
}

/// <summary>Where a referred conversion came from (Web Plan §7).</summary>
public enum ReferralSource
{
    /// <summary>A booked demo attributed to the referral.</summary>
    Demo,

    /// <summary>A Worker Passport checkout attributed to the referral.</summary>
    WorkerPassport,

    /// <summary>Any other attributed conversion.</summary>
    Other,
}

/// <summary>
/// A partner application (Web Plan §7.2). Recorded as <see cref="PartnerApplicationStatus.Pending"/>;
/// approval is a separate human step. <see cref="ControlsSiteAccess"/> answers the §7.3 relationship
/// question, and <see cref="HasAccessConflict"/> makes an applicant who controls site access ineligible.
/// </summary>
/// <param name="Id">Application id.</param>
/// <param name="SubmittedUtc">When it was submitted.</param>
/// <param name="Name">Applicant name.</param>
/// <param name="Company">Applicant company.</param>
/// <param name="Email">Contact email.</param>
/// <param name="HowTheyWork">How they work with subcontractors.</param>
/// <param name="RelationshipToSites">Their stated relationship to any site/contractor.</param>
/// <param name="ControlsSiteAccess">Whether they control or influence site access (§7.3).</param>
/// <param name="Status">Current lifecycle status.</param>
public sealed record PartnerApplication(
    Guid Id,
    DateTimeOffset SubmittedUtc,
    string Name,
    string Company,
    string Email,
    string HowTheyWork,
    string RelationshipToSites,
    bool ControlsSiteAccess,
    PartnerApplicationStatus Status)
{
    /// <summary>True when the applicant controls/influences site access, so they can never be approved (§7.3).</summary>
    public bool HasAccessConflict => ControlsSiteAccess;
}

/// <summary>An approved partner (Web Plan §7.2) with a unique trackable referral code.</summary>
/// <param name="Id">Partner id.</param>
/// <param name="ApplicationId">The application this partner was approved from.</param>
/// <param name="Name">Partner name.</param>
/// <param name="Company">Partner company.</param>
/// <param name="Email">Contact email.</param>
/// <param name="ReferralCode">Unique trackable referral code.</param>
/// <param name="ApprovedUtc">When approved.</param>
/// <param name="Active">Whether the partner is active (referral links/dashboard live).</param>
public sealed record Partner(
    Guid Id,
    Guid ApplicationId,
    string Name,
    string Company,
    string Email,
    string ReferralCode,
    DateTimeOffset ApprovedUtc,
    bool Active);

/// <summary>
/// A tracked referral (Web Plan §7): a conversion attributed to a partner's code. Attribution persists
/// across sessions (via the referral cookie), so credit lands even when the conversion isn't same-session.
/// </summary>
/// <param name="Id">Referral id.</param>
/// <param name="PartnerId">The credited partner.</param>
/// <param name="ReferralCode">The code that attributed it.</param>
/// <param name="Source">Where the conversion came from.</param>
/// <param name="SubjectCompany">The referred company, if known.</param>
/// <param name="CreatedUtc">When recorded.</param>
public sealed record Referral(
    Guid Id,
    Guid PartnerId,
    string ReferralCode,
    ReferralSource Source,
    string? SubjectCompany,
    DateTimeOffset CreatedUtc);

/// <summary>
/// A commission tied to a specific <see cref="Referral"/> (Web Plan §7.2): 20% of first-year
/// subcontractor revenue, payable after funds clear, reversible within a 90-day clawback window.
/// </summary>
/// <param name="Id">Commission id.</param>
/// <param name="ReferralId">The referral this commission is earned against.</param>
/// <param name="PartnerId">The partner earning it.</param>
/// <param name="Amount">Commission amount.</param>
/// <param name="Currency">Currency code.</param>
/// <param name="Status">Current status.</param>
/// <param name="RaisedUtc">When raised.</param>
/// <param name="ClawbackDeadlineUtc">Last moment a clawback may be applied.</param>
/// <param name="ReversedUtc">When reversed, if it was.</param>
public sealed record Commission(
    Guid Id,
    Guid ReferralId,
    Guid PartnerId,
    decimal Amount,
    string Currency,
    CommissionStatus Status,
    DateTimeOffset RaisedUtc,
    DateTimeOffset ClawbackDeadlineUtc,
    DateTimeOffset? ReversedUtc);
