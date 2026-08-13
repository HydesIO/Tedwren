using System.Collections.Concurrent;

namespace Tedwren.Web.Partners;

/// <summary>
/// In-memory <see cref="IPartnerStore"/> for launch (Web Plan §7) — thread-safe, registered as a
/// singleton. It is the store double the same way the in-memory content/lead pieces are: a real
/// database-backed store slots in behind the interface later with no service changes.
/// </summary>
public sealed class InMemoryPartnerStore : IPartnerStore
{
    private readonly ConcurrentDictionary<Guid, PartnerApplication> _applications = new();
    private readonly ConcurrentDictionary<Guid, Partner> _partners = new();
    private readonly ConcurrentDictionary<string, Guid> _partnersByCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Guid, Referral> _referrals = new();
    private readonly ConcurrentDictionary<Guid, Commission> _commissions = new();

    /// <inheritdoc />
    public PartnerApplication AddApplication(PartnerApplication application)
    {
        _applications[application.Id] = application;
        return application;
    }

    /// <inheritdoc />
    public PartnerApplication? GetApplication(Guid id) =>
        _applications.TryGetValue(id, out var app) ? app : null;

    /// <inheritdoc />
    public void UpdateApplication(PartnerApplication application) => _applications[application.Id] = application;

    /// <inheritdoc />
    public IReadOnlyList<PartnerApplication> Applications() =>
        _applications.Values.OrderByDescending(a => a.SubmittedUtc).ToList();

    /// <inheritdoc />
    public Partner AddPartner(Partner partner)
    {
        _partners[partner.Id] = partner;
        _partnersByCode[partner.ReferralCode] = partner.Id;
        return partner;
    }

    /// <inheritdoc />
    public Partner? GetPartnerByCode(string referralCode) =>
        _partnersByCode.TryGetValue(referralCode, out var id) && _partners.TryGetValue(id, out var partner)
            ? partner
            : null;

    /// <inheritdoc />
    public bool CodeExists(string referralCode) => _partnersByCode.ContainsKey(referralCode);

    /// <inheritdoc />
    public Referral AddReferral(Referral referral)
    {
        _referrals[referral.Id] = referral;
        return referral;
    }

    /// <inheritdoc />
    public Referral? GetReferral(Guid id) => _referrals.TryGetValue(id, out var r) ? r : null;

    /// <inheritdoc />
    public IReadOnlyList<Referral> ReferralsForPartner(Guid partnerId) =>
        _referrals.Values.Where(r => r.PartnerId == partnerId).OrderByDescending(r => r.CreatedUtc).ToList();

    /// <inheritdoc />
    public Commission AddCommission(Commission commission)
    {
        _commissions[commission.Id] = commission;
        return commission;
    }

    /// <inheritdoc />
    public Commission? GetCommission(Guid id) => _commissions.TryGetValue(id, out var c) ? c : null;

    /// <inheritdoc />
    public void UpdateCommission(Commission commission) => _commissions[commission.Id] = commission;

    /// <inheritdoc />
    public IReadOnlyList<Commission> CommissionsForPartner(Guid partnerId) =>
        _commissions.Values.Where(c => c.PartnerId == partnerId).OrderByDescending(c => c.RaisedUtc).ToList();
}
