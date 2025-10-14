using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Caching.Invalidation;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Caching.Invalidation
{
    public class DefaultCacheInvalidatorTests
    {
        private readonly Mock<IMemoryCache> _memoryCacheMock;
        private readonly Mock<IDistributedCache> _distributedCacheMock;
        private readonly Mock<ILogger<DefaultCacheInvalidator>> _loggerMock;
        private readonly Mock<ICacheKeyTracker> _keyTrackerMock;
        private readonly DefaultCacheInvalidator _invalidator;

        public DefaultCacheInvalidatorTests()
        {
            _memoryCacheMock = new Mock<IMemoryCache>();
            _distributedCacheMock = new Mock<IDistributedCache>();
            _loggerMock = new Mock<ILogger<DefaultCacheInvalidator>>();
            _keyTrackerMock = new Mock<ICacheKeyTracker>();

            _invalidator = new DefaultCacheInvalidator(
                _memoryCacheMock.Object,
                _loggerMock.Object,
                _keyTrackerMock.Object,
                _distributedCacheMock.Object);
        }

        [Fact]
        public void Constructor_WithNullMemoryCache_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DefaultCacheInvalidator(null!, _loggerMock.Object, _keyTrackerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DefaultCacheInvalidator(_memoryCacheMock.Object, null!, _keyTrackerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullKeyTracker_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new DefaultCacheInvalidator(_memoryCacheMock.Object, _loggerMock.Object, null!));
        }

        [Fact]
        public void Constructor_WithNullDistributedCache_DoesNotThrow()
        {
            // Arrange, Act & Assert
            var exception = Record.Exception(() =>
                new DefaultCacheInvalidator(_memoryCacheMock.Object, _loggerMock.Object, _keyTrackerMock.Object, null));

            Assert.Null(exception);
        }

        [Fact]
        public async Task InvalidateByPatternAsync_WithMatchingKeys_InvalidatesAllKeys()
        {
            // Arrange
            var pattern = "test:*";
            var keys = new[] { "test:1", "test:2", "test:3" };
            _keyTrackerMock.Setup(x => x.GetKeysByPattern(pattern)).Returns(keys);

            // Act
            await _invalidator.InvalidateByPatternAsync(pattern);

            // Assert
            _memoryCacheMock.Verify(x => x.Remove("test:1"), Times.Once);
            _memoryCacheMock.Verify(x => x.Remove("test:2"), Times.Once);
            _memoryCacheMock.Verify(x => x.Remove("test:3"), Times.Once);
            _distributedCacheMock.Verify(x => x.RemoveAsync("test:1", It.IsAny<CancellationToken>()), Times.Once);
            _distributedCacheMock.Verify(x => x.RemoveAsync("test:2", It.IsAny<CancellationToken>()), Times.Once);
            _distributedCacheMock.Verify(x => x.RemoveAsync("test:3", It.IsAny<CancellationToken>()), Times.Once);
            _keyTrackerMock.Verify(x => x.RemoveKey("test:1"), Times.Once);
            _keyTrackerMock.Verify(x => x.RemoveKey("test:2"), Times.Once);
            _keyTrackerMock.Verify(x => x.RemoveKey("test:3"), Times.Once);
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Invalidated 3 cache entries matching pattern: test:*")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task InvalidateByPatternAsync_WithNoMatchingKeys_DoesNotInvalidateAnything()
        {
            // Arrange
            var pattern = "test:*";
            var keys = Array.Empty<string>();
            _keyTrackerMock.Setup(x => x.GetKeysByPattern(pattern)).Returns(keys);

            // Act
            await _invalidator.InvalidateByPatternAsync(pattern);

            // Assert
            _memoryCacheMock.Verify(x => x.Remove(It.IsAny<string>()), Times.Never);
            _distributedCacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _keyTrackerMock.Verify(x => x.RemoveKey(It.IsAny<string>()), Times.Never);
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Invalidated 0 cache entries matching pattern: test:*")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task InvalidateByTagAsync_WithMatchingKeys_InvalidatesAllKeys()
        {
            // Arrange
            var tag = "user-data";
            var keys = new[] { "user:1", "user:2" };
            _keyTrackerMock.Setup(x => x.GetKeysByTag(tag)).Returns(keys);

            // Act
            await _invalidator.InvalidateByTagAsync(tag);

            // Assert
            _memoryCacheMock.Verify(x => x.Remove("user:1"), Times.Once);
            _memoryCacheMock.Verify(x => x.Remove("user:2"), Times.Once);
            _distributedCacheMock.Verify(x => x.RemoveAsync("user:1", It.IsAny<CancellationToken>()), Times.Once);
            _distributedCacheMock.Verify(x => x.RemoveAsync("user:2", It.IsAny<CancellationToken>()), Times.Once);
            _keyTrackerMock.Verify(x => x.RemoveKey("user:1"), Times.Once);
            _keyTrackerMock.Verify(x => x.RemoveKey("user:2"), Times.Once);
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Invalidated 2 cache entries with tag: user-data")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task InvalidateByDependencyAsync_WithMatchingKeys_InvalidatesAllKeys()
        {
            // Arrange
            var dependencyKey = "user:123";
            var keys = new[] { "profile:123", "settings:123" };
            _keyTrackerMock.Setup(x => x.GetKeysByDependency(dependencyKey)).Returns(keys);

            // Act
            await _invalidator.InvalidateByDependencyAsync(dependencyKey);

            // Assert
            _memoryCacheMock.Verify(x => x.Remove("profile:123"), Times.Once);
            _memoryCacheMock.Verify(x => x.Remove("settings:123"), Times.Once);
            _distributedCacheMock.Verify(x => x.RemoveAsync("profile:123", It.IsAny<CancellationToken>()), Times.Once);
            _distributedCacheMock.Verify(x => x.RemoveAsync("settings:123", It.IsAny<CancellationToken>()), Times.Once);
            _keyTrackerMock.Verify(x => x.RemoveKey("profile:123"), Times.Once);
            _keyTrackerMock.Verify(x => x.RemoveKey("settings:123"), Times.Once);
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Invalidated 2 cache entries dependent on: user:123")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task InvalidateByKeyAsync_WithDistributedCacheAvailable_InvalidatesBothCaches()
        {
            // Arrange
            var key = "test:key";

            // Act
            await _invalidator.InvalidateByKeyAsync(key);

            // Assert
            _memoryCacheMock.Verify(x => x.Remove(key), Times.Once);
            _distributedCacheMock.Verify(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
            _keyTrackerMock.Verify(x => x.RemoveKey(key), Times.Once);
            _loggerMock.Verify(x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Invalidated cache key: test:key")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task InvalidateByKeyAsync_WithDistributedCacheFailure_LogsWarningAndContinues()
        {
            // Arrange
            var key = "test:key";
            _distributedCacheMock.Setup(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Distributed cache error"));

            // Act
            await _invalidator.InvalidateByKeyAsync(key);

            // Assert
            _memoryCacheMock.Verify(x => x.Remove(key), Times.Once);
            _distributedCacheMock.Verify(x => x.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
            _keyTrackerMock.Verify(x => x.RemoveKey(key), Times.Once);
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to remove key test:key from distributed cache")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task InvalidateByKeyAsync_WithoutDistributedCache_InvalidatesOnlyMemoryCache()
        {
            // Arrange
            var invalidatorWithoutDistributed = new DefaultCacheInvalidator(
                _memoryCacheMock.Object,
                _loggerMock.Object,
                _keyTrackerMock.Object);
            var key = "test:key";

            // Act
            await invalidatorWithoutDistributed.InvalidateByKeyAsync(key);

            // Assert
            _memoryCacheMock.Verify(x => x.Remove(key), Times.Once);
            _distributedCacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _keyTrackerMock.Verify(x => x.RemoveKey(key), Times.Once);
        }

        [Fact]
        public async Task ClearAllAsync_WithMemoryCacheAsMemoryCacheInstance_CompactsCache()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var invalidator = new DefaultCacheInvalidator(
                memoryCache,
                _loggerMock.Object,
                _keyTrackerMock.Object,
                _distributedCacheMock.Object);
            var keys = new[] { "key1", "key2" };
            _keyTrackerMock.Setup(x => x.GetAllKeys()).Returns(keys);

            // Act
            await invalidator.ClearAllAsync();

            // Assert
            _distributedCacheMock.Verify(x => x.RemoveAsync("key1", It.IsAny<CancellationToken>()), Times.Once);
            _distributedCacheMock.Verify(x => x.RemoveAsync("key2", It.IsAny<CancellationToken>()), Times.Once);
            _keyTrackerMock.Verify(x => x.ClearAll(), Times.Once);
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Cleared all cache entries")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task ClearAllAsync_WithDistributedCacheFailure_LogsWarningAndContinues()
        {
            // Arrange
            var keys = new[] { "key1", "key2" };
            _keyTrackerMock.Setup(x => x.GetAllKeys()).Returns(keys);
            _distributedCacheMock.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Distributed cache error"));

            // Act
            await _invalidator.ClearAllAsync();

            // Assert
            _distributedCacheMock.Verify(x => x.RemoveAsync("key1", It.IsAny<CancellationToken>()), Times.Once);
            _distributedCacheMock.Verify(x => x.RemoveAsync("key2", It.IsAny<CancellationToken>()), Times.Never);
            _keyTrackerMock.Verify(x => x.ClearAll(), Times.Once);
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Failed to clear distributed cache")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task ClearAllAsync_WithoutDistributedCache_ClearsOnlyMemoryCacheAndTracker()
        {
            // Arrange
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var invalidatorWithoutDistributed = new DefaultCacheInvalidator(
                memoryCache,
                _loggerMock.Object,
                _keyTrackerMock.Object);

            // Act
            await invalidatorWithoutDistributed.ClearAllAsync();

            // Assert
            _distributedCacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _keyTrackerMock.Verify(x => x.ClearAll(), Times.Once);
        }

        [Fact]
        public async Task InvalidateByPatternAsync_WithCancellationToken_PassesTokenToDistributedCache()
        {
            // Arrange
            var pattern = "test:*";
            var keys = new[] { "test:1" };
            var cancellationToken = new CancellationToken(true);
            _keyTrackerMock.Setup(x => x.GetKeysByPattern(pattern)).Returns(keys);

            // Act
            await _invalidator.InvalidateByPatternAsync(pattern, cancellationToken);

            // Assert
            _distributedCacheMock.Verify(x => x.RemoveAsync("test:1", cancellationToken), Times.Once);
        }

        [Fact]
        public async Task InvalidateByTagAsync_WithCancellationToken_PassesTokenToDistributedCache()
        {
            // Arrange
            var tag = "test";
            var keys = new[] { "test:1" };
            var cancellationToken = new CancellationToken(true);
            _keyTrackerMock.Setup(x => x.GetKeysByTag(tag)).Returns(keys);

            // Act
            await _invalidator.InvalidateByTagAsync(tag, cancellationToken);

            // Assert
            _distributedCacheMock.Verify(x => x.RemoveAsync("test:1", cancellationToken), Times.Once);
        }

        [Fact]
        public async Task InvalidateByDependencyAsync_WithCancellationToken_PassesTokenToDistributedCache()
        {
            // Arrange
            var dependencyKey = "dep:1";
            var keys = new[] { "test:1" };
            var cancellationToken = new CancellationToken(true);
            _keyTrackerMock.Setup(x => x.GetKeysByDependency(dependencyKey)).Returns(keys);

            // Act
            await _invalidator.InvalidateByDependencyAsync(dependencyKey, cancellationToken);

            // Assert
            _distributedCacheMock.Verify(x => x.RemoveAsync("test:1", cancellationToken), Times.Once);
        }

        [Fact]
        public async Task InvalidateByKeyAsync_WithCancellationToken_PassesTokenToDistributedCache()
        {
            // Arrange
            var key = "test:1";
            var cancellationToken = new CancellationToken(true);

            // Act
            await _invalidator.InvalidateByKeyAsync(key, cancellationToken);

            // Assert
            _distributedCacheMock.Verify(x => x.RemoveAsync(key, cancellationToken), Times.Once);
        }

        [Fact]
        public async Task ClearAllAsync_WithCancellationToken_PassesTokenToDistributedCache()
        {
            // Arrange
            var keys = new[] { "key1" };
            var cancellationToken = new CancellationToken(true);
            _keyTrackerMock.Setup(x => x.GetAllKeys()).Returns(keys);

            // Act
            await _invalidator.ClearAllAsync(cancellationToken);

            // Assert
            _distributedCacheMock.Verify(x => x.RemoveAsync("key1", cancellationToken), Times.Once);
        }
    }
}