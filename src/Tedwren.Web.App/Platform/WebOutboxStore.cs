using Tedwren.Mobile.Core.Sync;

namespace Tedwren.Web.App.Platform;

/// <summary>
/// Browser <see cref="IOutboxStore"/> for the emulator: an in-memory append-only outbox for the app session. It
/// assigns a monotonic <see cref="OutboxItem.Sequence"/> and drains in that order, exactly like the device's
/// encrypted store, so the sync engine behaves identically. WA6 adds <c>localStorage</c> persistence so queued
/// captures survive a page reload. (WASM is single-threaded, but the lock keeps the store contract honest.)
/// </summary>
public sealed class WebOutboxStore : IOutboxStore
{
    private readonly List<OutboxItem> _items = new();
    private readonly object _gate = new();
    private long _sequence;

    /// <summary>Appends an item, assigning its <see cref="OutboxItem.Sequence"/>, and returns it.</summary>
    public Task<OutboxItem> EnqueueAsync(OutboxItem item, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            item.Sequence = ++_sequence;
            _items.Add(item);
        }

        return Task.FromResult(item);
    }

    /// <summary>The items ready to sync now — Pending/Failed whose backoff has elapsed — in append order.</summary>
    public Task<IReadOnlyList<OutboxItem>> GetDrainableAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            IReadOnlyList<OutboxItem> due = _items
                .Where(i => (i.Status is OutboxItemStatus.Pending or OutboxItemStatus.Failed)
                            && (i.NextAttemptUtc is null || i.NextAttemptUtc <= now))
                .OrderBy(i => i.Sequence)
                .ToList();
            return Task.FromResult(due);
        }
    }

    /// <summary>Persists an item's checkpoint / status / attempt changes.</summary>
    public Task UpdateAsync(OutboxItem item, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var index = _items.FindIndex(i => i.Id == item.Id);
            if (index >= 0)
            {
                _items[index] = item;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>Counts for the UI badge: items still to sync (Pending + Failed) and items needing attention.</summary>
    public Task<OutboxCounts> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var pending = _items.Count(i => i.Status is OutboxItemStatus.Pending or OutboxItemStatus.Failed);
            var attention = _items.Count(i => i.Status == OutboxItemStatus.NeedsAttention);
            return Task.FromResult(new OutboxCounts(pending, attention));
        }
    }
}
