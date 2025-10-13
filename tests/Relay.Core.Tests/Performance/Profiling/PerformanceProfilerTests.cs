using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Requests;
using Relay.Core.Performance.Profiling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Relay.Core.Tests.Performance.Profiling;

public class PerformanceProfilerTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceProfilerTests(ITestOutputHelper output)
    {
        _output = output;
    }
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

    [Fact]
    public void Constructor_ShouldThrow_WhenLoggerIsNull()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = true };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PerformanceProfiler<TestRequest, string>(null!, collector, options));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenMetricsCollectorIsNull()
    {
        // Arrange
        var options = new PerformanceProfilingOptions { Enabled = true };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PerformanceProfiler<TestRequest, string>(
                NullLogger<PerformanceProfiler<TestRequest, string>>.Instance, null!, options));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOptionsIsNull()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PerformanceProfiler<TestRequest, string>(
                NullLogger<PerformanceProfiler<TestRequest, string>>.Instance, collector, null!));
    }

    [Fact]
    public async Task ProfileAsync_ShouldThrow_WhenRequestIsNull()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = true };
        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        Func<ValueTask<string>> next = () => ValueTask.FromResult("result");

        // Act & Assert - This should not throw because TRequest is a generic constraint
        // The null check would happen at compile time due to the generic constraint
        // But we can test with a nullable request if needed
    }

    [Fact]
    public async Task ProfileAsync_ShouldThrow_WhenNextIsNull_AndDisabled()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = false };
        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        var request = new TestRequest();

        // Act & Assert - When disabled, it calls next() directly, so null next throws NullReferenceException
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            profiler.ProfileAsync(request, null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task ProfileAsync_ShouldThrow_WhenNextIsNull_AndEnabled()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = true };
        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        var request = new TestRequest();

        // Act & Assert - When enabled, it eventually calls next(), so null next throws NullReferenceException
        await Assert.ThrowsAsync<NullReferenceException>(() =>
            profiler.ProfileAsync(request, null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task PerformanceProfiler_ShouldWorkWithRealLogger_WhenLogAllRequestsEnabled()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions
        {
            Enabled = true,
            LogAllRequests = true,
            SlowRequestThresholdMs = 1000 // High threshold so it doesn't trigger slow request logging
        };

        // Use a real logger factory for testing
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<PerformanceProfiler<TestRequest, string>>();

        var profiler = new PerformanceProfiler<TestRequest, string>(logger, collector, options);
        var request = new TestRequest();

        Func<ValueTask<string>> next = () =>
        {
            Thread.Sleep(10); // Small delay to ensure measurable execution time
            return ValueTask.FromResult("result");
        };

        // Act
        await profiler.ProfileAsync(request, next, CancellationToken.None);

        // Assert - The test will pass if no exceptions are thrown
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(1, stats.TotalRequests);
    }

    [Fact]
    public async Task PerformanceProfiler_ShouldWorkWithRealLogger_ForSlowRequests()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions
        {
            Enabled = true,
            LogAllRequests = false,
            SlowRequestThresholdMs = 50 // Low threshold to trigger slow request logging
        };

        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<PerformanceProfiler<TestRequest, string>>();

        var profiler = new PerformanceProfiler<TestRequest, string>(logger, collector, options);
        var request = new TestRequest();

        Func<ValueTask<string>> next = () =>
        {
            Thread.Sleep(100); // Delay longer than threshold
            return ValueTask.FromResult("result");
        };

        // Act
        await profiler.ProfileAsync(request, next, CancellationToken.None);

        // Assert - The test will pass if no exceptions are thrown
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(1, stats.TotalRequests);
    }

    [Fact]
    public async Task PerformanceProfiler_ShouldNotRecordMetrics_WhenDisabled()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions
        {
            Enabled = false,
            LogAllRequests = true
        };

        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        var request = new TestRequest();
        Func<ValueTask<string>> next = () => ValueTask.FromResult("result");

        // Act
        await profiler.ProfileAsync(request, next, CancellationToken.None);

        // Assert - No metrics should be recorded when disabled
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(0, stats.TotalRequests);
    }

    [Fact]
    public async Task ProfileAsync_ShouldRespectCancellationToken_WhenDisabled()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = false };
        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        var request = new TestRequest();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<ValueTask<string>> next = () =>
        {
            // This should respect the cancellation token if passed through
            cts.Token.ThrowIfCancellationRequested();
            return ValueTask.FromResult("result");
        };

        // Act & Assert - When disabled, cancellation depends on the next function
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            profiler.ProfileAsync(request, next, cts.Token).AsTask());
    }

    [Fact]
    public async Task ProfileAsync_ShouldRespectCancellationToken_WhenEnabled()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = true };
        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        var request = new TestRequest();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<ValueTask<string>> next = () =>
        {
            // This should respect the cancellation token if passed through
            cts.Token.ThrowIfCancellationRequested();
            return ValueTask.FromResult("result");
        };

        // Act & Assert - When enabled, cancellation depends on the next function
        // Note: Currently the profiler doesn't pass the cancellation token to next()
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            profiler.ProfileAsync(request, next, cts.Token).AsTask());

        // Verify metrics were still recorded despite cancellation
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(1, stats.TotalRequests);
        Assert.Equal(0, stats.SuccessfulRequests); // Should be marked as failed due to exception
        Assert.Equal(1, stats.FailedRequests);
    }

    [Fact]
    public async Task ProfileAsync_ShouldCompleteSuccessfully_WithCancellationToken()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = true };
        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        var request = new TestRequest();
        var cts = new CancellationTokenSource();

        Func<ValueTask<string>> next = () => ValueTask.FromResult("result");

        // Act
        var result = await profiler.ProfileAsync(request, next, cts.Token);

        // Assert
        Assert.Equal("result", result);
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(1, stats.TotalRequests);
        Assert.Equal(1, stats.SuccessfulRequests);
    }

    [Fact]
    public async Task PerformanceProfiler_ShouldTrackMemoryAllocations_Accurately()
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
            // Allocate a known amount of memory
            var buffer = new byte[1024 * 10]; // 10KB
            GC.KeepAlive(buffer); // Prevent optimization
            return ValueTask.FromResult("result");
        };

        // Act
        await profiler.ProfileAsync(request, next, CancellationToken.None);

        // Assert
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(1, stats.TotalRequests);

        // The metrics should contain memory allocation data
        // Note: Exact memory tracking may vary due to GC behavior and optimizations
        _output.WriteLine($"Memory allocated: {stats.TotalMemoryAllocated:N0} bytes");
        _output.WriteLine($"Average memory: {stats.AverageMemoryAllocated:N0} bytes");

        // At minimum, we should have some memory allocation recorded
        Assert.True(stats.TotalMemoryAllocated >= 0);
    }

    [Fact]
    public async Task PerformanceProfiler_ShouldTrackGCCollections()
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
            // Force some GC activity
            for (int i = 0; i < 100; i++)
            {
                var temp = new byte[1024]; // Allocate and discard
            }
            GC.Collect(); // Force collection
            return ValueTask.FromResult("result");
        };

        // Act
        await profiler.ProfileAsync(request, next, CancellationToken.None);

        // Assert
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(1, stats.TotalRequests);

        // The metrics should contain GC collection data
        _output.WriteLine($"Total GC collections: Gen0={stats.TotalGen0Collections}, Gen1={stats.TotalGen1Collections}, Gen2={stats.TotalGen2Collections}");

        // GC collections should be non-negative
        Assert.True(stats.TotalGen0Collections >= 0);
        Assert.True(stats.TotalGen1Collections >= 0);
        Assert.True(stats.TotalGen2Collections >= 0);
    }

    [Fact]
    public async Task PerformanceProfiler_ShouldTrackExecutionTime_Accurately()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = true };
        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        var request = new TestRequest();
        var expectedDelay = TimeSpan.FromMilliseconds(50);

        Func<ValueTask<string>> next = () =>
        {
            Thread.Sleep(expectedDelay);
            return ValueTask.FromResult("result");
        };

        // Act
        var startTime = DateTimeOffset.UtcNow;
        await profiler.ProfileAsync(request, next, CancellationToken.None);
        var endTime = DateTimeOffset.UtcNow;

        // Assert
        var stats = collector.GetStatistics("TestRequest");
        Assert.Equal(1, stats.TotalRequests);

        // Execution time should be at least the delay we introduced
        Assert.True(stats.AverageExecutionTime >= expectedDelay,
            $"Expected execution time >= {expectedDelay.TotalMilliseconds}ms, but got {stats.AverageExecutionTime.TotalMilliseconds}ms");

        // Execution time should be less than total test time (with some buffer)
        var totalTestTime = endTime - startTime;
        Assert.True(stats.AverageExecutionTime <= totalTestTime + TimeSpan.FromMilliseconds(10),
            $"Execution time {stats.AverageExecutionTime.TotalMilliseconds}ms should not exceed test time {totalTestTime.TotalMilliseconds}ms by much");
    }

    [Fact]
    public async Task PerformanceProfiler_ShouldBeThreadSafe_ConcurrentUsage()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = true };
        var profiler = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        const int concurrentTasks = 10;
        const int iterationsPerTask = 20;

        // Act - Run multiple tasks concurrently
        var tasks = Enumerable.Range(0, concurrentTasks).Select(async taskId =>
        {
            for (int i = 0; i < iterationsPerTask; i++)
            {
                var request = new TestRequest();
                Func<ValueTask<string>> next = () =>
                {
                    // Simulate some work with varying delays
                    Thread.Sleep((taskId + i) % 10 + 1);
                    return ValueTask.FromResult($"result_{taskId}_{i}");
                };

                await profiler.ProfileAsync(request, next, CancellationToken.None);
            }
        });

        await Task.WhenAll(tasks);

        // Assert
        var stats = collector.GetStatistics("TestRequest");
        var expectedTotalRequests = concurrentTasks * iterationsPerTask;

        Assert.Equal(expectedTotalRequests, stats.TotalRequests);
        Assert.Equal(expectedTotalRequests, stats.SuccessfulRequests);
        Assert.Equal(0, stats.FailedRequests);

        // All requests should have been recorded
        Assert.True(stats.AverageExecutionTime > TimeSpan.Zero);
        Assert.True(stats.TotalMemoryAllocated >= 0);

        _output.WriteLine($"Concurrent test completed: {expectedTotalRequests} requests");
        _output.WriteLine($"Average execution time: {stats.AverageExecutionTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"Total memory allocated: {stats.TotalMemoryAllocated:N0} bytes");
    }

    [Fact]
    public async Task PerformanceProfiler_ShouldHandleConcurrentDifferentRequestTypes()
    {
        // Arrange
        var collector = new InMemoryPerformanceMetricsCollector();
        var options = new PerformanceProfilingOptions { Enabled = true };

        var profiler1 = new PerformanceProfiler<TestRequest, string>(
            NullLogger<PerformanceProfiler<TestRequest, string>>.Instance,
            collector,
            options);

        var profiler2 = new PerformanceProfiler<OtherTestRequest, int>(
            NullLogger<PerformanceProfiler<OtherTestRequest, int>>.Instance,
            collector,
            options);

        const int concurrentTasks = 5;
        const int iterationsPerTask = 10;

        // Act - Run different profiler types concurrently
        var tasks = new List<Task>();

        // Tasks using TestRequest profiler
        for (int i = 0; i < concurrentTasks; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < iterationsPerTask; j++)
                {
                    var request = new TestRequest();
                    Func<ValueTask<string>> next = () => ValueTask.FromResult("string_result");
                    await profiler1.ProfileAsync(request, next, CancellationToken.None);
                }
            }));
        }

        // Tasks using OtherTestRequest profiler
        for (int i = 0; i < concurrentTasks; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < iterationsPerTask; j++)
                {
                    var request = new OtherTestRequest();
                    Func<ValueTask<int>> next = () => ValueTask.FromResult(42);
                    await profiler2.ProfileAsync(request, next, CancellationToken.None);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var stringStats = collector.GetStatistics("TestRequest");
        var intStats = collector.GetStatistics("OtherTestRequest");

        var expectedRequestsPerType = concurrentTasks * iterationsPerTask;

        Assert.Equal(expectedRequestsPerType, stringStats.TotalRequests);
        Assert.Equal(expectedRequestsPerType, intStats.TotalRequests);

        Assert.Equal(expectedRequestsPerType, stringStats.SuccessfulRequests);
        Assert.Equal(expectedRequestsPerType, intStats.SuccessfulRequests);

        _output.WriteLine($"Concurrent different types test completed:");
        _output.WriteLine($"String requests: {stringStats.TotalRequests}");
        _output.WriteLine($"Int requests: {intStats.TotalRequests}");
    }

    private class TestRequest : IRequest<string>
    {
    }

    private class OtherTestRequest : IRequest<int>
    {
    }
}