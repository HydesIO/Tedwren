using Microsoft.Extensions.Options;

namespace Tedwren.Web.Partners;

/// <summary>
/// Referral attribution and commission rules (Web Plan §7.2). A conversion is credited to an active
/// partner's code; a commission is 20% of first-year subcontractor revenue tied to that specific
/// referral, and it can be clawed back within the 90-day window — reversible against the exact referral
/// record, which is why the commission carries its <see cref="Commission.ReferralId"/> from the start.
/// </summary>
public sealed class ReferralService
{
    private readonly IPartnerStore _store;
    private readonly PartnerOptions _options;
    private readonly TimeProvider _clock;

    /// <summary>Injects the store, options and clock.</summary>
    /// <param name="store">Partner store.</param>
    /// <param name="options">Partner options.</param>
    /// <param name="clock">Time provider.</param>
    public ReferralService(IPartnerStore store, IOptions<PartnerOptions> options, TimeProvider clock)
    {
        _store = store;
        _options = options.Value;
        _clock = clock;
    }

    /// <summary>
    /// Records a conversion against a referral code (Web Plan §7). Returns null when the code is unknown
    /// or the partner is inactive, so an unattributable event is ignored rather than mis-credited.
    /// </summary>
    /// <param name="referralCode">The code the conversion is attributed to.</param>
    /// <param name="source">Where the conversion came from.</param>
    /// <param name="subjectCompany">The referred company, if known.</param>
    public Referral? Capture(string referralCode, ReferralSource source, string? subjectCompany)
    {
        var partner = _store.GetPartnerByCode(referralCode);
        if (partner is null || !partner.Active)
        {
            return null;
        }

        return _store.AddReferral(new Referral(
            Guid.NewGuid(), partner.Id, partner.ReferralCode, source, subjectCompany, _clock.GetUtcNow()));
    }

    /// <summary>
    /// Raises a commission against a referral: <see cref="PartnerOptions.CommissionRate"/> of the given
    /// first-year revenue, with a clawback deadline <see cref="PartnerOptions.ClawbackDays"/> out.
    /// </summary>
    /// <param name="referralId">The referral to earn the commission against.</param>
    /// <param name="firstYearRevenue">First-year subcontractor revenue the commission is a share of.</param>
    public Commission RaiseCommission(Guid referralId, decimal firstYearRevenue)
    {
        var referral = _store.GetReferral(referralId)
            ?? throw new KeyNotFoundException($"No referral {referralId}.");

        var now = _clock.GetUtcNow();
        var commission = new Commission(
            Guid.NewGuid(), referral.Id, referral.PartnerId,
            decimal.Round(firstYearRevenue * _options.CommissionRate, 2),
            _options.Currency, CommissionStatus.Pending,
            now, now.AddDays(_options.ClawbackDays), ReversedUtc: null);
        return _store.AddCommission(commission);
    }

    /// <summary>Marks a commission paid (after funds clear).</summary>
    /// <param name="commissionId">The commission to mark paid.</param>
    public Commission MarkPaid(Guid commissionId)
    {
        var commission = Get(commissionId);
        var paid = commission with { Status = CommissionStatus.Paid };
        _store.UpdateCommission(paid);
        return paid;
    }

    /// <summary>
    /// Claws back (reverses) a commission within the clawback window (Web Plan §7.2). Refuses if the
    /// window has passed or it is already reversed. The reversal is against the commission's specific
    /// referral, so attribution stays intact.
    /// </summary>
    /// <param name="commissionId">The commission to reverse.</param>
    public Commission Clawback(Guid commissionId)
    {
        var commission = Get(commissionId);

        if (commission.Status == CommissionStatus.Reversed)
        {
            return commission; // idempotent
        }

        if (_clock.GetUtcNow() > commission.ClawbackDeadlineUtc)
        {
            throw new InvalidOperationException(
                $"Commission {commissionId} is past its clawback deadline and cannot be reversed.");
        }

        var reversed = commission with { Status = CommissionStatus.Reversed, ReversedUtc = _clock.GetUtcNow() };
        _store.UpdateCommission(reversed);
        return reversed;
    }

    /// <summary>Fetches a commission or throws when it is missing.</summary>
    private Commission Get(Guid commissionId) =>
        _store.GetCommission(commissionId) ?? throw new KeyNotFoundException($"No commission {commissionId}.");
}
