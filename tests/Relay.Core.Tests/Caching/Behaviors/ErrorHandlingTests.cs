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

#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace Relay.Core.Tests.Caching.Behaviors;

public class ErrorHandlingTests
{
    private readonly Mock<ICacheKeyGenerator> _keyGeneratorMock;
    private readonly Mock<ICacheSerializer> _serializerMock;

    public ErrorHandlingTests()
    {
        _keyGeneratorMock = new Mock<ICacheKeyGenerator>();
        _serializerMock = new Mock<ICacheSerializer>();
    }

    [Fact]
    public async Task HandleAsync_WithDistributedCacheException_ShouldLogWarningAndContinue()
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

        // Setup distributed cache to throw exception
        distributedCacheMock.Setup(x => x.GetAsync(cacheKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Distributed cache error"));

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

        // Verify warning was logged
        loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to get data from distributed cache")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

        // Cleanup
        memoryCache.Dispose();
    }

    [Fact]
    public async Task HandleAsync_WithSerializationFailure_ShouldLogWarningAndReturnResponse()
    {
        // Arrange
        var request = new CachedRequest();
        var response = new TestResponse();
        var cacheKey = "test-fixed-key";

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var loggerMock = new Mock<ILogger<RelayCachingPipelineBehavior<CachedRequest, TestResponse>>>();
        var serializerMock = new Mock<ICacheSerializer>();
        var metricsMock = new Mock<ICacheMetrics>();

        // Setup serializer to throw exception
        serializerMock.Setup(x => x.Serialize(response)).Throws(new Exception("Serialization failed"));

        var behavior = new RelayCachingPipelineBehavior<CachedRequest, TestResponse>(
            memoryCache,
            loggerMock.Object,
            _keyGeneratorMock.Object,
            serializerMock.Object,
            metricsMock.Object);

        _keyGeneratorMock.Setup(x => x.GenerateKey(request, It.IsAny<RelayCacheAttribute>())).Returns(cacheKey);

        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Same(response, result);

        // Verify warning was logged
        loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to cache response")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

        // Cleanup
        memoryCache.Dispose();
    }
}

