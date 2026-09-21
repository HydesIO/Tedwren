using System.Text.Json;
using Microsoft.JSInterop;
using Tedwren.Mobile.Core.Sync;

namespace Tedwren.Web.App.Platform;

/// <summary>
/// Browser <see cref="IOutboxStore"/> for the emulator: an append-only outbox held in memory and persisted to
/// <c>localStorage</c>, so queued captures survive a page reload and still drain in <see cref="OutboxItem.Sequence"/>
/// order on reconnect — mirroring the device's encrypted store. The JS runtime is optional so the store can be
/// constructed plainly in unit tests (in-memory only). (WASM is single-threaded, but the lock keeps the contract
/// honest.)
/// </summary>
public sealed class WebOutboxStore : IOutboxStore
{
    private const string StorageKey = "tw.outbox";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly List<OutboxItem> _items = new();
    private readonly object _gate = new();
    private readonly IJSInProcessRuntime? _js;
    private long _sequence;
    private bool _hydrated;

    /// <summary>Creates the store; the JS runtime (when in-process) backs the <c>localStorage</c> persistence.</summary>
    public WebOutboxStore(IJSRuntime? js = null) => _js = js as IJSInProcessRuntime;

    /// <summary>Appends an item, assigning its <see cref="OutboxItem.Sequence"/>, and returns it.</summary>
    public Task<OutboxItem> EnqueueAsync(OutboxItem item, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            EnsureHydrated();
            item.Sequence = ++_sequence;
            _items.Add(item);
            Persist();
        }

        return Task.FromResult(item);
    }

    /// <summary>The items ready to sync now — Pending/Failed whose backoff has elapsed — in append order.</summary>
    public Task<IReadOnlyList<OutboxItem>> GetDrainableAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            EnsureHydrated();
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
            EnsureHydrated();
            var index = _items.FindIndex(i => i.Id == item.Id);
            if (index >= 0)
            {
                _items[index] = item;
                Persist();
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>Counts for the UI badge: items still to sync (Pending + Failed) and items needing attention.</summary>
    public Task<OutboxCounts> GetCountsAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            EnsureHydrated();
            var pending = _items.Count(i => i.Status is OutboxItemStatus.Pending or OutboxItemStatus.Failed);
            var attention = _items.Count(i => i.Status == OutboxItemStatus.NeedsAttention);
            return Task.FromResult(new OutboxCounts(pending, attention));
        }
    }

    // Loads any persisted queue on first use (call under the lock). No-op without a JS runtime (tests).
    private void EnsureHydrated()
    {
        if (_hydrated)
        {
            return;
        }

        _hydrated = true;
        if (_js is null)
        {
            return;
        }

        try
        {
            var json = _js.Invoke<string?>("localStorage.getItem", StorageKey);
            if (!string.IsNullOrEmpty(json) && JsonSerializer.Deserialize<List<OutboxItem>>(json, Json) is { Count: > 0 } saved)
            {
                _items.AddRange(saved);
                _sequence = saved.Max(i => i.Sequence);
            }
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            // A corrupt/absent store degrades to an empty outbox rather than throwing.
        }
    }

    // Writes the whole queue back to localStorage (call under the lock). No-op without a JS runtime (tests).
    private void Persist()
    {
        if (_js is null)
        {
            return;
        }

        try
        {
            _js.InvokeVoid("localStorage.setItem", StorageKey, JsonSerializer.Serialize(_items, Json));
        }
        catch (Exception ex) when (ex is JSException or JsonException or InvalidOperationException)
        {
            // Best-effort (e.g. storage full / blocked) — the in-memory queue still drains this session.
        }
    }
}
