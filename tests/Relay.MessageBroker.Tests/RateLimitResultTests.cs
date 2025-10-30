using Relay.MessageBroker.RateLimit;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RateLimitResultTests
{
    [Fact]
    public void Allow_WithRemainingRequests_ShouldCreateAllowResult()
    {
        // Arrange & Act
        var result = RateLimitResult.Allow(99, DateTimeOffset.UtcNow.AddSeconds(60));

        // Assert
        Assert.True(result.Allowed);
        Assert.Equal(99, result.RemainingRequests);
        Assert.Null(result.RetryAfter);
        Assert.NotNull(result.ResetAt);
    }

    [Fact]
    public void Allow_WithNullRemainingRequests_ShouldCreateAllowResult()
    {
        // Arrange & Act
        var result = RateLimitResult.Allow(null, null);

        // Assert
        Assert.True(result.Allowed);
        Assert.Null(result.RemainingRequests);
        Assert.Null(result.RetryAfter);
        Assert.Null(result.ResetAt);
    }

    [Fact]
    public void Reject_WithRetryAfter_ShouldCreateRejectResult()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(30);
        var resetAt = DateTimeOffset.UtcNow.AddSeconds(60);

        // Act
        var result = RateLimitResult.Reject(retryAfter, resetAt);

        // Assert
        Assert.False(result.Allowed);
        Assert.Equal(retryAfter, result.RetryAfter);
        Assert.Equal(0, result.RemainingRequests);
        Assert.Equal(resetAt, result.ResetAt);
    }

    [Fact]
    public void Reject_WithNullResetAt_ShouldCreateRejectResult()
    {
        // Arrange
        var retryAfter = TimeSpan.FromSeconds(30);

        // Act
        var result = RateLimitResult.Reject(retryAfter, null);

        // Assert
        Assert.False(result.Allowed);
        Assert.Equal(retryAfter, result.RetryAfter);
        Assert.Equal(0, result.RemainingRequests);
        Assert.Null(result.ResetAt);
    }
}