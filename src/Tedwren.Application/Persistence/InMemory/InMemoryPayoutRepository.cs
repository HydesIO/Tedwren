using System.Collections.Concurrent;
using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence.InMemory;

/// <summary>In-memory <see cref="IPayoutRepository"/> (test-only double), singleton so payouts persist per host.</summary>
public sealed class InMemoryPayoutRepository : IPayoutRepository
{
    private readonly ConcurrentDictionary<Guid, Payout> _payouts = new();

    /// <summary>Returns every payout, most recent first.</summary>
    public Task<IReadOnlyList<Payout>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Payout> rows = _payouts.Values.OrderByDescending(p => p.CreatedUtc).ToList();
        return Task.FromResult(rows);
    }

    /// <summary>Returns the payout with this GoCardless payout id, or null.</summary>
    public Task<Payout?> GetByGoCardlessPayoutIdAsync(string goCardlessPayoutId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_payouts.Values.FirstOrDefault(p => p.GoCardlessPayoutId == goCardlessPayoutId));

    /// <summary>Adds a payout.</summary>
    public Task AddAsync(Payout payout, CancellationToken cancellationToken = default)
    {
        _payouts[payout.Id] = payout;
        return Task.CompletedTask;
    }

    /// <summary>Updates a payout.</summary>
    public Task UpdateAsync(Payout payout, CancellationToken cancellationToken = default)
    {
        _payouts[payout.Id] = payout;
        return Task.CompletedTask;
    }
}
