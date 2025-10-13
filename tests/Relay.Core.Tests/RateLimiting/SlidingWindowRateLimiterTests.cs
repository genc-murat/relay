using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
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
        retryAfter.Should().BeGreaterThan(TimeSpan.Zero);
        retryAfter.Should().BeLessThanOrEqualTo(TimeSpan.FromSeconds(3)); // Window + buffer
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
        retryAfter.Should().Be(TimeSpan.Zero);
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
        stats.CurrentCount.Should().Be(0);
        stats.Limit.Should().Be(50);
        stats.Remaining.Should().Be(50);
        stats.WindowDuration.Should().Be(TimeSpan.FromSeconds(30));
        stats.CurrentWindowRequests.Should().Be(0);
        stats.PreviousWindowRequests.Should().Be(0);
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
        result.Should().Contain("Used: 25/100");
        result.Should().Contain("Current: 15");
        result.Should().Contain("Previous: 10");
        result.Should().Contain("Remaining: 75");
        result.Should().Contain("Window: 60s");
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
        logger.LoggedMessages.Should().Contain(msg =>
            msg.LogLevel == LogLevel.Debug &&
            msg.Message.Contains("Rate limit exceeded"));
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
        logger.LoggedMessages.Should().Contain(msg =>
            msg.LogLevel == LogLevel.Information &&
            msg.Message.Contains("Rate limit reset"));
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
