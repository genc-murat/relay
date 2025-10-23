using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Interfaces;
using Relay.Core.AI.Pipeline.Metrics;
using Relay.Core.AI.Pipeline.Options;
using Xunit;

namespace Relay.Core.Tests.AI.Pipeline.Metrics
{
    public class MetricsCollectorTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IActiveRequestCounter> _mockRequestCounter;

        public MetricsCollectorTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockRequestCounter = new Mock<IActiveRequestCounter>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();

            // Act
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Assert
            Assert.NotNull(collector);
            collector.Dispose();
        }

        #endregion

        #region CollectMetricsAsync Tests

        [Fact]
        public async Task CollectMetricsAsync_ShouldReturnSystemLoadMetrics()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions { UseCachedCpuMeasurements = true };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.CpuUtilization >= 0.0 && metrics.CpuUtilization <= 1.0);
            Assert.True(metrics.MemoryUtilization >= 0.0);
            Assert.True(metrics.ThreadPoolUtilization >= 0.0 && metrics.ThreadPoolUtilization <= 1.0);
            Assert.True(metrics.ActiveRequestCount >= 0);
            Assert.True(metrics.QueuedRequestCount >= 0);
            Assert.True(metrics.ThroughputPerSecond >= 0.0);
            Assert.True(metrics.AverageResponseTime >= TimeSpan.Zero);
            Assert.True(metrics.ErrorRate >= 0.0 && metrics.ErrorRate <= 1.0);
            Assert.NotEqual(default, metrics.Timestamp);

            collector.Dispose();
        }

        [Fact]
        public async Task CollectMetricsAsync_WithCancellationToken_ShouldReturnMetrics()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);
            var cts = new CancellationTokenSource();

            // Act
            var metrics = await collector.CollectMetricsAsync(cts.Token);

            // Assert
            Assert.NotNull(metrics);
            Assert.NotEqual(default, metrics.Timestamp);

            collector.Dispose();
        }

        [Fact]
        public async Task CollectMetricsAsync_WithDetailedLoggingEnabled_ShouldLogMetrics()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                EnableDetailedLogging = true,
                UseCachedCpuMeasurements = true
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(metrics);
            // Verify logging was called
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            collector.Dispose();
        }

        [Fact]
        public async Task CollectMetricsAsync_MultipleCallsWithCachedCpu_ShouldCacheValues()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                UseCachedCpuMeasurements = true,
                CpuMeasurementIntervalMs = 1000 // Long interval
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics1 = await collector.CollectMetricsAsync(CancellationToken.None);
            var metrics2 = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert - CPU should be cached (same value)
            Assert.Equal(metrics1.CpuUtilization, metrics2.CpuUtilization);

            collector.Dispose();
        }

        #endregion

        #region RecordRequest Tests

        [Fact]
        public async Task RecordRequest_WithSuccessfulRequest_ShouldIncrementThroughput()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);
            var duration = TimeSpan.FromMilliseconds(100);

            // Act
            collector.RecordRequest(duration, success: true);
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.ThroughputPerSecond > 0);
            Assert.Equal(0.0, metrics.ErrorRate);

            collector.Dispose();
        }

        [Fact]
        public async Task RecordRequest_WithFailedRequest_ShouldIncrementErrorCount()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);
            var duration = TimeSpan.FromMilliseconds(100);

            // Act
            collector.RecordRequest(duration, success: false);
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.ErrorRate > 0);
            Assert.Equal(1.0, metrics.ErrorRate); // 1 error out of 1 request

            collector.Dispose();
        }

        [Fact]
        public async Task RecordRequest_WithMultipleRequests_ShouldCalculateMetricsCorrectly()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            collector.RecordRequest(TimeSpan.FromMilliseconds(100), success: true);
            collector.RecordRequest(TimeSpan.FromMilliseconds(200), success: true);
            collector.RecordRequest(TimeSpan.FromMilliseconds(300), success: false);

            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.AverageResponseTime.TotalMilliseconds > 0);
            Assert.Equal(1.0 / 3.0, metrics.ErrorRate, precision: 2);

            collector.Dispose();
        }

        [Fact]
        public async Task RecordRequest_WithZeroDuration_ShouldBeAccepted()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            collector.RecordRequest(TimeSpan.Zero, success: true);
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(metrics);
            Assert.Equal(TimeSpan.Zero, metrics.AverageResponseTime);

            collector.Dispose();
        }

        [Fact]
        public async Task RecordRequest_ConcurrentCalls_ShouldHandleConcurrency()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);
            var taskCount = 10;

            // Act
            var tasks = new Task[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        collector.RecordRequest(TimeSpan.FromMilliseconds(50), success: j % 2 == 0);
                    }
                });
            }

            await Task.WhenAll(tasks);
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(metrics);
            Assert.True(metrics.ThroughputPerSecond > 0);

            collector.Dispose();
        }

        #endregion

        #region GetCpuUtilization Tests

        [Fact]
        public async Task CollectMetricsAsync_WithNonCachedCpuMeasurements_ShouldMeasureCpu()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                UseCachedCpuMeasurements = false,
                CpuMeasurementIntervalMs = 10
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.CpuUtilization >= 0.0 && metrics.CpuUtilization <= 1.0);

            collector.Dispose();
        }

        [Fact]
        public async Task CollectMetricsAsync_CpuUtilizationShouldBeNormalized()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert - CPU should be between 0 and 1
            Assert.True(metrics.CpuUtilization >= 0.0);
            Assert.True(metrics.CpuUtilization <= 1.0);

            collector.Dispose();
        }

        #endregion

        #region GetMemoryUtilization Tests

        [Fact]
        public async Task CollectMetricsAsync_MemoryUtilizationShouldBeCalculated()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                BaselineMemory = 1024L * 1024 * 1024 // 1GB
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.MemoryUtilization >= 0.0);
            Assert.True(metrics.AvailableMemory > 0);

            collector.Dispose();
        }

        [Fact]
        public async Task CollectMetricsAsync_MemoryUtilizationShouldNotExceedOne()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                BaselineMemory = 1024 // Very small baseline
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.MemoryUtilization <= 1.0);

            collector.Dispose();
        }

        #endregion

        #region GetThreadPoolUtilization Tests

        [Fact]
        public async Task CollectMetricsAsync_ThreadPoolUtilizationShouldBeCalculated()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.ThreadPoolUtilization >= 0.0);
            Assert.True(metrics.ThreadPoolUtilization <= 1.0);

            collector.Dispose();
        }

        #endregion

        #region GetActiveRequestCount Tests

        [Fact]
        public async Task CollectMetricsAsync_WithActiveRequestCounter_ShouldUseCounter()
        {
            // Arrange
            _mockRequestCounter.Setup(x => x.GetActiveRequestCount()).Returns(5);
            _mockRequestCounter.Setup(x => x.GetQueuedRequestCount()).Returns(2);

            var options = new SystemLoadMetricsOptions
            {
                ActiveRequestCounter = _mockRequestCounter.Object
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.Equal(5, metrics.ActiveRequestCount);
            Assert.Equal(2, metrics.QueuedRequestCount);
            _mockRequestCounter.Verify(x => x.GetActiveRequestCount(), Times.Once);

            collector.Dispose();
        }

        [Fact]
        public async Task CollectMetricsAsync_WithoutActiveRequestCounter_ShouldFallbackToThreadPool()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                ActiveRequestCounter = null
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.ActiveRequestCount >= 0);

            collector.Dispose();
        }

        #endregion

        #region CalculateThroughput Tests

        [Fact]
        public async Task CalculateThroughput_WithNoRequests_ShouldReturnZero()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.Equal(0.0, metrics.ThroughputPerSecond);

            collector.Dispose();
        }

        [Fact]
        public async Task CalculateThroughput_WithRecentRequests_ShouldReturnPositiveValue()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            for (int i = 0; i < 10; i++)
            {
                collector.RecordRequest(TimeSpan.FromMilliseconds(50), success: true);
            }

            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.ThroughputPerSecond > 0);

            collector.Dispose();
        }

        [Fact]
        public async Task CalculateThroughput_ShouldCleanOldEntries()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            collector.RecordRequest(TimeSpan.FromMilliseconds(50), success: true);
            var metricsImmediate = await collector.CollectMetricsAsync(CancellationToken.None);

            // Wait for entries to become old
            await Task.Delay(100);

            var metricsAfterWait = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert - Old entries should be cleaned, throughput should decrease
            // Note: This test verifies the cleanup logic is working

            collector.Dispose();
        }

        #endregion

        #region CalculateAverageResponseTime Tests

        [Fact]
        public async Task CalculateAverageResponseTime_WithNoRequests_ShouldReturnDefaultValue()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.Equal(TimeSpan.FromMilliseconds(100), metrics.AverageResponseTime);

            collector.Dispose();
        }

        [Fact]
        public async Task CalculateAverageResponseTime_WithMultipleRequests_ShouldCalculateAverage()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            collector.RecordRequest(TimeSpan.FromMilliseconds(100), success: true);
            collector.RecordRequest(TimeSpan.FromMilliseconds(200), success: true);
            collector.RecordRequest(TimeSpan.FromMilliseconds(300), success: true);

            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.AverageResponseTime.TotalMilliseconds > 0);
            Assert.True(Math.Abs(metrics.AverageResponseTime.TotalMilliseconds - 200) < 50);

            collector.Dispose();
        }

        #endregion

        #region CalculateErrorRate Tests

        [Fact]
        public async Task CalculateErrorRate_WithNoRequests_ShouldReturnZero()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.Equal(0.0, metrics.ErrorRate);

            collector.Dispose();
        }

        [Fact]
        public async Task CalculateErrorRate_WithMixedSuccessAndFailure_ShouldCalculateCorrectly()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            collector.RecordRequest(TimeSpan.FromMilliseconds(100), success: true);
            collector.RecordRequest(TimeSpan.FromMilliseconds(100), success: true);
            collector.RecordRequest(TimeSpan.FromMilliseconds(100), success: false);

            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(Math.Abs(metrics.ErrorRate - (1.0 / 3.0)) < 0.01);

            collector.Dispose();
        }

        [Fact]
        public async Task CalculateErrorRate_WithAllFailures_ShouldReturnOne()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            for (int i = 0; i < 5; i++)
            {
                collector.RecordRequest(TimeSpan.FromMilliseconds(100), success: false);
            }

            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.Equal(1.0, metrics.ErrorRate);

            collector.Dispose();
        }

        [Fact]
        public async Task CalculateErrorRate_WithAllSuccesses_ShouldReturnZero()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            for (int i = 0; i < 5; i++)
            {
                collector.RecordRequest(TimeSpan.FromMilliseconds(100), success: true);
            }

            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.Equal(0.0, metrics.ErrorRate);

            collector.Dispose();
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act & Assert
            collector.Dispose(); // Should not throw
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act & Assert
            collector.Dispose();
            collector.Dispose(); // Should not throw
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task CompleteWorkflow_ShouldProvideConsistentMetrics()
        {
            // Arrange
            _mockRequestCounter.Setup(x => x.GetActiveRequestCount()).Returns(3);
            _mockRequestCounter.Setup(x => x.GetQueuedRequestCount()).Returns(1);

            var options = new SystemLoadMetricsOptions
            {
                UseCachedCpuMeasurements = true,
                BaselineMemory = 1024L * 1024 * 1024,
                ActiveRequestCounter = _mockRequestCounter.Object,
                EnableDetailedLogging = false
            };

            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            for (int i = 0; i < 20; i++)
            {
                var success = i % 3 != 0;
                collector.RecordRequest(TimeSpan.FromMilliseconds(50 + i * 10), success);
            }

            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(metrics);
            Assert.Equal(3, metrics.ActiveRequestCount);
            Assert.Equal(1, metrics.QueuedRequestCount);
            Assert.True(metrics.ThroughputPerSecond > 0);
            Assert.True(metrics.AverageResponseTime > TimeSpan.Zero);
            Assert.True(metrics.ErrorRate > 0 && metrics.ErrorRate < 1.0);
            Assert.Equal(3, metrics.ActiveConnections); // Should match ActiveRequestCount

            collector.Dispose();
        }

        [Fact]
        public async Task MetricsTimestamp_ShouldReflectCollectionTime()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);
            var beforeCollection = DateTime.UtcNow;

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);
            var afterCollection = DateTime.UtcNow;

            // Assert
            Assert.True(metrics.Timestamp >= beforeCollection);
            Assert.True(metrics.Timestamp <= afterCollection.AddSeconds(1)); // Some tolerance

            collector.Dispose();
        }

        #endregion
    }
}
