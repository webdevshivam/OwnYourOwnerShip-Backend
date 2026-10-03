using Microsoft.AspNetCore.Mvc;
using Tracker.App.Services;

namespace Tracker.App.Controllers;

/// <summary>
/// Controller demonstrating how to use Redis caching via ICacheService.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
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
    public async Task<IActionResult> Get(string key)
    {
        var value = await _cacheService.GetAsync<object>(key);

        if (value is null)
        {
            return NotFound(new { message = $"Key '{key}' not found in cache." });
        }

        return Ok(new { key, value });
    }

    /// <summary>
    /// Saves a key-value pair to Redis with an optional expiration time (in minutes).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Set([FromBody] CacheRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Key))
        {
            return BadRequest(new { message = "Key cannot be empty." });
        }

        var expiry = request.ExpiryMinutes.HasValue
            ? TimeSpan.FromMinutes(request.ExpiryMinutes.Value)
            : TimeSpan.FromMinutes(30); // Default: 30 minutes

        await _cacheService.SetAsync(request.Key, request.Value, expiry);

        return Ok(new
        {
            message = $"Key '{request.Key}' successfully saved to cache.",
            expiresInMinutes = expiry.TotalMinutes
        });
    }

    /// <summary>
    /// Removes a key from Redis cache.
    /// </summary>
    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        var removed = await _cacheService.RemoveAsync(key);

        if (!removed)
        {
            return NotFound(new { message = $"Key '{key}' was not found in cache." });
        }

        return Ok(new { message = $"Key '{key}' deleted successfully." });
    }
}

/// <summary>
/// Request model for caching data.
/// </summary>
public record CacheRequest(string Key, object Value, int? ExpiryMinutes);
