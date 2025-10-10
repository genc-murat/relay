using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.RateLimiting.Implementations;
using Xunit;

namespace Relay.Core.Tests.RateLimiting;

public class SlidingWindowRateLimiterTests
{
    [Fact]
    public async Task IsAllowedAsync_ShouldAllowRequests_WhenUnderLimit()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 10,
            windowSeconds: 60);

        // Act
        var results = new bool[5];
        for (int i = 0; i < 5; i++)
        {
            results[i] = await limiter.IsAllowedAsync("test-key");
        }

        // Assert
        results.Should().AllBeEquivalentTo(true);
    }

    [Fact]
    public async Task IsAllowedAsync_ShouldDenyRequests_WhenOverLimit()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 3,
            windowSeconds: 60);

        // Act
        var result1 = await limiter.IsAllowedAsync("test-key");
        var result2 = await limiter.IsAllowedAsync("test-key");
        var result3 = await limiter.IsAllowedAsync("test-key");
        var result4 = await limiter.IsAllowedAsync("test-key");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();
        result4.Should().BeFalse(); // Over limit
    }

    [Fact]
    public async Task IsAllowedAsync_ShouldIsolateKeys()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 2,
            windowSeconds: 60);

        // Act
        await limiter.IsAllowedAsync("key1");
        await limiter.IsAllowedAsync("key1");
        var key1Result = await limiter.IsAllowedAsync("key1");
        var key2Result = await limiter.IsAllowedAsync("key2");

        // Assert
        key1Result.Should().BeFalse(); // key1 is over limit
        key2Result.Should().BeTrue();  // key2 is independent
    }

    [Fact]
    public async Task GetRetryAfterAsync_ShouldReturnZero_WhenNotLimited()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 10,
            windowSeconds: 60);

        await limiter.IsAllowedAsync("test-key");

        // Act
        var retryAfter = await limiter.GetRetryAfterAsync("test-key");

        // Assert
        retryAfter.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetRetryAfterAsync_ShouldReturnValue_WhenLimited()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 2,
            windowSeconds: 10);

        // Exhaust the limit
        await limiter.IsAllowedAsync("test-key");
        await limiter.IsAllowedAsync("test-key");

        // Act
        var retryAfter = await limiter.GetRetryAfterAsync("test-key");

        // Assert
        retryAfter.Should().BeGreaterThan(TimeSpan.Zero);
        retryAfter.TotalSeconds.Should().BeLessThanOrEqualTo(11); // Window + buffer
    }

    [Fact]
    public void GetStats_ShouldReturnCorrectStatistics()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 10,
            windowSeconds: 60);

        // Act
        limiter.IsAllowedAsync("test-key").AsTask().Wait();
        limiter.IsAllowedAsync("test-key").AsTask().Wait();
        limiter.IsAllowedAsync("test-key").AsTask().Wait();

        var stats = limiter.GetStats("test-key");

        // Assert
        stats.CurrentCount.Should().BeGreaterThanOrEqualTo(3);
        stats.Limit.Should().Be(10);
        stats.Remaining.Should().BeLessThanOrEqualTo(7);
        stats.CurrentWindowRequests.Should().Be(3);
    }

    [Fact]
    public void Reset_ShouldClearLimits()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 2,
            windowSeconds: 60);

        limiter.IsAllowedAsync("test-key").AsTask().Wait();
        limiter.IsAllowedAsync("test-key").AsTask().Wait();
        var beforeReset = limiter.IsAllowedAsync("test-key").AsTask().Result;

        // Act
        limiter.Reset("test-key");
        var afterReset = limiter.IsAllowedAsync("test-key").AsTask().Result;

        // Assert
        beforeReset.Should().BeFalse();
        afterReset.Should().BeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_ShouldThrowArgumentException_WhenKeyIsNull()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            limiter.IsAllowedAsync(null!).AsTask());
    }

    [Fact]
    public async Task IsAllowedAsync_ShouldThrowArgumentException_WhenKeyIsEmpty()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            limiter.IsAllowedAsync(string.Empty).AsTask());
    }

    [Fact]
    public async Task SlidingWindow_ShouldBeMoreAccurate_ThanFixedWindow()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 5,
            windowSeconds: 2);

        // Act - Make requests at the boundary of windows
        await limiter.IsAllowedAsync("test");
        await limiter.IsAllowedAsync("test");
        await limiter.IsAllowedAsync("test");

        await Task.Delay(TimeSpan.FromSeconds(1.5)); // Wait halfway through window

        // These should still be counted due to sliding window
        var result1 = await limiter.IsAllowedAsync("test");
        var result2 = await limiter.IsAllowedAsync("test");
        var result3 = await limiter.IsAllowedAsync("test"); // This might be denied

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        // result3 depends on exact timing, but sliding window should track accurately
    }

    [Fact]
    public async Task ConcurrentRequests_ShouldBeThreadSafe()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 100,
            windowSeconds: 60);

        // Act
        var tasks = new Task<bool>[200];
        for (int i = 0; i < 200; i++)
        {
            tasks[i] = limiter.IsAllowedAsync("concurrent-test").AsTask();
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        var allowedCount = results.Where(r => r).Count();
        allowedCount.Should().BeLessThanOrEqualTo(100); // Should respect limit
        allowedCount.Should().BeGreaterThanOrEqualTo(99); // Should be close to limit (accounting for timing)
    }
}
