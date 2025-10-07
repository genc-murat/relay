using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace Relay.Core.Caching.Metrics;

/// <summary>
/// Default implementation of cache metrics.
/// </summary>
public class DefaultCacheMetrics : ICacheMetrics
{
    private readonly ILogger<DefaultCacheMetrics> _logger;
    private readonly ConcurrentDictionary<string, CacheStatistics> _statisticsByType;
    private readonly CacheStatistics _globalStatistics;

    public DefaultCacheMetrics(ILogger<DefaultCacheMetrics> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _statisticsByType = new ConcurrentDictionary<string, CacheStatistics>();
        _globalStatistics = new CacheStatistics();
    }

    public void RecordHit(string cacheKey, string requestType)
    {
        _globalStatistics.Hits++;
        GetStatisticsForType(requestType).Hits++;

        _logger.LogDebug("Cache hit recorded for key: {CacheKey}, type: {RequestType}", cacheKey, requestType);
    }

    public void RecordMiss(string cacheKey, string requestType)
    {
        _globalStatistics.Misses++;
        GetStatisticsForType(requestType).Misses++;

        _logger.LogDebug("Cache miss recorded for key: {CacheKey}, type: {RequestType}", cacheKey, requestType);
    }

    public void RecordSet(string cacheKey, string requestType, long dataSize)
    {
        _globalStatistics.Sets++;
        _globalStatistics.TotalDataSize += dataSize;
        
        var typeStats = GetStatisticsForType(requestType);
        typeStats.Sets++;
        typeStats.TotalDataSize += dataSize;

        _logger.LogDebug("Cache set recorded for key: {CacheKey}, type: {RequestType}, size: {DataSize} bytes", 
            cacheKey, requestType, dataSize);
    }

    public void RecordEviction(string cacheKey, string requestType)
    {
        _globalStatistics.Evictions++;
        GetStatisticsForType(requestType).Evictions++;

        _logger.LogDebug("Cache eviction recorded for key: {CacheKey}, type: {RequestType}", cacheKey, requestType);
    }

    public CacheStatistics GetStatistics(string? requestType = null)
    {
        if (string.IsNullOrEmpty(requestType))
        {
            return _globalStatistics;
        }

        return GetStatisticsForType(requestType);
    }

    private CacheStatistics GetStatisticsForType(string requestType)
    {
        return _statisticsByType.GetOrAdd(requestType, _ => new CacheStatistics());
    }
}