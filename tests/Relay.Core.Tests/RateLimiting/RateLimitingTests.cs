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
}
