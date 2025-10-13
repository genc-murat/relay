using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Cache
{
    public class DefaultAIPredictionCacheTests
    {
        private readonly ILogger<DefaultAIPredictionCache> _logger;
        private readonly DefaultAIPredictionCache _cache;

        public DefaultAIPredictionCacheTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<DefaultAIPredictionCache>();
            _cache = new DefaultAIPredictionCache(_logger);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DefaultAIPredictionCache(null!));
        }

        #endregion

        #region GetCachedPredictionAsync Tests

        [Fact]
        public async Task GetCachedPredictionAsync_Should_Throw_When_Key_Is_Null()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _cache.GetCachedPredictionAsync(null!).AsTask());
        }

        [Fact]
        public async Task GetCachedPredictionAsync_Should_Throw_When_Key_Is_Whitespace()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _cache.GetCachedPredictionAsync("   ").AsTask());
        }

        [Fact]
        public async Task GetCachedPredictionAsync_Should_Return_Null_For_NonExistent_Key()
        {
            // Act
            var result = await _cache.GetCachedPredictionAsync("nonexistent");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetCachedPredictionAsync_Should_Return_Cached_Value_For_Valid_Key()
        {
            // Arrange
            var key = "test_key";
            var recommendation = CreateTestRecommendation();
            await _cache.SetCachedPredictionAsync(key, recommendation, TimeSpan.FromMinutes(5));

            // Act
            var result = await _cache.GetCachedPredictionAsync(key);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(recommendation.Strategy, result!.Strategy);
            Assert.Equal(recommendation.ConfidenceScore, result.ConfidenceScore);
        }

        [Fact]
        public async Task GetCachedPredictionAsync_Should_Return_Null_For_Expired_Key()
        {
            // Arrange
            var key = "expired_key";
            var recommendation = CreateTestRecommendation();
            await _cache.SetCachedPredictionAsync(key, recommendation, TimeSpan.FromMilliseconds(1));

            // Wait for expiry
            await Task.Delay(10);

            // Act
            var result = await _cache.GetCachedPredictionAsync(key);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region SetCachedPredictionAsync Tests

        [Fact]
        public async Task SetCachedPredictionAsync_Should_Throw_When_Key_Is_Null()
        {
            // Arrange
            var recommendation = CreateTestRecommendation();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _cache.SetCachedPredictionAsync(null!, recommendation, TimeSpan.FromMinutes(1)).AsTask());
        }

        [Fact]
        public async Task SetCachedPredictionAsync_Should_Throw_When_Key_Is_Whitespace()
        {
            // Arrange
            var recommendation = CreateTestRecommendation();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _cache.SetCachedPredictionAsync("   ", recommendation, TimeSpan.FromMinutes(1)).AsTask());
        }

        [Fact]
        public async Task SetCachedPredictionAsync_Should_Throw_When_Recommendation_Is_Null()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _cache.SetCachedPredictionAsync("key", null!, TimeSpan.FromMinutes(1)).AsTask());
        }

        [Fact]
        public async Task SetCachedPredictionAsync_Should_Throw_When_Expiry_Is_Zero()
        {
            // Arrange
            var recommendation = CreateTestRecommendation();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _cache.SetCachedPredictionAsync("key", recommendation, TimeSpan.Zero).AsTask());
        }

        [Fact]
        public async Task SetCachedPredictionAsync_Should_Throw_When_Expiry_Is_Negative()
        {
            // Arrange
            var recommendation = CreateTestRecommendation();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _cache.SetCachedPredictionAsync("key", recommendation, TimeSpan.FromMinutes(-1)).AsTask());
        }

        [Fact]
        public async Task SetCachedPredictionAsync_Should_Store_Value_Successfully()
        {
            // Arrange
            var key = "store_key";
            var recommendation = CreateTestRecommendation();

            // Act
            await _cache.SetCachedPredictionAsync(key, recommendation, TimeSpan.FromMinutes(5));

            // Assert - retrieve and verify
            var result = await _cache.GetCachedPredictionAsync(key);
            Assert.NotNull(result);
            Assert.Equal(recommendation.Strategy, result!.Strategy);
        }

        [Fact]
        public async Task SetCachedPredictionAsync_Should_Update_Existing_Key()
        {
            // Arrange
            var key = "update_key";
            var originalRecommendation = CreateTestRecommendation();
            var updatedRecommendation = CreateTestRecommendation(OptimizationStrategy.MemoryPooling);

            await _cache.SetCachedPredictionAsync(key, originalRecommendation, TimeSpan.FromMinutes(5));

            // Act
            await _cache.SetCachedPredictionAsync(key, updatedRecommendation, TimeSpan.FromMinutes(5));

            // Assert
            var result = await _cache.GetCachedPredictionAsync(key);
            Assert.NotNull(result);
            Assert.Equal(OptimizationStrategy.MemoryPooling, result!.Strategy);
        }

        #endregion

        #region Cleanup Tests

        [Fact]
        public async Task CleanupExpiredEntries_Should_Remove_Expired_Entries()
        {
            // Arrange
            var expiredKey = "expired";
            var validKey = "valid";
            var recommendation = CreateTestRecommendation();

            await _cache.SetCachedPredictionAsync(expiredKey, recommendation, TimeSpan.FromMilliseconds(1));
            await _cache.SetCachedPredictionAsync(validKey, recommendation, TimeSpan.FromMinutes(5));

            // Wait for expiry
            await Task.Delay(10);

            // Act - Trigger cleanup by accessing the private method via reflection (for testing)
            var cleanupMethod = typeof(DefaultAIPredictionCache).GetMethod("CleanupExpiredEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cleanupMethod?.Invoke(_cache, new object[] { null });

            // Assert
            var expiredResult = await _cache.GetCachedPredictionAsync(expiredKey);
            var validResult = await _cache.GetCachedPredictionAsync(validKey);

            Assert.Null(expiredResult);
            Assert.NotNull(validResult);
        }

        #endregion

        #region Helper Methods

        private static OptimizationRecommendation CreateTestRecommendation(OptimizationStrategy strategy = OptimizationStrategy.EnableCaching)
        {
            return new OptimizationRecommendation
            {
                Strategy = strategy,
                ConfidenceScore = 0.85,
                EstimatedImprovement = TimeSpan.FromMilliseconds(100),
                Reasoning = "Test recommendation",
                Parameters = new System.Collections.Generic.Dictionary<string, object> { ["test"] = "value" },
                Priority = OptimizationPriority.Medium,
                EstimatedGainPercentage = 0.15,
                Risk = RiskLevel.Low
            };
        }

        #endregion
    }
}