using Microsoft.AspNetCore.Mvc;
using Tracker.App.Common.Constants;
using Tracker.App.Common.Models;
using Tracker.App.Common.Routing;
using Tracker.App.Services;

namespace Tracker.App.Controllers;

/// <summary>
/// Controller demonstrating how to use Redis caching via ICacheService.
/// Inherits BaseApiController for consistent route prefixing and ApiResponse envelopes.
/// </summary>
public class CacheController : BaseApiController
{
    private readonly ICacheService _cacheService;

    public CacheController(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    /// <summary>
    /// Retrieves a cached value by key.
    /// </summary>
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string key)
    {
        var value = await _cacheService.GetAsync<object>(key);

        if (value is null)
        {
            return Failure(string.Format(ApiConstants.CacheKeyNotFoundMessage, key), StatusCodes.Status404NotFound);
        }

        return Success(new { key, value });
    }

    /// <summary>
    /// Saves a key-value pair to Redis with an optional expiration time (in minutes).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Set([FromBody] CacheRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
        {
            return Failure(ApiConstants.CacheKeyEmptyMessage, StatusCodes.Status400BadRequest);
        }

        var expiry = request.ExpiryMinutes.HasValue
            ? TimeSpan.FromMinutes(request.ExpiryMinutes.Value)
            : TimeSpan.FromMinutes(30);

        await _cacheService.SetAsync(request.Key, request.Value, expiry);

        var data = new
        {
            key = request.Key,
            expiresInMinutes = expiry.TotalMinutes
        };

        return Success(data, string.Format(ApiConstants.CacheKeySavedMessage, request.Key));
    }

    /// <summary>
    /// Removes a key from Redis cache.
    /// </summary>
    [HttpDelete("{key}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string key)
    {
        var removed = await _cacheService.RemoveAsync(key);

        if (!removed)
        {
            return Failure(string.Format(ApiConstants.CacheKeyNotFoundMessage, key), StatusCodes.Status404NotFound);
        }

        return Success<object?>(null, string.Format(ApiConstants.CacheKeyDeletedMessage, key));
    }
}

/// <summary>
/// Request model for caching data.
/// </summary>
public record CacheRequest(string Key, object Value, int? ExpiryMinutes);
