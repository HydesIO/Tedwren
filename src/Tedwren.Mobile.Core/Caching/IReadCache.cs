namespace Tedwren.Mobile.Core.Caching;

/// <summary>
/// A small per-resource read cache so operative screens show their last-known data offline (M3). The M3
/// implementation is plain JSON in the app sandbox; M5 replaces it with the encrypted SQLite+SQLCipher store
/// (also home to the outbox), so callers depend on this abstraction, not the storage engine.
/// </summary>
public interface IReadCache
{
    /// <summary>Returns the cached value for a key, or default when absent/unreadable.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Stores (or replaces) the cached value for a key.</summary>
    Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);
}
