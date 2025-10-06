
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Caching;
using Relay.Core.Configuration;
using Relay.Core.Configuration.Options;
using Xunit;

namespace Relay.Core.Tests.Caching
{
    public class AdvancedCachingPipelineBehaviorTests
    {
        public class TestRequest : IRequest<TestResponse> { public int Id { get; set; } }
        public class TestResponse { public string? Data { get; set; } }

        [Cache(20)]
        public class AttributedTestRequest : IRequest<TestResponse> { public int Id { get; set; } }

        private readonly Mock<ILogger<AdvancedCachingPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
        private readonly Mock<IDistributedCache> _distributedCacheMock;
        private readonly MemoryCache _memoryCache;
        private readonly Mock<IOptions<RelayOptions>> _optionsMock;

        public AdvancedCachingPipelineBehaviorTests()
        {
            _loggerMock = new Mock<ILogger<AdvancedCachingPipelineBehavior<TestRequest, TestResponse>>>();
            _distributedCacheMock = new Mock<IDistributedCache>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _optionsMock = new Mock<IOptions<RelayOptions>>();
        }

        private AdvancedCachingPipelineBehavior<TRequest, TResponse> CreateBehavior<TRequest, TResponse>(
            RelayOptions? options = null)
            where TRequest : IRequest<TResponse>
        {
            var logger = new Mock<ILogger<AdvancedCachingPipelineBehavior<TRequest, TResponse>>>();
            var opts = Options.Create(options ?? new RelayOptions());
            return new AdvancedCachingPipelineBehavior<TRequest, TResponse>(
                _memoryCache,
                logger.Object,
                opts,
                _distributedCacheMock.Object);
        }

        [Fact]
        public void Constructor_WithNullMemoryCache_ThrowsArgumentNullException()
        {
            Action act = () => new AdvancedCachingPipelineBehavior<TestRequest, TestResponse>(null!, _loggerMock.Object, _optionsMock.Object);
            act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("memoryCache");
        }

        [Fact]
        public async Task HandleAsync_ShouldNotCache_WhenCachingIsDisabled()
        {
            // Arrange
            var options = new RelayOptions { DefaultCachingOptions = { EnableAutomaticCaching = false } };
            var behavior = CreateBehavior<TestRequest, TestResponse>(options);
            var request = new TestRequest { Id = 1 };
            var callCount = 0;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                callCount++;
                return new ValueTask<TestResponse>(new TestResponse { Data = "live" });
            });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            callCount.Should().Be(2);
        }

        [Fact]
        public async Task HandleAsync_ShouldUseMemoryCache_WhenEnabled()
        {
            // Arrange
            var options = new RelayOptions { DefaultCachingOptions = { EnableAutomaticCaching = true, DefaultCacheDurationSeconds = 30 } };
            var behavior = CreateBehavior<TestRequest, TestResponse>(options);
            var request = new TestRequest { Id = 2 };
            var callCount = 0;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                callCount++;
                return new ValueTask<TestResponse>(new TestResponse { Data = "cached" });
            });

            // Act
            var result1 = await behavior.HandleAsync(request, next, CancellationToken.None);
            var result2 = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            callCount.Should().Be(1);
            result1.Data.Should().Be("cached");
            result2.Data.Should().Be("cached");
        }

        [Fact]
        public async Task HandleAsync_ShouldUseDistributedCache_WhenEnabledAndMemoryMisses()
        {
            // Arrange
            var options = new RelayOptions { DefaultCachingOptions = { EnableAutomaticCaching = true, EnableDistributedCaching = true, DefaultCacheDurationSeconds = 30 } };
            var behavior = CreateBehavior<TestRequest, TestResponse>(options);
            var request = new TestRequest { Id = 3 };
            var response = new TestResponse { Data = "dist-cached" };
            var serializedResponse = JsonSerializer.SerializeToUtf8Bytes(response);
            var callCount = 0;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                callCount++;
                return new ValueTask<TestResponse>(response);
            });

            _distributedCacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                 .ReturnsAsync(serializedResponse);

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            callCount.Should().Be(0); // Should not call next(), should get from distributed cache
            result.Data.Should().Be("dist-cached");
            _distributedCacheMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldSetDistributedCache_WhenEnabledAndHandlerExecutes()
        {
            // Arrange
            var options = new RelayOptions { DefaultCachingOptions = { EnableAutomaticCaching = true, EnableDistributedCaching = true, DefaultCacheDurationSeconds = 30 } };
            var behavior = CreateBehavior<TestRequest, TestResponse>(options);
            var request = new TestRequest { Id = 4 };
            var response = new TestResponse { Data = "live-and-cached" };
            var callCount = 0;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                callCount++;
                return new ValueTask<TestResponse>(response);
            });

            _distributedCacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                 .ReturnsAsync((byte[]?)null);

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            callCount.Should().Be(1);
            _distributedCacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_ShouldRespectAbsoluteExpiration()
        {
            // Arrange
            var options = new RelayOptions { DefaultCachingOptions = { EnableAutomaticCaching = true, DefaultCacheDurationSeconds = 1 } };
            var behavior = CreateBehavior<TestRequest, TestResponse>(options);
            var request = new TestRequest { Id = 5 };
            var callCount = 0;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                callCount++;
                return new ValueTask<TestResponse>(new TestResponse { Data = "expiring" });
            });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None); // Call 1
            await Task.Delay(1100);
            await behavior.HandleAsync(request, next, CancellationToken.None); // Call 2

            // Assert
            callCount.Should().Be(2);
        }

        [Fact]
        public async Task HandleAsync_ShouldRespectSlidingExpiration()
        {
            // Arrange
            var options = new RelayOptions { DefaultCachingOptions = { EnableAutomaticCaching = true, UseSlidingExpiration = true, SlidingExpirationSeconds = 2 } };
            var behavior = CreateBehavior<TestRequest, TestResponse>(options);
            var request = new TestRequest { Id = 6 };
            var callCount = 0;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                callCount++;
                return new ValueTask<TestResponse>(new TestResponse { Data = "sliding" });
            });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None); // Call 1
            await Task.Delay(1000);
            await behavior.HandleAsync(request, next, CancellationToken.None); // Call 2 (extends cache)
            await Task.Delay(1000);
            await behavior.HandleAsync(request, next, CancellationToken.None); // Call 3 (should be cached)

            // Assert
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task HandleAsync_ShouldUseCacheAttribute_WhenPresent()
        {
            // Arrange
            var options = new RelayOptions { DefaultCachingOptions = { EnableAutomaticCaching = false } }; // Caching disabled globally
            var behavior = CreateBehavior<AttributedTestRequest, TestResponse>(options);
            var request = new AttributedTestRequest { Id = 7 };
            var callCount = 0;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                callCount++;
                return new ValueTask<TestResponse>(new TestResponse { Data = "attributed" });
            });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            callCount.Should().Be(1);
        }

        [Fact]
        public async Task HandleAsync_ShouldUseHandlerSpecificOptions()
        {
            // Arrange
            var options = new RelayOptions();
            options.CachingOverrides[typeof(TestRequest).FullName!] = new CachingOptions { EnableAutomaticCaching = true, DefaultCacheDurationSeconds = 1 };
            var behavior = CreateBehavior<TestRequest, TestResponse>(options);
            var request = new TestRequest { Id = 8 };
            var callCount = 0;
            var next = new RequestHandlerDelegate<TestResponse>(() =>
            {
                callCount++;
                return new ValueTask<TestResponse>(new TestResponse { Data = "override" });
            });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None); // Call 1
            await behavior.HandleAsync(request, next, CancellationToken.None); // Call 2 (cached)
            await Task.Delay(1100);
            await behavior.HandleAsync(request, next, CancellationToken.None); // Call 3 (expired)

            // Assert
            callCount.Should().Be(2);
        }
    }
}
