using Relay.MessageBroker.RateLimit;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RateLimitExceededExceptionTests
{
    [Fact]
    public void Constructor_WithMessageRetryAfterAndResetAt_ShouldSetProperties()
    {
        // Arrange
        var message = "Rate limit exceeded";
        var retryAfter = TimeSpan.FromSeconds(30);
        var resetAt = DateTimeOffset.UtcNow.AddSeconds(60);

        // Act
        var exception = new RateLimitExceededException(message, retryAfter, resetAt);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(retryAfter, exception.RetryAfter);
        Assert.Equal(resetAt, exception.ResetAt);
    }

    [Fact]
    public void Constructor_WithMessageRetryAfterAndNullResetAt_ShouldSetProperties()
    {
        // Arrange
        var message = "Rate limit exceeded";
        var retryAfter = TimeSpan.FromSeconds(30);

        // Act
        var exception = new RateLimitExceededException(message, retryAfter, null);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(retryAfter, exception.RetryAfter);
        Assert.Null(exception.ResetAt);
    }

    [Fact]
    public void Constructor_WithMessageRetryAfterResetAtAndInnerException_ShouldSetProperties()
    {
        // Arrange
        var message = "Rate limit exceeded";
        var retryAfter = TimeSpan.FromSeconds(30);
        var resetAt = DateTimeOffset.UtcNow.AddSeconds(60);
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new RateLimitExceededException(message, retryAfter, resetAt, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(retryAfter, exception.RetryAfter);
        Assert.Equal(resetAt, exception.ResetAt);
        Assert.Equal(innerException, exception.InnerException);
    }
}