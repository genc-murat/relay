using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.RateLimiting.Implementations;
using Xunit;

namespace Relay.Core.Tests.RateLimiting;

public class RateLimitingConstructorTests
{
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
}