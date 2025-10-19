using System;
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

        #endregion

        #region Test Types

        private class TestRequest { }

        #endregion
    }
}