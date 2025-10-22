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

public class CancellationTests
{
    private readonly Mock<ICacheKeyGenerator> _keyGeneratorMock;
    private readonly Mock<ICacheSerializer> _serializerMock;

    public CancellationTests()
    {
        _keyGeneratorMock = new Mock<ICacheKeyGenerator>();
        _serializerMock = new Mock<ICacheSerializer>();
    }

    [Fact]
    public async Task HandleAsync_WithCancellationToken_ShouldPassTokenToDistributedCache()
    {
        // Arrange
        var request = new DistributedCachedRequest();
        var response = new TestResponse();
        var cacheKey = "distributed-test-key";
        var serializedResponse = new byte[] { 1, 2, 3 };
        var cancellationToken = new CancellationTokenSource().Token;

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCacheMock = new Mock<IDistributedCache>();
        var loggerMock = new Mock<ILogger<RelayCachingPipelineBehavior<DistributedCachedRequest, TestResponse>>>();
        var serializerMock = new Mock<ICacheSerializer>();
        var metricsMock = new Mock<ICacheMetrics>();

        // Setup distributed cache to return null (miss)
        distributedCacheMock.Setup(x => x.GetAsync(cacheKey, cancellationToken))
            .ReturnsAsync((byte[])null);

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
        var result = await behavior.HandleAsync(request, next, cancellationToken);

        // Assert
        Assert.Same(response, result);

        // Verify the cancellation token was passed to distributed cache operations
        distributedCacheMock.Verify(x => x.GetAsync(cacheKey, cancellationToken), Times.Once);
        distributedCacheMock.Verify(x => x.SetAsync(cacheKey, serializedResponse, It.IsAny<DistributedCacheEntryOptions>(), cancellationToken), Times.Once);

        // Cleanup
        memoryCache.Dispose();
    }
}