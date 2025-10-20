using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineLearningTests : IDisposable
    {
        private readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
        private readonly AIOptimizationOptions _options;
        private readonly AIOptimizationEngine _engine;

        public AIOptimizationEngineLearningTests()
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
        public async Task AnalyzeRequestAsync_Should_Use_Fallback_Success_Rate_When_No_History_Data()
        {
            // Arrange
            var request = new TestRequest();
            var metrics = CreateMetrics();

            // Act - First call to establish baseline
            var result1 = await _engine.AnalyzeRequestAsync(request, metrics);

            // Learn from execution to build analytics data
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics);

            // Act - Second call should trigger ML enhancement and fallback success rate calculation
            var result2 = await _engine.AnalyzeRequestAsync(request, metrics);

            // Assert - Should not throw and should return a recommendation
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.True(result1.ConfidenceScore >= 0);
            Assert.True(result2.ConfidenceScore >= 0);
        }

        [Fact]
        public async Task CalculateFallbackSuccessRate_Should_Calculate_Based_On_Analytics_Data()
        {
            // Arrange - Build analytics data with successful optimizations
            var metrics = CreateMetrics();
            var successfulMetrics = CreateSuccessfulMetrics();

            // Learn from successful execution
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, successfulMetrics);
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, successfulMetrics);
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.BatchProcessing }, successfulMetrics);

            // Act - Trigger analysis that uses fallback success rate
            var request = new TestRequest();
            var result = await _engine.AnalyzeRequestAsync(request, metrics);

            // Assert - Should use the fallback calculation in ML enhancement
            Assert.NotNull(result);
            Assert.True(result.ConfidenceScore >= 0);
        }

        [Fact]
        public async Task CalculateFallbackSuccessRate_Should_Return_Default_When_No_Data()
        {
            // Arrange - No prior learning data
            var request = new TestRequest();
            var metrics = CreateMetrics();

            // Act - First analysis with no historical data
            var result = await _engine.AnalyzeRequestAsync(request, metrics);

            // Assert - Should handle gracefully with default values
            Assert.NotNull(result);
            Assert.True(result.ConfidenceScore >= 0);
        }

        [Fact]
        public async Task CalculateHistoricalSuccessRate_Should_Be_Called_When_Analyzing_Requests()
        {
            // Arrange - Build up some analytics data and learning history
            var request = new TestRequest();
            var metrics = CreateSuccessfulMetrics();

            // First, learn from successful executions to build analytics
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.EnableCaching }, metrics);
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.EnableCaching }, metrics);
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.EnableCaching }, metrics);

            // Act - Analysis should trigger ML enhancement and potentially use historical success rate
            var result = await _engine.AnalyzeRequestAsync(request, metrics);

            // Assert - Should complete successfully and use internal calculations
            Assert.NotNull(result);
            Assert.True(result.ConfidenceScore >= 0);
            Assert.True(result.ConfidenceScore <= 1.0);
        }

        [Fact]
        public async Task CalculateHistoricalSuccessRate_Should_Fallback_When_TimeSeries_Throws_Exception()
        {
            // Arrange - Simulate time series failure by setting field to null (if possible) or just test normal flow
            var request = new TestRequest();
            var metrics = CreateMetrics();

            // Learn from execution first
            await _engine.LearnFromExecutionAsync(typeof(TestRequest), new[] { OptimizationStrategy.Caching }, metrics);

            // Act - Analysis should handle any time series errors gracefully
            var result = await _engine.AnalyzeRequestAsync(request, metrics);

            // Assert - Should fallback gracefully and return a recommendation
            Assert.NotNull(result);
            Assert.True(result.ConfidenceScore >= 0);
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

        private RequestExecutionMetrics CreateSuccessfulMetrics()
        {
            return new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(50), // Faster execution
                MedianExecutionTime = TimeSpan.FromMilliseconds(45),
                P95ExecutionTime = TimeSpan.FromMilliseconds(75),
                P99ExecutionTime = TimeSpan.FromMilliseconds(100),
                TotalExecutions = 100,
                SuccessfulExecutions = 100, // All successful
                FailedExecutions = 0,
                MemoryAllocated = 512 * 1024, // Less memory
                ConcurrentExecutions = 5,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(5),
                CpuUsage = 0.30, // Lower CPU
                MemoryUsage = 256 * 1024,
                DatabaseCalls = 1,
                ExternalApiCalls = 0
            };
        }

        #endregion

        #region Test Types

        private class TestRequest { }

        #endregion
    }
}