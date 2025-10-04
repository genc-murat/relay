
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Caching;
using Xunit;

namespace Relay.Core.Tests.Caching
{
    public class DistributedCachingPipelineBehaviorTests
    {
        public class NonCachedRequest : IRequest<TestResponse> { }

        [DistributedCache(AbsoluteExpirationSeconds = 10, SlidingExpirationSeconds = 5, Region = "TestRegion")]
        public class CachedRequest : IRequest<TestResponse> { public int Id { get; set; } }

        public class TestResponse { public string? Data { get; set; } }

        private readonly Mock<IDistributedCache> _cacheMock;
        private readonly Mock<ICacheKeyGenerator> _keyGeneratorMock;
        private readonly Mock<ICacheSerializer> _serializerMock;

        public DistributedCachingPipelineBehaviorTests()
        {
            _cacheMock = new Mock<IDistributedCache>();
            _keyGeneratorMock = new Mock<ICacheKeyGenerator>();
            _serializerMock = new Mock<ICacheSerializer>();
        }

        private DistributedCachingPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>()
            where TRequest : IRequest<TResponse>
        {
            var logger = new Mock<ILogger<DistributedCachingPipelineBehavior<TRequest, TResponse>>>();
            return new DistributedCachingPipelineBehavior<TRequest, TResponse>(
                _cacheMock.Object,
                logger.Object,
                _keyGeneratorMock.Object,
                _serializerMock.Object);
        }

        [Fact]
        public async Task HandleAsync_ShouldBypassCache_ForRequestWithoutAttribute()
        {
            // Arrange
            var behavior = CreateBehavior<NonCachedRequest, TestResponse>();
            var request = new NonCachedRequest();
            var nextCalled = false;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                nextCalled = true;
                return new ValueTask<TestResponse>(new TestResponse { Data = "live" });
            });

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
            result.Data.Should().Be("live");
            _cacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_ShouldFetchFromCache_OnCacheHit()
        {
            // Arrange
            var behavior = CreateBehavior<CachedRequest, TestResponse>();
            var request = new CachedRequest { Id = 1 };
            var cachedResponse = new TestResponse { Data = "cached" };
            var serializedResponse = Encoding.UTF8.GetBytes("serialized");

            _keyGeneratorMock.Setup(g => g.GenerateKey(request, It.IsAny<DistributedCacheAttribute>())).Returns("cache-key");
            _cacheMock.Setup(c => c.GetAsync("cache-key", It.IsAny<CancellationToken>())).ReturnsAsync(serializedResponse);
            _serializerMock.Setup(s => s.Deserialize<TestResponse>(serializedResponse)).Returns(cachedResponse);

            var nextCalled = false;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                nextCalled = true;
                return new ValueTask<TestResponse>(new TestResponse { Data = "live" });
            });

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeFalse();
            result.Data.Should().Be("cached");
        }

        [Fact]
        public async Task HandleAsync_ShouldExecuteNextAndSetCache_OnCacheMiss()
        {
            // Arrange
            var behavior = CreateBehavior<CachedRequest, TestResponse>();
            var request = new CachedRequest { Id = 2 };
            var liveResponse = new TestResponse { Data = "live" };
            var serializedResponse = Encoding.UTF8.GetBytes("serialized");

            _keyGeneratorMock.Setup(g => g.GenerateKey(request, It.IsAny<DistributedCacheAttribute>())).Returns("cache-key-miss");
            _cacheMock.Setup(c => c.GetAsync("cache-key-miss", It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
            _serializerMock.Setup(s => s.Serialize(liveResponse)).Returns(serializedResponse);

            var nextCalled = false;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                nextCalled = true;
                return new ValueTask<TestResponse>(liveResponse);
            });

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
            result.Data.Should().Be("live");
            _cacheMock.Verify(c => c.SetAsync("cache-key-miss", serializedResponse, It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldHandleDeserializationFailureGracefully()
        {
            // Arrange
            var behavior = CreateBehavior<CachedRequest, TestResponse>();
            var request = new CachedRequest { Id = 3 };
            var liveResponse = new TestResponse { Data = "live-after-fail" };
            var invalidCachedData = Encoding.UTF8.GetBytes("invalid-json");

            _keyGeneratorMock.Setup(g => g.GenerateKey(request, It.IsAny<DistributedCacheAttribute>())).Returns("cache-key-fail");
            _cacheMock.Setup(c => c.GetAsync("cache-key-fail", It.IsAny<CancellationToken>())).ReturnsAsync(invalidCachedData);
            _serializerMock.Setup(s => s.Deserialize<TestResponse>(invalidCachedData)).Throws(new Exception("Deserialization failed"));

            var nextCalled = false;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                nextCalled = true;
                return new ValueTask<TestResponse>(liveResponse);
            });

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
            result.Data.Should().Be("live-after-fail");
        }

        [Fact]
        public async Task HandleAsync_ShouldUseCorrectCacheOptionsFromAttribute()
        {
            // Arrange
            var behavior = CreateBehavior<CachedRequest, TestResponse>();
            var request = new CachedRequest { Id = 4 };
            var liveResponse = new TestResponse { Data = "live" };
            var serializedResponse = Encoding.UTF8.GetBytes("serialized");

            _keyGeneratorMock.Setup(g => g.GenerateKey(request, It.IsAny<DistributedCacheAttribute>())).Returns("cache-key-options");
            _cacheMock.Setup(c => c.GetAsync("cache-key-options", It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
            _serializerMock.Setup(s => s.Serialize(liveResponse)).Returns(serializedResponse);

            var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(liveResponse));

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            _cacheMock.Verify(c => c.SetAsync(
                "cache-key-options",
                serializedResponse,
                It.Is<DistributedCacheEntryOptions>(o =>
                    o.AbsoluteExpirationRelativeToNow == TimeSpan.FromSeconds(10) &&
                    o.SlidingExpiration == TimeSpan.FromSeconds(5)),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
