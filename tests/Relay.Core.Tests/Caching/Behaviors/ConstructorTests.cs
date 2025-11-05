using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Caching;
using Relay.Core.Caching.Behaviors;
using Relay.Core.Caching.Metrics;
using System;
using Xunit;

namespace Relay.Core.Tests.Caching.Behaviors;

public class ConstructorTests
{
    private readonly Mock<ILogger<RelayCachingPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<ICacheKeyGenerator> _keyGeneratorMock;
    private readonly Mock<ICacheSerializer> _serializerMock;
    private readonly Mock<ICacheMetrics> _metricsMock;

    public ConstructorTests()
    {
        _loggerMock = new Mock<ILogger<RelayCachingPipelineBehavior<TestRequest, TestResponse>>>();
        _keyGeneratorMock = new Mock<ICacheKeyGenerator>();
        _serializerMock = new Mock<ICacheSerializer>();
        _metricsMock = new Mock<ICacheMetrics>();
    }

    [Fact]
    public void Constructor_WithNullMemoryCache_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RelayCachingPipelineBehavior<TestRequest, TestResponse>(
                null!,
                _loggerMock.Object,
                _keyGeneratorMock.Object,
                _serializerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RelayCachingPipelineBehavior<TestRequest, TestResponse>(
                memoryCache,
                null!,
                _keyGeneratorMock.Object,
                _serializerMock.Object));

        // Cleanup
        memoryCache.Dispose();
    }

    [Fact]
    public void Constructor_WithNullKeyGenerator_ShouldThrowArgumentNullException()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RelayCachingPipelineBehavior<TestRequest, TestResponse>(
                memoryCache,
                _loggerMock.Object,
                null!,
                _serializerMock.Object));

        // Cleanup
        memoryCache.Dispose();
    }

    [Fact]
    public void Constructor_WithNullSerializer_ShouldThrowArgumentNullException()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RelayCachingPipelineBehavior<TestRequest, TestResponse>(
                memoryCache,
                _loggerMock.Object,
                _keyGeneratorMock.Object,
                null!));

        // Cleanup
        memoryCache.Dispose();
    }
}

