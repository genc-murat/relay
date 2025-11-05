using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Caching;
using Relay.Core.Caching.Attributes;
using Relay.Core.Caching.Behaviors;
using Relay.Core.Caching.Invalidation;
using Relay.Core.Caching.Metrics;
using Relay.Core.Contracts.Pipeline;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Caching.Behaviors;

public class TrackingTests
{
    private readonly Mock<ICacheKeyGenerator> _keyGeneratorMock;
    private readonly Mock<ICacheSerializer> _serializerMock;

    public TrackingTests()
    {
        _keyGeneratorMock = new Mock<ICacheKeyGenerator>();
        _serializerMock = new Mock<ICacheSerializer>();
    }

    [Fact]
    public async Task HandleAsync_WithCacheInvalidationAndTracking_ShouldTrackKeys()
    {
        // Arrange
        var request = new TrackedCachedRequest();
        var response = new TestResponse();
        var cacheKey = "tracked-test-key";
        var serializedResponse = new byte[] { 1, 2, 3 };
        var tags = new[] { "tag1", "tag2" };

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var loggerMock = new Mock<ILogger<RelayCachingPipelineBehavior<TrackedCachedRequest, TestResponse>>>();
        var serializerMock = new Mock<ICacheSerializer>();
        var metricsMock = new Mock<ICacheMetrics>();
        var invalidatorMock = new Mock<ICacheInvalidator>();
        var keyTrackerMock = new Mock<ICacheKeyTracker>();

        serializerMock.Setup(x => x.Serialize(response)).Returns(serializedResponse);

        var behavior = new RelayCachingPipelineBehavior<TrackedCachedRequest, TestResponse>(
            memoryCache,
            loggerMock.Object,
            _keyGeneratorMock.Object,
            serializerMock.Object,
            metricsMock.Object,
            invalidatorMock.Object,
            keyTrackerMock.Object);

        _keyGeneratorMock.Setup(x => x.GenerateKey(request, It.IsAny<RelayCacheAttribute>())).Returns(cacheKey);

        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Same(response, result);

        // Verify key was tracked with tags
        keyTrackerMock.Verify(x => x.AddKey(cacheKey, tags), Times.Once);

        // Cleanup
        memoryCache.Dispose();
    }
}

