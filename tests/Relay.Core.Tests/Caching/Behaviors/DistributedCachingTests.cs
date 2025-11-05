using Microsoft.Extensions.Caching.Distributed;
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

namespace Relay.Core.Tests.Caching.Behaviors;

public class DistributedCachingTests
{
    private readonly Mock<ICacheKeyGenerator> _keyGeneratorMock;
    private readonly Mock<ICacheSerializer> _serializerMock;

    public DistributedCachingTests()
    {
        _keyGeneratorMock = new Mock<ICacheKeyGenerator>();
        _serializerMock = new Mock<ICacheSerializer>();
    }

    [Fact]
    public async Task HandleAsync_WithDistributedCacheHit_ShouldReturnCachedResponseAndUpdateMemoryCache()
    {
        // Arrange
        var request = new DistributedCachedRequest();
        var cachedResponse = new TestResponse();
        var cacheKey = "distributed-test-key";
        var serializedResponse = new byte[] { 1, 2, 3 };

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<RelayCachingPipelineBehavior<DistributedCachedRequest, TestResponse>>>();
        var serializerMock = new Mock<ICacheSerializer>();
        var metricsMock = new Mock<ICacheMetrics>();

        // Setup distributed cache to return serialized data
        distributedCacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serializedResponse);

        // Setup serializer to deserialize the response
        serializerMock.Setup(x => x.Deserialize<TestResponse>(serializedResponse))
            .Returns(cachedResponse);

        var behavior = new RelayCachingPipelineBehavior<DistributedCachedRequest, TestResponse>(
            memoryCache,
            loggerMock.Object,
            _keyGeneratorMock.Object,
            serializerMock.Object,
            metricsMock.Object,
            distributedCache: distributedCacheMock.Object);

        _keyGeneratorMock.Setup(x => x.GenerateKey(request, It.IsAny<RelayCacheAttribute>())).Returns(cacheKey);

        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(new TestResponse()));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Same(cachedResponse, result);
        metricsMock.Verify(x => x.RecordHit(cacheKey, nameof(DistributedCachedRequest)), Times.Once);

        // Verify the response was also stored in memory cache
        Assert.True(memoryCache.TryGetValue(cacheKey, out object memoryCachedValue));
        Assert.Same(cachedResponse, memoryCachedValue);

        // Cleanup
        memoryCache.Dispose();
    }

    [Fact]
    public async Task HandleAsync_WithDistributedCacheMiss_ShouldExecuteHandlerAndCacheInBothCaches()
    {
        // Arrange
        var request = new DistributedCachedRequest();
        var response = new TestResponse();
        var cacheKey = "distributed-test-key";
        var serializedResponse = new byte[] { 1, 2, 3 };

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<RelayCachingPipelineBehavior<DistributedCachedRequest, TestResponse>>>();
        var serializerMock = new Mock<ICacheSerializer>();
        var metricsMock = new Mock<ICacheMetrics>();

        // Setup distributed cache to return null (miss)
        distributedCacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[])null);

        // Setup serializer
        serializerMock.Setup(x => x.Serialize(response)).Returns(serializedResponse);

        var behavior = new RelayCachingPipelineBehavior<DistributedCachedRequest, TestResponse>(
            memoryCache,
            loggerMock.Object,
            _keyGeneratorMock.Object,
            serializerMock.Object,
            metricsMock.Object,
            distributedCache: distributedCacheMock.Object);

        _keyGeneratorMock.Setup(x => x.GenerateKey(request, It.IsAny<RelayCacheAttribute>())).Returns(cacheKey);

        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Same(response, result);
        metricsMock.Verify(x => x.RecordMiss(cacheKey, nameof(DistributedCachedRequest)), Times.Once);
        metricsMock.Verify(x => x.RecordSet(cacheKey, nameof(DistributedCachedRequest), serializedResponse.Length), Times.Once);

        // Verify response was cached in memory
        Assert.True(memoryCache.TryGetValue(cacheKey, out object memoryCachedValue));
        Assert.Same(response, memoryCachedValue);

        // Verify response was cached in distributed cache
        distributedCacheMock.Verify(x => x.SetAsync(cacheKey, serializedResponse, It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);

        // Cleanup
        memoryCache.Dispose();
    }
}

