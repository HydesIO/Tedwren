using Tedwren.Domain.Entities;

namespace Tedwren.Application.Persistence;

/// <summary>
/// Persistence contract for BACS payouts (Tedwren settlement; not tenant-scoped). Keyed by GoCardless payout
/// id so a re-sync updates rather than duplicates.
/// </summary>
public interface IPayoutRepository
{
    /// <summary>Returns every payout, most recent first.</summary>
    Task<IReadOnlyList<Payout>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the payout with this GoCardless payout id, or null.</summary>
    Task<Payout?> GetByGoCardlessPayoutIdAsync(string goCardlessPayoutId, CancellationToken cancellationToken = default);

    /// <summary>Inserts a new payout.</summary>
    Task AddAsync(Payout payout, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing payout.</summary>
    Task UpdateAsync(Payout payout, CancellationToken cancellationToken = default);

    /// <summary>Permanently removes a payout by id (used only by the demo-data teardown).</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
