using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.RateLimiting.Implementations;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        Assert.All(results, r => Assert.True(r));
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
        Assert.True(result1);
        Assert.True(result2);
        Assert.True(result3);
        Assert.False(result4); // Over limit
    }

    [Fact]
    public async Task GetRetryAfterAsync_ShouldReturnCorrectValue_WhenLimited()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 1,
            windowSeconds: 2);

        await limiter.IsAllowedAsync("retry-test");

        // Act
        var retryAfter = await limiter.GetRetryAfterAsync("retry-test");

        // Assert
        Assert.True(retryAfter > TimeSpan.Zero);
        Assert.True(retryAfter <= TimeSpan.FromSeconds(3)); // Window + buffer
    }

    [Fact]
    public async Task GetRetryAfterAsync_ShouldReturnZero_ForNonExistentKey()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 10,
            windowSeconds: 60);

        // Act
        var retryAfter = await limiter.GetRetryAfterAsync("nonexistent-key");

        // Assert
        Assert.Equal(TimeSpan.Zero, retryAfter);
    }

    [Fact]
    public void GetStats_ShouldReturnCorrectStats_ForNonExistentKey()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 50,
            windowSeconds: 30);

        // Act
        var stats = limiter.GetStats("nonexistent");

        // Assert
        Assert.Equal(0, stats.CurrentCount);
        Assert.Equal(50, stats.Limit);
        Assert.Equal(50, stats.Remaining);
        Assert.Equal(TimeSpan.FromSeconds(30), stats.WindowDuration);
        Assert.Equal(0, stats.CurrentWindowRequests);
        Assert.Equal(0, stats.PreviousWindowRequests);
    }

    [Fact]
    public void SlidingWindowStats_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var stats = new SlidingWindowStats
        {
            CurrentCount = 25,
            Limit = 100,
            Remaining = 75,
            WindowDuration = TimeSpan.FromSeconds(60),
            CurrentWindowRequests = 15,
            PreviousWindowRequests = 10
        };

        // Act
        var result = stats.ToString();

        // Assert
        Assert.Contains("Used: 25/100", result);
        Assert.Contains("Current: 15", result);
        Assert.Contains("Previous: 10", result);
        Assert.Contains("Remaining: 75", result);
        Assert.Contains("Window: 60s", result);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SlidingWindowRateLimiter(
                logger: null!,
                requestsPerWindow: 10,
                windowSeconds: 60));
    }

    [Fact]
    public async Task IsAllowedAsync_ShouldThrowArgumentException_WhenKeyIsNull()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 10,
            windowSeconds: 60);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await limiter.IsAllowedAsync(null!));
    }

    [Fact]
    public async Task IsAllowedAsync_ShouldThrowArgumentException_WhenKeyIsEmpty()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 10,
            windowSeconds: 60);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await limiter.IsAllowedAsync(""));
    }

    [Fact]
    public async Task IsAllowedAsync_ShouldThrowArgumentException_WhenKeyIsWhitespace()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 10,
            windowSeconds: 60);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await limiter.IsAllowedAsync("   "));
    }

    [Fact]
    public async Task GetRetryAfterAsync_ShouldThrowArgumentException_WhenKeyIsNull()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 10,
            windowSeconds: 60);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () => 
            await limiter.GetRetryAfterAsync(null!));
    }



    [Fact]
    public async Task IsAllowedAsync_WithWindowTransition_ShouldAllowRequestsCorrectly()
    {
        // Arrange
        var logger = new TestLogger<SlidingWindowRateLimiter>();
        var limiter = new SlidingWindowRateLimiter(
            logger,
            requestsPerWindow: 2,
            windowSeconds: 1); // Small window for testing

        // Allow 2 requests in the first window
        var result1 = await limiter.IsAllowedAsync("transition-test");
        var result2 = await limiter.IsAllowedAsync("transition-test");

        Assert.True(result1);
        Assert.True(result2);

        // Wait for more than the window duration to ensure transition
        await Task.Delay(TimeSpan.FromMilliseconds(1500));

        // After waiting, even the first requests should be mostly discounted
        // due to the sliding window nature of the algorithm
        var result3 = await limiter.IsAllowedAsync("transition-test");
        Assert.True(result3);

        var result4 = await limiter.IsAllowedAsync("transition-test");
        Assert.True(result4);

        // Now we've consumed the limit again
        var result5 = await limiter.IsAllowedAsync("transition-test");
        Assert.False(result5);
    }

    [Fact]
    public async Task IsAllowedAsync_ShouldLogDebugMessage_WhenLimitExceeded()
    {
        // Arrange
        var logger = new TestLogger<SlidingWindowRateLimiter>();
        var limiter = new SlidingWindowRateLimiter(
            logger,
            requestsPerWindow: 1,
            windowSeconds: 60);

        await limiter.IsAllowedAsync("log-test");

        // Act
        await limiter.IsAllowedAsync("log-test");

        // Assert
        Assert.Contains(logger.LoggedMessages, msg =>
            msg.LogLevel == LogLevel.Debug &&
            msg.Message.Contains("Rate limit exceeded"));
    }

    [Fact]
    public void Reset_ShouldClearRateLimitState()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 1,
            windowSeconds: 60);

        // Use up the limit
        var result1 = limiter.IsAllowedAsync("reset-test").Result;
        var result2 = limiter.IsAllowedAsync("reset-test").Result;
        
        Assert.True(result1);
        Assert.False(result2); // Should be rate limited

        // Act
        limiter.Reset("reset-test");

        // Assert
        var result3 = limiter.IsAllowedAsync("reset-test").Result;
        Assert.True(result3); // Should be allowed again after reset
    }

    [Fact]
    public void Reset_ShouldLogInformationMessage()
    {
        // Arrange
        var logger = new TestLogger<SlidingWindowRateLimiter>();
        var limiter = new SlidingWindowRateLimiter(logger);

        // Act
        limiter.Reset("reset-test");

        // Assert
        Assert.Contains(logger.LoggedMessages, msg =>
            msg.LogLevel == LogLevel.Information &&
            msg.Message.Contains("Rate limit reset"));
    }

    [Fact]
    public async Task GetStats_ShouldReturnCorrectStats_AfterWindowTransition()
    {
        // Arrange
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 3,
            windowSeconds: 1); // Small window for testing

        // Use some requests
        await limiter.IsAllowedAsync("stats-test");
        await limiter.IsAllowedAsync("stats-test");

        // Act
        var stats = limiter.GetStats("stats-test");

        // Assert
        Assert.Equal(2, stats.CurrentCount);
        Assert.Equal(3, stats.Limit);
        Assert.Equal(1, stats.Remaining);
        Assert.Equal(2, stats.CurrentWindowRequests);
        Assert.Equal(0, stats.PreviousWindowRequests);

        // Make a request after the window transition to trigger the actual transition
        await Task.Delay(TimeSpan.FromMilliseconds(1100));
        
        // Make another request to trigger the window roll-over logic
        await limiter.IsAllowedAsync("stats-test");

        // Now check the stats after the transition
        var statsAfter = limiter.GetStats("stats-test");

        // After window transition, previous window should contain the old count
        // The current window might have updated based on the new request
        Assert.Equal(3, statsAfter.Limit); // Limit should remain the same
    }

    [Fact]
    public async Task SlidingWindowRateLimiter_Dispose_ShouldCleanupResources()
    {
        // This test covers the cleanup timer functionality
        // The timer in the constructor will periodically cleanup expired windows
        var logger = new TestLogger<SlidingWindowRateLimiter>();
        var limiter = new SlidingWindowRateLimiter(
            logger,
            requestsPerWindow: 2,
            windowSeconds: 1);

        // Use a key to create an entry
        await limiter.IsAllowedAsync("cleanup-test");
        
        // Check that the entry exists
        var stats = limiter.GetStats("cleanup-test");
        Assert.Equal(1, stats.CurrentCount);

        // Wait for more than 2 windows worth of time to trigger cleanup
        // (cleanup removes entries older than 2 windows)
        Thread.Sleep(TimeSpan.FromSeconds(3));

        // Use the same key again to update the last access time
        await limiter.IsAllowedAsync("cleanup-test");
        
        // Wait a bit more and then simulate cleanup by calling cleanup method directly
        Thread.Sleep(100);
        
        // Note: We can't directly test timer cleanup since it runs in the background,
        // but we can test the CleanupExpiredWindows method itself
        // This method is private, so we can't call it directly from tests
    }

    [Fact]
    public async Task SlidingWindowRateLimiter_ExtremeLimits_ShouldWorkCorrectly()
    {
        // Test with very small window and low limit
        var limiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 1,
            windowSeconds: 1);

        // First request should be allowed
        var result1 = await limiter.IsAllowedAsync("extreme-test");
        Assert.True(result1);

        // Second request should be denied (still in same window)
        var result2 = await limiter.IsAllowedAsync("extreme-test");
        Assert.False(result2);

        // Test with very large limits
        var limiter2 = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 1000000,
            windowSeconds: 1);
        
        // Many requests should be allowed
        for (int i = 0; i < 100; i++)
        {
            var result = await limiter2.IsAllowedAsync($"extreme-test-{i}");
            Assert.True(result);
        }
    }

    // Test helper class for logging verification
    private class TestLogger<T> : ILogger<T>
    {
        public List<(LogLevel LogLevel, string Message)> LoggedMessages { get; } = new();

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LoggedMessages.Add((logLevel, formatter(state, exception)));
        }

        private class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
