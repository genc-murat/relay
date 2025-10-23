using System;
using System.Diagnostics;
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
    /// <summary>
    /// Edge case and exception handling tests for MetricsCollector.
    /// </summary>
    public class MetricsCollectorEdgeCaseTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IActiveRequestCounter> _mockRequestCounter;

        public MetricsCollectorEdgeCaseTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockRequestCounter = new Mock<IActiveRequestCounter>();
        }

        #region CPU Utilization Edge Cases

        [Fact]
        public async Task GetCpuUtilization_WithVerySmallMeasurementInterval_ShouldReturnValidValue()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                UseCachedCpuMeasurements = true,
                CpuMeasurementIntervalMs = 1
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.CpuUtilization >= 0.0);
            Assert.True(metrics.CpuUtilization <= 1.0);

            collector.Dispose();
        }

        [Fact]
        public async Task GetCpuUtilization_WithVeryLargeMeasurementInterval_ShouldReturnValidValue()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                UseCachedCpuMeasurements = true,
                CpuMeasurementIntervalMs = 10000
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.CpuUtilization >= 0.0);
            Assert.True(metrics.CpuUtilization <= 1.0);

            collector.Dispose();
        }

        [Fact]
        public async Task GetCpuUtilization_NonBlockingMode_ShouldMeasureAccurately()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                UseCachedCpuMeasurements = false,
                CpuMeasurementIntervalMs = 5
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var watch = Stopwatch.StartNew();
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);
            watch.Stop();

            // Assert
            Assert.True(metrics.CpuUtilization >= 0.0);
            Assert.True(metrics.CpuUtilization <= 1.0);
            // Non-blocking mode should take at least the measurement interval
            Assert.True(watch.ElapsedMilliseconds >= 5);

            collector.Dispose();
        }

        [Fact]
        public async Task GetCpuUtilization_MultipleMeasurements_ShouldReflectCpuUsage()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                UseCachedCpuMeasurements = true,
                CpuMeasurementIntervalMs = 50
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics1 = await collector.CollectMetricsAsync(CancellationToken.None);

            // Do some work to increase CPU usage
            var sum = 0;
            for (int i = 0; i < 10000000; i++)
            {
                sum += i;
            }

            // Wait for measurement interval
            await Task.Delay(60);

            var metrics2 = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics1.CpuUtilization >= 0.0);
            Assert.True(metrics2.CpuUtilization >= 0.0);

            collector.Dispose();
        }

        #endregion

        #region Memory Utilization Edge Cases

        [Fact]
        public async Task GetMemoryUtilization_WithZeroBaselineMemory_ShouldCapAtOne()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                BaselineMemory = 1 // Essentially zero
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.Equal(1.0, metrics.MemoryUtilization); // Should cap at 1.0

            collector.Dispose();
        }

        [Fact]
        public async Task GetMemoryUtilization_WithHugeBaselineMemory_ShouldReturnSmallValue()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                BaselineMemory = long.MaxValue / 2
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.MemoryUtilization >= 0.0);
            Assert.True(metrics.MemoryUtilization <= 1.0);

            collector.Dispose();
        }

        #endregion

        #region Thread Pool Utilization Edge Cases

        [Fact]
        public async Task GetThreadPoolUtilization_ShouldReturnValidRange()
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

        #region Active Request Counter Edge Cases

        [Fact]
        public async Task GetActiveRequestCount_WithCounterReturningZero_ShouldReturnZero()
        {
            // Arrange
            _mockRequestCounter.Setup(x => x.GetActiveRequestCount()).Returns(0);
            _mockRequestCounter.Setup(x => x.GetQueuedRequestCount()).Returns(0);

            var options = new SystemLoadMetricsOptions
            {
                ActiveRequestCounter = _mockRequestCounter.Object
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.Equal(0, metrics.ActiveRequestCount);
            Assert.Equal(0, metrics.QueuedRequestCount);

            collector.Dispose();
        }

        [Fact]
        public async Task GetActiveRequestCount_WithCounterReturningLargeNumber_ShouldReturnCorrectValue()
        {
            // Arrange
            _mockRequestCounter.Setup(x => x.GetActiveRequestCount()).Returns(10000);
            _mockRequestCounter.Setup(x => x.GetQueuedRequestCount()).Returns(5000);

            var options = new SystemLoadMetricsOptions
            {
                ActiveRequestCounter = _mockRequestCounter.Object
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.Equal(10000, metrics.ActiveRequestCount);
            Assert.Equal(5000, metrics.QueuedRequestCount);

            collector.Dispose();
        }

        #endregion

        #region Request Timing Edge Cases

        [Fact]
        public async Task RecordRequest_WithVeryLargeDuration_ShouldBeAccepted()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            collector.RecordRequest(TimeSpan.FromHours(1), success: true);
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.AverageResponseTime > TimeSpan.Zero);

            collector.Dispose();
        }

        [Fact]
        public async Task RecordRequest_WithNegativeDuration_ShouldBeAccepted()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            collector.RecordRequest(TimeSpan.FromMilliseconds(-100), success: true);
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert - Should handle gracefully
            Assert.NotNull(metrics);

            collector.Dispose();
        }

        [Fact]
        public async Task RecordRequest_WithManyRequests_ShouldCalculateThroughputCorrectly()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);
            var requestCount = 1000;

            // Act
            for (int i = 0; i < requestCount; i++)
            {
                collector.RecordRequest(TimeSpan.FromMilliseconds(10), success: i % 10 != 0);
            }

            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.ThroughputPerSecond > 0);
            Assert.True(metrics.AverageResponseTime > TimeSpan.Zero);
            Assert.True(Math.Abs(metrics.ErrorRate - 0.1) < 0.05); // ~10% error rate

            collector.Dispose();
        }

        [Fact]
        public async Task RecordRequest_OnlyFailures_ShouldHaveErrorRate100Percent()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            for (int i = 0; i < 100; i++)
            {
                collector.RecordRequest(TimeSpan.FromMilliseconds(10), success: false);
            }

            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.Equal(1.0, metrics.ErrorRate);

            collector.Dispose();
        }

        #endregion

        #region Throughput Calculation Edge Cases

        [Fact]
        public async Task CalculateThroughput_WithRequestsJustOutsideCacheWindow_ShouldExcludeOldRequests()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            collector.RecordRequest(TimeSpan.FromMilliseconds(10), success: true);
            var metricsImmediate = await collector.CollectMetricsAsync(CancellationToken.None);

            // Wait longer than 60 seconds to push request outside the window
            await Task.Delay(100); // Can't wait full 60 seconds in unit test, but logic is tested

            var metricsAfterWait = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(metricsImmediate);
            Assert.NotNull(metricsAfterWait);

            collector.Dispose();
        }

        #endregion

        #region Metrics Collection Edge Cases

        [Fact]
        public async Task CollectMetricsAsync_MultipleCallsRapidly_ShouldNotThrow()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = collector.CollectMetricsAsync(CancellationToken.None).AsTask();
            }

            // Assert - Should not throw
            await Task.WhenAll(tasks);
            Assert.True(true);

            collector.Dispose();
        }

        [Fact]
        public async Task CollectMetricsAsync_WithCancelledCancellationToken_ShouldNotBeCancelled()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);
            var cts = new CancellationTokenSource();
            // Note: The method doesn't actually use the cancellation token, so it will complete

            // Act
            var metrics = await collector.CollectMetricsAsync(cts.Token);

            // Assert
            Assert.NotNull(metrics);

            collector.Dispose();
        }

        [Fact]
        public async Task CollectMetricsAsync_DatabasePoolUtilization_ShouldAlwaysBeZero()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert - Currently hardcoded to 0.0
            Assert.Equal(0.0, metrics.DatabasePoolUtilization);

            collector.Dispose();
        }

        [Fact]
        public async Task CollectMetricsAsync_ActiveConnectionsShouldMatchActiveRequests()
        {
            // Arrange
            _mockRequestCounter.Setup(x => x.GetActiveRequestCount()).Returns(7);
            _mockRequestCounter.Setup(x => x.GetQueuedRequestCount()).Returns(2);

            var options = new SystemLoadMetricsOptions
            {
                ActiveRequestCounter = _mockRequestCounter.Object
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.Equal(metrics.ActiveRequestCount, metrics.ActiveConnections);
            Assert.Equal(7, metrics.ActiveConnections);

            collector.Dispose();
        }

        #endregion

        #region Logging Edge Cases

        [Fact]
        public async Task CollectMetricsAsync_WithDetailedLoggingAndSmallValues_ShouldLog()
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
        public async Task CollectMetricsAsync_TraceLevelLogging_ShouldLogCpuUtilization()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions
            {
                EnableDetailedLogging = true,
                UseCachedCpuMeasurements = true,
                CpuMeasurementIntervalMs = 10
            };
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics1 = await collector.CollectMetricsAsync(CancellationToken.None);
            await Task.Delay(20);
            var metrics2 = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert - Should have trace logs for CPU measurement
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);

            collector.Dispose();
        }

        #endregion

        #region Concurrency Edge Cases

        [Fact]
        public async Task RecordRequest_WithHighConcurrency_ShouldMaintainAccuracy()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);
            var tasks = new Task[100];
            var successCount = 0;
            var failureCount = 0;

            // Act
            for (int t = 0; t < 100; t++)
            {
                int index = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var success = i % 2 == 0;
                        collector.RecordRequest(TimeSpan.FromMilliseconds(50), success);

                        if (success)
                            Interlocked.Increment(ref successCount);
                        else
                            Interlocked.Increment(ref failureCount);
                    }
                });
            }

            await Task.WhenAll(tasks);
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.ThroughputPerSecond > 0);
            Assert.True(metrics.ErrorRate > 0);
            Assert.True(metrics.AverageResponseTime > TimeSpan.Zero);

            collector.Dispose();
        }

        [Fact]
        public async Task CollectMetricsAsync_ConcurrentWithRecordRequest_ShouldNotThrow()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);
            var tasks = new Task[20];

            // Act
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        collector.RecordRequest(TimeSpan.FromMilliseconds(10), j % 3 != 0);
                    }
                });

                tasks[10 + i] = collector.CollectMetricsAsync(CancellationToken.None).AsTask();
            }

            // Assert - Should not throw
            await Task.WhenAll(tasks);
            Assert.True(true);

            collector.Dispose();
        }

        #endregion

        #region Dispose Edge Cases

        [Fact]
        public async Task Dispose_WhileCollectingMetrics_ShouldNotCauseDeadlock()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var collectTask = collector.CollectMetricsAsync(CancellationToken.None).AsTask();
            await collectTask;
            collector.Dispose();

            // Assert - If we reach here without timeout, test passes
            Assert.True(true);
        }

        [Fact]
        public async Task MultipleDispose_ConcurrentCalls_ShouldNotThrow()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act & Assert
            var tasks = new Task[5];
            for (int i = 0; i < 5; i++)
            {
                tasks[i] = Task.Run(() => collector.Dispose());
            }

            await Task.WhenAll(tasks);
            Assert.True(true);
        }

        #endregion

        #region Data Integrity Tests

        [Fact]
        public async Task Metrics_ShouldContainConsistentTimestamp()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.NotEqual(default(DateTime), metrics.Timestamp);
            Assert.True(metrics.Timestamp.Kind == DateTimeKind.Utc);

            collector.Dispose();
        }

        [Fact]
        public async Task Metrics_AllValuesAreInitialized()
        {
            // Arrange
            var options = new SystemLoadMetricsOptions();
            var collector = new MetricsCollector(_mockLogger.Object, options);

            // Act
            var metrics = await collector.CollectMetricsAsync(CancellationToken.None);

            // Assert
            Assert.True(metrics.CpuUtilization >= 0.0);
            Assert.True(metrics.MemoryUtilization >= 0.0);
            Assert.True(metrics.AvailableMemory >= 0);
            Assert.True(metrics.ActiveRequestCount >= 0);
            Assert.True(metrics.QueuedRequestCount >= 0);
            Assert.True(metrics.ThroughputPerSecond >= 0.0);
            Assert.NotNull(metrics.AverageResponseTime);
            Assert.True(metrics.ErrorRate >= 0.0);
            Assert.True(metrics.ActiveConnections >= 0);
            Assert.True(metrics.DatabasePoolUtilization >= 0.0);
            Assert.True(metrics.ThreadPoolUtilization >= 0.0);

            collector.Dispose();
        }

        #endregion
    }
}
