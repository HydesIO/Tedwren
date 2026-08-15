using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for affiliates, their payouts and their agreements (commercial database).</summary>
public interface IAffiliateRepository
{
    /// <summary>Persists a new affiliate.</summary>
    Task AddAsync(Affiliate affiliate, CancellationToken cancellationToken = default);

    /// <summary>Returns an affiliate by id, or null.</summary>
    Task<Affiliate?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns every affiliate, newest-updated first.</summary>
    Task<IReadOnlyList<Affiliate>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing affiliate.</summary>
    Task UpdateAsync(Affiliate affiliate, CancellationToken cancellationToken = default);

    /// <summary>Persists a new payout.</summary>
    Task AddPayoutAsync(AffiliatePayout payout, CancellationToken cancellationToken = default);

    /// <summary>Returns a payout by id, or null.</summary>
    Task<AffiliatePayout?> GetPayoutAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns an affiliate's payouts, newest first.</summary>
    Task<IReadOnlyList<AffiliatePayout>> ListPayoutsAsync(Guid affiliateId, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing payout (marking it paid).</summary>
    Task UpdatePayoutAsync(AffiliatePayout payout, CancellationToken cancellationToken = default);

    /// <summary>Persists a new agreement.</summary>
    Task AddAgreementAsync(AffiliateAgreement agreement, CancellationToken cancellationToken = default);

    /// <summary>Returns an agreement by its public token, or null.</summary>
    Task<AffiliateAgreement?> GetAgreementByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Returns the agreement for an affiliate, or null.</summary>
    Task<AffiliateAgreement?> GetAgreementByAffiliateAsync(Guid affiliateId, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to an existing agreement (signature, status, PDF).</summary>
    Task UpdateAgreementAsync(AffiliateAgreement agreement, CancellationToken cancellationToken = default);
}
