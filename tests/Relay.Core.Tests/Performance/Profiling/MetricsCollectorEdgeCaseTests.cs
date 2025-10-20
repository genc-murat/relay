using Relay.Core.Performance.Profiling;
using System;
using System.Reflection;
using Xunit;

namespace Relay.Core.Tests.Performance.Profiling;

public class MetricsCollectorEdgeCaseTests
{
    [Fact]
    public void MetricsCollector_GetStatistics_ShouldReturnEmptyStats_ForNonExistentRequestType()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Act
        var stats = collector.GetStatistics("NonExistentType");

        // Assert
        Assert.Equal("NonExistentType", stats.RequestType);
        Assert.Equal(0, stats.TotalRequests);
        Assert.Equal(0, stats.SuccessfulRequests);
        Assert.Equal(0, stats.FailedRequests);
        Assert.Equal(TimeSpan.Zero, stats.AverageExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.MinExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.MaxExecutionTime);
        Assert.Equal(0, stats.TotalMemoryAllocated);
    }

    [Fact]
    public void MetricsCollector_GetStatistics_Global_ShouldHandleEmptyCollector()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Act
        var stats = collector.GetStatistics(null);

        // Assert
        Assert.Equal("All Requests", stats.RequestType);
        Assert.Equal(0, stats.TotalRequests);
        Assert.Equal(0, stats.SuccessfulRequests);
        Assert.Equal(0, stats.FailedRequests);
        Assert.Equal(TimeSpan.Zero, stats.AverageExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.MinExecutionTime);
        Assert.Equal(TimeSpan.Zero, stats.MaxExecutionTime);
        Assert.Equal(0, stats.TotalMemoryAllocated);
    }

    [Fact]
    public void RequestMetricsAggregate_GetPercentile_ShouldHandleEmptyTimes()
    {
        // Arrange - Create aggregate using reflection since it's private
        var collectorType = typeof(InMemoryPerformanceMetricsCollector);
        var aggregateType = collectorType.GetNestedType("RequestMetricsAggregate", BindingFlags.NonPublic);
        var aggregate = Activator.CreateInstance(aggregateType!);

        // Act
        var getPercentileMethod = aggregateType!.GetMethod("GetPercentile", BindingFlags.Instance | BindingFlags.Public);
        var p50 = (TimeSpan)getPercentileMethod!.Invoke(aggregate, new object[] { 50 })!;
        var p95 = (TimeSpan)getPercentileMethod!.Invoke(aggregate, new object[] { 95 })!;
        var p99 = (TimeSpan)getPercentileMethod!.Invoke(aggregate, new object[] { 99 })!;

        // Assert
        Assert.Equal(TimeSpan.Zero, p50);
        Assert.Equal(TimeSpan.Zero, p95);
        Assert.Equal(TimeSpan.Zero, p99);
    }

    [Fact]
    public void RequestMetricsAggregate_GetAverageExecutionTime_ShouldHandleEmptyTimes()
    {
        // Arrange - Create aggregate using reflection
        var collectorType = typeof(InMemoryPerformanceMetricsCollector);
        var aggregateType = collectorType.GetNestedType("RequestMetricsAggregate", BindingFlags.NonPublic);
        var aggregate = Activator.CreateInstance(aggregateType!);

        // Act
        var getAverageMethod = aggregateType!.GetMethod("GetAverageExecutionTime", BindingFlags.Instance | BindingFlags.Public);
        var average = (TimeSpan)getAverageMethod!.Invoke(aggregate, null)!;

        // Assert
        Assert.Equal(TimeSpan.Zero, average);
    }

    [Fact]
    public void RequestMetricsAggregate_GetAverageMemoryAllocated_ShouldHandleZeroCount()
    {
        // Arrange - Create aggregate using reflection
        var collectorType = typeof(InMemoryPerformanceMetricsCollector);
        var aggregateType = collectorType.GetNestedType("RequestMetricsAggregate", BindingFlags.NonPublic);
        var aggregate = Activator.CreateInstance(aggregateType!);

        // Act
        var getAverageMemoryMethod = aggregateType!.GetMethod("GetAverageMemoryAllocated", BindingFlags.Instance | BindingFlags.Public);
        var average = (long)getAverageMemoryMethod!.Invoke(aggregate, null)!;

        // Assert
        Assert.Equal(0, average);
    }

    [Fact]
    public void RequestMetricsAggregate_ShouldInitializeMinMaxCorrectly()
    {
        // Arrange - Create aggregate using reflection
        var collectorType = typeof(InMemoryPerformanceMetricsCollector);
        var aggregateType = collectorType.GetNestedType("RequestMetricsAggregate", BindingFlags.NonPublic);
        var aggregate = Activator.CreateInstance(aggregateType!);

        // Create metrics object
        var metrics = new RequestPerformanceMetrics
        {
            RequestType = "Test",
            ExecutionTime = TimeSpan.FromMilliseconds(100),
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act - Add metrics
        var addMetricsMethod = aggregateType!.GetMethod("AddMetrics", BindingFlags.Instance | BindingFlags.Public);
        addMetricsMethod!.Invoke(aggregate, new object[] { metrics });

        // Assert - Check properties
        var minTimeProp = aggregateType.GetProperty("MinExecutionTime", BindingFlags.Instance | BindingFlags.Public);
        var maxTimeProp = aggregateType.GetProperty("MaxExecutionTime", BindingFlags.Instance | BindingFlags.Public);
        var countProp = aggregateType.GetProperty("Count", BindingFlags.Instance | BindingFlags.Public);

        var minTime = (TimeSpan)minTimeProp!.GetValue(aggregate)!;
        var maxTime = (TimeSpan)maxTimeProp!.GetValue(aggregate)!;
        var count = (long)countProp!.GetValue(aggregate)!;

        Assert.Equal(TimeSpan.FromMilliseconds(100), minTime);
        Assert.Equal(TimeSpan.FromMilliseconds(100), maxTime);
        Assert.Equal(1, count);
    }
}