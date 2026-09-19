using Tedwren.Mobile.Core.Platform;

namespace Tedwren.Mobile.Core.Sync;

/// <summary>The engine's current state, for the UI badge (M5).</summary>
public sealed record SyncState(bool IsSyncing, int PendingCount, int NeedsAttentionCount, DateTimeOffset? LastSyncUtc)
{
    /// <summary>The initial (nothing-known-yet) state.</summary>
    public static readonly SyncState Empty = new(false, 0, 0, null);
}

/// <summary>Progress during a drain — item <paramref name="Completed"/> of <paramref name="Total"/>.</summary>
public readonly record struct SyncProgress(int Completed, int Total, string Kind);

/// <summary>The outcome of a drain pass.</summary>
public readonly record struct SyncResult(int Synced, int Pending, int NeedsAttention);

/// <summary>
/// Drains the append-only outbox to the server (M5): ordered, idempotent, with retry/backoff and a "needs attention"
/// terminal state. Online-only — an offline drain is a no-op that leaves captures queued (never lost, R2/R3-aligned
/// for evidence/forms; attendance/gate are never queued at all). Concurrent triggers coalesce (a drain already
/// running is not re-entered). It reacts to connectivity coming back, and exposes <see cref="State"/> +
/// <see cref="StateChanged"/> + <see cref="Progressed"/> for the UI. Pure logic — unit-tested with in-memory doubles
/// and a fake <see cref="TimeProvider"/>.
/// </summary>
public sealed class SyncEngine
{
    /// <summary>Retry attempts before an item is parked as "needs attention".</summary>
    private const int MaxAttempts = 5;

    private readonly IOutboxStore _store;
    private readonly IConnectivityService _connectivity;
    private readonly IReadOnlyDictionary<string, IOutboxItemHandler> _handlers;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>Creates the engine over the outbox store, connectivity, the item handlers and a time source.</summary>
    public SyncEngine(IOutboxStore store, IConnectivityService connectivity, IEnumerable<IOutboxItemHandler> handlers, TimeProvider? timeProvider = null)
    {
        _store = store;
        _connectivity = connectivity;
        _handlers = handlers.ToDictionary(h => h.Kind, StringComparer.OrdinalIgnoreCase);
        _time = timeProvider ?? TimeProvider.System;
        _connectivity.ConnectivityChanged += OnConnectivityChanged;
    }

    /// <summary>The latest known sync state (for the dashboard badge).</summary>
    public SyncState State { get; private set; } = SyncState.Empty;

    /// <summary>Raised whenever <see cref="State"/> changes.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Raised per item during a drain, for a progress indicator.</summary>
    public event EventHandler<SyncProgress>? Progressed;

    /// <summary>Requests a background drain (fire-and-forget). Used post-enqueue, on app-resume and on reconnect.</summary>
    public void RequestSync() => _ = DrainSafelyAsync();

    private void OnConnectivityChanged(object? sender, bool connected)
    {
        if (connected)
        {
            RequestSync();
        }
    }

    private async Task DrainSafelyAsync()
    {
        try
        {
            await DrainAsync();
        }
        catch
        {
            // Best-effort background sync; failures are reflected in item state, not thrown to a caller.
        }
    }

    /// <summary>Drains the outbox once. Offline is a safe no-op; only the state is refreshed.</summary>
    public async Task<SyncResult> DrainAsync(CancellationToken cancellationToken = default)
    {
        if (!_connectivity.IsConnected)
        {
            await RefreshStateAsync(isSyncing: false, touchLastSync: false, cancellationToken);
            return new SyncResult(0, State.PendingCount, State.NeedsAttentionCount);
        }

        // Coalesce concurrent triggers: if a drain is already running, don't re-enter.
        if (!await _gate.WaitAsync(0, cancellationToken))
        {
            return new SyncResult(0, State.PendingCount, State.NeedsAttentionCount);
        }

        var synced = 0;
        try
        {
            await RefreshStateAsync(isSyncing: true, touchLastSync: false, cancellationToken);

            var now = _time.GetUtcNow();
            var items = await _store.GetDrainableAsync(now, cancellationToken);
            var completed = 0;

            foreach (var item in items)
            {
                if (!_connectivity.IsConnected)
                {
                    break; // connectivity dropped mid-drain — leave the rest queued.
                }

                Progressed?.Invoke(this, new SyncProgress(completed, items.Count, item.Kind));

                if (!_handlers.TryGetValue(item.Kind, out var handler))
                {
                    item.Status = OutboxItemStatus.NeedsAttention;
                    item.LastError = $"No handler for '{item.Kind}'.";
                    await _store.UpdateAsync(item, cancellationToken);
                    continue;
                }

                try
                {
                    await handler.ExecuteAsync(item, _store, cancellationToken);
                    item.Status = OutboxItemStatus.Done;
                    item.CompletedUtc = now;
                    item.LastError = null;
                    await _store.UpdateAsync(item, cancellationToken);
                    synced++;
                    completed++;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    item.AttemptCount++;
                    item.LastError = ex.Message;
                    if (IsPermanent(ex) || item.AttemptCount >= MaxAttempts)
                    {
                        item.Status = OutboxItemStatus.NeedsAttention;
                    }
                    else
                    {
                        item.Status = OutboxItemStatus.Failed;
                        item.NextAttemptUtc = now + Backoff(item.AttemptCount);
                    }

                    await _store.UpdateAsync(item, cancellationToken);
                }
            }

            await RefreshStateAsync(isSyncing: false, touchLastSync: true, cancellationToken);
            return new SyncResult(synced, State.PendingCount, State.NeedsAttentionCount);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RefreshStateAsync(bool isSyncing, bool touchLastSync, CancellationToken cancellationToken)
    {
        var counts = await _store.GetCountsAsync(cancellationToken);
        State = new SyncState(isSyncing, counts.Pending, counts.NeedsAttention, touchLastSync ? _time.GetUtcNow() : State.LastSyncUtc);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>A 4xx is a permanent failure (retrying won't help); everything else is transient.</summary>
    private static bool IsPermanent(Exception ex) =>
        ex is HttpRequestException { StatusCode: { } status } && (int)status >= 400 && (int)status < 500;

    /// <summary>Exponential backoff, capped at 5 minutes: 5s, 10s, 20s, 40s…</summary>
    private static TimeSpan Backoff(int attempt) =>
        TimeSpan.FromSeconds(Math.Min(5 * Math.Pow(2, attempt - 1), 300));
}
