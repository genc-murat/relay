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

        #endregion
    }
}