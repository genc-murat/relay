using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class DefaultMetricsProviderSnapshotTests
{
    private readonly Mock<ILogger<DefaultMetricsProvider>> _loggerMock;
    private readonly TestableDefaultMetricsProvider _metricsProvider;

    public DefaultMetricsProviderSnapshotTests()
    {
        _loggerMock = new Mock<ILogger<DefaultMetricsProvider>>();
        _metricsProvider = new TestableDefaultMetricsProvider(_loggerMock.Object);
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
}