using Tedwren.Mobile.Core.Caching;

namespace Tedwren.Mobile.Core.Tests.Caching;

/// <summary>Verifies the JSON file read cache round-trips values and misses cleanly.</summary>
public class JsonFileReadCacheTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "tw-cache-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Set_then_get_round_trips()
    {
        var cache = new JsonFileReadCache(_dir);
        await cache.SetAsync("operative.dashboard", new Sample("hello", 42));

        var got = await cache.GetAsync<Sample>("operative.dashboard");

        Assert.NotNull(got);
        Assert.Equal("hello", got!.Text);
        Assert.Equal(42, got.Number);
    }

    [Fact]
    public async Task Missing_key_returns_default()
    {
        var cache = new JsonFileReadCache(_dir);
        Assert.Null(await cache.GetAsync<Sample>("absent"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    private sealed record Sample(string Text, int Number);
}
