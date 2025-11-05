using Relay.Core.Performance.Profiling;
using System;
using Xunit;

namespace Relay.Core.Tests.Performance.Profiling;

/// <summary>
/// Tests for PerformanceStatistics record struct
/// </summary>
public class PerformanceStatisticsTests
{
    [Fact]
    public void PerformanceStatistics_DefaultConstructor_InitializesProperties()
    {
        // Act
        var stats = new PerformanceStatistics();

        // Assert
        Assert.Null(stats.RequestType);
        Assert.Equal(0, stats.TotalRequests);
        Assert.Equal(0, stats.SuccessfulRequests);
        Assert.Equal(0, stats.FailedRequests);
        Assert.Equal(TimeSpan.Zero, stats.AverageExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.MinExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.MaxExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.P50ExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.P95ExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.P99ExecutionTime);
        Assert.Equal(0, stats.TotalMemoryAllocated);
        Assert.Equal(0, stats.AverageMemoryAllocated);
        Assert.Equal(0, stats.TotalGen0Collections);
        Assert.Equal(0, stats.TotalGen1Collections);
        Assert.Equal(0, stats.TotalGen2Collections);
        Assert.Equal(0.0, stats.SuccessRate);
    }

    [Fact]
    public void PerformanceStatistics_WithValues_CalculatesSuccessRate()
    {
        // Act
        var stats = new PerformanceStatistics
        {
            RequestType = "TestRequest",
            TotalRequests = 10,
            SuccessfulRequests = 8,
            FailedRequests = 2
        };

        // Assert
        Assert.Equal(80.0, stats.SuccessRate);
    }

    [Fact]
    public void PerformanceStatistics_WithZeroTotalRequests_ReturnsZeroSuccessRate()
    {
        // Act
        var stats = new PerformanceStatistics
        {
            RequestType = "TestRequest",
            TotalRequests = 0,
            SuccessfulRequests = 0,
            FailedRequests = 0
        };

        // Assert
        Assert.Equal(0.0, stats.SuccessRate);
    }

    [Fact]
    public void PerformanceStatistics_ObjectInitialization_Works()
    {
        // Act
        var stats = new PerformanceStatistics
        {
            RequestType = "TestRequest",
            TotalRequests = 100,
            SuccessfulRequests = 95,
            FailedRequests = 5,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50),
            MinExecutionTime = TimeSpan.FromMilliseconds(10),
            MaxExecutionTime = TimeSpan.FromMilliseconds(200),
            P50ExecutionTime = TimeSpan.FromMilliseconds(45),
            P95ExecutionTime = TimeSpan.FromMilliseconds(120),
            P99ExecutionTime = TimeSpan.FromMilliseconds(180),
            TotalMemoryAllocated = 1024000,
            AverageMemoryAllocated = 10240,
            TotalGen0Collections = 5,
            TotalGen1Collections = 2,
            TotalGen2Collections = 0
        };

        // Assert
        Assert.Equal("TestRequest", stats.RequestType);
        Assert.Equal(100, stats.TotalRequests);
        Assert.Equal(95, stats.SuccessfulRequests);
        Assert.Equal(5, stats.FailedRequests);
        Assert.Equal(TimeSpan.FromMilliseconds(50), stats.AverageExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(10), stats.MinExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(200), stats.MaxExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(45), stats.P50ExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(120), stats.P95ExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(180), stats.P99ExecutionTime);
        Assert.Equal(1024000, stats.TotalMemoryAllocated);
        Assert.Equal(10240, stats.AverageMemoryAllocated);
        Assert.Equal(5, stats.TotalGen0Collections);
        Assert.Equal(2, stats.TotalGen1Collections);
        Assert.Equal(0, stats.TotalGen2Collections);
        Assert.Equal(95.0, stats.SuccessRate);
    }

    [Fact]
    public void PerformanceStatistics_ToString_ReturnsFormattedString()
    {
        // Arrange
        var stats = new PerformanceStatistics
        {
            RequestType = "TestRequest",
            TotalRequests = 1000,
            SuccessfulRequests = 950,
            AverageExecutionTime = TimeSpan.FromMilliseconds(45.67),
            P95ExecutionTime = TimeSpan.FromMilliseconds(120.5)
        };

        // Act
        var result = stats.ToString();

        // Assert
        Assert.Contains("TestRequest", result);
        Assert.Contains("1,000", result); // Formatted number
        Assert.Contains("45.67ms", result);
        Assert.Contains("120.50ms", result);
        Assert.Contains("95.0%", result);
    }

    [Fact]
    public void PerformanceStatistics_RecordEquality_Works()
    {
        // Arrange
        var stats1 = new PerformanceStatistics
        {
            RequestType = "TestRequest",
            TotalRequests = 100,
            SuccessfulRequests = 95
        };

        var stats2 = new PerformanceStatistics
        {
            RequestType = "TestRequest",
            TotalRequests = 100,
            SuccessfulRequests = 95
        };

        var stats3 = new PerformanceStatistics
        {
            RequestType = "DifferentRequest",
            TotalRequests = 100,
            SuccessfulRequests = 95
        };

        // Assert
        Assert.Equal(stats1, stats2);
        Assert.NotEqual(stats1, stats3);
    }

    [Fact]
    public void PerformanceStatistics_WithLargeNumbers_HandlesCorrectly()
    {
        // Act
        var stats = new PerformanceStatistics
        {
            RequestType = "HighVolumeRequest",
            TotalRequests = 1000000,
            SuccessfulRequests = 999000,
            FailedRequests = 1000,
            TotalMemoryAllocated = long.MaxValue / 2,
            AverageMemoryAllocated = int.MaxValue
        };

        // Assert
        Assert.Equal(1000000, stats.TotalRequests);
        Assert.Equal(999000, stats.SuccessfulRequests);
        Assert.Equal(1000, stats.FailedRequests);
        Assert.Equal(99.9, stats.SuccessRate, 1); // Allow 1 decimal precision
        Assert.Equal(long.MaxValue / 2, stats.TotalMemoryAllocated);
        Assert.Equal(int.MaxValue, stats.AverageMemoryAllocated);
    }
}
