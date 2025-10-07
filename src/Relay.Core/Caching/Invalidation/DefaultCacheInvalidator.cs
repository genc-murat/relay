using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Caching.Invalidation;

/// <summary>
/// Default implementation of cache invalidator.
/// </summary>
public class DefaultCacheInvalidator : ICacheInvalidator
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache? _distributedCache;
    private readonly ILogger<DefaultCacheInvalidator> _logger;
    private readonly ICacheKeyTracker _keyTracker;

    public DefaultCacheInvalidator(
        IMemoryCache memoryCache,
        ILogger<DefaultCacheInvalidator> logger,
        ICacheKeyTracker keyTracker,
        IDistributedCache? distributedCache = null)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyTracker = keyTracker ?? throw new ArgumentNullException(nameof(keyTracker));
        _distributedCache = distributedCache;
    }

    public async Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var keys = _keyTracker.GetKeysByPattern(pattern);
        
        foreach (var key in keys)
        {
            await InvalidateByKeyAsync(key, cancellationToken);
        }

        _logger.LogInformation("Invalidated {Count} cache entries matching pattern: {Pattern}", keys.Count, pattern);
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        var keys = _keyTracker.GetKeysByTag(tag);
        
        foreach (var key in keys)
        {
            await InvalidateByKeyAsync(key, cancellationToken);
        }

        _logger.LogInformation("Invalidated {Count} cache entries with tag: {Tag}", keys.Count, tag);
    }

    public async Task InvalidateByDependencyAsync(string dependencyKey, CancellationToken cancellationToken = default)
    {
        var keys = _keyTracker.GetKeysByDependency(dependencyKey);
        
        foreach (var key in keys)
        {
            await InvalidateByKeyAsync(key, cancellationToken);
        }

        _logger.LogInformation("Invalidated {Count} cache entries dependent on: {Dependency}", keys.Count, dependencyKey);
    }

    public async Task InvalidateByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        // Remove from memory cache
        _memoryCache.Remove(key);

        // Remove from distributed cache if available
        if (_distributedCache != null)
        {
            try
            {
                await _distributedCache.RemoveAsync(key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove key {Key} from distributed cache", key);
            }
        }

        // Remove from key tracker
        _keyTracker.RemoveKey(key);

        _logger.LogDebug("Invalidated cache key: {Key}", key);
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        // Clear memory cache
        if (_memoryCache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }

        // Clear distributed cache if available
        if (_distributedCache != null)
        {
            try
            {
                // Note: This implementation depends on the distributed cache provider
                // Some providers may not support clearing all keys
                var allKeys = _keyTracker.GetAllKeys();
                foreach (var key in allKeys)
                {
                    await _distributedCache.RemoveAsync(key, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear distributed cache");
            }
        }

        // Clear key tracker
        _keyTracker.ClearAll();

        _logger.LogInformation("Cleared all cache entries");
    }
}