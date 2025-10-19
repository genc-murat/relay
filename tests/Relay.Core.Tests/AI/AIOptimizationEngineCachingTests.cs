using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineCachingTests : IDisposable
    {
        private readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
        private readonly AIOptimizationOptions _options;
        private readonly AIOptimizationEngine _engine;

        public AIOptimizationEngineCachingTests()
        {
            _loggerMock = new Mock<ILogger<AIOptimizationEngine>>();
            _options = new AIOptimizationOptions
            {
                DefaultBatchSize = 10,
                MaxBatchSize = 100,
                ModelUpdateInterval = TimeSpan.FromMinutes(5),
                ModelTrainingDate = DateTime.UtcNow,
                ModelVersion = "1.0.0",
                LastRetrainingDate = DateTime.UtcNow.AddDays(-1)
            };

            var optionsMock = new Mock<IOptions<AIOptimizationOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            _engine = new AIOptimizationEngine(_loggerMock.Object, optionsMock.Object);
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }

        [Fact]
        public async Task ShouldCacheAsync_Should_Throw_When_Disposed()
        {
            // Arrange
            _engine.Dispose();
            var accessPatterns = new AccessPattern[0];

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns));
        }

        [Fact]
        public async Task ShouldCacheAsync_Should_Return_Caching_Recommendation()
        {
            // Arrange
            var accessPatterns = new[]
            {
                new AccessPattern
                {
                    AccessCount = 10,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                    Timestamp = DateTime.UtcNow,
                    RequestKey = "test",
                    WasCacheHit = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(100),
                    AccessFrequency = 2.0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    DataVolatility = 0.1,
                    SampleSize = 10
                }
            };

            // Act
            var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

            // Assert
            Assert.NotNull(recommendation);
            Assert.True(recommendation.ExpectedHitRate >= 0 && recommendation.ExpectedHitRate <= 1);
        }

        [Fact]
        public async Task ShouldCacheAsync_Should_Recommend_Caching_For_High_Repeat_Rate()
        {
            // Arrange - Multiple access patterns with repeated keys to create high repeat rate
            var accessPatterns = new[]
            {
                new AccessPattern
                {
                    AccessCount = 5,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                    Timestamp = DateTime.UtcNow,
                    RequestKey = "frequent",
                    WasCacheHit = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(100),
                    AccessFrequency = 2.0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    DataVolatility = 0.1,
                    SampleSize = 5
                },
                new AccessPattern
                {
                    AccessCount = 3,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(3),
                    Timestamp = DateTime.UtcNow.AddMinutes(1),
                    RequestKey = "frequent", // Same key - repeat
                    WasCacheHit = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(95),
                    AccessFrequency = 2.5,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(95),
                    DataVolatility = 0.1,
                    SampleSize = 3
                },
                new AccessPattern
                {
                    AccessCount = 2,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(10),
                    Timestamp = DateTime.UtcNow.AddMinutes(2),
                    RequestKey = "rare",
                    WasCacheHit = false,
                    ExecutionTime = TimeSpan.FromMilliseconds(150),
                    AccessFrequency = 0.2,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(150),
                    DataVolatility = 0.3,
                    SampleSize = 2
                }
            };

            // Act
            var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

            // Assert
            Assert.NotNull(recommendation);
            Assert.True(recommendation.ShouldCache, "Should recommend caching for high repeat rate");
            Assert.True(recommendation.ExpectedHitRate > 0.5, "Expected hit rate should be high");
        }

        [Fact]
        public async Task ShouldCacheAsync_Should_Not_Recommend_Caching_For_Low_Repeat_Rate()
        {
            // Arrange - All unique keys, no repeats
            var accessPatterns = new[]
            {
                new AccessPattern
                {
                    AccessCount = 1,
                    TimeSinceLastAccess = TimeSpan.FromHours(1),
                    Timestamp = DateTime.UtcNow,
                    RequestKey = "rare1",
                    WasCacheHit = false,
                    ExecutionTime = TimeSpan.FromMilliseconds(50),
                    AccessFrequency = 0.1,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    DataVolatility = 0.9,
                    SampleSize = 1
                },
                new AccessPattern
                {
                    AccessCount = 1,
                    TimeSinceLastAccess = TimeSpan.FromHours(2),
                    Timestamp = DateTime.UtcNow.AddMinutes(1),
                    RequestKey = "rare2",
                    WasCacheHit = false,
                    ExecutionTime = TimeSpan.FromMilliseconds(60),
                    AccessFrequency = 0.05,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(60),
                    DataVolatility = 0.8,
                    SampleSize = 1
                },
                new AccessPattern
                {
                    AccessCount = 1,
                    TimeSinceLastAccess = TimeSpan.FromHours(3),
                    Timestamp = DateTime.UtcNow.AddMinutes(2),
                    RequestKey = "rare3",
                    WasCacheHit = false,
                    ExecutionTime = TimeSpan.FromMilliseconds(55),
                    AccessFrequency = 0.03,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(55),
                    DataVolatility = 0.7,
                    SampleSize = 1
                }
            };

            // Act
            var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

            // Assert
            Assert.NotNull(recommendation);
            Assert.False(recommendation.ShouldCache, "Should not recommend caching for low repeat rate");
        }

        [Fact]
        public async Task ShouldCacheAsync_Should_Handle_Empty_Access_Patterns()
        {
            // Arrange
            var accessPatterns = Array.Empty<AccessPattern>();

            // Act
            var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

            // Assert
            Assert.NotNull(recommendation);
            Assert.False(recommendation.ShouldCache, "Should not recommend caching with no access patterns");
        }

        [Fact]
        public async Task ShouldCacheAsync_Should_Handle_Multiple_Access_Patterns()
        {
            // Arrange
            var accessPatterns = new[]
            {
                new AccessPattern
                {
                    AccessCount = 20,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(10),
                    Timestamp = DateTime.UtcNow,
                    RequestKey = "pattern1",
                    WasCacheHit = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(80),
                    AccessFrequency = 5.0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(80),
                    DataVolatility = 0.2,
                    SampleSize = 20
                },
                new AccessPattern
                {
                    AccessCount = 15,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(15),
                    Timestamp = DateTime.UtcNow,
                    RequestKey = "pattern2",
                    WasCacheHit = false,
                    ExecutionTime = TimeSpan.FromMilliseconds(120),
                    AccessFrequency = 3.0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(120),
                    DataVolatility = 0.3,
                    SampleSize = 15
                }
            };

            // Act
            var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

            // Assert
            Assert.NotNull(recommendation);
            Assert.True(recommendation.ExpectedHitRate >= 0 && recommendation.ExpectedHitRate <= 1);
        }

        [Fact]
        public async Task ShouldCacheAsync_Should_Consider_Data_Volatility()
        {
            // Arrange - High volatility should reduce caching recommendation
            var volatilePatterns = new[]
            {
                new AccessPattern
                {
                    AccessCount = 30,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                    Timestamp = DateTime.UtcNow,
                    RequestKey = "volatile",
                    WasCacheHit = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(100),
                    AccessFrequency = 8.0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    DataVolatility = 0.9, // High volatility
                    SampleSize = 30
                }
            };

            var stablePatterns = new[]
            {
                new AccessPattern
                {
                    AccessCount = 30,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                    Timestamp = DateTime.UtcNow,
                    RequestKey = "stable",
                    WasCacheHit = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(100),
                    AccessFrequency = 8.0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    DataVolatility = 0.1, // Low volatility
                    SampleSize = 30
                }
            };

            // Act
            var volatileRecommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), volatilePatterns);
            var stableRecommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), stablePatterns);

            // Assert
            Assert.NotNull(volatileRecommendation);
            Assert.NotNull(stableRecommendation);
            // Stable data should generally have higher or equal hit rate expectation
            Assert.True(stableRecommendation.ExpectedHitRate >= volatileRecommendation.ExpectedHitRate * 0.8,
                "Stable data should have similar or better caching prospects");
        }

        [Fact]
        public async Task ShouldCacheAsync_Should_Handle_Very_Large_Access_Count()
        {
            // Arrange
            var accessPatterns = new[]
            {
                new AccessPattern
                {
                    AccessCount = int.MaxValue,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                    Timestamp = DateTime.UtcNow,
                    RequestKey = "popular",
                    WasCacheHit = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(100),
                    AccessFrequency = 1000.0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    DataVolatility = 0.1,
                    SampleSize = int.MaxValue
                }
            };

            // Act
            var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

            // Assert
            Assert.NotNull(recommendation);
            Assert.True(recommendation.ExpectedHitRate >= 0 && recommendation.ExpectedHitRate <= 1);
        }

        [Fact]
        public async Task ShouldCacheAsync_Should_Handle_Null_RequestType()
        {
            // Arrange
            var accessPatterns = new[]
            {
                new AccessPattern
                {
                    AccessCount = 10,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                    Timestamp = DateTime.UtcNow,
                    RequestKey = "test",
                    WasCacheHit = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(100),
                    AccessFrequency = 2.0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    DataVolatility = 0.1,
                    SampleSize = 10
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _engine.ShouldCacheAsync(null!, accessPatterns));
        }

        [Fact]
        public async Task ShouldCacheAsync_Should_Handle_Null_AccessPatterns()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _engine.ShouldCacheAsync(typeof(TestRequest), null!));
        }

        [Fact]
        public async Task ShouldCacheAsync_Should_Handle_Mixed_Cache_Hit_Patterns()
        {
            // Arrange - Mix of hits and misses
            var accessPatterns = new[]
            {
                new AccessPattern
                {
                    AccessCount = 10,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                    Timestamp = DateTime.UtcNow,
                    RequestKey = "key1",
                    WasCacheHit = true, // Hit
                    ExecutionTime = TimeSpan.FromMilliseconds(10),
                    AccessFrequency = 5.0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(10),
                    DataVolatility = 0.1,
                    SampleSize = 10
                },
                new AccessPattern
                {
                    AccessCount = 10,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                    Timestamp = DateTime.UtcNow,
                    RequestKey = "key2",
                    WasCacheHit = false, // Miss
                    ExecutionTime = TimeSpan.FromMilliseconds(200),
                    AccessFrequency = 5.0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(200),
                    DataVolatility = 0.1,
                    SampleSize = 10
                },
                new AccessPattern
                {
                    AccessCount = 10,
                    TimeSinceLastAccess = TimeSpan.FromMinutes(5),
                    Timestamp = DateTime.UtcNow,
                    RequestKey = "key3",
                    WasCacheHit = true, // Hit
                    ExecutionTime = TimeSpan.FromMilliseconds(10),
                    AccessFrequency = 5.0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(10),
                    DataVolatility = 0.1,
                    SampleSize = 10
                }
            };

            // Act
            var recommendation = await _engine.ShouldCacheAsync(typeof(TestRequest), accessPatterns);

            // Assert
            Assert.NotNull(recommendation);
            Assert.True(recommendation.ExpectedHitRate >= 0 && recommendation.ExpectedHitRate <= 1);
        }

        #region Test Types

        private class TestRequest { }

        #endregion
    }
}