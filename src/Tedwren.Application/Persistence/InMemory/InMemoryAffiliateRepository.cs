using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>Test-only in-memory <see cref="IAffiliateRepository"/> (affiliates, payouts and agreements).</summary>
public sealed class InMemoryAffiliateRepository : IAffiliateRepository
{
    private readonly ConcurrentDictionary<Guid, Affiliate> _affiliates = new();
    private readonly ConcurrentDictionary<Guid, AffiliatePayout> _payouts = new();
    private readonly ConcurrentDictionary<Guid, AffiliateAgreement> _agreements = new();

    /// <summary>Persists a new affiliate.</summary>
    public Task AddAsync(Affiliate affiliate, CancellationToken cancellationToken = default)
    {
        _affiliates[affiliate.Id] = affiliate;
        return Task.CompletedTask;
    }

    /// <summary>Returns an affiliate by id, or null.</summary>
    public Task<Affiliate?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_affiliates.GetValueOrDefault(id));

    /// <summary>Returns every affiliate, newest-updated first.</summary>
    public Task<IReadOnlyList<Affiliate>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Affiliate>>(_affiliates.Values.OrderByDescending(a => a.UpdatedUtc).ToList());

    /// <summary>Persists changes to an existing affiliate.</summary>
    public Task UpdateAsync(Affiliate affiliate, CancellationToken cancellationToken = default)
    {
        _affiliates[affiliate.Id] = affiliate;
        return Task.CompletedTask;
    }

    /// <summary>Persists a new payout.</summary>
    public Task AddPayoutAsync(AffiliatePayout payout, CancellationToken cancellationToken = default)
    {
        _payouts[payout.Id] = payout;
        return Task.CompletedTask;
    }

    /// <summary>Returns a payout by id, or null.</summary>
    public Task<AffiliatePayout?> GetPayoutAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_payouts.GetValueOrDefault(id));

    /// <summary>Returns an affiliate's payouts, newest first.</summary>
    public Task<IReadOnlyList<AffiliatePayout>> ListPayoutsAsync(Guid affiliateId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AffiliatePayout>>(
            _payouts.Values.Where(p => p.AffiliateId == affiliateId).OrderByDescending(p => p.CreatedUtc).ToList());

    /// <summary>Persists changes to an existing payout.</summary>
    public Task UpdatePayoutAsync(AffiliatePayout payout, CancellationToken cancellationToken = default)
    {
        _payouts[payout.Id] = payout;
        return Task.CompletedTask;
    }

    /// <summary>Persists a new agreement.</summary>
    public Task AddAgreementAsync(AffiliateAgreement agreement, CancellationToken cancellationToken = default)
    {
        _agreements[agreement.Id] = agreement;
        return Task.CompletedTask;
    }

    /// <summary>Returns an agreement by its public token, or null.</summary>
    public Task<AffiliateAgreement?> GetAgreementByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        Task.FromResult(_agreements.Values.FirstOrDefault(a => a.Token == token));

    /// <summary>Returns the agreement for an affiliate, or null.</summary>
    public Task<AffiliateAgreement?> GetAgreementByAffiliateAsync(Guid affiliateId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_agreements.Values.Where(a => a.AffiliateId == affiliateId).OrderByDescending(a => a.CreatedUtc).FirstOrDefault());

    /// <summary>Persists changes to an existing agreement.</summary>
    public Task UpdateAgreementAsync(AffiliateAgreement agreement, CancellationToken cancellationToken = default)
    {
        _agreements[agreement.Id] = agreement;
        return Task.CompletedTask;
    }
}
