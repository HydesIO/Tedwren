using System.Security.Cryptography;

namespace Tedwren.Web.Partners;

/// <summary>
/// Application, approval and dashboard rules for the partner programme (Web Plan §7.2, §7.3). Submitting
/// only ever creates a <see cref="PartnerApplicationStatus.Pending"/> record — there is no self-serve
/// activation. Approval is an explicit human step that <b>refuses</b> anyone who controls or influences
/// site access (§7.3), and only then mints a partner with a unique referral code.
/// </summary>
public sealed class PartnerService
{
    private const string CodePrefix = "TW-";
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no ambiguous 0/O/1/I
    private const int CodeLength = 6;

    private readonly IPartnerStore _store;
    private readonly TimeProvider _clock;

    /// <summary>Injects the store and clock.</summary>
    /// <param name="store">Partner store.</param>
    /// <param name="clock">Time provider.</param>
    public PartnerService(IPartnerStore store, TimeProvider clock)
    {
        _store = store;
        _clock = clock;
    }

    /// <summary>
    /// Records a partner application as pending (Web Plan §7.2). No referral code or dashboard is created
    /// here — that only happens on human approval.
    /// </summary>
    public PartnerApplication Submit(
        string name, string company, string email, string howTheyWork,
        string relationshipToSites, bool controlsSiteAccess)
    {
        var application = new PartnerApplication(
            Guid.NewGuid(), _clock.GetUtcNow(), name, company, email,
            howTheyWork, relationshipToSites, controlsSiteAccess, PartnerApplicationStatus.Pending);
        return _store.AddApplication(application);
    }

    /// <summary>
    /// Approves a pending application and mints an active partner with a unique referral code. Refuses
    /// (throws) if the applicant controls/influences site access (§7.3) or the application isn't pending.
    /// </summary>
    /// <param name="applicationId">The application to approve.</param>
    public Partner Approve(Guid applicationId)
    {
        var application = _store.GetApplication(applicationId)
            ?? throw new KeyNotFoundException($"No application {applicationId}.");

        if (application.Status != PartnerApplicationStatus.Pending)
        {
            throw new InvalidOperationException($"Application {applicationId} is already {application.Status}.");
        }

        if (application.HasAccessConflict)
        {
            // §7.3: the partner programme is never a site-access channel. This applicant cannot be approved.
            throw new InvalidOperationException(
                "Applicant controls or influences site access and cannot be approved (Web Plan §7.3).");
        }

        var partner = new Partner(
            Guid.NewGuid(), application.Id, application.Name, application.Company, application.Email,
            GenerateUniqueCode(), _clock.GetUtcNow(), Active: true);
        _store.AddPartner(partner);
        _store.UpdateApplication(application with { Status = PartnerApplicationStatus.Approved });
        return partner;
    }

    /// <summary>Rejects a pending application.</summary>
    /// <param name="applicationId">The application to reject.</param>
    public PartnerApplication Reject(Guid applicationId)
    {
        var application = _store.GetApplication(applicationId)
            ?? throw new KeyNotFoundException($"No application {applicationId}.");
        var rejected = application with { Status = PartnerApplicationStatus.Rejected };
        _store.UpdateApplication(rejected);
        return rejected;
    }

    /// <summary>
    /// Builds a partner's dashboard by their referral code (Web Plan §7.2) — referrals plus commission
    /// totals by status. Returns null for an unknown or inactive code (the dashboard is not public).
    /// </summary>
    /// <param name="referralCode">The partner's referral code.</param>
    public PartnerDashboard? GetDashboard(string referralCode)
    {
        var partner = _store.GetPartnerByCode(referralCode);
        if (partner is null || !partner.Active)
        {
            return null;
        }

        var referrals = _store.ReferralsForPartner(partner.Id);
        var commissions = _store.CommissionsForPartner(partner.Id);
        return new PartnerDashboard(
            partner,
            referrals,
            commissions,
            Total(commissions, CommissionStatus.Paid),
            Total(commissions, CommissionStatus.Pending),
            Total(commissions, CommissionStatus.Reversed));
    }

    /// <summary>Sums the amount of commissions in a given status.</summary>
    private static decimal Total(IReadOnlyList<Commission> commissions, CommissionStatus status) =>
        commissions.Where(c => c.Status == status).Sum(c => c.Amount);

    /// <summary>Generates a referral code that is not already taken.</summary>
    private string GenerateUniqueCode()
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = CodePrefix + RandomString(CodeLength);
            if (!_store.CodeExists(code))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Could not generate a unique referral code.");
    }

    /// <summary>Builds a random uppercase code from the unambiguous alphabet.</summary>
    private static string RandomString(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
        }

        return new string(chars);
    }
}

/// <summary>A partner's dashboard view (Web Plan §7.2): their referrals and commission totals by status.</summary>
/// <param name="Partner">The partner.</param>
/// <param name="Referrals">Their referrals, newest first.</param>
/// <param name="Commissions">Their commissions, newest first.</param>
/// <param name="PaidTotal">Total commission paid.</param>
/// <param name="PendingTotal">Total commission pending.</param>
/// <param name="ReversedTotal">Total commission clawed back.</param>
public sealed record PartnerDashboard(
    Partner Partner,
    IReadOnlyList<Referral> Referrals,
    IReadOnlyList<Commission> Commissions,
    decimal PaidTotal,
    decimal PendingTotal,
    decimal ReversedTotal);
