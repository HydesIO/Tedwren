using Tedwren.Abstractions.Contracts.Affiliates;

namespace Tedwren.Abstractions.Services;

/// <summary>
/// The affiliate/partner programme (Web Plan §7): set up affiliates on a profit-share commission plan, see the
/// accounts they've brought in and the commission earned, record payouts, and issue a publicly-signable
/// agreement that produces a countersigned PDF. Admin operations are platform-admin; the agreement view/sign/
/// pdf operations are anonymous, token-gated. Extends beyond the PRD — anchored to the website plan §7.
/// </summary>
public interface IAffiliateService
{
    /// <summary>Returns every affiliate, newest-updated first.</summary>
    Task<IReadOnlyList<AffiliateDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns an affiliate with its associated accounts, payouts and agreement, or null.</summary>
    Task<AffiliateDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Creates an affiliate, drafts their agreement and emails them their setup + agreement link.</summary>
    Task<AffiliateDto> CreateAsync(CreateAffiliateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Updates an affiliate's details and commission plan. Null when it doesn't exist.</summary>
    Task<AffiliateDto?> UpdateAsync(Guid id, UpdateAffiliateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Raises a payout for an affiliate. Null when the affiliate doesn't exist.</summary>
    Task<AffiliatePayoutDto?> CreatePayoutAsync(Guid affiliateId, CreatePayoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>Marks a payout paid. Null when it doesn't exist.</summary>
    Task<AffiliatePayoutDto?> MarkPayoutPaidAsync(Guid payoutId, CancellationToken cancellationToken = default);

    /// <summary>Returns the public, token-gated view of an agreement, or null when the token is unknown.</summary>
    Task<AffiliateAgreementViewDto?> GetAgreementAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Signs an agreement (captures the signature, generates the PDF, emails confirmation). Null when the token is unknown or already signed.</summary>
    Task<AffiliateAgreementViewDto?> SignAgreementAsync(string token, SignAffiliateAgreementRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the signed agreement PDF for a token, or null when unavailable.</summary>
    Task<byte[]?> GetAgreementPdfAsync(string token, CancellationToken cancellationToken = default);
}
