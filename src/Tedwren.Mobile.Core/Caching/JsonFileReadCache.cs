using System.Text.Json;

namespace Tedwren.Mobile.Core.Caching;

/// <summary>
/// An <see cref="IReadCache"/> that stores each key as a JSON file in a directory (the app's sandboxed data
/// directory on device). Survives app restarts. <b>Not encrypted at rest yet</b> — it holds only the operative's
/// own data on their biometric-locked, device-bound phone, and M5 replaces this with the SQLCipher store. All
/// I/O is guarded so a corrupt/absent file degrades to a cache miss rather than throwing.
/// </summary>
public sealed class JsonFileReadCache : IReadCache
{
    private readonly string _directory;

    /// <summary>Creates the cache over a directory, creating it if needed.</summary>
    public JsonFileReadCache(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    /// <summary>Returns the cached value for a key, or default when absent/unreadable.</summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var path = PathFor(key);
        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return default;
        }
    }

    /// <summary>Stores (or replaces) the cached value for a key; a write failure is swallowed (best-effort cache).</summary>
    public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream = File.Create(PathFor(key));
            await JsonSerializer.SerializeAsync(stream, value, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort: a cache write failure must never break a live read.
        }
    }

    private string PathFor(string key) => Path.Combine(_directory, Sanitize(key) + ".json");

    private static string Sanitize(string key) =>
        string.Concat(key.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.' ? c : '_'));
}
