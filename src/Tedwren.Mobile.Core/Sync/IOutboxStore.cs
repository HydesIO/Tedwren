namespace Tedwren.Mobile.Core.Sync;

/// <summary>Pending / needs-attention counts for the sync badge (M5).</summary>
public readonly record struct OutboxCounts(int Pending, int NeedsAttention);

/// <summary>
/// The append-only outbox of offline captures (M5). Implemented by the encrypted SQLite/SQLCipher store in the MAUI
/// head; an in-memory double backs the unit tests. Items are drained in <see cref="OutboxItem.Sequence"/> order and
/// never reordered.
/// </summary>
public interface IOutboxStore
{
    /// <summary>Appends an item, assigning its <see cref="OutboxItem.Sequence"/>, and returns it.</summary>
    Task<OutboxItem> EnqueueAsync(OutboxItem item, CancellationToken cancellationToken = default);

    /// <summary>The items ready to sync now — Pending/Failed whose backoff has elapsed — in append order.</summary>
    Task<IReadOnlyList<OutboxItem>> GetDrainableAsync(DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>Persists an item's checkpoint / status / attempt changes.</summary>
    Task UpdateAsync(OutboxItem item, CancellationToken cancellationToken = default);

    /// <summary>Counts for the UI badge: items still to sync (Pending + Failed) and items needing attention.</summary>
    Task<OutboxCounts> GetCountsAsync(CancellationToken cancellationToken = default);
}
