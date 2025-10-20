using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.Telemetry;
using Relay.Core.Tests.Testing;
using Xunit;

namespace Relay.Core.Tests;

public class MetricsTests
{
    private readonly DefaultMetricsProvider _metricsProvider;
    private readonly ILogger<DefaultMetricsProvider> _logger;

    public MetricsTests()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<DefaultMetricsProvider>();
        _metricsProvider = new DefaultMetricsProvider(_logger);
    }

    [Fact]
    public void RecordHandlerExecution_ShouldStoreMetrics()
    {
        // Arrange
        var metrics = new HandlerExecutionMetrics
        {
            OperationId = "test-op-1",
            RequestType = typeof(TestRequest<string>),
            ResponseType = typeof(string),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(100),
            Success = true
        };

        // Act
        _metricsProvider.RecordHandlerExecution(metrics);

        // Assert
        var stats = _metricsProvider.GetHandlerExecutionStats(typeof(TestRequest<string>), "TestHandler");
        Assert.Equal(1, stats.TotalExecutions);
        Assert.Equal(1, stats.SuccessfulExecutions);
        Assert.Equal(0, stats.FailedExecutions);
        Assert.Equal(1.0, stats.SuccessRate);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.AverageExecutionTime);
    }

    [Fact]
    public void RecordNotificationPublish_ShouldStoreMetrics()
    {
        // Arrange
        var metrics = new NotificationPublishMetrics
        {
            OperationId = "test-notification-1",
            NotificationType = typeof(TestNotification),
            HandlerCount = 3,
            Duration = TimeSpan.FromMilliseconds(50),
            Success = true
        };

        // Act
        _metricsProvider.RecordNotificationPublish(metrics);

        // Assert
        var stats = _metricsProvider.GetNotificationPublishStats(typeof(TestNotification));
        Assert.Equal(1, stats.TotalPublishes);
        Assert.Equal(1, stats.SuccessfulPublishes);
        Assert.Equal(0, stats.FailedPublishes);
        Assert.Equal(1.0, stats.SuccessRate);
        Assert.Equal(3.0, stats.AverageHandlerCount);
    }

    [Fact]
    public void RecordStreamingOperation_ShouldStoreMetrics()
    {
        // Arrange
        var metrics = new StreamingOperationMetrics
        {
            OperationId = "test-stream-1",
            RequestType = typeof(TestStreamRequest<string>),
            ResponseType = typeof(string),
            HandlerName = "StreamHandler",
            Duration = TimeSpan.FromMilliseconds(200),
            ItemCount = 100,
            Success = true
        };

        // Act
        _metricsProvider.RecordStreamingOperation(metrics);

        // Assert
        var stats = _metricsProvider.GetStreamingOperationStats(typeof(TestStreamRequest<string>), "StreamHandler");
        Assert.Equal(1, stats.TotalOperations);
        Assert.Equal(1, stats.SuccessfulOperations);
        Assert.Equal(0, stats.FailedOperations);
        Assert.Equal(100, stats.TotalItemsStreamed);
        Assert.Equal(100.0, stats.AverageItemsPerOperation);
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
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(50),
                Success = true
            },
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true
            },
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(150),
                Success = false,
                Exception = new InvalidOperationException("Test error")
            },
            new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(200),
                Success = true
            }
        };

        // Act
        foreach (var execution in executions)
        {
            _metricsProvider.RecordHandlerExecution(execution);
        }

        // Assert
        var stats = _metricsProvider.GetHandlerExecutionStats(typeof(TestRequest<string>), "TestHandler");
        Assert.Equal(4, stats.TotalExecutions);
        Assert.Equal(3, stats.SuccessfulExecutions);
        Assert.Equal(1, stats.FailedExecutions);
        Assert.Equal(0.75, stats.SuccessRate);
        Assert.Equal(TimeSpan.FromMilliseconds(125), stats.AverageExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(50), stats.MinExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(200), stats.MaxExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.P50ExecutionTime);
    }

    [Fact]
    public void DetectAnomalies_WithSlowExecution_ShouldDetectAnomaly()
    {
        // Arrange - Create baseline executions
        for (int i = 0; i < 20; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "TestHandler",
                Duration = TimeSpan.FromMilliseconds(100 + (i % 10)), // 100-109ms baseline
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Add a slow execution
        _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
        {
            OperationId = "slow-execution",
            RequestType = typeof(TestRequest<string>),
            HandlerName = "TestHandler",
            Duration = TimeSpan.FromMilliseconds(300), // 3x slower than average
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert
        Assert.NotEmpty(anomalies);
        var slowAnomaly = anomalies.FirstOrDefault(a => a.Type == AnomalyType.SlowExecution);
        Assert.NotNull(slowAnomaly);
        Assert.Equal("slow-execution", slowAnomaly.OperationId);
        Assert.Equal(typeof(TestRequest<string>), slowAnomaly.RequestType);
        Assert.True(slowAnomaly.Severity > 2.0); // Should be significantly higher than threshold
    }

    [Fact]
    public void DetectAnomalies_WithHighFailureRate_ShouldDetectAnomaly()
    {
        // Arrange - Create executions with high failure rate
        for (int i = 0; i < 15; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                RequestType = typeof(TestRequest<string>),
                HandlerName = "FailingHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = i < 5, // 5 successful, 10 failed = 66% failure rate
                Exception = i >= 5 ? new InvalidOperationException("Test error") : null,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-i)
            });
        }

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1)).ToList();

        // Assert
        Assert.NotEmpty(anomalies);
        var failureAnomaly = anomalies.FirstOrDefault(a => a.Type == AnomalyType.HighFailureRate);
        Assert.NotNull(failureAnomaly);
        Assert.Equal(typeof(TestRequest<string>), failureAnomaly.RequestType);
        Assert.Equal("FailingHandler", failureAnomaly.HandlerName);
        Assert.True(failureAnomaly.Severity > 0.5); // High failure rate
    }

    [Fact]
    public void RecordTimingBreakdown_ShouldStoreAndRetrieveBreakdown()
    {
        // Arrange
        var breakdown = new TimingBreakdown
        {
            OperationId = "test-breakdown",
            TotalDuration = TimeSpan.FromMilliseconds(200),
            PhaseTimings = new Dictionary<string, TimeSpan>
            {
                ["Validation"] = TimeSpan.FromMilliseconds(20),
                ["Processing"] = TimeSpan.FromMilliseconds(150),
                ["Serialization"] = TimeSpan.FromMilliseconds(30)
            },
            Metadata = new Dictionary<string, object>
            {
                ["RequestSize"] = 1024,
                ["ResponseSize"] = 2048
            }
        };

        // Act
        _metricsProvider.RecordTimingBreakdown(breakdown);
        var retrieved = _metricsProvider.GetTimingBreakdown("test-breakdown");

        // Assert
        Assert.Equal(breakdown.OperationId, retrieved.OperationId);
        Assert.Equal(breakdown.TotalDuration, retrieved.TotalDuration);
        Assert.Equal(3, retrieved.PhaseTimings.Count);
        Assert.Equal(TimeSpan.FromMilliseconds(20), retrieved.PhaseTimings["Validation"]);
        Assert.Equal(TimeSpan.FromMilliseconds(150), retrieved.PhaseTimings["Processing"]);
        Assert.Equal(TimeSpan.FromMilliseconds(30), retrieved.PhaseTimings["Serialization"]);
        Assert.Equal(2, retrieved.Metadata.Count);
        Assert.Equal(1024, retrieved.Metadata["RequestSize"]);
        Assert.Equal(2048, retrieved.Metadata["ResponseSize"]);
    }

    [Fact]
    public void GetHandlerExecutionStats_WithNoData_ShouldReturnEmptyStats()
    {
        // Act
        var stats = _metricsProvider.GetHandlerExecutionStats(typeof(TestRequest<string>), "NonExistentHandler");

        // Assert
        Assert.Equal(typeof(TestRequest<string>), stats.RequestType);
        Assert.Equal("NonExistentHandler", stats.HandlerName);
        Assert.Equal(0, stats.TotalExecutions);
        Assert.Equal(0, stats.SuccessfulExecutions);
        Assert.Equal(0, stats.FailedExecutions);
        Assert.Equal(0, stats.SuccessRate);
    }

    [Fact]
    public void GetTimingBreakdown_WithNonExistentId_ShouldReturnEmptyBreakdown()
    {
        // Act
        var breakdown = _metricsProvider.GetTimingBreakdown("non-existent");

        // Assert
        Assert.Equal("non-existent", breakdown.OperationId);
        Assert.Equal(TimeSpan.Zero, breakdown.TotalDuration);
        Assert.Empty(breakdown.PhaseTimings);
        Assert.Empty(breakdown.Metadata);
    }

    [Fact]
    public async Task MetricsProvider_ShouldHandleConcurrentAccess()
    {
        // Arrange
        var tasks = new List<Task>();
        const int concurrentOperations = 100;

        // Act - Simulate concurrent metric recording
        for (int i = 0; i < concurrentOperations; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
                {
                    OperationId = $"concurrent-{index}",
                    RequestType = typeof(TestRequest<string>),
                    HandlerName = "ConcurrentHandler",
                    Duration = TimeSpan.FromMilliseconds(100 + index),
                    Success = index % 2 == 0,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }));
        }

        await Task.WhenAll(tasks.ToArray());

        // Assert
        var stats = _metricsProvider.GetHandlerExecutionStats(typeof(TestRequest<string>), "ConcurrentHandler");
        Assert.Equal(concurrentOperations, stats.TotalExecutions);
        Assert.Equal(concurrentOperations / 2, stats.SuccessfulExecutions);
        Assert.Equal(concurrentOperations / 2, stats.FailedExecutions);
        Assert.Equal(0.5, stats.SuccessRate);
    }

    [Fact]
    public void MetricsProvider_ShouldLimitMemoryUsage()
    {
        // Arrange - Record more than the limit (1000) to test memory management
        const int recordCount = 1500;

        // Act
        for (int i = 0; i < recordCount; i++)
        {
            _metricsProvider.RecordHandlerExecution(new HandlerExecutionMetrics
            {
                OperationId = $"memory-test-{i}",
                RequestType = typeof(TestRequest<string>),
                HandlerName = "MemoryTestHandler",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddSeconds(-i)
            });
        }

        // Assert - Should have limited the stored records
        var stats = _metricsProvider.GetHandlerExecutionStats(typeof(TestRequest<string>), "MemoryTestHandler");
        Assert.True(stats.TotalExecutions <= 1000, $"Expected <= 1000 executions, got {stats.TotalExecutions}");
    }
}

