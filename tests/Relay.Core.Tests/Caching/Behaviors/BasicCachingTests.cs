using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Caching;
using Relay.Core.Caching.Attributes;
using Relay.Core.Caching.Behaviors;
using Relay.Core.Caching.Metrics;
using Relay.Core.Contracts.Pipeline;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable CS8601 // Possible null reference assignment

namespace Relay.Core.Tests.Caching.Behaviors;

public class BasicCachingTests
{
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly Mock<ILogger<RelayCachingPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<ICacheKeyGenerator> _keyGeneratorMock;
    private readonly Mock<ICacheSerializer> _serializerMock;
    private readonly Mock<ICacheMetrics> _metricsMock;
    private readonly RelayCachingPipelineBehavior<TestRequest, TestResponse> _behavior;

    public BasicCachingTests()
    {
        _memoryCacheMock = new Mock<IMemoryCache>();
        _loggerMock = new Mock<ILogger<RelayCachingPipelineBehavior<TestRequest, TestResponse>>>();
        _keyGeneratorMock = new Mock<ICacheKeyGenerator>();
        _serializerMock = new Mock<ICacheSerializer>();
        _metricsMock = new Mock<ICacheMetrics>();

        _behavior = new RelayCachingPipelineBehavior<TestRequest, TestResponse>(
            _memoryCacheMock.Object,
            _loggerMock.Object,
            _keyGeneratorMock.Object,
            _serializerMock.Object,
            _metricsMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithoutCacheAttribute_ShouldExecuteNext()
    {
        // Arrange
        var request = new TestRequest();
        var response = new TestResponse();
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        _memoryCacheMock.Verify(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<object>.IsAny), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithDisabledCacheAttribute_ShouldExecuteNext()
    {
        // Arrange
        var request = new DisabledCacheRequest();
        var response = new TestResponse();
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        _memoryCacheMock.Verify(x => x.TryGetValue(It.IsAny<string>(), out It.Ref<object>.IsAny), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithCacheHit_ShouldReturnCachedResponse()
    {
        // Arrange
        var request = new CachedRequest();
        var cachedResponse = new TestResponse();
        var cacheKey = "test-fixed-key";

        // Use a simple approach - create a test memory cache
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        memoryCache.Set(cacheKey, cachedResponse);

        // Create a new logger with the correct type
        var loggerMock = new Mock<ILogger<RelayCachingPipelineBehavior<CachedRequest, TestResponse>>>();

        var behavior = new RelayCachingPipelineBehavior<CachedRequest, TestResponse>(
            memoryCache,
            loggerMock.Object,
            _keyGeneratorMock.Object,
            _serializerMock.Object,
            _metricsMock.Object);

        _keyGeneratorMock.Setup(x => x.GenerateKey(request, It.IsAny<RelayCacheAttribute>())).Returns(cacheKey);

        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(new TestResponse()));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Same(cachedResponse, result);
        _metricsMock.Verify(x => x.RecordHit(cacheKey, nameof(CachedRequest)), Times.Once);

        // Cleanup
        memoryCache.Dispose();
    }

    [Fact]
    public async Task HandleAsync_WithCacheMiss_ShouldExecuteAndCacheResponse()
    {
        // Arrange
        var request = new CachedRequest();
        var response = new TestResponse();
        var cacheKey = "test-fixed-key"; // Use the same key as in the attribute
        var serializedResponse = new byte[] { 1, 2, 3 };

        // Use a real memory cache for this test
        var memoryCache = new MemoryCache(new MemoryCacheOptions());

        // Create a new logger with the correct type
        var loggerMock = new Mock<ILogger<RelayCachingPipelineBehavior<CachedRequest, TestResponse>>>();

        var behavior = new RelayCachingPipelineBehavior<CachedRequest, TestResponse>(
            memoryCache,
            loggerMock.Object,
            _keyGeneratorMock.Object,
            _serializerMock.Object,
            _metricsMock.Object);

        // Since the KeyPattern doesn't contain {RequestHash}, it should use the key generator
        _keyGeneratorMock.Setup(x => x.GenerateKey(request, It.IsAny<RelayCacheAttribute>())).Returns(cacheKey);
        _serializerMock.Setup(x => x.Serialize(response)).Returns(serializedResponse);

        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Same(response, result);
        Assert.True(memoryCache.TryGetValue(cacheKey, out object cachedValue));
        Assert.Same(response, cachedValue);
        _metricsMock.Verify(x => x.RecordMiss(cacheKey, nameof(CachedRequest)), Times.Once);
        _metricsMock.Verify(x => x.RecordSet(cacheKey, nameof(CachedRequest), serializedResponse.Length), Times.Once);

        // Cleanup
        memoryCache.Dispose();
    }
}

