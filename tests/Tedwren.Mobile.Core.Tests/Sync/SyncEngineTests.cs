using System.Net;
using Tedwren.Mobile.Core.Sync;

namespace Tedwren.Mobile.Core.Tests.Sync;

/// <summary>
/// Verifies the M5 sync engine: ordered draining, transient retry/backoff → needs-attention, permanent-4xx parking,
/// offline no-op, reconnect triggering, progress and re-entrancy coalescing. All native-free (in-memory doubles).
/// </summary>
public class SyncEngineTests
{
    private static OutboxItem Item(string kind) => new() { Id = Guid.NewGuid(), Kind = kind, PayloadJson = "{}" };

    [Fact]
    public async Task Drains_in_sequence_order()
    {
        var store = new InMemoryOutboxStore();
        var handler = new FakeHandler("test", _ => Task.CompletedTask);
        var engine = new SyncEngine(store, new FakeConnectivity(true), new[] { handler });

        var a = await store.EnqueueAsync(Item("test"));
        var b = await store.EnqueueAsync(Item("test"));
        var c = await store.EnqueueAsync(Item("test"));

        var result = await engine.DrainAsync();

        Assert.Equal(new[] { a.Id, b.Id, c.Id }, handler.Executed);
        Assert.All(store.All, i => Assert.Equal(OutboxItemStatus.Done, i.Status));
        Assert.Equal(3, result.Synced);
        Assert.Equal(0, engine.State.PendingCount);
    }

    [Fact]
    public async Task Transient_failure_backs_off_then_retries_then_parks()
    {
        var store = new InMemoryOutboxStore();
        var failures = 0;
        var handler = new FakeHandler("test", _ =>
        {
            failures++;
            throw new HttpRequestException("boom", null, HttpStatusCode.ServiceUnavailable);
        });
        var time = new MutableTimeProvider(DateTimeOffset.UtcNow);
        var engine = new SyncEngine(store, new FakeConnectivity(true), new[] { handler }, time);
        var item = await store.EnqueueAsync(Item("test"));

        await engine.DrainAsync();
        Assert.Equal(OutboxItemStatus.Failed, item.Status);
        Assert.Equal(1, item.AttemptCount);
        Assert.NotNull(item.NextAttemptUtc);

        // Not retried before the backoff elapses (same clock).
        await engine.DrainAsync();
        Assert.Equal(1, item.AttemptCount);

        // Retried after the clock advances; eventually parked as needs-attention at the attempt limit.
        for (var i = 0; i < 10 && item.Status != OutboxItemStatus.NeedsAttention; i++)
        {
            time.Advance(TimeSpan.FromMinutes(10));
            await engine.DrainAsync();
        }

        Assert.Equal(OutboxItemStatus.NeedsAttention, item.Status);
        Assert.Equal(5, item.AttemptCount);
        Assert.True(failures >= 5);
        Assert.Equal(1, engine.State.NeedsAttentionCount);
    }

    [Fact]
    public async Task Permanent_4xx_parks_immediately()
    {
        var store = new InMemoryOutboxStore();
        var handler = new FakeHandler("test", _ => throw new HttpRequestException("bad", null, HttpStatusCode.BadRequest));
        var engine = new SyncEngine(store, new FakeConnectivity(true), new[] { handler });
        var item = await store.EnqueueAsync(Item("test"));

        await engine.DrainAsync();

        Assert.Equal(OutboxItemStatus.NeedsAttention, item.Status);
        Assert.Equal(1, item.AttemptCount);
    }

    [Fact]
    public async Task Offline_is_a_no_op()
    {
        var store = new InMemoryOutboxStore();
        var handler = new FakeHandler("test", _ => Task.CompletedTask);
        var engine = new SyncEngine(store, new FakeConnectivity(false), new[] { handler });
        var item = await store.EnqueueAsync(Item("test"));

        var result = await engine.DrainAsync();

        Assert.Empty(handler.Executed);
        Assert.Equal(OutboxItemStatus.Pending, item.Status);
        Assert.Equal(1, result.Pending);
    }

    [Fact]
    public async Task Unknown_kind_parks_as_needs_attention()
    {
        var store = new InMemoryOutboxStore();
        var engine = new SyncEngine(store, new FakeConnectivity(true), Array.Empty<IOutboxItemHandler>());
        var item = await store.EnqueueAsync(Item("mystery"));

        await engine.DrainAsync();

        Assert.Equal(OutboxItemStatus.NeedsAttention, item.Status);
    }

    [Fact]
    public async Task Reconnecting_triggers_a_drain()
    {
        var store = new InMemoryOutboxStore();
        var handler = new FakeHandler("test", _ => Task.CompletedTask);
        var connectivity = new FakeConnectivity(false);
        _ = new SyncEngine(store, connectivity, new[] { handler });
        await store.EnqueueAsync(Item("test"));

        connectivity.Set(true); // fires ConnectivityChanged → RequestSync (fire-and-forget)

        await WaitForAsync(() => handler.Executed.Count == 1, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Reports_progress_per_item()
    {
        var store = new InMemoryOutboxStore();
        var handler = new FakeHandler("test", _ => Task.CompletedTask);
        var engine = new SyncEngine(store, new FakeConnectivity(true), new[] { handler });
        var progress = new List<SyncProgress>();
        engine.Progressed += (_, p) => progress.Add(p);
        await store.EnqueueAsync(Item("test"));
        await store.EnqueueAsync(Item("test"));

        await engine.DrainAsync();

        Assert.Equal(2, progress.Count);
        Assert.All(progress, p => Assert.Equal(2, p.Total));
    }

    [Fact]
    public async Task Concurrent_drains_do_not_double_process()
    {
        var store = new InMemoryOutboxStore();
        var gate = new TaskCompletionSource();
        var entered = new TaskCompletionSource();
        var handler = new FakeHandler("test", async _ =>
        {
            entered.TrySetResult();
            await gate.Task;
        });
        var engine = new SyncEngine(store, new FakeConnectivity(true), new[] { handler });
        await store.EnqueueAsync(Item("test"));
        await store.EnqueueAsync(Item("test"));

        var first = engine.DrainAsync();        // starts, blocks inside the first item
        await entered.Task;                      // the first drain is now in-flight
        var second = await engine.DrainAsync();  // must coalesce (no-op), not re-enter

        gate.SetResult();
        await first;

        Assert.Equal(0, second.Synced);
        Assert.Equal(2, handler.Executed.Count); // each item processed exactly once
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(15);
        }

        Assert.True(condition(), "Condition was not met within the timeout.");
    }
}
