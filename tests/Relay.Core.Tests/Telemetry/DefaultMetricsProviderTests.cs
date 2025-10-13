using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

/// <summary>
/// Test class for DefaultMetricsProvider with access to protected methods
/// </summary>
public class TestableDefaultMetricsProvider : DefaultMetricsProvider
{
    public TestableDefaultMetricsProvider(ILogger<DefaultMetricsProvider>? logger = null)
        : base(logger)
    {
    }

    public new IEnumerable<List<HandlerExecutionMetrics>> GetHandlerExecutionsSnapshot(DateTimeOffset cutoff)
    {
        return base.GetHandlerExecutionsSnapshot(cutoff);
    }

    public new IEnumerable<List<StreamingOperationMetrics>> GetStreamingOperationsSnapshot(DateTimeOffset cutoff)
    {
        return base.GetStreamingOperationsSnapshot(cutoff);
    }
}

public class DefaultMetricsProviderTests
{
    private readonly Mock<ILogger<DefaultMetricsProvider>> _loggerMock;
    private readonly TestableDefaultMetricsProvider _metricsProvider;

    public DefaultMetricsProviderTests()
    {
        _loggerMock = new Mock<ILogger<DefaultMetricsProvider>>();
        _metricsProvider = new TestableDefaultMetricsProvider(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldInitializeCorrectly()
    {
        // Act
        var provider = new DefaultMetricsProvider(null);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_WithLogger_ShouldInitializeCorrectly()
    {
        // Act
        var provider = new DefaultMetricsProvider(_loggerMock.Object);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void GetNotificationPublishStats_WithNoData_ShouldReturnEmptyStats()
    {
        // Act
        var stats = _metricsProvider.GetNotificationPublishStats(typeof(TestNotification));

        // Assert
        Assert.Equal(typeof(TestNotification), stats.NotificationType);
        Assert.Equal(0, stats.TotalPublishes);
        Assert.Equal(0, stats.SuccessfulPublishes);
        Assert.Equal(0, stats.FailedPublishes);
        Assert.Equal(0, stats.SuccessRate);
    }

    [Fact]
    public void GetStreamingOperationStats_WithNoData_ShouldReturnEmptyStats()
    {
        // Act
        var stats = _metricsProvider.GetStreamingOperationStats(typeof(TestStreamRequest<string>), "TestHandler");

        // Assert
        Assert.Equal(typeof(TestStreamRequest<string>), stats.RequestType);
        Assert.Equal("TestHandler", stats.HandlerName);
        Assert.Equal(0, stats.TotalOperations);
        Assert.Equal(0, stats.SuccessfulOperations);
        Assert.Equal(0, stats.FailedOperations);
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
    public void RecordTimingBreakdown_ShouldCleanupOldBreakdowns_WhenLimitExceeded()
    {
        // Arrange - Record more than the limit (10000) to test cleanup
        const int recordCount = 11000;
        const int expectedCleanupCount = 1000; // recordCount - MaxTimingBreakdowns

        for (int i = 0; i < recordCount; i++)
        {
            var breakdown = new TimingBreakdown
            {
                OperationId = $"cleanup-test-{i}",
                TotalDuration = TimeSpan.FromMilliseconds(100)
            };
            _metricsProvider.RecordTimingBreakdown(breakdown);
        }

        // Act - Try to retrieve some breakdowns
        var firstBreakdown = _metricsProvider.GetTimingBreakdown("cleanup-test-0");
        var middleBreakdown = _metricsProvider.GetTimingBreakdown("cleanup-test-500");
        var lastBreakdown = _metricsProvider.GetTimingBreakdown($"cleanup-test-{recordCount - 1}");

        // Assert - Some old entries should be cleaned up, newer ones should remain
        // The first entry should either be cleaned up (empty breakdown) or still exist
        Assert.Equal("cleanup-test-0", firstBreakdown.OperationId);
        Assert.Equal("cleanup-test-500", middleBreakdown.OperationId);
        Assert.Equal($"cleanup-test-{recordCount - 1}", lastBreakdown.OperationId);

        // The last entry should definitely exist with the correct duration
        Assert.Equal(TimeSpan.FromMilliseconds(100), lastBreakdown.TotalDuration);

        // At least some cleanup should have happened (we can't predict exactly which entries are removed
        // since the cleanup removes the "oldest" keys, but since we add them in order, the first ones should be removed)
        var hasCleanupOccurred = firstBreakdown.TotalDuration == TimeSpan.Zero ||
                                middleBreakdown.TotalDuration == TimeSpan.Zero;
        Assert.True(hasCleanupOccurred, "Some timing breakdowns should have been cleaned up when limit exceeded");
    }

    [Fact]
    public void GetHandlerExecutionsSnapshot_ShouldReturnFilteredData()
    {
        // Arrange
        var oldTime = DateTimeOffset.UtcNow.AddHours(-2);
        var newTime = DateTimeOffset.UtcNow;

        // Add old execution
        _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            RequestType = typeof(TestRequest<string>),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(100),
            Success = true,
            Timestamp = oldTime
        });

        // Add new execution
        _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            RequestType = typeof(TestRequest<string>),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(200),
            Success = true,
            Timestamp = newTime
        });

        // Act
        var snapshots = _metricsProvider.GetHandlerExecutionsSnapshot(DateTimeOffset.UtcNow.AddHours(-1)).ToList();

        // Assert
        Assert.Single(snapshots);
        var snapshot = snapshots[0];
        Assert.Single(snapshot);
        Assert.Equal(TimeSpan.FromMilliseconds(200), snapshot[0].Duration);
        Assert.Equal(newTime, snapshot[0].Timestamp);
    }

    [Fact]
    public void GetStreamingOperationsSnapshot_ShouldReturnFilteredData()
    {
        // Arrange
        var oldTime = DateTimeOffset.UtcNow.AddHours(-2);
        var newTime = DateTimeOffset.UtcNow;

        // Add old operation
        _metricsProvider.RecordStreamingOperation(new StreamingOperationMetrics
        {
            RequestType = typeof(TestStreamRequest<string>),
            ResponseType = typeof(string),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(100),
            ItemCount = 10,
            Success = true,
            Timestamp = oldTime
        });

        // Add new operation
        _metricsProvider.RecordStreamingOperation(new StreamingOperationMetrics
        {
            RequestType = typeof(TestStreamRequest<string>),
            ResponseType = typeof(string),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(200),
            ItemCount = 20,
            Success = true,
            Timestamp = newTime
        });

        // Act
        var snapshots = _metricsProvider.GetStreamingOperationsSnapshot(DateTimeOffset.UtcNow.AddHours(-1)).ToList();

        // Assert
        Assert.Single(snapshots);
        var snapshot = snapshots[0];
        Assert.Single(snapshot);
        Assert.Equal(20, snapshot[0].ItemCount);
        Assert.Equal(newTime, snapshot[0].Timestamp);
    }

    [Fact]
    public void GetHandlerExecutionsSnapshot_WithNoRecentData_ShouldReturnEmpty()
    {
        // Arrange
        _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            RequestType = typeof(TestRequest<string>),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(100),
            Success = true,
            Timestamp = DateTimeOffset.UtcNow.AddHours(-2)
        });

        // Act
        var snapshots = _metricsProvider.GetHandlerExecutionsSnapshot(DateTimeOffset.UtcNow.AddHours(-1)).ToList();

        // Assert
        Assert.Empty(snapshots);
    }

    [Fact]
    public void GetStreamingOperationsSnapshot_WithNoRecentData_ShouldReturnEmpty()
    {
        // Arrange
        _metricsProvider.RecordStreamingOperation(new StreamingOperationMetrics
        {
            RequestType = typeof(TestStreamRequest<string>),
            ResponseType = typeof(string),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(100),
            ItemCount = 10,
            Success = true,
            Timestamp = DateTimeOffset.UtcNow.AddHours(-2)
        });

        // Act
        var snapshots = _metricsProvider.GetStreamingOperationsSnapshot(DateTimeOffset.UtcNow.AddHours(-1)).ToList();

        // Assert
        Assert.Empty(snapshots);
    }

    [Fact]
    public void RecordHandlerExecution_ShouldLogDebugMessage()
    {
        // Arrange
        var metrics = new HandlerExecutionMetrics
        {
            RequestType = typeof(TestRequest<string>),
            ResponseType = typeof(string),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(150),
            Success = true
        };

        // Act
        _metricsProvider.RecordHandlerExecution(metrics);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded handler execution: TestRequest`1 -> String in 150ms (Success: True)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordNotificationPublish_ShouldLogDebugMessage()
    {
        // Arrange
        var metrics = new NotificationPublishMetrics
        {
            NotificationType = typeof(TestNotification),
            HandlerCount = 5,
            Duration = TimeSpan.FromMilliseconds(75),
            Success = true
        };

        // Act
        _metricsProvider.RecordNotificationPublish(metrics);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded notification publish: TestNotification to 5 handlers in 75ms (Success: True)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordStreamingOperation_ShouldLogDebugMessage()
    {
        // Arrange
        var metrics = new StreamingOperationMetrics
        {
            RequestType = typeof(TestStreamRequest<string>),
            ResponseType = typeof(string),
            HandlerName = "StreamHandler",
            Duration = TimeSpan.FromMilliseconds(300),
            ItemCount = 50,
            Success = true
        };

        // Act
        _metricsProvider.RecordStreamingOperation(metrics);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded streaming operation: TestStreamRequest`1 -> String (50 items) in 300ms (Success: True)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetNotificationPublishStats_WithMultiplePublishes_ShouldCalculateCorrectStats()
    {
        // Arrange
        var publishes = new[]
        {
            new NotificationPublishMetrics
            {
                NotificationType = typeof(TestNotification),
                HandlerCount = 2,
                Duration = TimeSpan.FromMilliseconds(50),
                Success = true
            },
            new NotificationPublishMetrics
            {
                NotificationType = typeof(TestNotification),
                HandlerCount = 3,
                Duration = TimeSpan.FromMilliseconds(75),
                Success = true
            },
            new NotificationPublishMetrics
            {
                NotificationType = typeof(TestNotification),
                HandlerCount = 1,
                Duration = TimeSpan.FromMilliseconds(100),
                Success = false,
                Exception = new Exception("Publish failed")
            }
        };

        // Act
        foreach (var publish in publishes)
        {
            _metricsProvider.RecordNotificationPublish(publish);
        }

        // Assert
        var stats = _metricsProvider.GetNotificationPublishStats(typeof(TestNotification));
        Assert.Equal(3, stats.TotalPublishes);
        Assert.Equal(2, stats.SuccessfulPublishes);
        Assert.Equal(1, stats.FailedPublishes);
        Assert.Equal(2.0 / 3.0, stats.SuccessRate, 3);
        Assert.Equal(TimeSpan.FromMilliseconds(75), stats.AveragePublishTime);
        Assert.Equal(TimeSpan.FromMilliseconds(50), stats.MinPublishTime);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.MaxPublishTime);
        Assert.Equal(2.0, stats.AverageHandlerCount);
    }

    [Fact]
    public void GetStreamingOperationStats_WithMultipleOperations_ShouldCalculateCorrectStats()
    {
        // Arrange
        var operations = new[]
        {
            new StreamingOperationMetrics
            {
                RequestType = typeof(TestStreamRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "StreamHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                ItemCount = 10,
                Success = true
            },
            new StreamingOperationMetrics
            {
                RequestType = typeof(TestStreamRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "StreamHandler",
                Duration = TimeSpan.FromMilliseconds(200),
                ItemCount = 20,
                Success = true
            },
            new StreamingOperationMetrics
            {
                RequestType = typeof(TestStreamRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "StreamHandler",
                Duration = TimeSpan.FromMilliseconds(150),
                ItemCount = 15,
                Success = false,
                Exception = new Exception("Stream failed")
            }
        };

        // Act
        foreach (var operation in operations)
        {
            _metricsProvider.RecordStreamingOperation(operation);
        }

        // Assert
        var stats = _metricsProvider.GetStreamingOperationStats(typeof(TestStreamRequest<string>), "StreamHandler");
        Assert.Equal(3, stats.TotalOperations);
        Assert.Equal(2, stats.SuccessfulOperations);
        Assert.Equal(1, stats.FailedOperations);
        Assert.Equal(2.0 / 3.0, stats.SuccessRate, 3);
        Assert.Equal(TimeSpan.FromMilliseconds(150), stats.AverageOperationTime);
        Assert.Equal(45, stats.TotalItemsStreamed);
        Assert.Equal(15.0, stats.AverageItemsPerOperation);
        Assert.Equal(45.0 / (100 + 200 + 150) * 1000, stats.ItemsPerSecond, 1); // Items per second
    }

    [Fact]
    public void GetStreamingOperationStats_WithZeroDuration_ShouldHandleDivisionByZero()
    {
        // Arrange
        _metricsProvider.RecordStreamingOperation(new StreamingOperationMetrics
        {
            RequestType = typeof(TestStreamRequest<string>),
            ResponseType = typeof(string),
            HandlerName = "StreamHandler",
            Duration = TimeSpan.Zero,
            ItemCount = 10,
            Success = true
        });

        // Act
        var stats = _metricsProvider.GetStreamingOperationStats(typeof(TestStreamRequest<string>), "StreamHandler");

        // Assert
        Assert.Equal(0, stats.ItemsPerSecond);
    }

    [Fact]
    public void GetHandlerExecutionStats_WithMultipleExecutions_ShouldCalculateCorrectStats()
    {
        // Arrange
        var executions = new[]
        {
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
            },
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(200),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-4)
            },
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(150),
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-3)
            },
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(300),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-2)
            },
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                ResponseType = typeof(string),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(250),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
            }
        };

        // Act
        foreach (var execution in executions)
        {
            _metricsProvider.RecordHandlerExecution(execution);
        }

        // Assert
        var stats = _metricsProvider.GetHandlerExecutionStats(typeof(TestRequest<string>), "TestHandler");
        Assert.Equal(5, stats.TotalExecutions);
        Assert.Equal(4, stats.SuccessfulExecutions);
        Assert.Equal(1, stats.FailedExecutions);
        Assert.Equal(4.0 / 5.0, stats.SuccessRate, 3);
        Assert.Equal(TimeSpan.FromMilliseconds(200), stats.AverageExecutionTime); // (100+200+150+300+250)/5 = 200
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.MinExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(300), stats.MaxExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(200), stats.P50ExecutionTime); // Median of sorted: 100,150,200,250,300 -> 200
        Assert.Equal(TimeSpan.FromMilliseconds(300), stats.P95ExecutionTime); // 95th percentile
        Assert.Equal(TimeSpan.FromMilliseconds(300), stats.P99ExecutionTime); // 99th percentile
    }

    [Fact]
    public void GetTimingBreakdown_WithExistingBreakdown_ShouldReturnBreakdown()
    {
        // Arrange
        var breakdown = new TimingBreakdown
        {
            OperationId = "test-operation",
            TotalDuration = TimeSpan.FromMilliseconds(500),
            PhaseTimings = new Dictionary<string, TimeSpan>
            {
                ["Handler"] = TimeSpan.FromMilliseconds(300),
                ["Validation"] = TimeSpan.FromMilliseconds(100),
                ["Pipeline"] = TimeSpan.FromMilliseconds(100)
            }
        };
        _metricsProvider.RecordTimingBreakdown(breakdown);

        // Act
        var retrieved = _metricsProvider.GetTimingBreakdown("test-operation");

        // Assert
        Assert.Equal("test-operation", retrieved.OperationId);
        Assert.Equal(TimeSpan.FromMilliseconds(500), retrieved.TotalDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(300), retrieved.PhaseTimings["Handler"]);
        Assert.Equal(TimeSpan.FromMilliseconds(100), retrieved.PhaseTimings["Validation"]);
        Assert.Equal(TimeSpan.FromMilliseconds(100), retrieved.PhaseTimings["Pipeline"]);
    }

    [Fact]
    public void GetTimingBreakdown_WithNonExistingBreakdown_ShouldReturnEmptyBreakdown()
    {
        // Act
        var retrieved = _metricsProvider.GetTimingBreakdown("non-existing-operation");

        // Assert
        Assert.Equal("non-existing-operation", retrieved.OperationId);
        Assert.Equal(TimeSpan.Zero, retrieved.TotalDuration);
        Assert.Empty(retrieved.PhaseTimings);
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

// Test classes
public class TestRequest<T> { }
public class TestStreamRequest<T> { }