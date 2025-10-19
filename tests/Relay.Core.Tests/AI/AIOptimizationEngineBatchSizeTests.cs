using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineBatchSizeTests : IDisposable
    {
        private readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
        private readonly AIOptimizationOptions _options;
        private readonly AIOptimizationEngine _engine;

        public AIOptimizationEngineBatchSizeTests()
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

        #region Helper Methods

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

        #endregion
    }
}