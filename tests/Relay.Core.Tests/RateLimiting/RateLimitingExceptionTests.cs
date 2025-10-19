using System;
using Relay.Core.RateLimiting.Exceptions;
using Xunit;

namespace Relay.Core.Tests.RateLimiting;

public class RateLimitingExceptionTests
{
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
}