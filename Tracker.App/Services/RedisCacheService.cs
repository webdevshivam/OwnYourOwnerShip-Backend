using System.Text.Json;
using StackExchange.Redis;

namespace Tracker.App.Services;

/// <summary>
/// Implementation of ICacheService using Redis (via StackExchange.Redis).
/// Objects are automatically serialized to JSON before storing in Redis,
/// and deserialized back to strongly-typed objects when retrieved.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _database = _redis.GetDatabase();
        _logger = logger;
    }

    /// <summary>
    /// Retrieves an item from Redis and deserializes it to type T.
    /// </summary>
    public async Task<T?> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be empty.", nameof(key));

        try
        {
            var value = await _database.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            // Convert stored JSON string back to original object type T
            string jsonString = value.ToString();
            return JsonSerializer.Deserialize<T>(jsonString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key {Key} from Redis", key);
            return default;
        }
    }

    /// <summary>
    /// Serializes an object to JSON and stores it in Redis with an optional expiry time.
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be empty.", nameof(key));

        if (value == null)
            return;

        try
        {
            // Serialize object to JSON string for readable and consistent storage
            var json = JsonSerializer.Serialize(value);

            // In StackExchange.Redis, Expiration accepts TimeSpan or default (no expiry)
            Expiration redisExpiry = expiry.HasValue ? expiry.Value : default;

            // Store in Redis (StringSet handles expiration automatically)
            await _database.StringSetAsync(key, json, redisExpiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting key {Key} in Redis", key);
            throw;
        }
    }

    /// <summary>
    /// Deletes a key from Redis.
    /// </summary>
    public async Task<bool> RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be empty.", nameof(key));

        try
        {
            return await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key {Key} from Redis", key);
            return false;
        }
    }

    /// <summary>
    /// Checks if a key exists in Redis.
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be empty.", nameof(key));

        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of key {Key} in Redis", key);
            return false;
        }
    }
}
