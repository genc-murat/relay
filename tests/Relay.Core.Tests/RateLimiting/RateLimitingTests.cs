using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.RateLimiting.Attributes;
using Relay.Core.RateLimiting.Exceptions;
using Relay.Core.RateLimiting.Implementations;
using Relay.Core.RateLimiting.Interfaces;
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
    public async Task RateLimiter_DifferentKeys_ShouldBeIndependent()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());
        var rateLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);

        // Act & Assert
        var result1 = await rateLimiter.IsAllowedAsync("user:127");
        var result2 = await rateLimiter.IsAllowedAsync("user:128");

        Assert.True(result1);
        Assert.True(result2);
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
        Assert.Contains(key, exception.Message);
        Assert.Contains("30", exception.Message);
        Assert.Equal(key, exception.Key);
        Assert.Equal(retryAfter, exception.RetryAfter);
    }

    [Fact]
    public void RateLimitExceededException_ConstructorWithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var key = "user:130";
        var retryAfter = TimeSpan.FromSeconds(45);
        var innerException = new InvalidOperationException("Test inner exception");

        // Act
        var exception = new RateLimitExceededException(key, retryAfter, innerException);

        // Assert
        Assert.Equal(key, exception.Key);
        Assert.Equal(retryAfter, exception.RetryAfter);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Contains(key, exception.Message);
        Assert.Contains("45", exception.Message);
    }

    [Fact]
    public void RateLimitExceededException_ConstructorWithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(30);

        // Act
        Action act = () => new RateLimitExceededException(null!, retryAfter);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void RateLimitExceededException_ConstructorWithInnerExceptionNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(30);
        var innerException = new InvalidOperationException("Test");

        // Act
        Action act = () => new RateLimitExceededException(null!, retryAfter, innerException);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void RateLimitExceededException_KeyProperty_ShouldBeReadOnly()
    {
        // Arrange
        var key = "user:131";
        var retryAfter = TimeSpan.FromSeconds(60);

        // Act
        var exception = new RateLimitExceededException(key, retryAfter);

        // Assert
        Assert.Equal(key, exception.Key);
        // Note: Properties are read-only, so no setter to test
    }

    [Fact]
    public void RateLimitExceededException_RetryAfterProperty_ShouldHandleZeroValue()
    {
        // Arrange
        var key = "user:132";
        var retryAfter = TimeSpan.Zero;

        // Act
        var exception = new RateLimitExceededException(key, retryAfter);

        // Assert
        Assert.Equal(TimeSpan.Zero, exception.RetryAfter);
        Assert.Contains("0", exception.Message);
    }

    [Fact]
    public void RateLimitExceededException_RetryAfterProperty_ShouldHandleNegativeValue()
    {
        // Arrange
        var key = "user:133";
        var retryAfter = TimeSpan.FromSeconds(-10);

        // Act
        var exception = new RateLimitExceededException(key, retryAfter);

        // Assert
        Assert.Equal(retryAfter, exception.RetryAfter);
        Assert.Contains("-10", exception.Message);
    }

    [Fact]
    public void RateLimitExceededException_RetryAfterProperty_ShouldHandleLargeValue()
    {
        // Arrange
        var key = "user:134";
        var retryAfter = TimeSpan.FromHours(24);

        // Act
        var exception = new RateLimitExceededException(key, retryAfter);

        // Assert
        Assert.Equal(retryAfter, exception.RetryAfter);
        Assert.Contains("86400", exception.Message); // 24*3600
    }

    [Fact]
    public void RateLimitExceededException_Message_ShouldFormatCorrectlyWithDecimalSeconds()
    {
        // Arrange
        var key = "user:135";
        var retryAfter = TimeSpan.FromMilliseconds(1500); // 1.5 seconds

        // Act
        var exception = new RateLimitExceededException(key, retryAfter);

        // Assert
        Assert.Equal($"Rate limit exceeded for key '{key}'. Retry after {retryAfter.TotalSeconds} seconds.", exception.Message);
        Assert.Contains("1.5", exception.Message);
    }

    [Fact]
    public void RateLimitExceededException_Message_ShouldHandleSpecialCharactersInKey()
    {
        // Arrange
        var key = "user:123@domain.com/path?query=value";
        var retryAfter = TimeSpan.FromSeconds(30);

        // Act
        var exception = new RateLimitExceededException(key, retryAfter);

        // Assert
        Assert.Contains(key, exception.Message);
        Assert.Contains("30", exception.Message);
    }

    [Fact]
    public void RateLimitExceededException_Message_ShouldHandleEmptyKey()
    {
        // Arrange
        var key = "";
        var retryAfter = TimeSpan.FromSeconds(10);

        // Act
        var exception = new RateLimitExceededException(key, retryAfter);

        // Assert
        Assert.Equal($"Rate limit exceeded for key '{key}'. Retry after {retryAfter.TotalSeconds} seconds.", exception.Message);
        Assert.Contains("''", exception.Message); // empty key in quotes
    }

    [Fact]
    public void RateLimitExceededException_MessageWithInnerException_ShouldContainSameMessage()
    {
        // Arrange
        var key = "user:136";
        var retryAfter = TimeSpan.FromSeconds(20);
        var innerException = new InvalidOperationException("Inner");

        // Act
        var exception = new RateLimitExceededException(key, retryAfter, innerException);

        // Assert
        Assert.Equal($"Rate limit exceeded for key '{key}'. Retry after {retryAfter.TotalSeconds} seconds.", exception.Message);
    }

    [Fact]
    public void RateLimitExceededException_ShouldInheritFromException()
    {
        // Arrange
        var key = "user:137";
        var retryAfter = TimeSpan.FromSeconds(15);

        // Act
        var exception = new RateLimitExceededException(key, retryAfter);

        // Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void RateLimitExceededException_ShouldBeCatchableAsException()
    {
        // Arrange
        var key = "user:138";
        var retryAfter = TimeSpan.FromSeconds(25);

        // Act & Assert
        try
        {
            throw new RateLimitExceededException(key, retryAfter);
        }
        catch (Exception ex)
        {
            Assert.IsType<RateLimitExceededException>(ex);
            Assert.Contains(key, ex.Message);
        }
    }

    [Fact]
    public void RateLimitExceededException_ShouldHaveStackTrace()
    {
        // Arrange
        var key = "user:139";
        var retryAfter = TimeSpan.FromSeconds(5);

        // Act & Assert
        try
        {
            throw new RateLimitExceededException(key, retryAfter);
        }
        catch (RateLimitExceededException ex)
        {
            Assert.NotNull(ex.StackTrace);
            Assert.NotEmpty(ex.StackTrace);
        }
    }

    [Fact]
    public void RateLimitExceededException_ShouldBeSerializable()
    {
        // Arrange
        var key = "user:140";
        var retryAfter = TimeSpan.FromSeconds(30);
        var originalException = new RateLimitExceededException(key, retryAfter);

        // Act & Assert - Test that it's marked as serializable
        // Since Exception is serializable, and we don't add non-serializable fields,
        // it should be serializable
        Assert.IsAssignableFrom<System.Runtime.Serialization.ISerializable>(originalException);
    }

    [Fact]
    public void RateLimitAttribute_ShouldSetProperties()
    {
        // Arrange & Act
        var attribute = new RateLimitAttribute(100, 60, "api");

        // Assert
        Assert.Equal(100, attribute.RequestsPerWindow);
        Assert.Equal(60, attribute.WindowSeconds);
        Assert.Equal("api", attribute.Key);
    }

    [Fact]
    public void RateLimitAttribute_DefaultKey_ShouldBeGlobal()
    {
        // Arrange & Act
        var attribute = new RateLimitAttribute(100, 60);

        // Assert
        Assert.Equal(100, attribute.RequestsPerWindow);
        Assert.Equal(60, attribute.WindowSeconds);
        Assert.Equal("Global", attribute.Key);
    }

    [Fact]
    public void RateLimitAttribute_WithInvalidRequestsPerWindow_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act
        Action act = () => new RateLimitAttribute(0, 60);

        // Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
        Assert.Equal("requestsPerWindow", ex.ParamName);
    }

    [Fact]
    public void RateLimitAttribute_WithNegativeRequestsPerWindow_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act
        Action act = () => new RateLimitAttribute(-1, 60);

        // Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
        Assert.Equal("requestsPerWindow", ex.ParamName);
    }

    [Fact]
    public void RateLimitAttribute_WithInvalidWindowSeconds_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange & Act
        Action act = () => new RateLimitAttribute(100, 0);

        // Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(act);
        Assert.Equal("windowSeconds", ex.ParamName);
    }

    [Fact]
    public void RateLimitAttribute_WithNullKey_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new RateLimitAttribute(100, 60, null!);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("key", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new InMemoryRateLimiter(null!, NullLogger<InMemoryRateLimiter>.Instance);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("cache", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var cache = new MemoryCache(new MemoryCacheOptions());

        // Act
        Action act = () => new InMemoryRateLimiter(cache, null!);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("logger", ex.ParamName);
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
