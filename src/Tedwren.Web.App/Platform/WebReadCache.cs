using System.Text.Json;
using Microsoft.JSInterop;
using Tedwren.Mobile.Core.Caching;

namespace Tedwren.Web.App.Platform;

/// <summary>
/// Browser <see cref="IReadCache"/> for the emulator: an in-memory JSON cache with write-through to
/// <c>localStorage</c> (per key), so a screen's last-known data survives a page reload — mirroring the device's
/// persistent read cache. It fails soft (a miss or a deserialize error returns default rather than throwing), so
/// the cache-then-network readers behave identically to the app. The JS runtime is optional so the store can be
/// constructed plainly in unit tests (in-memory only).
/// </summary>
public sealed class WebReadCache : IReadCache
{
    private const string Prefix = "tw.cache.";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, string> _entries = new();
    private readonly object _gate = new();
    private readonly IJSInProcessRuntime? _js;

    /// <summary>Creates the cache; the JS runtime (when in-process) backs the <c>localStorage</c> persistence.</summary>
    public WebReadCache(IJSRuntime? js = null) => _js = js as IJSInProcessRuntime;

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
            json = LocalGet(key);
            if (!string.IsNullOrEmpty(json))
            {
                lock (_gate)
                {
                    _entries[key] = json;
                }
            }
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

    /// <summary>Stores (or replaces) the cached value for a key; a serialize/storage failure is swallowed.</summary>
    public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, Json);
            lock (_gate)
            {
                _entries[key] = json;
            }

            LocalSet(key, json);
        }
        catch (JsonException)
        {
            // Best-effort: a cache write failure must never break a live read.
        }

        return Task.CompletedTask;
    }

    private string? LocalGet(string key)
    {
        try
        {
            return _js?.Invoke<string?>("localStorage.getItem", Prefix + key);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            return null;
        }
    }

    private void LocalSet(string key, string json)
    {
        try
        {
            _js?.InvokeVoid("localStorage.setItem", Prefix + key, json);
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException)
        {
            // Best-effort (e.g. storage full / blocked).
        }
    }
}
