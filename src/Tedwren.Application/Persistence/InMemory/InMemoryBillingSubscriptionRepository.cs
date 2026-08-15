using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IBillingSubscriptionRepository"/> (test-only double), singleton per host.</summary>
public sealed class InMemoryBillingSubscriptionRepository : IBillingSubscriptionRepository
{
    private readonly ConcurrentDictionary<Guid, BillingSubscription> _subscriptions = new();

    /// <summary>Returns every subscription.</summary>
    public Task<IReadOnlyList<BillingSubscription>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<BillingSubscription> rows = _subscriptions.Values.OrderByDescending(s => s.CreatedUtc).ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns a company's subscription, or null.</summary>
    public Task<BillingSubscription?> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_subscriptions.Values.FirstOrDefault(s => s.CompanyId == companyId));

    /// <summary>Adds a subscription.</summary>
    public Task AddAsync(BillingSubscription subscription, CancellationToken cancellationToken = default)
    {
        _subscriptions[subscription.Id] = subscription;
        return Task.CompletedTask;
    }

    /// <summary>Updates a subscription.</summary>
    public Task UpdateAsync(BillingSubscription subscription, CancellationToken cancellationToken = default)
    {
        _subscriptions[subscription.Id] = subscription;
        return Task.CompletedTask;
    }
}
