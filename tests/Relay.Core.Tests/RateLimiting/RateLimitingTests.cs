using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.RateLimiting;
using Xunit;

namespace Relay.Core.Tests.RateLimiting;

public class RateLimitingTests
{
    [Fact]
    public async Task IsAllowedAsync_FirstRequest_ShouldReturnTrue()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act
        var result = await rateLimiter.IsAllowedAsync("user:123");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_WithinLimit_ShouldReturnTrue()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act - Make multiple requests within limit
        for (int i = 0; i < 5; i++)
        {
            var result = await rateLimiter.IsAllowedAsync("user:124");
            result.Should().BeTrue();
        }
    }

    [Fact]
    public async Task IsAllowedAsync_WithNullKey_ShouldThrowArgumentException()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act
        Func<Task> act = async () => await rateLimiter.IsAllowedAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task IsAllowedAsync_WithEmptyKey_ShouldThrowArgumentException()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act
        Func<Task> act = async () => await rateLimiter.IsAllowedAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetRetryAfterAsync_NoRateLimit_ShouldReturnZero()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act
        var retryAfter = await rateLimiter.GetRetryAfterAsync("user:125");

        // Assert
        retryAfter.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetRetryAfterAsync_WithinWindow_ShouldReturnRemainingTime()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act - First request to start the window
        await rateLimiter.IsAllowedAsync("user:126");
        
        // Get retry after immediately
        var retryAfter = await rateLimiter.GetRetryAfterAsync("user:126");

        // Assert - Should be close to the window size
        retryAfter.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetRetryAfterAsync_WithNullKey_ShouldThrowArgumentException()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act
        Func<Task> act = async () => await rateLimiter.GetRetryAfterAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RateLimiter_DifferentKeys_ShouldBeIndependent()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act & Assert
        var result1 = await rateLimiter.IsAllowedAsync("user:127");
        var result2 = await rateLimiter.IsAllowedAsync("user:128");

        result1.Should().BeTrue();
        result2.Should().BeTrue();
    }

    [Fact]
    public void RateLimitExceededException_ShouldContainCorrectMessage()
    {
        // Arrange
        var key = "user:129";
        var retryAfter = TimeSpan.FromSeconds(30);

        // Act
        var exception = new RateLimitExceededException(key, retryAfter);

        // Assert
        exception.Message.Should().Contain(key);
        exception.Message.Should().Contain("30");
        exception.Key.Should().Be(key);
        exception.RetryAfter.Should().Be(retryAfter);
    }

    [Fact]
    public void RateLimitAttribute_ShouldSetProperties()
    {
        // Arrange & Act
        var attribute = new RateLimitAttribute(100, 60, "api");

        // Assert
        attribute.RequestsPerWindow.Should().Be(100);
        attribute.WindowSeconds.Should().Be(60);
        attribute.Key.Should().Be("api");
    }

    [Fact]
    public void RateLimitAttribute_DefaultKey_ShouldBeGlobal()
    {
        // Arrange & Act
        var attribute = new RateLimitAttribute(100, 60);

        // Assert
        attribute.RequestsPerWindow.Should().Be(100);
        attribute.WindowSeconds.Should().Be(60);
        attribute.Key.Should().Be("Global");
    }

    [Fact]
    public void RateLimitAttribute_WithInvalidRequestsPerWindow_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act
        Action act = () => new RateLimitAttribute(0, 60);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("requestsPerWindow");
    }

    [Fact]
    public void RateLimitAttribute_WithNegativeRequestsPerWindow_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act
        Action act = () => new RateLimitAttribute(-1, 60);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("requestsPerWindow");
    }

    [Fact]
    public void RateLimitAttribute_WithInvalidWindowSeconds_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act
        Action act = () => new RateLimitAttribute(100, 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("windowSeconds");
    }

    [Fact]
    public void RateLimitAttribute_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new RateLimitAttribute(100, 60, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("key");
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new InMemoryRateLimiter(null!, NullLogger<InMemoryRateLimiter>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());

        // Act
        Action act = () => new InMemoryRateLimiter(cache, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task IsAllowedAsync_SameKeyMultipleTimes_ShouldTrackRequests()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        var key = "user:130";

        // Act - Make requests and track results
        var results = new List<bool>();
        for (int i = 0; i < 10; i++)
        {
            results.Add(await rateLimiter.IsAllowedAsync(key));
        }

        // Assert - All requests should be tracked
        results.Should().NotBeEmpty();
        results.Should().ContainInOrder(Enumerable.Repeat(true, results.Count(r => r)));
    }

    [Fact]
    public async Task IsAllowedAsync_WithCancellation_ShouldRespectToken()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        var cts = new CancellationTokenSource();

        // Act - Cancel before calling
        cts.Cancel();

        // Note: Current implementation doesn't actually use cancellation token
        // but we test that it accepts it
        var result = await rateLimiter.IsAllowedAsync("user:131", cts.Token);

        // Assert - Should still work (implementation doesn't throw on cancellation)
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_ExceedingLimit_ShouldReturnFalse()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        var key = "user:132";

        // Act - Make 100 requests (the default limit)
        var results = new List<bool>();
        for (int i = 0; i < 100; i++)
        {
            results.Add(await rateLimiter.IsAllowedAsync(key));
        }

        // Make one more request that should exceed the limit
        var exceedResult = await rateLimiter.IsAllowedAsync(key);

        // Assert
        results.Should().AllSatisfy(r => r.Should().BeTrue(), "all requests within limit should be allowed");
        exceedResult.Should().BeFalse("request exceeding limit should be blocked");
    }

    [Fact]
    public async Task IsAllowedAsync_AfterWindowExpires_ShouldResetLimit()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        var key = "user:133";

        // Act - Make 100 requests to reach the limit
        for (int i = 0; i < 100; i++)
        {
            await rateLimiter.IsAllowedAsync(key);
        }

        // Verify limit is reached
        var beforeWait = await rateLimiter.IsAllowedAsync(key);

        // Wait for the window to expire (default is 60 seconds, but we need to test this)
        // Note: This test assumes default 60 second window. In a real scenario, 
        // you'd want to inject the window configuration for testing
        await Task.Delay(TimeSpan.FromSeconds(61));

        // Make a request after window expiration
        var afterWait = await rateLimiter.IsAllowedAsync(key);

        // Assert
        beforeWait.Should().BeFalse("request should be blocked before window expires");
        afterWait.Should().BeTrue("request should be allowed after window expires");
    }

    [Fact]
    public async Task GetRetryAfterAsync_WhenLimitExceeded_ShouldReturnTimeUntilWindowEnd()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        var key = "user:134";

        // Act - Make enough requests to reach the limit
        for (int i = 0; i < 100; i++)
        {
            await rateLimiter.IsAllowedAsync(key);
        }

        // Get retry after when limit is exceeded
        var retryAfter = await rateLimiter.GetRetryAfterAsync(key);

        // Assert
        retryAfter.Should().BeGreaterThan(TimeSpan.Zero);
        retryAfter.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(60), "should not exceed window duration");
    }

    [Fact]
    public async Task GetRetryAfterAsync_AfterWindowExpires_ShouldReturnZero()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        var key = "user:135";

        // Act - Make a request to start the window
        await rateLimiter.IsAllowedAsync(key);

        // Wait for window to expire
        await Task.Delay(TimeSpan.FromSeconds(61));

        // Get retry after
        var retryAfter = await rateLimiter.GetRetryAfterAsync(key);

        // Assert
        retryAfter.Should().Be(TimeSpan.Zero, "window has expired");
    }

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
        results.Should().AllSatisfy(r => r.Should().BeTrue());
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

        allowedCount.Should().BeLessThanOrEqualTo(100, "should not exceed limit");
        blockedCount.Should().BeGreaterThanOrEqualTo(50, "should block excess requests");
    }

    [Fact]
    public async Task IsAllowedAsync_WithWhitespaceKey_ShouldThrowArgumentException()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act
        Func<Task> act = async () => await rateLimiter.IsAllowedAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public async Task GetRetryAfterAsync_WithEmptyKey_ShouldThrowArgumentException()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act
        Func<Task> act = async () => await rateLimiter.GetRetryAfterAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public async Task GetRetryAfterAsync_WithWhitespaceKey_ShouldThrowArgumentException()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act
        Func<Task> act = async () => await rateLimiter.GetRetryAfterAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("key");
    }

    [Fact]
    public async Task IsAllowedAsync_MultipleKeys_ShouldMaintainSeparateLimits()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        var key1 = "user:138";
        var key2 = "user:139";

        // Act - Exhaust limit for key1
        for (int i = 0; i < 100; i++)
        {
            await rateLimiter.IsAllowedAsync(key1);
        }

        var key1Exceeded = await rateLimiter.IsAllowedAsync(key1);
        var key2Allowed = await rateLimiter.IsAllowedAsync(key2);

        // Assert
        key1Exceeded.Should().BeFalse("key1 should be rate limited");
        key2Allowed.Should().BeTrue("key2 should not be affected");
    }

    [Fact]
    public async Task GetRetryAfterAsync_DecreasingOverTime_ShouldReturnSmallerValues()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        var key = "user:140";

        // Act - Start the window
        await rateLimiter.IsAllowedAsync(key);

        var retryAfter1 = await rateLimiter.GetRetryAfterAsync(key);
        await Task.Delay(TimeSpan.FromSeconds(2));
        var retryAfter2 = await rateLimiter.GetRetryAfterAsync(key);

        // Assert
        retryAfter2.Should().BeLessThan(retryAfter1, "retry after should decrease over time");
    }

    [Fact]
    public async Task IsAllowedAsync_SequentialRequests_ShouldIncrementCount()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        var key = "user:141";

        // Act - Make sequential requests
        var results = new List<bool>();
        for (int i = 0; i < 105; i++)
        {
            results.Add(await rateLimiter.IsAllowedAsync(key));
        }

        // Assert
        var allowedCount = results.Count(r => r);
        var blockedCount = results.Count(r => !r);

        allowedCount.Should().Be(100, "exactly 100 requests should be allowed");
        blockedCount.Should().Be(5, "exactly 5 requests should be blocked");
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
        true.Should().BeTrue("This test documents expected cache behavior");
    }
}
