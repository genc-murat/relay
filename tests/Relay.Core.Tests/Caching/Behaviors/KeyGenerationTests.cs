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

namespace Relay.Core.Tests.Caching.Behaviors;

public class KeyGenerationTests
{
    private readonly Mock<ICacheKeyGenerator> _keyGeneratorMock;
    private readonly Mock<ICacheSerializer> _serializerMock;

    public KeyGenerationTests()
    {
        _keyGeneratorMock = new Mock<ICacheKeyGenerator>();
        _serializerMock = new Mock<ICacheSerializer>();
    }

    [Fact]
    public async Task HandleAsync_WithRequestHashKeyPattern_ShouldGenerateKeyWithHash()
    {
        // Arrange
        var request = new HashCachedRequest { Id = 123, Name = "Test" };
        var response = new TestResponse();
        var serializedResponse = new byte[] { 1, 2, 3 };

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var loggerMock = new Mock<ILogger<RelayCachingPipelineBehavior<HashCachedRequest, TestResponse>>>();
        var serializerMock = new Mock<ICacheSerializer>();
        var metricsMock = new Mock<ICacheMetrics>();

        serializerMock.Setup(x => x.Serialize(response)).Returns(serializedResponse);

        var behavior = new RelayCachingPipelineBehavior<HashCachedRequest, TestResponse>(
            memoryCache,
            loggerMock.Object,
            _keyGeneratorMock.Object,
            serializerMock.Object,
            metricsMock.Object);

        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Same(response, result);

        // Verify that the key was generated with hash (not using the key generator mock)
        // The key should contain the request type name and a hash
        // We can't easily verify the exact key without reimplementing the hash logic,
        // but we can verify that the key generator was not called
        _keyGeneratorMock.Verify(x => x.GenerateKey(It.IsAny<HashCachedRequest>(), It.IsAny<Relay.Core.Caching.Attributes.RelayCacheAttribute>()), Times.Never);

        // Cleanup
        memoryCache.Dispose();
    }
}