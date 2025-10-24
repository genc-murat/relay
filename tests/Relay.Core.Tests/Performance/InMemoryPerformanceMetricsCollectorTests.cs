using Relay.Core.Performance.Profiling;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Performance;

public class InMemoryPerformanceMetricsCollectorTests
{
    [Fact]
    public void InMemoryPerformanceMetricsCollector_Constructor_WithCustomMaxRecentMetrics_ShouldSetMaxValue()
    {
        // Arrange & Act
        var collector = new InMemoryPerformanceMetricsCollector(maxRecentMetrics: 50);
        
        // Use reflection to access the private field
        var maxRecentMetricsField = typeof(InMemoryPerformanceMetricsCollector)
            .GetField("_maxRecentMetrics", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
        
        var maxRecentMetricsValue = (int)maxRecentMetricsField!.GetValue(collector)!;

        // Assert
        Assert.Equal(50, maxRecentMetricsValue);
    }

    [Fact]
    public void InMemoryPerformanceMetricsCollector_GetStatistics_Global_ShouldCalculateCorrectValues()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        
        // Add some metrics
        collector.RecordMetrics(new RequestPerformanceMetrics
        {
            RequestType = "TypeA",
            ExecutionTime = TimeSpan.FromMilliseconds(100),
            MemoryAllocated = 1024,
            Gen0Collections = 1,
            Gen1Collections = 0,
            Gen2Collections = 0,
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        collector.RecordMetrics(new RequestPerformanceMetrics
        {
            RequestType = "TypeB",
            ExecutionTime = TimeSpan.FromMilliseconds(200),
            MemoryAllocated = 2048,
            Gen0Collections = 2,
            Gen1Collections = 1,
            Gen2Collections = 0,
            Success = false,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Act
        var globalStats = collector.GetStatistics(null); // null to get global stats

        // Assert - Global statistics should aggregate from recent metrics
        Assert.Equal("All Requests", globalStats.RequestType);
        Assert.Equal(2, globalStats.TotalRequests);
        Assert.Equal(1, globalStats.SuccessfulRequests);
        Assert.Equal(1, globalStats.FailedRequests);
        Assert.Equal(TimeSpan.FromMilliseconds(150), globalStats.AverageExecutionTime); // (100+200)/2
        Assert.Equal(TimeSpan.FromMilliseconds(100), globalStats.MinExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(200), globalStats.MaxExecutionTime);
        Assert.Equal(3072, globalStats.TotalMemoryAllocated); // 1024+2048
    }

    [Fact]
    public void InMemoryPerformanceMetricsCollector_GetStatistics_Global_WithEmptyRecentMetrics()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        // Don't add any metrics to the collector

        // Act
        var globalStats = collector.GetStatistics(null);

        // Assert
        Assert.Equal("All Requests", globalStats.RequestType);
        Assert.Equal(0, globalStats.TotalRequests);
        Assert.Equal(TimeSpan.Zero, globalStats.AverageExecutionTime);
        Assert.Equal(TimeSpan.Zero, globalStats.MinExecutionTime);
        Assert.Equal(TimeSpan.Zero, globalStats.MaxExecutionTime);
        Assert.Equal(0, globalStats.TotalMemoryAllocated);
    }

    [Fact]
    public void InMemoryPerformanceMetricsCollector_RecordMetrics_ShouldTrackGarbageCollection()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Act
        collector.RecordMetrics(new RequestPerformanceMetrics
        {
            RequestType = "TestRequest",
            ExecutionTime = TimeSpan.FromMilliseconds(150),
            MemoryAllocated = 4096,
            Gen0Collections = 3,
            Gen1Collections = 2,
            Gen2Collections = 1,
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Assert
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(4096, stats.TotalMemoryAllocated);
        Assert.Equal(3, stats.TotalGen0Collections);
        Assert.Equal(2, stats.TotalGen1Collections);
        Assert.Equal(1, stats.TotalGen2Collections);
    }

    [Fact]
    public void InMemoryPerformanceMetricsCollector_RecordMetrics_WithLargeNumberOfRequests()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector(maxRecentMetrics: 5);

        // Act - Add more than maxRecentMetrics
        for (int i = 0; i < 10; i++)
        {
            collector.RecordMetrics(new RequestPerformanceMetrics
            {
                RequestType = "TestRequest",
                ExecutionTime = TimeSpan.FromMilliseconds(100 + i),
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        // Assert
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(10, stats.TotalRequests); // Total should include all requests
        
        // The recent metrics should be limited to maxRecentMetrics
        var globalStats = collector.GetStatistics(null);
        Assert.Equal(10, globalStats.TotalRequests); // Total requests tracked separately
    }

    [Fact]
    public void InMemoryPerformanceMetricsCollector_GetAverageMemoryAllocated_ShouldCalculateCorrectly()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Add metrics with different memory allocations
        collector.RecordMetrics(new RequestPerformanceMetrics
        {
            RequestType = "TestRequest",
            ExecutionTime = TimeSpan.FromMilliseconds(100),
            MemoryAllocated = 1000,
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        collector.RecordMetrics(new RequestPerformanceMetrics
        {
            RequestType = "TestRequest",
            ExecutionTime = TimeSpan.FromMilliseconds(200),
            MemoryAllocated = 3000,
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Act
        var stats = collector.GetStatistics("TestRequest");

        // Assert
        Assert.Equal(4000, stats.TotalMemoryAllocated); // 1000 + 3000
        Assert.Equal(2000, stats.AverageMemoryAllocated); // (1000 + 3000) / 2
    }

    [Fact]
    public void InMemoryPerformanceMetricsCollector_Reset_ShouldClearAllFields()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Add some metrics
        collector.RecordMetrics(new RequestPerformanceMetrics
        {
            RequestType = "TestRequest",
            ExecutionTime = TimeSpan.FromMilliseconds(100),
            MemoryAllocated = 1024,
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Verify metrics exist before reset
        var statsBefore = collector.GetStatistics("TestRequest");
        Assert.Equal(1, statsBefore.TotalRequests);

        // Act
        collector.Reset();

        // Assert
        var statsAfter = collector.GetStatistics("TestRequest");
        Assert.Equal(0, statsAfter.TotalRequests);
        Assert.Equal(0, statsAfter.TotalMemoryAllocated);
        Assert.Equal(TimeSpan.Zero, statsAfter.AverageExecutionTime);

        // Check global stats too
        var globalStats = collector.GetStatistics(null);
        Assert.Equal(0, globalStats.TotalRequests);
    }

    [Fact]
    public async Task InMemoryPerformanceMetricsCollector_RecordMetrics_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Act - Multiple threads adding metrics concurrently
        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            int taskIndex = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    collector.RecordMetrics(new RequestPerformanceMetrics
                    {
                        RequestType = $"RequestType{taskIndex % 3}", // Use 3 different types
                        ExecutionTime = TimeSpan.FromMilliseconds(100 + j),
                        MemoryAllocated = 1024,
                        Success = j % 2 == 0, // Alternate success/failure
                        Timestamp = DateTimeOffset.UtcNow
                    });
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert - All metrics should be recorded without data corruption
        var type0Stats = collector.GetStatistics("RequestType0");
        var type1Stats = collector.GetStatistics("RequestType1");
        var type2Stats = collector.GetStatistics("RequestType2");

        Assert.True(type0Stats.TotalRequests > 0);
        Assert.True(type1Stats.TotalRequests > 0);
        Assert.True(type2Stats.TotalRequests > 0);

        // Total should equal sum of all request types
        var total = type0Stats.TotalRequests + type1Stats.TotalRequests + type2Stats.TotalRequests;
        Assert.Equal(100, total); // 10 tasks * 10 records each
    }

    [Fact]
    public void InMemoryPerformanceMetricsCollector_GetStatistics_WithNullRequestType_ShouldReturnGlobalStats()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Add some metrics
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
            Success = false,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Act
        var globalStats = collector.GetStatistics(null); // Pass null explicitly

        // Assert - Should return global statistics
        Assert.Equal("All Requests", globalStats.RequestType);
        Assert.Equal(2, globalStats.TotalRequests);
    }

    [Fact]
    public void InMemoryPerformanceMetricsCollector_GetStatistics_WithEmptyStringRequestType_ShouldBehaveLikeSpecificRequest()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Add metrics for an empty string request type
        collector.RecordMetrics(new RequestPerformanceMetrics
        {
            RequestType = "", // Empty string
            ExecutionTime = TimeSpan.FromMilliseconds(150),
            Success = true,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Act
        var stats = collector.GetStatistics(""); // Get stats for empty string

        // Assert
        Assert.Equal("", stats.RequestType);
        Assert.Equal(1, stats.TotalRequests);
        Assert.Equal(TimeSpan.FromMilliseconds(150), stats.AverageExecutionTime);
    }
}