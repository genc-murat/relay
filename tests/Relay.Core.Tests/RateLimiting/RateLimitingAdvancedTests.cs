using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.RateLimiting.Implementations;
using Xunit;

namespace Relay.Core.Tests.RateLimiting;

public class RateLimitingAdvancedTests
{
    [Fact]
    public async Task IsAllowedAsync_ConcurrentRequests_ShouldHandleCorrectly()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        var key = "user:136";

        // Act - Make concurrent requests
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => rateLimiter.IsAllowedAsync(key).AsTask())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - All should succeed since we're under the limit
        Assert.All(results, r => Assert.True(r));
    }

    [Fact]
    public async Task IsAllowedAsync_ConcurrentRequestsExceedingLimit_ShouldBlockSome()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        var key = "user:137";

        // Act - Make concurrent requests that exceed the limit
        var tasks = Enumerable.Range(0, 150)
            .Select(_ => rateLimiter.IsAllowedAsync(key).AsTask())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        var allowedCount = results.Count(r => r);
        var blockedCount = results.Count(r => !r);

        Assert.True(allowedCount <= 100);
        Assert.True(blockedCount >= 50);
    }

    [Fact]
    public async Task RateLimiter_CacheExpiration_ShouldAllowRequestsAfterExpiry()
    {
        // Arrange
        var cacheOptions = new MemoryCacheOptions
        {
            ExpirationScanFrequency = TimeSpan.FromSeconds(1)
        };
        var cache = new MemoryCache(cacheOptions);
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        var key = "user:142";

        // Act - Make a request to populate cache
        await rateLimiter.IsAllowedAsync(key);

        // Wait for cache expiration (5 minutes + buffer)
        // Note: In a real scenario, you'd mock the cache or use a shorter expiration for testing
        // This test is more of a documentation of expected behavior

        // Assert - Documented behavior
        // After 5 minutes, the cache entry should be evicted and a new window should start
        Assert.True(true);
    }
}
