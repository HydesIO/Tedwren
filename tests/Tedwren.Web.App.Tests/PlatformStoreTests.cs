using Tedwren.Mobile.Core.Forms;
using Tedwren.Mobile.Core.Sync;
using Tedwren.Web.App.Platform;

namespace Tedwren.Web.App.Tests;

/// <summary>
/// Tests for the emulator's browser platform stores (the in-memory read cache, outbox and form-draft store), which
/// stand in for the device's SQLCipher store. They must honour the same contracts the sync engine depends on.
/// </summary>
public class PlatformStoreTests
{
    [Fact]
    public async Task WebReadCache_round_trips_a_value()
    {
        var cache = new WebReadCache();
        await cache.SetAsync("k", new Sample("hi", 3));
        Assert.Equal(new Sample("hi", 3), await cache.GetAsync<Sample>("k"));
    }

    [Fact]
    public async Task WebReadCache_missing_key_returns_default()
    {
        Assert.Null(await new WebReadCache().GetAsync<Sample>("absent"));
    }

    [Fact]
    public async Task WebOutboxStore_assigns_sequence_and_drains_pending()
    {
        var store = new WebOutboxStore();
        var a = await store.EnqueueAsync(new OutboxItem { Kind = "evidence" });
        var b = await store.EnqueueAsync(new OutboxItem { Kind = "evidence" });
        Assert.True(b.Sequence > a.Sequence);

        var due = await store.GetDrainableAsync(DateTimeOffset.UtcNow);
        Assert.Equal(2, due.Count);

        a.Status = OutboxItemStatus.Done;
        await store.UpdateAsync(a);
        Assert.Single(await store.GetDrainableAsync(DateTimeOffset.UtcNow));

        var counts = await store.GetCountsAsync();
        Assert.Equal(1, counts.Pending);
    }

    [Fact]
    public async Task WebOutboxStore_respects_backoff()
    {
        var store = new WebOutboxStore();
        await store.EnqueueAsync(new OutboxItem
        {
            Kind = "evidence",
            Status = OutboxItemStatus.Failed,
            NextAttemptUtc = DateTimeOffset.UtcNow.AddMinutes(5),
        });

        Assert.Empty(await store.GetDrainableAsync(DateTimeOffset.UtcNow));
        Assert.Single(await store.GetDrainableAsync(DateTimeOffset.UtcNow.AddMinutes(6)));
    }

    [Fact]
    public async Task WebFormDraftStore_saves_gets_and_deletes()
    {
        var store = new WebFormDraftStore();
        var key = Guid.NewGuid();
        await store.SaveAsync(new FormDraft { Key = key, PayloadJson = "{}" });
        Assert.NotNull(await store.GetAsync(key));

        await store.DeleteAsync(key);
        Assert.Null(await store.GetAsync(key));
    }

    private sealed record Sample(string Text, int Number);
}
