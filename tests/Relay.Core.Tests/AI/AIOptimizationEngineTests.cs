using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Models;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineTests : IDisposable
    {
        private readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
        private readonly AIOptimizationOptions _options;
        private readonly AIOptimizationEngine _engine;

        public AIOptimizationEngineTests()
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
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<AIOptimizationOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AIOptimizationEngine(null!, optionsMock.Object));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Options_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AIOptimizationEngine(_loggerMock.Object, null!));
        }

        [Fact]
        public async Task AnalyzeRequestAsync_Should_Throw_When_Disposed()
        {
            // Arrange
            _engine.Dispose();
            var request = new TestRequest();
            var metrics = CreateMetrics();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await _engine.AnalyzeRequestAsync(request, metrics));
        }

        [Fact]
        public async Task AnalyzeRequestAsync_Should_Return_Recommendation_For_New_Request_Type()
        {
            // Arrange
            var request = new TestRequest();
            var metrics = CreateMetrics();

            // Act
            var recommendation = await _engine.AnalyzeRequestAsync(request, metrics);

            // Assert
            Assert.NotNull(recommendation);
            Assert.True(recommendation.ConfidenceScore >= 0 && recommendation.ConfidenceScore <= 1);
        }

        [Fact]
        public async Task AnalyzeRequestAsync_Should_Update_Request_Analytics()
        {
            // Arrange
            var request = new TestRequest();
            var metrics = CreateMetrics();

            // Act
            await _engine.AnalyzeRequestAsync(request, metrics);
            var stats = _engine.GetModelStatistics();

            // Assert
            Assert.True(stats.TotalPredictions > 0);
        }

        [Fact]
        public async Task PredictOptimalBatchSizeAsync_Should_Throw_When_Disposed()
        {
            // Arrange
            _engine.Dispose();
            var loadMetrics = CreateLoadMetrics();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), loadMetrics));
        }

        [Fact]
        public async Task PredictOptimalBatchSizeAsync_Should_Return_Valid_Batch_Size()
        {
            // Arrange
            var loadMetrics = CreateLoadMetrics();

            // Act
            var batchSize = await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), loadMetrics);

            // Assert
            Assert.True(batchSize >= 1 && batchSize <= _options.MaxBatchSize);
        }

        [Fact]
        public async Task PredictOptimalBatchSizeAsync_Should_Adjust_For_System_Load()
        {
            // Arrange
            var highLoadMetrics = new SystemLoadMetrics
            {
                CpuUtilization = 0.9,
                MemoryUtilization = 0.8,
                ActiveConnections = 1000,
                QueuedRequestCount = 500,
                AvailableMemory = 1024 * 1024 * 1024, // 1GB
                ActiveRequestCount = 100,
                ThroughputPerSecond = 50.0,
                AverageResponseTime = TimeSpan.FromMilliseconds(200),
                ErrorRate = 0.05,
                Timestamp = DateTime.UtcNow,
                DatabasePoolUtilization = 0.9,
                ThreadPoolUtilization = 0.8
            };

            var lowLoadMetrics = new SystemLoadMetrics
            {
                CpuUtilization = 0.1,
                MemoryUtilization = 0.2,
                ActiveConnections = 100,
                QueuedRequestCount = 10,
                AvailableMemory = 4 * 1024 * 1024 * 1024L, // 4GB
                ActiveRequestCount = 20,
                ThroughputPerSecond = 200.0,
                AverageResponseTime = TimeSpan.FromMilliseconds(25),
                ErrorRate = 0.001,
                Timestamp = DateTime.UtcNow,
                DatabasePoolUtilization = 0.2,
                ThreadPoolUtilization = 0.1
            };

            // Act
            var highLoadBatchSize = await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), highLoadMetrics);
            var lowLoadBatchSize = await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), lowLoadMetrics);

            // Assert
            Assert.True(lowLoadBatchSize >= highLoadBatchSize, "Batch size should be larger when system load is lower");
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
        public async Task LearnFromExecutionAsync_Should_Not_Throw_When_Disposed()
        {
            // Arrange
            _engine.Dispose();
            var optimizations = new[] { OptimizationStrategy.Caching };
            var metrics = CreateMetrics();

            // Act & Assert - Should complete without throwing (learning operations fail silently)
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), optimizations, metrics);
        }

        [Fact]
        public async Task LearnFromExecutionAsync_Should_Update_Analytics_When_Learning_Enabled()
        {
            // Arrange
            var optimizations = new[] { OptimizationStrategy.Caching };
            var metrics = CreateMetrics();

            // Act
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), optimizations, metrics);

            // Assert - Learning should have occurred (no exception thrown)
        }

        [Fact]
        public async Task LearnFromExecutionAsync_Should_Do_Nothing_When_Learning_Disabled()
        {
            // Arrange
            _engine.SetLearningMode(false);
            var optimizations = new[] { OptimizationStrategy.Caching };
            var metrics = CreateMetrics();

            // Act
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), optimizations, metrics);

            // Assert - Should complete without throwing
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Throw_When_Disposed()
        {
            // Arrange
            _engine.Dispose();
            var timeWindow = TimeSpan.FromHours(1);

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await _engine.GetSystemInsightsAsync(timeWindow));
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Return_Insights()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(1);

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights);
            Assert.Equal(timeWindow, insights.AnalysisPeriod);
            Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
        }

        [Fact]
        public void SetLearningMode_Should_Update_Learning_State()
        {
            // Act
            _engine.SetLearningMode(false);

            // Assert - Should not throw, state is updated internally
        }

        [Fact]
        public void GetModelStatistics_Should_Return_Statistics()
        {
            // Act
            var stats = _engine.GetModelStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.AccuracyScore >= 0 && stats.AccuracyScore <= 1);
            Assert.Equal(_options.ModelVersion, stats.ModelVersion);
        }

        [Fact]
        public void Dispose_Should_Handle_Multiple_Calls()
        {
            // Act
            _engine.Dispose();
            _engine.Dispose(); // Second call should not throw

            // Assert - No exception thrown
        }

        [Fact]
        public async Task AnalyzeRequestAsync_Should_Handle_Null_Request()
        {
            // Arrange
            var metrics = CreateMetrics();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _engine.AnalyzeRequestAsync<TestRequest>(null!, metrics));
        }

        [Fact]
        public async Task AnalyzeRequestAsync_Should_Handle_Null_Metrics()
        {
            // Arrange
            var request = new TestRequest();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _engine.AnalyzeRequestAsync(request, null!));
        }

        [Fact]
        public async Task AnalyzeRequestAsync_Should_Handle_Extreme_Execution_Times()
        {
            // Arrange
            var request = new TestRequest();
            var extremeMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(0.1), // Very fast
                MedianExecutionTime = TimeSpan.FromMilliseconds(0.05),
                P95ExecutionTime = TimeSpan.FromMilliseconds(0.2),
                P99ExecutionTime = TimeSpan.FromMilliseconds(0.5),
                TotalExecutions = 1000,
                SuccessfulExecutions = 999,
                FailedExecutions = 1,
                MemoryAllocated = 1024,
                ConcurrentExecutions = 1,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(10),
                CpuUsage = 0.01,
                MemoryUsage = 512,
                DatabaseCalls = 0,
                ExternalApiCalls = 0
            };

            // Act
            var recommendation = await _engine.AnalyzeRequestAsync(request, extremeMetrics);

            // Assert
            Assert.NotNull(recommendation);
            Assert.True(recommendation.ConfidenceScore >= 0 && recommendation.ConfidenceScore <= 1);
        }

        [Fact]
        public async Task AnalyzeRequestAsync_Should_Handle_Very_Slow_Requests()
        {
            // Arrange
            var request = new TestRequest();
            var slowMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromSeconds(30), // Very slow
                MedianExecutionTime = TimeSpan.FromSeconds(25),
                P95ExecutionTime = TimeSpan.FromSeconds(45),
                P99ExecutionTime = TimeSpan.FromMinutes(1),
                TotalExecutions = 50,
                SuccessfulExecutions = 45,
                FailedExecutions = 5,
                MemoryAllocated = 1024 * 1024 * 1024, // 1GB
                ConcurrentExecutions = 5,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromHours(1),
                CpuUsage = 0.9,
                MemoryUsage = 1024 * 1024 * 512,
                DatabaseCalls = 10,
                ExternalApiCalls = 5
            };

            // Act
            var recommendation = await _engine.AnalyzeRequestAsync(request, slowMetrics);

            // Assert
            Assert.NotNull(recommendation);
            Assert.True(recommendation.ConfidenceScore >= 0 && recommendation.ConfidenceScore <= 1);
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
        public async Task PredictOptimalBatchSizeAsync_Should_Handle_Zero_Load()
        {
            // Arrange
            var zeroLoadMetrics = new SystemLoadMetrics
            {
                CpuUtilization = 0.0,
                MemoryUtilization = 0.0,
                ActiveConnections = 0,
                QueuedRequestCount = 0,
                AvailableMemory = 8L * 1024 * 1024 * 1024, // 8GB
                ActiveRequestCount = 0,
                ThroughputPerSecond = 0.0,
                AverageResponseTime = TimeSpan.FromMilliseconds(1),
                ErrorRate = 0.0,
                Timestamp = DateTime.UtcNow,
                DatabasePoolUtilization = 0.0,
                ThreadPoolUtilization = 0.0
            };

            // Act
            var batchSize = await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), zeroLoadMetrics);

            // Assert
            Assert.True(batchSize >= 1 && batchSize <= _options.MaxBatchSize);
        }

        [Fact]
        public async Task PredictOptimalBatchSizeAsync_Should_Handle_Maximum_Load()
        {
            // Arrange
            var maxLoadMetrics = new SystemLoadMetrics
            {
                CpuUtilization = 1.0,
                MemoryUtilization = 1.0,
                ActiveConnections = 10000,
                QueuedRequestCount = 5000,
                AvailableMemory = 1024, // 1KB
                ActiveRequestCount = 1000,
                ThroughputPerSecond = 10.0,
                AverageResponseTime = TimeSpan.FromSeconds(10),
                ErrorRate = 0.5,
                Timestamp = DateTime.UtcNow,
                DatabasePoolUtilization = 1.0,
                ThreadPoolUtilization = 1.0
            };

            // Act
            var batchSize = await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), maxLoadMetrics);

            // Assert
            Assert.True(batchSize >= 1 && batchSize <= _options.MaxBatchSize);
            Assert.True(batchSize <= _options.DefaultBatchSize, "Batch size should be reduced under maximum load");
        }

        [Fact]
        public async Task LearnFromExecutionAsync_Should_Handle_Empty_Optimizations_Array()
        {
            // Arrange
            var optimizations = Array.Empty<OptimizationStrategy>();
            var metrics = CreateMetrics();

            // Act & Assert - Should not throw
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), optimizations, metrics);
        }

        [Fact]
        public async Task LearnFromExecutionAsync_Should_Handle_Multiple_Strategies()
        {
            // Arrange
            var optimizations = new[]
            {
                OptimizationStrategy.Caching,
                OptimizationStrategy.BatchProcessing,
                OptimizationStrategy.MemoryPooling
            };
            var metrics = CreateMetrics();

            // Act & Assert - Should not throw
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), optimizations, metrics);
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Handle_Zero_Time_Window()
        {
            // Arrange
            var timeWindow = TimeSpan.Zero;

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights);
            Assert.Equal(timeWindow, insights.AnalysisPeriod);
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Handle_Large_Time_Window()
        {
            // Arrange
            var timeWindow = TimeSpan.FromDays(365);

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights);
            Assert.Equal(timeWindow, insights.AnalysisPeriod);
        }

        [Fact]
        public void GetModelStatistics_Should_Return_Valid_Statistics_After_Learning()
        {
            // Arrange - Perform some operations to generate statistics
            var statsBefore = _engine.GetModelStatistics();

            // Act - Perform some learning operations
            _engine.SetLearningMode(true);

            // Assert
            var statsAfter = _engine.GetModelStatistics();
            Assert.NotNull(statsAfter);
            Assert.Equal(statsBefore.ModelVersion, statsAfter.ModelVersion);
            Assert.True(statsAfter.AccuracyScore >= 0 && statsAfter.AccuracyScore <= 1);
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
        public async Task AnalyzeRequestAsync_Should_Improve_Confidence_With_More_Data()
        {
            // Arrange
            var request = new TestRequest();
            var initialMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                MedianExecutionTime = TimeSpan.FromMilliseconds(95),
                P95ExecutionTime = TimeSpan.FromMilliseconds(150),
                P99ExecutionTime = TimeSpan.FromMilliseconds(200),
                TotalExecutions = 5, // Low sample size
                SuccessfulExecutions = 4,
                FailedExecutions = 1,
                MemoryAllocated = 1024 * 1024,
                ConcurrentExecutions = 2,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(1),
                CpuUsage = 0.45,
                MemoryUsage = 512 * 1024,
                DatabaseCalls = 2,
                ExternalApiCalls = 1
            };

            var matureMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                MedianExecutionTime = TimeSpan.FromMilliseconds(95),
                P95ExecutionTime = TimeSpan.FromMilliseconds(150),
                P99ExecutionTime = TimeSpan.FromMilliseconds(200),
                TotalExecutions = 1000, // High sample size
                SuccessfulExecutions = 950,
                FailedExecutions = 50,
                MemoryAllocated = 1024 * 1024,
                ConcurrentExecutions = 2,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromHours(1),
                CpuUsage = 0.45,
                MemoryUsage = 512 * 1024,
                DatabaseCalls = 2,
                ExternalApiCalls = 1
            };

            // Act
            var initialRecommendation = await _engine.AnalyzeRequestAsync(request, initialMetrics);
            var matureRecommendation = await _engine.AnalyzeRequestAsync(request, matureMetrics);

            // Assert
            Assert.NotNull(initialRecommendation);
            Assert.NotNull(matureRecommendation);
            // More data should generally provide higher confidence (though not guaranteed due to other factors)
            Assert.True(matureRecommendation.ConfidenceScore >= initialRecommendation.ConfidenceScore * 0.9,
                "More data should not significantly reduce confidence");
        }

        [Fact]
        public async Task PredictOptimalBatchSizeAsync_Should_Consider_Request_Type_Characteristics()
        {
            // Arrange
            var fastRequestType = typeof(FastTestRequest);
            var slowRequestType = typeof(SlowTestRequest);

            var loadMetrics = CreateLoadMetrics();

            // First, train with different characteristics
            var fastMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(10), // Fast
                MedianExecutionTime = TimeSpan.FromMilliseconds(8),
                P95ExecutionTime = TimeSpan.FromMilliseconds(20),
                P99ExecutionTime = TimeSpan.FromMilliseconds(50),
                TotalExecutions = 100,
                SuccessfulExecutions = 98,
                FailedExecutions = 2,
                MemoryAllocated = 1024,
                ConcurrentExecutions = 1,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(10),
                CpuUsage = 0.1,
                MemoryUsage = 512,
                DatabaseCalls = 0,
                ExternalApiCalls = 0
            };

            var slowMetrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromSeconds(5), // Slow
                MedianExecutionTime = TimeSpan.FromSeconds(4),
                P95ExecutionTime = TimeSpan.FromSeconds(8),
                P99ExecutionTime = TimeSpan.FromSeconds(15),
                TotalExecutions = 50,
                SuccessfulExecutions = 45,
                FailedExecutions = 5,
                MemoryAllocated = 10 * 1024 * 1024,
                ConcurrentExecutions = 5,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(10),
                CpuUsage = 0.8,
                MemoryUsage = 5 * 1024 * 1024,
                DatabaseCalls = 5,
                ExternalApiCalls = 2
            };

            // Train the engine
            await _engine.AnalyzeRequestAsync<FastTestRequest>(new FastTestRequest(), fastMetrics);
            await _engine.AnalyzeRequestAsync<SlowTestRequest>(new SlowTestRequest(), slowMetrics);

            // Act
            var fastBatchSize = await _engine.PredictOptimalBatchSizeAsync(fastRequestType, loadMetrics);
            var slowBatchSize = await _engine.PredictOptimalBatchSizeAsync(slowRequestType, loadMetrics);

            // Assert
            Assert.True(fastBatchSize >= 1 && fastBatchSize <= _options.MaxBatchSize);
            Assert.True(slowBatchSize >= 1 && slowBatchSize <= _options.MaxBatchSize);
            // Fast requests should generally allow larger batch sizes than slow ones
            Assert.True(fastBatchSize >= slowBatchSize,
                "Fast requests should support larger batch sizes than slow requests");
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Include_All_Required_Components()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(2);

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights);
            Assert.NotNull(insights.Bottlenecks);
            Assert.NotNull(insights.Opportunities);
            Assert.NotNull(insights.Predictions);
            Assert.NotNull(insights.KeyMetrics);
            Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
            Assert.True(insights.PerformanceGrade >= 'A' && insights.PerformanceGrade <= 'F');
        }

        [Fact]
        public async Task Multiple_Request_Types_Should_Be_Handled_Independently()
        {
            // Arrange
            var requestType1 = typeof(RequestType1);
            var requestType2 = typeof(RequestType2);

            // Normal performance - should not trigger optimization
            var metrics1 = CreateMetrics();

            // Very slow performance with high CPU usage - should trigger SIMD optimization
            var metrics2 = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromSeconds(5), // Very slow
                MedianExecutionTime = TimeSpan.FromSeconds(4),
                P95ExecutionTime = TimeSpan.FromSeconds(8),
                P99ExecutionTime = TimeSpan.FromSeconds(15),
                TotalExecutions = 50,
                SuccessfulExecutions = 45,
                FailedExecutions = 5,
                MemoryAllocated = 10 * 1024 * 1024,
                ConcurrentExecutions = 5,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(10),
                CpuUsage = 0.95, // High CPU usage
                MemoryUsage = 5 * 1024 * 1024,
                DatabaseCalls = 5,
                ExternalApiCalls = 2
            };

            // Act
            var recommendation1 = await _engine.AnalyzeRequestAsync(new RequestType1(), metrics1);
            var recommendation2 = await _engine.AnalyzeRequestAsync(new RequestType2(), metrics2);

            // Assert
            Assert.NotNull(recommendation1);
            Assert.NotNull(recommendation2);
            // Different metrics should potentially lead to different strategies
            // At minimum, they should have different confidence scores
            Assert.True(Math.Abs(recommendation1.ConfidenceScore - recommendation2.ConfidenceScore) > 0.01,
                "Different request types should have different confidence scores based on their metrics");
        }

        [Fact]
        public async Task AnalyzeRequestAsync_Should_Handle_Cancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var request = new TestRequest();
            var metrics = CreateMetrics();

            // Act & Assert - TaskCanceledException inherits from OperationCanceledException
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await _engine.AnalyzeRequestAsync(request, metrics, cts.Token));
        }

        [Fact]
        public async Task PredictOptimalBatchSizeAsync_Should_Handle_Null_RequestType()
        {
            // Arrange
            var loadMetrics = CreateLoadMetrics();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _engine.PredictOptimalBatchSizeAsync(null!, loadMetrics));
        }

        [Fact]
        public async Task PredictOptimalBatchSizeAsync_Should_Handle_Null_LoadMetrics()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), null!));
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
        public async Task LearnFromExecutionAsync_Should_Handle_Null_RequestType()
        {
            // Arrange
            var optimizations = new[] { OptimizationStrategy.Caching };
            var metrics = CreateMetrics();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _engine.LearnFromExecutionAsync(null!, optimizations, metrics));
        }

        [Fact]
        public async Task LearnFromExecutionAsync_Should_Handle_Null_Optimizations()
        {
            // Arrange
            var metrics = CreateMetrics();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _engine.LearnFromExecutionAsync(typeof(TestRequest), null!, metrics));
        }

        [Fact]
        public async Task LearnFromExecutionAsync_Should_Handle_Null_Metrics()
        {
            // Arrange
            var optimizations = new[] { OptimizationStrategy.Caching };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _engine.LearnFromExecutionAsync(typeof(TestRequest), optimizations, null!));
        }

        [Fact]
        public async Task AnalyzeRequestAsync_Should_Handle_High_Concurrency()
        {
            // Arrange
            var request = new TestRequest();
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                MedianExecutionTime = TimeSpan.FromMilliseconds(95),
                P95ExecutionTime = TimeSpan.FromMilliseconds(150),
                P99ExecutionTime = TimeSpan.FromMilliseconds(200),
                TotalExecutions = 1000,
                SuccessfulExecutions = 950,
                FailedExecutions = 50,
                MemoryAllocated = 1024 * 1024,
                ConcurrentExecutions = 1000, // Very high concurrency
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(5),
                CpuUsage = 0.95,
                MemoryUsage = 512 * 1024,
                DatabaseCalls = 2,
                ExternalApiCalls = 1
            };

            // Act
            var recommendation = await _engine.AnalyzeRequestAsync(request, metrics);

            // Assert
            Assert.NotNull(recommendation);
            Assert.True(recommendation.ConfidenceScore >= 0 && recommendation.ConfidenceScore <= 1);
        }

        [Fact]
        public async Task AnalyzeRequestAsync_Should_Handle_Zero_Executions()
        {
            // Arrange
            var request = new TestRequest();
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.Zero,
                MedianExecutionTime = TimeSpan.Zero,
                P95ExecutionTime = TimeSpan.Zero,
                P99ExecutionTime = TimeSpan.Zero,
                TotalExecutions = 0,
                SuccessfulExecutions = 0,
                FailedExecutions = 0,
                MemoryAllocated = 0,
                ConcurrentExecutions = 0,
                LastExecution = DateTime.MinValue,
                SamplePeriod = TimeSpan.Zero,
                CpuUsage = 0,
                MemoryUsage = 0,
                DatabaseCalls = 0,
                ExternalApiCalls = 0
            };

            // Act
            var recommendation = await _engine.AnalyzeRequestAsync(request, metrics);

            // Assert
            Assert.NotNull(recommendation);
            Assert.True(recommendation.ConfidenceScore >= 0 && recommendation.ConfidenceScore <= 1);
        }

        [Fact]
        public async Task AnalyzeRequestAsync_Should_Handle_High_Failure_Rate()
        {
            // Arrange
            var request = new TestRequest();
            var metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                MedianExecutionTime = TimeSpan.FromMilliseconds(95),
                P95ExecutionTime = TimeSpan.FromMilliseconds(150),
                P99ExecutionTime = TimeSpan.FromMilliseconds(200),
                TotalExecutions = 100,
                SuccessfulExecutions = 20,
                FailedExecutions = 80, // 80% failure rate
                MemoryAllocated = 1024 * 1024,
                ConcurrentExecutions = 10,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(5),
                CpuUsage = 0.45,
                MemoryUsage = 512 * 1024,
                DatabaseCalls = 2,
                ExternalApiCalls = 1
            };

            // Act
            var recommendation = await _engine.AnalyzeRequestAsync(request, metrics);

            // Assert
            Assert.NotNull(recommendation);
            Assert.True(recommendation.ConfidenceScore >= 0 && recommendation.ConfidenceScore <= 1);
        }

        [Fact]
        public async Task PredictOptimalBatchSizeAsync_Should_Handle_Negative_Memory()
        {
            // Arrange - Test edge case with negative available memory (should be handled gracefully)
            var loadMetrics = new SystemLoadMetrics
            {
                CpuUtilization = 0.5,
                MemoryUtilization = 0.6,
                ActiveConnections = 500,
                QueuedRequestCount = 100,
                AvailableMemory = -1024, // Negative memory
                ActiveRequestCount = 50,
                ThroughputPerSecond = 100.0,
                AverageResponseTime = TimeSpan.FromMilliseconds(50),
                ErrorRate = 0.01,
                Timestamp = DateTime.UtcNow,
                DatabasePoolUtilization = 0.3,
                ThreadPoolUtilization = 0.4
            };

            // Act
            var batchSize = await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), loadMetrics);

            // Assert
            Assert.True(batchSize >= 1 && batchSize <= _options.MaxBatchSize);
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
        public async Task GetSystemInsightsAsync_Should_Handle_Negative_Time_Window()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(-1);

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights);
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Provide_Consistent_Results()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(1);

            // Act
            var insights1 = await _engine.GetSystemInsightsAsync(timeWindow);
            var insights2 = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights1);
            Assert.NotNull(insights2);
            Assert.Equal(insights1.AnalysisPeriod, insights2.AnalysisPeriod);
        }

        [Fact]
        public async Task AnalyzeRequestAsync_Should_Handle_Multiple_Sequential_Calls()
        {
            // Arrange
            var request = new TestRequest();
            var metrics = CreateMetrics();

            // Act - Make multiple sequential calls
            var recommendation1 = await _engine.AnalyzeRequestAsync(request, metrics);
            var recommendation2 = await _engine.AnalyzeRequestAsync(request, metrics);
            var recommendation3 = await _engine.AnalyzeRequestAsync(request, metrics);

            // Assert
            Assert.NotNull(recommendation1);
            Assert.NotNull(recommendation2);
            Assert.NotNull(recommendation3);
            // Confidence should improve or stay stable with more data
            Assert.True(recommendation3.ConfidenceScore >= recommendation1.ConfidenceScore * 0.9);
        }

        [Fact]
        public async Task PredictOptimalBatchSizeAsync_Should_Be_Consistent_For_Same_Input()
        {
            // Arrange
            var loadMetrics = CreateLoadMetrics();

            // Act
            var batchSize1 = await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), loadMetrics);
            var batchSize2 = await _engine.PredictOptimalBatchSizeAsync(typeof(TestRequest), loadMetrics);

            // Assert
            Assert.Equal(batchSize1, batchSize2);
        }

        [Fact]
        public void GetModelStatistics_Should_Update_After_Predictions()
        {
            // Arrange
            var initialStats = _engine.GetModelStatistics();

            // Act - Perform some operations
            var task = _engine.AnalyzeRequestAsync(new TestRequest(), CreateMetrics());
            task.AsTask().Wait();

            var updatedStats = _engine.GetModelStatistics();

            // Assert
            Assert.NotNull(initialStats);
            Assert.NotNull(updatedStats);
            Assert.True(updatedStats.TotalPredictions >= initialStats.TotalPredictions);
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

        #region Helper Methods

        private RequestExecutionMetrics CreateMetrics()
        {
            return new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                MedianExecutionTime = TimeSpan.FromMilliseconds(95),
                P95ExecutionTime = TimeSpan.FromMilliseconds(150),
                P99ExecutionTime = TimeSpan.FromMilliseconds(200),
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                FailedExecutions = 5,
                MemoryAllocated = 1024 * 1024,
                ConcurrentExecutions = 10,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(5),
                CpuUsage = 0.45,
                MemoryUsage = 512 * 1024,
                DatabaseCalls = 2,
                ExternalApiCalls = 1
            };
        }

        private SystemLoadMetrics CreateLoadMetrics()
        {
            return new SystemLoadMetrics
            {
                CpuUtilization = 0.5,
                MemoryUtilization = 0.6,
                ActiveConnections = 500,
                QueuedRequestCount = 100,
                AvailableMemory = 1024 * 1024 * 1024, // 1GB
                ActiveRequestCount = 50,
                ThroughputPerSecond = 100.0,
                AverageResponseTime = TimeSpan.FromMilliseconds(50),
                ErrorRate = 0.01,
                Timestamp = DateTime.UtcNow,
                DatabasePoolUtilization = 0.3,
                ThreadPoolUtilization = 0.4
            };
        }

        #endregion

        #region Test Types

        private class TestRequest { }

        private class FastTestRequest { }

        private class SlowTestRequest { }

        private class RequestType1 { }

        private class RequestType2 { }

        #endregion
    }
}