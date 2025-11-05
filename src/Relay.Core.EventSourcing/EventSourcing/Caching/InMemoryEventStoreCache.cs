using Microsoft.Extensions.Caching.Memory;
using Relay.Core.EventSourcing.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.EventSourcing.Caching;

/// <summary>
/// In-memory implementation of IEventStoreCache using IMemoryCache.
/// </summary>
public class InMemoryEventStoreCache : IEventStoreCache
{
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryEventStoreCache"/> class.
    /// </summary>
    /// <param name="cache">The memory cache instance.</param>
    /// <param name="cacheExpirationMinutes">Cache expiration time in minutes (default: 30).</param>
    public InMemoryEventStoreCache(IMemoryCache cache, int cacheExpirationMinutes = 30)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheExpirationMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(cacheExpirationMinutes / 2)
        };
    }

    /// <inheritdoc />
    public ValueTask<List<Event>?> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(aggregateId);
        var events = _cache.Get<List<Event>>(cacheKey);
        return ValueTask.FromResult(events);
    }

    /// <inheritdoc />
    public ValueTask SetEventsAsync(Guid aggregateId, List<Event> events, CancellationToken cancellationToken = default)
    {
        if (events == null)
        {
            throw new ArgumentNullException(nameof(events));
        }

        var cacheKey = GetCacheKey(aggregateId);
        _cache.Set(cacheKey, events, _cacheOptions);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask InvalidateAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(aggregateId);
        _cache.Remove(cacheKey);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        // IMemoryCache doesn't have a built-in Clear method
        // This is a limitation of the in-memory cache
        // For production scenarios, consider using a distributed cache like Redis

        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0); // Compact removes all entries
        }

        return ValueTask.CompletedTask;
    }

    private static string GetCacheKey(Guid aggregateId) => $"EventStore:{aggregateId}";
}
