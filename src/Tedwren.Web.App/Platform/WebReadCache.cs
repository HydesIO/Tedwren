using System.Text.Json;
using Tedwren.Mobile.Core.Caching;

namespace Tedwren.Web.App.Platform;

/// <summary>
/// Browser <see cref="IReadCache"/> for the emulator: an in-memory JSON cache that lives for the app session. It
/// mirrors the device read cache's fail-soft contract — a miss or a deserialize error returns default rather than
/// throwing — so the cache-then-network readers (<c>OperativeDataService</c>/<c>ManagerDataService</c>) behave
/// identically to the app. WA6 adds <c>localStorage</c> persistence so the cache survives a page reload.
/// </summary>
public sealed class WebReadCache : IReadCache
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, string> _entries = new();
    private readonly object _gate = new();

    /// <summary>Returns the cached value for a key, or default when absent/unreadable.</summary>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        string? json;
        lock (_gate)
        {
            _entries.TryGetValue(key, out json);
        }

        if (string.IsNullOrEmpty(json))
        {
            return Task.FromResult<T?>(default);
        }

        try
        {
            return Task.FromResult(JsonSerializer.Deserialize<T>(json, Json));
        }
        catch (JsonException)
        {
            return Task.FromResult<T?>(default);
        }
    }

    /// <summary>Stores (or replaces) the cached value for a key; a serialize failure is swallowed (best-effort cache).</summary>
    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, Json);
            lock (_gate)
            {
                _entries[key] = json;
            }
        }
        catch (JsonException)
        {
            // Best-effort: a cache write failure must never break a live read.
        }

        return Task.CompletedTask;
    }
}
