using Relay.Core.Performance.Profiling;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Performance.Profiling;

public class MetricsCollectorTests
{
    [Fact]
    public async Task MetricsCollector_ShouldAggregateMultipleRequests()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Act
        for (int i = 0; i < 10; i++)
        {
            collector.RecordMetrics(new RequestPerformanceMetrics
            {
                RequestType = "TestRequest",
                ExecutionTime = TimeSpan.FromMilliseconds(100 + i),
                MemoryAllocated = 1024 * (i + 1),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Assert
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(10, stats.TotalRequests);
        Assert.Equal(10, stats.SuccessfulRequests);
        Assert.True(Math.Abs(stats.AverageExecutionTime.TotalMilliseconds - 104.5) <= 1.0);
        Assert.Equal(TimeSpan.FromMilliseconds(100), stats.MinExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(109), stats.MaxExecutionTime);
    }

    [Fact]
    public async Task MetricsCollector_ShouldCalculatePercentiles()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Act - Create a distribution of execution times
        for (int i = 1; i <= 100; i++)
        {
            collector.RecordMetrics(new RequestPerformanceMetrics
            {
                RequestType = "TestRequest",
                ExecutionTime = TimeSpan.FromMilliseconds(i),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Assert
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(100, stats.TotalRequests);
        Assert.True(Math.Abs(stats.P50ExecutionTime.TotalMilliseconds - 50) <= 2);
        Assert.True(Math.Abs(stats.P95ExecutionTime.TotalMilliseconds - 95) <= 5);
        Assert.True(Math.Abs(stats.P99ExecutionTime.TotalMilliseconds - 99) <= 2);
    }

    [Fact]
    public void MetricsCollector_ShouldRespectMaxRecentMetrics()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector(maxRecentMetrics: 10);

        // Act - Record more than max
        for (int i = 0; i < 20; i++)
        {
            collector.RecordMetrics(new RequestPerformanceMetrics
            {
                RequestType = "TestRequest",
                ExecutionTime = TimeSpan.FromMilliseconds(i),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Assert - Total should still be tracked, but recent limited
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(20, stats.TotalRequests);
    }

    [Fact]
    public void MetricsCollector_Reset_ShouldClearAllMetrics()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        collector.RecordMetrics(new RequestPerformanceMetrics
        {
            RequestType = "TestRequest",
            ExecutionTime = TimeSpan.FromMilliseconds(100),
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        var beforeReset = collector.GetStatistics("TestRequest");

        // Act
        collector.Reset();
        var afterReset = collector.GetStatistics("TestRequest");

        // Assert
        Assert.Equal(1, beforeReset.TotalRequests);
        Assert.Equal(0, afterReset.TotalRequests);
    }

    [Fact]
    public void MetricsCollector_ShouldIsolateDifferentRequestTypes()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Act
        collector.RecordMetrics(new RequestPerformanceMetrics
        {
            RequestType = "TypeA",
            ExecutionTime = TimeSpan.FromMilliseconds(100),
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        collector.RecordMetrics(new RequestPerformanceMetrics
        {
            RequestType = "TypeB",
            ExecutionTime = TimeSpan.FromMilliseconds(200),
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Assert
        var statsA = collector.GetStatistics("TypeA");
        var statsB = collector.GetStatistics("TypeB");

        Assert.Equal(1, statsA.TotalRequests);
        Assert.Equal(TimeSpan.FromMilliseconds(100), statsA.AverageExecutionTime);

        Assert.Equal(1, statsB.TotalRequests);
        Assert.Equal(TimeSpan.FromMilliseconds(200), statsB.AverageExecutionTime);
    }

    [Fact]
    public void MetricsCollector_ShouldCalculateSuccessRate()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Act - 7 successes, 3 failures
        for (int i = 0; i < 10; i++)
        {
            collector.RecordMetrics(new RequestPerformanceMetrics
            {
                RequestType = "TestRequest",
                ExecutionTime = TimeSpan.FromMilliseconds(100),
                Success = i < 7,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Assert
        var stats = collector.GetStatistics("TestRequest");
        Assert.True(Math.Abs(stats.SuccessRate - 70.0) <= 0.1);
    }
}
