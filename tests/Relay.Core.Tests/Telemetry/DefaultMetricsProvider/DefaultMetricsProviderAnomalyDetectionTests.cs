using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class DefaultMetricsProviderAnomalyDetectionTests
{
    private readonly Mock<ILogger<DefaultMetricsProvider>> _loggerMock;
    private readonly TestableDefaultMetricsProvider _metricsProvider;

    public DefaultMetricsProviderAnomalyDetectionTests()
    {
        _loggerMock = new Mock<ILogger<DefaultMetricsProvider>>();
        _metricsProvider = new TestableDefaultMetricsProvider(_loggerMock.Object);
    }

    [Fact]
    public void DetectAnomalies_WithInsufficientData_ShouldReturnEmpty()
    {
        // Arrange - Add less than 10 executions
        for (int i = 0; i < 5; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectAnomalies_WithNoAnomalies_ShouldReturnEmpty()
    {
        // Arrange - Add normal executions
        for (int i = 0; i < 20; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(100 + (i % 5)), // Small variation
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectAnomalies_WithMixedSuccessRates_ShouldNotDetectHighFailureRate_WhenBelowThreshold()
    {
        // Arrange - Create executions with 5% failure rate (below 10% threshold)
        for (int i = 0; i < 100; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = i < 95, // 95 successful, 5 failed = 5% failure rate
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert - Should not detect high failure rate anomaly
        var failureAnomalies = anomalies.Where(a => a.Type == AnomalyType.HighFailureRate).ToList();
        Assert.Empty(failureAnomalies);
    }

    [Fact]
    public void DetectAnomalies_WithHighFailureRate_ShouldDetectHighFailureRateAnomaly()
    {
        // Arrange - Create executions with 15% failure rate (above 10% threshold)
        for (int i = 0; i < 100; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "FailingHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = i < 85, // 85 successful, 15 failed = 15% failure rate
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert - Should detect high failure rate anomaly
        var failureAnomalies = anomalies.Where(a => a.Type == AnomalyType.HighFailureRate).ToList();
        Assert.Single(failureAnomalies);
        var anomaly = failureAnomalies[0];
        Assert.Equal(typeof(TestRequest<string>), anomaly.RequestType);
        Assert.Equal("FailingHandler", anomaly.HandlerName);
        Assert.Contains("failure rate of 15.0%", anomaly.Description);
        Assert.True(Math.Abs(anomaly.Severity - 0.15) < 0.001); // Allow small floating-point precision differences
    }

    [Fact]
    public void DetectAnomalies_WithSlowExecution_ShouldDetectSlowExecutionAnomaly()
    {
        // Arrange - Create baseline executions with normal duration
        for (int i = 0; i < 20; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "SlowHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i - 10) // Older executions
            });
        }

        // Add a very slow recent execution (more than 2x the average)
        _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            OperationId = "slow-operation",
            RequestType = typeof(TestRequest<string>),
            HandlerName = "SlowHandler",
            Duration = TimeSpan.FromMilliseconds(250), // 2.5x slower than 100ms average
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert - Should detect slow execution anomaly
        var slowAnomalies = anomalies.Where(a => a.Type == AnomalyType.SlowExecution).ToList();
        Assert.Single(slowAnomalies);
        var anomaly = slowAnomalies[0];
        Assert.Equal("SlowHandler", anomaly.HandlerName);
        Assert.Contains("took 250.00ms", anomaly.Description);
        Assert.Contains("x the average", anomaly.Description);
        Assert.Equal(TimeSpan.FromMilliseconds(250), anomaly.ActualDuration);
        Assert.True(Math.Abs((anomaly.ExpectedDuration - TimeSpan.FromMilliseconds(107.1428)).TotalMilliseconds) < 0.001); // Average of 21 executions: (20*100 + 250) / 21 ≈ 107.1428 due to tick precision
        Assert.True(Math.Abs(anomaly.Severity - 2.3328) < 0.01); // 250 / 107.1428 ≈ 2.3328
    }

    [Fact]
    public void DetectAnomalies_ShouldOrderBySeverityDescending()
    {
        // Arrange - Create multiple anomalies with different severities
        // High severity anomaly
        for (int i = 0; i < 20; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "HighSeverityHandler",
                Duration = TimeSpan.FromMilliseconds(10), // Very fast baseline
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Add very slow execution (high severity)
        _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            OperationId = "very-slow",
            RequestType = typeof(TestRequest<string>),
            HandlerName = "HighSeverityHandler",
            Duration = TimeSpan.FromMilliseconds(1000), // 100x slower
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Medium severity anomaly
        for (int i = 0; i < 20; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "MediumSeverityHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Add moderately slow execution
        _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            OperationId = "moderately-slow",
            RequestType = typeof(TestRequest<string>),
            HandlerName = "MediumSeverityHandler",
            Duration = TimeSpan.FromMilliseconds(250), // 2.5x slower
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert
        Assert.Equal(2, anomalies.Count);
        Assert.True(anomalies[0].Severity > anomalies[1].Severity); // First should be more severe
        Assert.Equal("very-slow", anomalies[0].OperationId);
        Assert.Equal("moderately-slow", anomalies[1].OperationId);
    }
}