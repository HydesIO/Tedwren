using Tedwren.Mobile.Core.Platform;
using Tedwren.Mobile.Core.Sync;

namespace Tedwren.Mobile.Core.Tests.Sync;

/// <summary>In-memory <see cref="IOutboxStore"/> for the sync tests — the engine mutates item references in place.</summary>
internal sealed class InMemoryOutboxStore : IOutboxStore
{
    private readonly List<OutboxItem> _items = new();
    private long _sequence;

    public IReadOnlyList<OutboxItem> All => _items;

    public Task<OutboxItem> EnqueueAsync(OutboxItem item, CancellationToken cancellationToken = default)
    {
        item.Sequence = ++_sequence;
        _items.Add(item);
        return Task.FromResult(item);
    }

    public Task<IReadOnlyList<OutboxItem>> GetDrainableAsync(DateTimeOffset now, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OutboxItem>>(_items
            .Where(i => i.Status is OutboxItemStatus.Pending or OutboxItemStatus.Failed
                        && (i.NextAttemptUtc is null || i.NextAttemptUtc <= now))
            .OrderBy(i => i.Sequence)
            .ToList());

    public Task UpdateAsync(OutboxItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<OutboxCounts> GetCountsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new OutboxCounts(
            _items.Count(i => i.Status is OutboxItemStatus.Pending or OutboxItemStatus.Failed),
            _items.Count(i => i.Status == OutboxItemStatus.NeedsAttention)));
}

/// <summary>A connectivity source the tests can toggle, raising <see cref="ConnectivityChanged"/>.</summary>
internal sealed class FakeConnectivity : IConnectivityService
{
    public FakeConnectivity(bool connected) => IsConnected = connected;

    public bool IsConnected { get; private set; }

    public event EventHandler<bool>? ConnectivityChanged;

    public void Set(bool connected)
    {
        IsConnected = connected;
        ConnectivityChanged?.Invoke(this, connected);
    }
}

/// <summary>A controllable outbox handler: records what it executed and runs the supplied behaviour.</summary>
internal sealed class FakeHandler : IOutboxItemHandler
{
    private readonly Func<OutboxItem, Task> _behavior;

    public FakeHandler(string kind, Func<OutboxItem, Task> behavior)
    {
        Kind = kind;
        _behavior = behavior;
    }

    public string Kind { get; }

    public List<Guid> Executed { get; } = new();

    public Task ExecuteAsync(OutboxItem item, IOutboxStore store, CancellationToken cancellationToken = default)
    {
        Executed.Add(item.Id);
        return _behavior(item);
    }
}

/// <summary>A time source the tests can advance for deterministic backoff assertions.</summary>
internal sealed class MutableTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public MutableTimeProvider(DateTimeOffset start) => _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
