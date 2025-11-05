using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Telemetry;
using Relay.Core.Testing;

public class DefaultMetricsProviderCoverageExtensionTests
{
    private readonly Mock<ILogger<Relay.Core.Telemetry.DefaultMetricsProvider>> _loggerMock;
    private readonly Relay.Core.Telemetry.DefaultMetricsProvider _metricsProvider;

    public DefaultMetricsProviderCoverageExtensionTests()
    {
        _loggerMock = new Mock<ILogger<Relay.Core.Telemetry.DefaultMetricsProvider>>();
        _metricsProvider = new Relay.Core.Telemetry.DefaultMetricsProvider(_loggerMock.Object);
    }

    [Fact]
    public void GetHandlerExecutionStats_With_Null_HandlerName_Should_Work()
    {
        // Arrange
        var requestType = typeof(string);
        
        // Act
        var stats = _metricsProvider.GetHandlerExecutionStats(requestType, null);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(requestType, stats.RequestType);
        Assert.Null(stats.HandlerName);
        Assert.Equal(0, stats.TotalExecutions);
    }

    [Fact]
    public void GetHandlerExecutionStats_With_Empty_HandlerName_Should_Work()
    {
        // Arrange
        var requestType = typeof(string);
        
        // Act
        var stats = _metricsProvider.GetHandlerExecutionStats(requestType, "");

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(requestType, stats.RequestType);
        Assert.Equal("", stats.HandlerName);
        Assert.Equal(0, stats.TotalExecutions);
    }

    [Fact]
    public void GetNotificationPublishStats_With_Generic_Type_Should_Work()
    {
        // Arrange
        var notificationType = typeof(List<>).MakeGenericType(typeof(string));

        // Act
        var stats = _metricsProvider.GetNotificationPublishStats(notificationType);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(notificationType, stats.NotificationType);
        Assert.Equal(0, stats.TotalPublishes);
    }

    [Fact]
    public void GetStreamingOperationStats_With_Null_HandlerName_Should_Work()
    {
        // Arrange
        var requestType = typeof(string);

        // Act
        var stats = _metricsProvider.GetStreamingOperationStats(requestType, null);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(requestType, stats.RequestType);
        Assert.Null(stats.HandlerName);
        Assert.Equal(0, stats.TotalOperations);
    }

    [Fact]
    public void GetHandlerExecutionStats_With_Extreme_Duration_Values_Should_Calculate_Stats()
    {
        // Arrange - Add metrics with extreme duration values
        var requestType = typeof(string);
        var handlerName = "TestHandler";

        var metrics1 = new HandlerExecutionMetrics
        {
            RequestType = requestType,
            ResponseType = typeof(string),
            HandlerName = handlerName,
            Duration = TimeSpan.FromMilliseconds(1), // Very fast
            Success = true,
            Timestamp = DateTimeOffset.UtcNow,
            OperationId = "op1"
        };

        var metrics2 = new HandlerExecutionMetrics
        {
            RequestType = requestType,
            ResponseType = typeof(string),
            HandlerName = handlerName,
            Duration = TimeSpan.FromMinutes(10), // Very slow
            Success = true,
            Timestamp = DateTimeOffset.UtcNow,
            OperationId = "op2"
        };

        _metricsProvider.RecordHandlerExecution(metrics1);
        _metricsProvider.RecordHandlerExecution(metrics2);

        // Act
        var stats = _metricsProvider.GetHandlerExecutionStats(requestType, handlerName);

        // Assert
        Assert.Equal(2, stats.TotalExecutions);
        Assert.Equal(TimeSpan.FromMilliseconds(1), stats.MinExecutionTime);
        Assert.Equal(TimeSpan.FromMinutes(10), stats.MaxExecutionTime);
    }

    [Fact]
    public void GetPercentile_With_Single_Value_Should_Return_That_Value()
    {
        // Arrange
        var durations = new List<TimeSpan> { TimeSpan.FromMilliseconds(100) };

        // Act - Use reflection to access the private GetPercentile method
        var method = typeof(Relay.Core.Telemetry.DefaultMetricsProvider)
            .GetMethod("GetPercentile", BindingFlags.NonPublic | BindingFlags.Static);
        var result = (TimeSpan)method.Invoke(null, new object[] { durations, 0.5 });

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(100), result);
    }

    [Fact]
    public void GetPercentile_With_Empty_List_Should_Return_Zero()
    {
        // Arrange
        var durations = new List<TimeSpan>();

        // Act - Use reflection to access the private GetPercentile method
        var method = typeof(Relay.Core.Telemetry.DefaultMetricsProvider)
            .GetMethod("GetPercentile", BindingFlags.NonPublic | BindingFlags.Static);
        var result = (TimeSpan)method.Invoke(null, new object[] { durations, 0.5 });

        // Assert
        Assert.Equal(TimeSpan.Zero, result);
    }

    [Fact]
    public void GetPercentile_With_Different_Percentiles_Should_Work()
    {
        // Arrange
        var durations = new List<TimeSpan>
        {
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(20),
            TimeSpan.FromMilliseconds(30),
            TimeSpan.FromMilliseconds(40),
            TimeSpan.FromMilliseconds(50)
        };

        // Act - Use reflection to access the private GetPercentile method
        var method = typeof(Relay.Core.Telemetry.DefaultMetricsProvider)
            .GetMethod("GetPercentile", BindingFlags.NonPublic | BindingFlags.Static);

        var p25 = (TimeSpan)method.Invoke(null, new object[] { durations, 0.25 });
        var p50 = (TimeSpan)method.Invoke(null, new object[] { durations, 0.50 });
        var p75 = (TimeSpan)method.Invoke(null, new object[] { durations, 0.75 });
        var p99 = (TimeSpan)method.Invoke(null, new object[] { durations, 0.99 });

        // Assert - Values should be within expected ranges
        Assert.InRange(p25.TotalMilliseconds, 10, 50);
        Assert.InRange(p50.TotalMilliseconds, 10, 50);
        Assert.InRange(p75.TotalMilliseconds, 10, 50);
        Assert.InRange(p99.TotalMilliseconds, 10, 50);
    }

    [Fact]
    public void DetectAnomalies_With_No_Data_Should_Return_Empty()
    {
        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1));

        // Assert
        Assert.Empty(anomalies);
    }

    [Fact]
    public void DetectAnomalies_With_Insufficient_Data_Should_Return_Empty()
    {
        // Arrange - Add less than the required 10 entries
        var requestType = typeof(string);
        var handlerName = "TestHandler";

        for (int i = 0; i < 5; i++) // Less than 10
        {
            var metrics = new HandlerExecutionMetrics
            {
                RequestType = requestType,
                ResponseType = typeof(string),
                HandlerName = handlerName,
                Duration = TimeSpan.FromMilliseconds(100 + i),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddSeconds(-i),
                OperationId = $"op{i}"
            };

            _metricsProvider.RecordHandlerExecution(metrics);
        }

        // Act
        var anomalies = _metricsProvider.DetectAnomalies(TimeSpan.FromHours(1));

        // Assert
        Assert.Empty(anomalies);
    }

    [Fact]
    public void GetHandlerExecutionStats_With_All_Failed_Executions_Should_Calculate_Stats()
    {
        // Arrange - Add all failed executions
        var requestType = typeof(string);
        var handlerName = "TestHandler";

        for (int i = 0; i < 5; i++)
        {
            var metrics = new HandlerExecutionMetrics
            {
                RequestType = requestType,
                ResponseType = typeof(string),
                HandlerName = handlerName,
                Duration = TimeSpan.FromMilliseconds(100),
                Success = false,
                Timestamp = DateTimeOffset.UtcNow.AddSeconds(-i),
                OperationId = $"op{i}"
            };

            _metricsProvider.RecordHandlerExecution(metrics);
        }

        // Act
        var stats = _metricsProvider.GetHandlerExecutionStats(requestType, handlerName);

        // Assert
        Assert.Equal(5, stats.TotalExecutions);
        Assert.Equal(0, stats.SuccessfulExecutions);
        Assert.Equal(5, stats.FailedExecutions);
        Assert.Equal(0.0, stats.SuccessRate);
    }

    [Fact]
    public void GetHandlerExecutionStats_With_All_Successful_Executions_Should_Calculate_Stats()
    {
        // Arrange - Add all successful executions
        var requestType = typeof(string);
        var handlerName = "TestHandler";

        for (int i = 0; i < 5; i++)
        {
            var metrics = new HandlerExecutionMetrics
            {
                RequestType = requestType,
                ResponseType = typeof(string),
                HandlerName = handlerName,
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow.AddSeconds(-i),
                OperationId = $"op{i}"
            };

            _metricsProvider.RecordHandlerExecution(metrics);
        }

        // Act
        var stats = _metricsProvider.GetHandlerExecutionStats(requestType, handlerName);

        // Assert
        Assert.Equal(5, stats.TotalExecutions);
        Assert.Equal(5, stats.SuccessfulExecutions);
        Assert.Equal(0, stats.FailedExecutions);
        Assert.Equal(1.0, stats.SuccessRate);
    }

    [Fact]
    public async Task RecordHandlerExecution_Parallel_Calls_Should_Be_Thread_Safe()
    {
        // Arrange
        var requestType = typeof(string);
        var handlerName = "TestHandler";
        var tasks = new List<Task>();

        // Act - Multiple async calls to test thread safety
        for (int i = 0; i < 20; i++)
        {
            int id = i;
            tasks.Add(Task.Run(() =>
            {
                var metrics = new HandlerExecutionMetrics
                {
                    RequestType = requestType,
                    ResponseType = typeof(string),
                    HandlerName = handlerName,
                    Duration = TimeSpan.FromMilliseconds(50 + id),
                    Success = id % 2 == 0,
                    Timestamp = DateTimeOffset.UtcNow.AddSeconds(-id),
                    OperationId = $"op{id}"
                };

                _metricsProvider.RecordHandlerExecution(metrics);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var stats = _metricsProvider.GetHandlerExecutionStats(requestType, handlerName);
        Assert.Equal(20, stats.TotalExecutions);
    }

    [Fact]
    public async Task RecordNotificationPublish_Parallel_Calls_Should_Be_Thread_Safe()
    {
        // Arrange
        var notificationType = typeof(string);
        var tasks = new List<Task>();

        // Act - Multiple async calls to test thread safety
        for (int i = 0; i < 20; i++)
        {
            int id = i;
            tasks.Add(Task.Run(() =>
            {
                var metrics = new NotificationPublishMetrics
                {
                    NotificationType = notificationType,
                    Duration = TimeSpan.FromMilliseconds(25 + id),
                    HandlerCount = 1,
                    Success = id % 2 == 0,
                    Timestamp = DateTimeOffset.UtcNow.AddSeconds(-id)
                };

                _metricsProvider.RecordNotificationPublish(metrics);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var stats = _metricsProvider.GetNotificationPublishStats(notificationType);
        Assert.Equal(20, stats.TotalPublishes);
    }

    [Fact]
    public async Task RecordStreamingOperation_Parallel_Calls_Should_Be_Thread_Safe()
    {
        // Arrange
        var requestType = typeof(string);
        var handlerName = "TestHandler";
        var tasks = new List<Task>();

        // Act - Multiple async calls to test thread safety
        for (int i = 0; i < 20; i++)
        {
            int id = i;
            tasks.Add(Task.Run(() =>
            {
                var metrics = new StreamingOperationMetrics
                {
                    RequestType = requestType,
                    ResponseType = typeof(string),
                    HandlerName = handlerName,
                    Duration = TimeSpan.FromMilliseconds(15 + id),
                    ItemCount = 1,
                    Success = id % 2 == 0,
                    Timestamp = DateTimeOffset.UtcNow.AddSeconds(-id),
                    OperationId = $"op{id}"
                };

                _metricsProvider.RecordStreamingOperation(metrics);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var stats = _metricsProvider.GetStreamingOperationStats(requestType, handlerName);
        Assert.Equal(20, stats.TotalOperations);
    }

    [Fact]
    public void GetHandlerExecutionStats_With_No_Executions_Should_Return_Default_Stats()
    {
        // Arrange
        var requestType = typeof(string);
        var handlerName = "NonExistentHandler";

        // Act
        var stats = _metricsProvider.GetHandlerExecutionStats(requestType, handlerName);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(requestType, stats.RequestType);
        Assert.Equal(handlerName, stats.HandlerName);
        Assert.Equal(0, stats.TotalExecutions);
        Assert.Equal(0, stats.SuccessfulExecutions);
        Assert.Equal(0, stats.FailedExecutions);
        Assert.Equal(0.0, stats.SuccessRate);
        Assert.Equal(TimeSpan.Zero, stats.AverageExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.MinExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.MaxExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.P50ExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.P95ExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.P99ExecutionTime);
        Assert.Equal(DateTimeOffset.MinValue, stats.LastExecution);
    }

    [Fact]
    public void GetNotificationPublishStats_With_No_Publishes_Should_Return_Default_Stats()
    {
        // Arrange
        var notificationType = typeof(string);

        // Act
        var stats = _metricsProvider.GetNotificationPublishStats(notificationType);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(notificationType, stats.NotificationType);
        Assert.Equal(0, stats.TotalPublishes);
        Assert.Equal(0, stats.SuccessfulPublishes);
        Assert.Equal(0, stats.FailedPublishes);
        Assert.Equal(0.0, stats.SuccessRate);
        Assert.Equal(TimeSpan.Zero, stats.AveragePublishTime);
        Assert.Equal(TimeSpan.Zero, stats.MinPublishTime);
        Assert.Equal(TimeSpan.Zero, stats.MaxPublishTime);
        Assert.Equal(0.0, stats.AverageHandlerCount);
        Assert.Equal(DateTimeOffset.MinValue, stats.LastPublish);
    }

    [Fact]
    public void GetStreamingOperationStats_With_No_Operations_Should_Return_Default_Stats()
    {
        // Arrange
        var requestType = typeof(string);
        var handlerName = "NonExistentHandler";

        // Act
        var stats = _metricsProvider.GetStreamingOperationStats(requestType, handlerName);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(requestType, stats.RequestType);
        Assert.Equal(handlerName, stats.HandlerName);
        Assert.Equal(0, stats.TotalOperations);
        Assert.Equal(0, stats.SuccessfulOperations);
        Assert.Equal(0, stats.FailedOperations);
        Assert.Equal(0.0, stats.SuccessRate);
        Assert.Equal(TimeSpan.Zero, stats.AverageOperationTime);
        Assert.Equal(0, stats.TotalItemsStreamed);
        Assert.Equal(0.0, stats.AverageItemsPerOperation);
        Assert.Equal(0.0, stats.ItemsPerSecond);
        Assert.Equal(DateTimeOffset.MinValue, stats.LastOperation);
    }
}
