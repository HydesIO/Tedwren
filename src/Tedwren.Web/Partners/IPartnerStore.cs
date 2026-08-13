namespace Tedwren.Web.Partners;

/// <summary>
/// Persistence seam for the partner programme (Web Plan §7). The launch implementation is in-memory;
/// a database-backed store replaces it behind this interface with no service changes. Kept deliberately
/// small — the services hold the rules, the store only holds records.
/// </summary>
public interface IPartnerStore
{
    /// <summary>Adds a partner application.</summary>
    PartnerApplication AddApplication(PartnerApplication application);

    /// <summary>Gets an application by id, or null.</summary>
    PartnerApplication? GetApplication(Guid id);

    /// <summary>Replaces an application (e.g. after a status change).</summary>
    void UpdateApplication(PartnerApplication application);

    /// <summary>All applications, newest first.</summary>
    IReadOnlyList<PartnerApplication> Applications();

    /// <summary>Adds an approved partner.</summary>
    Partner AddPartner(Partner partner);

    /// <summary>Gets a partner by their referral code, or null.</summary>
    Partner? GetPartnerByCode(string referralCode);

    /// <summary>Whether a referral code is already taken.</summary>
    bool CodeExists(string referralCode);

    /// <summary>Adds a referral.</summary>
    Referral AddReferral(Referral referral);

    /// <summary>Gets a referral by id, or null.</summary>
    Referral? GetReferral(Guid id);

    /// <summary>All referrals for a partner, newest first.</summary>
    IReadOnlyList<Referral> ReferralsForPartner(Guid partnerId);

    /// <summary>Adds a commission.</summary>
    Commission AddCommission(Commission commission);

    /// <summary>Gets a commission by id, or null.</summary>
    Commission? GetCommission(Guid id);

    /// <summary>Replaces a commission (e.g. after a clawback).</summary>
    void UpdateCommission(Commission commission);

    /// <summary>All commissions for a partner, newest first.</summary>
    IReadOnlyList<Commission> CommissionsForPartner(Guid partnerId);
}
