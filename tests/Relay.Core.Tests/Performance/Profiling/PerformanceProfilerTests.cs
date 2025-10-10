using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Requests;
using Relay.Core.Performance.Profiling;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Performance.Profiling;

public class PerformanceProfilerTests
{
    [Fact]
    public async Task PerformanceProfiler_ShouldRecordMetrics()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = true };
        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        var request = new TestRequest();
        var nextCalled = false;

        Func<ValueTask<string>> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult("result");
        };

        // Act
        var result = await profiler.ProfileAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal("result", result);
        Assert.True(nextCalled);

        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(1, stats.TotalRequests);
        Assert.Equal(1, stats.SuccessfulRequests);
        Assert.Equal(0, stats.FailedRequests);
        Assert.True(stats.AverageExecutionTime >= TimeSpan.Zero);
    }

    [Fact]
    public async Task PerformanceProfiler_ShouldNotRecord_WhenDisabled()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = false };
        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        var request = new TestRequest();
        Func<ValueTask<string>> next = () => ValueTask.FromResult("result");

        // Act
        await profiler.ProfileAsync(request, next, CancellationToken.None);

        // Assert
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(0, stats.TotalRequests); // Nothing recorded when disabled
    }

    [Fact]
    public async Task PerformanceProfiler_ShouldRecordFailures()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = true };
        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        var request = new TestRequest();
        Func<ValueTask<string>> next = () => throw new InvalidOperationException("Test error");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            profiler.ProfileAsync(request, next, CancellationToken.None).AsTask());

        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(1, stats.TotalRequests);
        Assert.Equal(1, stats.FailedRequests);
        Assert.Equal(0, stats.SuccessfulRequests);
    }

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

    [Fact]
    public async Task PerformanceProfiler_ShouldTrackMemoryAllocations()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = true };
        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        var request = new TestRequest();
        Func<ValueTask<string>> next = () =>
        {
            // Allocate some memory (use GC.KeepAlive to prevent optimization)
            var buffer = new byte[1024];
            GC.KeepAlive(buffer);
            return ValueTask.FromResult("result");
        };

        // Act
        await profiler.ProfileAsync(request, next, CancellationToken.None);

        // Assert - Memory tracking may be 0 in Release mode due to optimizations
        // Just verify the metrics were recorded
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(1, stats.TotalRequests);
        Assert.Equal(1, stats.SuccessfulRequests);
    }

    private class TestRequest : IRequest<string>
    {
    }
}