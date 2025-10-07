using FluentAssertions;
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

public class EnhancedCachingPipelineBehaviorTests
{
    private readonly Mock<IMemoryCache> _memoryCacheMock;
    private readonly Mock<IDistributedCache> _distributedCacheMock;
    private readonly Mock<ILogger<EnhancedCachingPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<ILogger<EnhancedCachingPipelineBehavior<TestRequestWithoutAttribute, TestResponse>>> _loggerWithoutAttributeMock;
    private readonly Mock<ICacheKeyGenerator> _keyGeneratorMock;
    private readonly Mock<ICacheSerializer> _serializerMock;
    private readonly Mock<ICacheMetrics> _metricsMock;
    private readonly Mock<ICacheInvalidator> _invalidatorMock;
    private readonly Mock<ICacheKeyTracker> _keyTrackerMock;
    private readonly EnhancedCachingPipelineBehavior<TestRequest, TestResponse> _behavior;

    public EnhancedCachingPipelineBehaviorTests()
    {
        _memoryCacheMock = new Mock<IMemoryCache>();
        _distributedCacheMock = new Mock<IDistributedCache>();
        _loggerMock = new Mock<ILogger<EnhancedCachingPipelineBehavior<TestRequest, TestResponse>>>();
        _loggerWithoutAttributeMock = new Mock<ILogger<EnhancedCachingPipelineBehavior<TestRequestWithoutAttribute, TestResponse>>>();
        _keyGeneratorMock = new Mock<ICacheKeyGenerator>();
        _serializerMock = new Mock<ICacheSerializer>();
        _metricsMock = new Mock<ICacheMetrics>();
        _invalidatorMock = new Mock<ICacheInvalidator>();
        _keyTrackerMock = new Mock<ICacheKeyTracker>();

        _behavior = new EnhancedCachingPipelineBehavior<TestRequest, TestResponse>(
            _memoryCacheMock.Object,
            _loggerMock.Object,
            _keyGeneratorMock.Object,
            _serializerMock.Object,
            _metricsMock.Object,
            _invalidatorMock.Object,
            _keyTrackerMock.Object,
            _distributedCacheMock.Object);
    }

[Fact]
    public async Task HandleAsync_WithoutCacheAttribute_ShouldExecuteNext()
    {
        // Arrange
        var behaviorWithoutAttribute = new EnhancedCachingPipelineBehavior<TestRequestWithoutAttribute, TestResponse>(
            _memoryCacheMock.Object,
            _loggerWithoutAttributeMock.Object,
            _keyGeneratorMock.Object,
            _serializerMock.Object,
            _metricsMock.Object,
            _invalidatorMock.Object,
            _keyTrackerMock.Object,
            _distributedCacheMock.Object);
        
        var request = new TestRequestWithoutAttribute();
        var response = new TestResponse();
        var nextMock = new Mock<RequestHandlerDelegate<TestResponse>>();
        nextMock.Setup(x => x()).ReturnsAsync(response);

        // Act
        var result = await behaviorWithoutAttribute.HandleAsync(request, nextMock.Object, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        nextMock.Verify(x => x(), Times.Once);
        _memoryCacheMock.Verify(x => x.TryGetValue(It.IsAny<object>(), out It.Ref<object>.IsAny), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithCacheHit_ShouldReturnCachedResponse()
    {
        // Arrange
        var request = new TestRequest();
        var cachedResponse = new TestResponse();
        var cacheKey = "test-key";
        var nextMock = new Mock<RequestHandlerDelegate<TestResponse>>();


        _keyGeneratorMock.Setup(x => x.GenerateKey(It.IsAny<TestRequest>(), It.IsAny<EnhancedCacheAttribute>()))
            .Returns(cacheKey);
        SetupMemoryCacheHit(cacheKey, cachedResponse);

        // Act
        var result = await _behavior.HandleAsync(request, nextMock.Object, CancellationToken.None);

        // Assert
        result.Should().Be(cachedResponse);
        nextMock.Verify(x => x(), Times.Never);
        _metricsMock.Verify(x => x.RecordHit(cacheKey, "TestRequest"), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithCacheMiss_ShouldExecuteNextAndCacheResponse()
    {
        // Arrange
        var request = new TestRequest();
        var response = new TestResponse();
        var cacheKey = "test-key";
        var serializedResponse = new byte[] { 1, 2, 3 };
        var nextMock = new Mock<RequestHandlerDelegate<TestResponse>>();
        nextMock.Setup(x => x()).ReturnsAsync(response);


        _keyGeneratorMock.Setup(x => x.GenerateKey(It.IsAny<TestRequest>(), It.IsAny<EnhancedCacheAttribute>()))
            .Returns(cacheKey);
        SetupMemoryCacheMiss(cacheKey);
        SetupMemoryCacheSet();
        _serializerMock.Setup(x => x.Serialize(response)).Returns(serializedResponse);

        // Act
        var result = await _behavior.HandleAsync(request, nextMock.Object, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        nextMock.Verify(x => x(), Times.Once);
        _metricsMock.Verify(x => x.RecordMiss(cacheKey, "TestRequest"), Times.Once);
        _metricsMock.Verify(x => x.RecordSet(cacheKey, "TestRequest", serializedResponse.Length), Times.Once);
        _keyTrackerMock.Verify(x => x.AddKey(cacheKey, It.IsAny<string[]>()), Times.Once);
        _memoryCacheMock.Verify(x => x.CreateEntry(cacheKey), Times.Once);
        _distributedCacheMock.Verify(x => x.SetAsync(cacheKey, serializedResponse, It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithDistributedCacheHit_ShouldCacheInMemoryAndReturnResponse()
    {
        // Arrange
        var request = new TestRequest();
        var response = new TestResponse();
        var cacheKey = "test-key";
        var serializedResponse = new byte[] { 1, 2, 3 };
        var nextMock = new Mock<RequestHandlerDelegate<TestResponse>>();


        _keyGeneratorMock.Setup(x => x.GenerateKey(It.IsAny<TestRequest>(), It.IsAny<EnhancedCacheAttribute>()))
            .Returns(cacheKey);
        SetupMemoryCacheMiss(cacheKey);
        SetupMemoryCacheSet();
        SetupDistributedCacheHit(cacheKey, serializedResponse);
        _serializerMock.Setup(x => x.Deserialize<TestResponse>(serializedResponse)).Returns(response);

        // Act
        var result = await _behavior.HandleAsync(request, nextMock.Object, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        nextMock.Verify(x => x(), Times.Never);
        _metricsMock.Verify(x => x.RecordHit(cacheKey, "TestRequest"), Times.Once);
        _memoryCacheMock.Verify(x => x.CreateEntry(cacheKey), Times.Once); // Should cache in memory after distributed hit
    }

    [Fact]
    public async Task HandleAsync_WithExceptionInCaching_ShouldStillReturnResponse()
    {
        // Arrange
        var request = new TestRequest();
        var response = new TestResponse();
        var cacheKey = "test-key";
        var nextMock = new Mock<RequestHandlerDelegate<TestResponse>>();
        nextMock.Setup(x => x()).ReturnsAsync(response);


        _keyGeneratorMock.Setup(x => x.GenerateKey(It.IsAny<TestRequest>(), It.IsAny<EnhancedCacheAttribute>()))
            .Returns(cacheKey);
        SetupMemoryCacheMiss(cacheKey);
        _serializerMock.Setup(x => x.Serialize(response)).Throws(new Exception("Serialization failed"));

        // Act
        var result = await _behavior.HandleAsync(request, nextMock.Object, CancellationToken.None);

        // Assert
        result.Should().Be(response);
        nextMock.Verify(x => x(), Times.Once);
        _metricsMock.Verify(x => x.RecordMiss(cacheKey, "TestRequest"), Times.Once);
    }

    private void SetupCacheAttribute()
    {
        // Mock the attribute check by using reflection to simulate the attribute
        // This is a simplified approach for testing
    }

    private void SetupMemoryCacheHit(string key, object value)
    {
        object outValue = value;
        _memoryCacheMock.Setup(x => x.TryGetValue(key, out outValue)).Returns(true);
    }

    private void SetupMemoryCacheMiss(string key)
    {
        object outValue = null!;
        _memoryCacheMock.Setup(x => x.TryGetValue(key, out outValue)).Returns(false);
    }

    private void SetupDistributedCacheHit(string key, byte[] value)
    {
        _distributedCacheMock.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(value);
    }

    private void SetupMemoryCacheSet()
    {
        var mockEntry = new Mock<ICacheEntry>();
        _memoryCacheMock.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(mockEntry.Object);
    }

[EnhancedCache(AbsoluteExpirationSeconds = 300)]
    public class TestRequest : IRequest<TestResponse>
    {
    }

    public class TestResponse
    {
    }

    public class TestRequestWithoutAttribute : IRequest<TestResponse>
    {
    }
}