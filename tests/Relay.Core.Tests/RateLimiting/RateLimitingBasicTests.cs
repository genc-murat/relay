using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.RateLimiting.Implementations;
using Xunit;

namespace Relay.Core.Tests.RateLimiting;

public class RateLimitingBasicTests
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
        Assert.True(result);
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
            Assert.True(result);
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
        await Assert.ThrowsAsync<ArgumentException>(act);
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
        await Assert.ThrowsAsync<ArgumentException>(act);
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
        var ex = await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Equal("key", ex.ParamName);
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
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.True(r));
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
        Assert.True(result);
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
        Assert.All(results, r => Assert.True(r));
        Assert.False(exceedResult);
    }

    [Fact]
    public async Task IsAllowedAsync_AfterWindowExpires_ShouldResetLimit()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance, 100, 1);
        var key = "user:133";

        // Act - Make 100 requests to reach the limit
        for (int i = 0; i < 100; i++)
        {
            await rateLimiter.IsAllowedAsync(key);
        }

        // Verify limit is reached
        var beforeWait = await rateLimiter.IsAllowedAsync(key);

        // Wait for the window to expire
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Make a request after window expiration
        var afterWait = await rateLimiter.IsAllowedAsync(key);

        // Assert
        Assert.False(beforeWait);
        Assert.True(afterWait);
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

        Assert.Equal(100, allowedCount);
        Assert.Equal(5, blockedCount);
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
        Assert.False(key1Exceeded);
        Assert.True(key2Allowed);
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
        Assert.Equal(TimeSpan.Zero, retryAfter);
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
        Assert.True(retryAfter > TimeSpan.Zero);
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
        await Assert.ThrowsAsync<ArgumentException>(act);
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
        var ex = await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Equal("key", ex.ParamName);
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
        var ex = await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Equal("key", ex.ParamName);
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
        Assert.True(retryAfter > TimeSpan.Zero);
        Assert.True(retryAfter <= TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task GetRetryAfterAsync_AfterWindowExpires_ShouldReturnZero()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance, 100, 1);
        var key = "user:135";

        // Act - Make a request to start the window
        await rateLimiter.IsAllowedAsync(key);

        // Wait for window to expire
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Get retry after
        var retryAfter = await rateLimiter.GetRetryAfterAsync(key);

        // Assert
        Assert.Equal(TimeSpan.Zero, retryAfter);
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
        Assert.True(retryAfter2 < retryAfter1);
    }

    [Fact]
    public void RateLimiter_DifferentKeys_ShouldBeIndependent()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act & Assert
        var result1 = rateLimiter.IsAllowedAsync("user:127").Result;
        var result2 = rateLimiter.IsAllowedAsync("user:128").Result;

        Assert.True(result1);
        Assert.True(result2);
    }
}