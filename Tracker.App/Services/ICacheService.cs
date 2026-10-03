namespace Tracker.App.Services;

/// <summary>
/// A simple and reusable caching interface.
/// Provides methods to store, retrieve, and delete cached data.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached object by its key.
    /// Returns default(T) if the key does not exist.
    /// </summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>
    /// Stores an object in the cache with an optional time-to-live (expiration).
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);

    /// <summary>
    /// Removes a cached item by key.
    /// </summary>
    Task<bool> RemoveAsync(string key);

    /// <summary>
    /// Checks whether a key exists in the cache.
    /// </summary>
    Task<bool> ExistsAsync(string key);
}
