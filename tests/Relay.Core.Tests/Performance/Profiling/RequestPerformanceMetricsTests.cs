using Relay.Core.Performance.Profiling;
using System;
using Xunit;

namespace Relay.Core.Tests.Performance.Profiling;

/// <summary>
/// Tests for RequestPerformanceMetrics record struct
/// </summary>
public class RequestPerformanceMetricsTests
{
    [Fact]
    public void RequestPerformanceMetrics_DefaultConstructor_InitializesProperties()
    {
        // Act
        var metrics = new RequestPerformanceMetrics();

        // Assert
        Assert.Null(metrics.RequestType);
        Assert.Equal(TimeSpan.Zero, metrics.ExecutionTime);
        Assert.Equal(0, metrics.MemoryAllocated);
        Assert.Equal(0, metrics.Gen0Collections);
        Assert.Equal(0, metrics.Gen1Collections);
        Assert.Equal(0, metrics.Gen2Collections);
        Assert.Equal(default(DateTimeOffset), metrics.Timestamp);
        Assert.False(metrics.Success);
    }

    [Fact]
    public void RequestPerformanceMetrics_ObjectInitialization_Works()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var metrics = new RequestPerformanceMetrics
        {
            RequestType = "TestRequest",
            ExecutionTime = TimeSpan.FromMilliseconds(150),
            MemoryAllocated = 2048,
            Gen0Collections = 2,
            Gen1Collections = 1,
            Gen2Collections = 0,
            Timestamp = timestamp,
            Success = true
        };

        // Assert
        Assert.Equal("TestRequest", metrics.RequestType);
        Assert.Equal(TimeSpan.FromMilliseconds(150), metrics.ExecutionTime);
        Assert.Equal(2048, metrics.MemoryAllocated);
        Assert.Equal(2, metrics.Gen0Collections);
        Assert.Equal(1, metrics.Gen1Collections);
        Assert.Equal(0, metrics.Gen2Collections);
        Assert.Equal(timestamp, metrics.Timestamp);
        Assert.True(metrics.Success);
    }

    [Fact]
    public void RequestPerformanceMetrics_WithFailedRequest_SetsSuccessToFalse()
    {
        // Act
        var metrics = new RequestPerformanceMetrics
        {
            RequestType = "FailedRequest",
            ExecutionTime = TimeSpan.FromSeconds(5),
            MemoryAllocated = 1024,
            Success = false
        };

        // Assert
        Assert.Equal("FailedRequest", metrics.RequestType);
        Assert.Equal(TimeSpan.FromSeconds(5), metrics.ExecutionTime);
        Assert.Equal(1024, metrics.MemoryAllocated);
        Assert.False(metrics.Success);
    }

    [Fact]
    public void RequestPerformanceMetrics_WithZeroExecutionTime_HandlesCorrectly()
    {
        // Act
        var metrics = new RequestPerformanceMetrics
        {
            RequestType = "InstantRequest",
            ExecutionTime = TimeSpan.Zero,
            MemoryAllocated = 0,
            Success = true
        };

        // Assert
        Assert.Equal("InstantRequest", metrics.RequestType);
        Assert.Equal(TimeSpan.Zero, metrics.ExecutionTime);
        Assert.Equal(0, metrics.MemoryAllocated);
        Assert.True(metrics.Success);
    }

    [Fact]
    public void RequestPerformanceMetrics_WithLargeMemoryAllocation_HandlesCorrectly()
    {
        // Act
        var metrics = new RequestPerformanceMetrics
        {
            RequestType = "MemoryIntensiveRequest",
            ExecutionTime = TimeSpan.FromMinutes(1),
            MemoryAllocated = long.MaxValue,
            Gen0Collections = int.MaxValue,
            Gen1Collections = int.MaxValue,
            Gen2Collections = int.MaxValue,
            Success = true
        };

        // Assert
        Assert.Equal("MemoryIntensiveRequest", metrics.RequestType);
        Assert.Equal(TimeSpan.FromMinutes(1), metrics.ExecutionTime);
        Assert.Equal(long.MaxValue, metrics.MemoryAllocated);
        Assert.Equal(int.MaxValue, metrics.Gen0Collections);
        Assert.Equal(int.MaxValue, metrics.Gen1Collections);
        Assert.Equal(int.MaxValue, metrics.Gen2Collections);
        Assert.True(metrics.Success);
    }

    [Fact]
    public void RequestPerformanceMetrics_RecordEquality_Works()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var metrics1 = new RequestPerformanceMetrics
        {
            RequestType = "TestRequest",
            ExecutionTime = TimeSpan.FromMilliseconds(100),
            MemoryAllocated = 1024,
            Timestamp = timestamp,
            Success = true
        };

        var metrics2 = new RequestPerformanceMetrics
        {
            RequestType = "TestRequest",
            ExecutionTime = TimeSpan.FromMilliseconds(100),
            MemoryAllocated = 1024,
            Timestamp = timestamp,
            Success = true
        };

        var metrics3 = new RequestPerformanceMetrics
        {
            RequestType = "DifferentRequest",
            ExecutionTime = TimeSpan.FromMilliseconds(100),
            MemoryAllocated = 1024,
            Timestamp = timestamp,
            Success = true
        };

        // Assert
        Assert.Equal(metrics1, metrics2);
        Assert.NotEqual(metrics1, metrics3);
    }

    [Fact]
    public void RequestPerformanceMetrics_WithNegativeExecutionTime_HandlesCorrectly()
    {
        // Act
        var metrics = new RequestPerformanceMetrics
        {
            RequestType = "NegativeTimeRequest",
            ExecutionTime = TimeSpan.FromMilliseconds(-50), // Unusual but possible
            MemoryAllocated = 512,
            Success = false
        };

        // Assert
        Assert.Equal("NegativeTimeRequest", metrics.RequestType);
        Assert.Equal(TimeSpan.FromMilliseconds(-50), metrics.ExecutionTime);
        Assert.Equal(512, metrics.MemoryAllocated);
        Assert.False(metrics.Success);
    }

    [Fact]
    public void RequestPerformanceMetrics_WithFutureTimestamp_HandlesCorrectly()
    {
        // Arrange
        var futureTimestamp = DateTimeOffset.UtcNow.AddYears(1);

        // Act
        var metrics = new RequestPerformanceMetrics
        {
            RequestType = "FutureRequest",
            ExecutionTime = TimeSpan.FromMilliseconds(200),
            Timestamp = futureTimestamp,
            Success = true
        };

        // Assert
        Assert.Equal("FutureRequest", metrics.RequestType);
        Assert.Equal(TimeSpan.FromMilliseconds(200), metrics.ExecutionTime);
        Assert.Equal(futureTimestamp, metrics.Timestamp);
        Assert.True(metrics.Success);
    }

    [Fact]
    public void RequestPerformanceMetrics_WithPastTimestamp_HandlesCorrectly()
    {
        // Arrange
        var pastTimestamp = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var metrics = new RequestPerformanceMetrics
        {
            RequestType = "LegacyRequest",
            ExecutionTime = TimeSpan.FromHours(1),
            Timestamp = pastTimestamp,
            Success = true
        };

        // Assert
        Assert.Equal("LegacyRequest", metrics.RequestType);
        Assert.Equal(TimeSpan.FromHours(1), metrics.ExecutionTime);
        Assert.Equal(pastTimestamp, metrics.Timestamp);
        Assert.True(metrics.Success);
    }
}
