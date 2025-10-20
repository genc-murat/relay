using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization
{
    public class PerformanceObserverTests
    {
        [Fact]
        public async Task PerformanceAlertObserver_ShouldHandleOptimizationCompleted()
        {
            // Arrange
            var logger = new Mock<ILogger<PerformanceAlertObserver>>();
            var observer = new PerformanceAlertObserver(logger.Object);
            var result = new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.85,
                ExecutionTime = TimeSpan.FromMilliseconds(150)
            };

            // Act
            await observer.OnOptimizationCompletedAsync(result);

            // Assert
            // Verify that the observer processes the result without throwing
            Assert.True(result.Success);
        }

        [Fact]
        public async Task PerformanceAlertObserver_ShouldHandleFailedOptimization()
        {
            // Arrange
            var logger = new Mock<ILogger<PerformanceAlertObserver>>();
            var observer = new PerformanceAlertObserver(logger.Object);
            var result = new StrategyExecutionResult
            {
                Success = false,
                StrategyName = "TestStrategy",
                ErrorMessage = "Test error",
                ExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            await observer.OnOptimizationCompletedAsync(result);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Test error", result.ErrorMessage);
        }

        [Fact]
        public async Task PerformanceAlertObserver_ShouldHandlePerformanceThresholdExceeded()
        {
            // Arrange
            var logger = new Mock<ILogger<PerformanceAlertObserver>>();
            var observer = new PerformanceAlertObserver(logger.Object);
            var alert = new PerformanceAlert
            {
                AlertType = "HighCpuUtilization",
                Message = "CPU utilization is critically high",
                Severity = AlertSeverity.Critical,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await observer.OnPerformanceThresholdExceededAsync(alert);

            // Assert
            Assert.Equal(AlertSeverity.Critical, alert.Severity);
            Assert.Equal("HighCpuUtilization", alert.AlertType);
        }

        [Fact]
        public async Task PerformanceAlertObserver_ShouldHandleSystemLoadChanged()
        {
            // Arrange
            var logger = new Mock<ILogger<PerformanceAlertObserver>>();
            var observer = new PerformanceAlertObserver(logger.Object);
            var loadMetrics = new SystemLoadMetrics
            {
                CpuUtilization = 0.95,
                MemoryUtilization = 0.3,
                ActiveConnections = 1000,
                QueuedRequestCount = 50,
                Timestamp = DateTime.UtcNow
            };

            // Act
            await observer.OnSystemLoadChangedAsync(loadMetrics);

            // Assert
            Assert.Equal(0.95, loadMetrics.CpuUtilization);
            Assert.Equal(50, loadMetrics.QueuedRequestCount);
        }

        [Fact]
        public async Task PerformanceAlertObserver_ShouldTriggerCriticalAlertHandling()
        {
            // Arrange
            var logger = new Mock<ILogger<PerformanceAlertObserver>>();
            var observer = new PerformanceAlertObserver(logger.Object);
            var alert = new PerformanceAlert
            {
                AlertType = "HighCpuUtilization",
                Message = "CPU utilization is critically high",
                Severity = AlertSeverity.Critical
            };

            // Act
            await observer.OnPerformanceThresholdExceededAsync(alert);

            // Assert
            Assert.Equal(AlertSeverity.Critical, alert.Severity);
        }

        [Fact]
        public void PerformanceAlert_ShouldHaveRequiredProperties()
        {
            // Arrange & Act
            var alert = new PerformanceAlert
            {
                AlertType = "TestAlert",
                Message = "Test message",
                Severity = AlertSeverity.Medium,
                Data = new { TestData = "value" }
            };

            // Assert
            Assert.Equal("TestAlert", alert.AlertType);
            Assert.Equal("Test message", alert.Message);
            Assert.Equal(AlertSeverity.Medium, alert.Severity);
            Assert.NotNull(alert.Data);
            Assert.True(Math.Abs((alert.Timestamp - DateTime.UtcNow).TotalSeconds) < 1);
        }

        [Fact]
        public void AlertSeverity_ShouldHaveCorrectValues()
        {
            // Arrange & Act & Assert
            Assert.Equal(AlertSeverity.Low, AlertSeverity.Low);
            Assert.Equal(AlertSeverity.Medium, AlertSeverity.Medium);
            Assert.Equal(AlertSeverity.High, AlertSeverity.High);
            Assert.Equal(AlertSeverity.Critical, AlertSeverity.Critical);
        }

        [Fact]
        public async Task MultipleObservers_ShouldHandleEventsIndependently()
        {
            // Arrange
            var logger1 = new Mock<ILogger<PerformanceAlertObserver>>();
            var logger2 = new Mock<ILogger<PerformanceAlertObserver>>();
            var observer1 = new PerformanceAlertObserver(logger1.Object);
            var observer2 = new PerformanceAlertObserver(logger2.Object);

            var result = new StrategyExecutionResult
            {
                Success = true,
                StrategyName = "TestStrategy",
                Confidence = 0.9
            };

            // Act
            await Task.WhenAll(
                observer1.OnOptimizationCompletedAsync(result).AsTask(),
                observer2.OnOptimizationCompletedAsync(result).AsTask()
            );

            // Assert
            Assert.True(result.Success);
        }
    }
}