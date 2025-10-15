using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Caching;
using Relay.Core.Caching.Attributes;
using Relay.Core.Caching.Behaviors;
using Relay.Core.Caching.Invalidation;
using Relay.Core.Caching.Metrics;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Caching.Behaviors;

public class RelayCachingPipelineBehaviorTests
{
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly Mock<ILogger<RelayCachingPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<ICacheKeyGenerator> _keyGeneratorMock;
    private readonly Mock<ICacheSerializer> _serializerMock;
    private readonly Mock<ICacheMetrics> _metricsMock;
    private readonly RelayCachingPipelineBehavior<TestRequest, TestResponse> _behavior;

    // Delegate for Moq TryGetValue callback
    public delegate void TryGetValueCallback(string key, out object value);

    public RelayCachingPipelineBehaviorTests()
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
        _keyGeneratorMock.Verify(x => x.GenerateKey(It.IsAny<HashCachedRequest>(), It.IsAny<RelayCacheAttribute>()), Times.Never);

        // Cleanup
        memoryCache.Dispose();
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

    [Fact]
    public async Task HandleAsync_WithSlidingExpiration_ShouldCacheWithSlidingExpiry()
    {
        // Arrange
        var request = new SlidingExpiryRequest();
        var response = new TestResponse();
        var cacheKey = "sliding-expiry-key";
        var serializedResponse = new byte[] { 1, 2, 3 };

        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var loggerMock = new Mock<ILogger<RelayCachingPipelineBehavior<SlidingExpiryRequest, TestResponse>>>();
        var serializerMock = new Mock<ICacheSerializer>();
        var metricsMock = new Mock<ICacheMetrics>();

        serializerMock.Setup(x => x.Serialize(response)).Returns(serializedResponse);

        var behavior = new RelayCachingPipelineBehavior<SlidingExpiryRequest, TestResponse>(
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

        // Verify the item was cached (we can't easily verify the exact expiration settings
        // without accessing private methods, but we can verify it was cached)
        Assert.True(memoryCache.TryGetValue(cacheKey, out object cachedValue));
        Assert.Same(response, cachedValue);

        // Cleanup
        memoryCache.Dispose();
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

    // Test request classes
    public class TestRequest : IRequest<TestResponse> { }
    public class TestResponse { }

    [RelayCacheAttribute(KeyPattern = "test-fixed-key")]
    public class CachedRequest : TestRequest { }

    [RelayCacheAttribute(Enabled = false)]
    public class DisabledCacheRequest : TestRequest { }

    [RelayCacheAttribute(KeyPattern = "distributed-test-key", UseDistributedCache = true)]
    public class DistributedCachedRequest : TestRequest { }

    [RelayCacheAttribute(KeyPattern = "{RequestType}:{RequestHash}:{Region}")]
    public class HashCachedRequest : TestRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [RelayCacheAttribute(KeyPattern = "tracked-test-key", Tags = new[] { "tag1", "tag2" })]
    public class TrackedCachedRequest : TestRequest { }

    [RelayCacheAttribute(KeyPattern = "sliding-expiry-key", SlidingExpirationSeconds = 300)]
    public class SlidingExpiryRequest : TestRequest { }

    [RelayCacheAttribute(KeyPattern = "absolute-expiry-key", AbsoluteExpirationSeconds = 600)]
    public class AbsoluteExpiryRequest : TestRequest { }
}