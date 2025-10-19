using System;
using Relay.Core.RateLimiting.Attributes;
using Xunit;

namespace Relay.Core.Tests.RateLimiting;

public class RateLimitingAttributeTests
{
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
}