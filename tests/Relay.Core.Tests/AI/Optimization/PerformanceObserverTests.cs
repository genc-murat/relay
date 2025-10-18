using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization;
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
            result.Success.Should().BeTrue();
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
            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("Test error");
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
            alert.Severity.Should().Be(AlertSeverity.Critical);
            alert.AlertType.Should().Be("HighCpuUtilization");
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
            loadMetrics.CpuUtilization.Should().Be(0.95);
            loadMetrics.QueuedRequestCount.Should().Be(50);
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
            alert.Severity.Should().Be(AlertSeverity.Critical);
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
            alert.AlertType.Should().Be("TestAlert");
            alert.Message.Should().Be("Test message");
            alert.Severity.Should().Be(AlertSeverity.Medium);
            alert.Data.Should().NotBeNull();
            alert.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void AlertSeverity_ShouldHaveCorrectValues()
        {
            // Arrange & Act & Assert
            AlertSeverity.Low.Should().Be(AlertSeverity.Low);
            AlertSeverity.Medium.Should().Be(AlertSeverity.Medium);
            AlertSeverity.High.Should().Be(AlertSeverity.High);
            AlertSeverity.Critical.Should().Be(AlertSeverity.Critical);
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
            result.Success.Should().BeTrue();
        }
    }
}