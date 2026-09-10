using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>Persistence contract for company billing subscriptions. Scoped by company (R15); one per company.</summary>
public interface IBillingSubscriptionRepository
{
    /// <summary>Returns every subscription.</summary>
    Task<IReadOnlyList<BillingSubscription>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns a company's subscription, or null when it has none.</summary>
    Task<BillingSubscription?> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new subscription.</summary>
    Task AddAsync(BillingSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing subscription.</summary>
    Task UpdateAsync(BillingSubscription subscription, CancellationToken cancellationToken = default);

    /// <summary>Permanently removes a subscription by id (used only by the demo-data teardown).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
