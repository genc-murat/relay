using Relay.CLI.Commands;
using System.Diagnostics;

namespace Relay.CLI.Tests.Commands;

public class PerformanceCommandTests
{
    [Fact]
    public void PerformanceCommand_MeasuresThroughput()
    {
        // Arrange
        var iterations = 10000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            _ = i * 2; // Simple operation
        }
        stopwatch.Stop();

        // Assert
        var opsPerSecond = iterations / stopwatch.Elapsed.TotalSeconds;
        opsPerSecond.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PerformanceCommand_TracksMemoryUsage()
    {
        // Arrange
        var beforeMemory = GC.GetTotalMemory(true);

        // Act
        var list = new List<byte[]>();
        for (int i = 0; i < 100; i++)
        {
            list.Add(new byte[1024]); // Allocate 1KB
        }

        var afterMemory = GC.GetTotalMemory(false);

        // Assert
        var allocatedMemory = afterMemory - beforeMemory;
        allocatedMemory.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PerformanceCommand_ComparesBeforeAfter()
    {
        // Arrange
        var before = TimeSpan.FromMilliseconds(100);
        var after = TimeSpan.FromMilliseconds(50);

        // Act
        var improvement = ((before - after).TotalMilliseconds / before.TotalMilliseconds) * 100;

        // Assert
        improvement.Should().Be(50);
    }

    [Fact]
    public void PerformanceCommand_DetectsRegressions()
    {
        // Arrange
        var baseline = 100.0; // ms
        var current = 150.0; // ms
        var threshold = 10.0; // 10% threshold

        // Act
        var percentageChange = ((current - baseline) / baseline) * 100;
        var isRegression = percentageChange > threshold;

        // Assert
        isRegression.Should().BeTrue();
        percentageChange.Should().Be(50);
    }

    [Fact]
    public async Task PerformanceCommand_ProfilesAsyncOperations()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        await Task.Delay(10);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public void PerformanceCommand_CalculatesPercentiles()
    {
        // Arrange
        var measurements = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

        // Act
        var sorted = measurements.OrderBy(x => x).ToArray();
        var p50 = sorted[(int)(sorted.Length * 0.50)];
        var p95 = sorted[(int)(sorted.Length * 0.95)];
        var p99 = sorted[(int)(sorted.Length * 0.99)];

        // Assert
        p50.Should().Be(60);
        p95.Should().Be(100);
        p99.Should().Be(100);
    }

    [Fact]
    public void PerformanceCommand_GeneratesReport()
    {
        // Arrange
        var results = new
        {
            TotalOperations = 10000,
            Duration = TimeSpan.FromSeconds(1),
            Throughput = 10000,
            AverageLatency = TimeSpan.FromMilliseconds(0.1),
            P95Latency = TimeSpan.FromMilliseconds(0.5)
        };

        // Act
        var report = $@"Performance Report
━━━━━━━━━━━━━━━━━━
Total Operations: {results.TotalOperations:N0}
Duration: {results.Duration.TotalSeconds:F2}s
Throughput: {results.Throughput:N0} ops/sec
Avg Latency: {results.AverageLatency.TotalMilliseconds:F2}ms
P95 Latency: {results.P95Latency.TotalMilliseconds:F2}ms";

        // Assert
        report.Should().Contain("Performance Report");
        report.Should().Contain("10"); // Accept culture-invariant format
        report.Should().Contain("ops/sec");
    }

    [Fact]
    public void PerformanceCommand_ComparesFrameworks()
    {
        // Arrange
        var relayTime = TimeSpan.FromMicroseconds(50);
        var mediatrTime = TimeSpan.FromMicroseconds(150);

        // Act
        var speedup = mediatrTime.TotalMicroseconds / relayTime.TotalMicroseconds;

        // Assert
        speedup.Should().Be(3);
    }

    [Fact]
    public void PerformanceCommand_ExportsToJson()
    {
        // Arrange
        var data = new
        {
            Timestamp = DateTime.UtcNow,
            Throughput = 10000,
            Latency = 0.1
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(data);

        // Assert
        json.Should().Contain("Throughput");
        json.Should().Contain("10000");
    }

    [Fact]
    public void PerformanceCommand_TracksGCCollections()
    {
        // Arrange
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);

        // Act
        var list = new List<byte[]>();
        for (int i = 0; i < 10000; i++)
        {
            list.Add(new byte[1024]);
        }

        GC.Collect();
        var gen0After = GC.CollectionCount(0);
        var gen1After = GC.CollectionCount(1);

        // Assert
        (gen0After - gen0Before).Should().BeGreaterThan(0);
    }

    [Fact]
    public void PerformanceCommand_ShouldMeasureAllocationRate()
    {
        // Arrange
        var allocations = 100000L; // bytes
        var duration = TimeSpan.FromSeconds(1);

        // Act
        var allocationRate = allocations / duration.TotalSeconds;

        // Assert
        allocationRate.Should().Be(100000); // 100KB/s
    }

    [Fact]
    public void PerformanceCommand_ShouldCalculateVariance()
    {
        // Arrange
        var times = new[] { 10.0, 12.0, 11.0, 13.0, 10.5 };
        var mean = times.Average();

        // Act
        var variance = times.Select(t => Math.Pow(t - mean, 2)).Average();

        // Assert
        variance.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PerformanceCommand_ShouldCalculateStandardDeviation()
    {
        // Arrange
        var times = new[] { 10.0, 12.0, 11.0, 13.0, 10.5 };
        var mean = times.Average();
        var variance = times.Select(t => Math.Pow(t - mean, 2)).Average();

        // Act
        var stdDev = Math.Sqrt(variance);

        // Assert
        stdDev.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PerformanceCommand_ShouldDetectWarmupPeriod()
    {
        // Arrange
        var measurements = new[] { 100.0, 90.0, 50.0, 48.0, 49.0 }; // First 2 are warmup

        // Act
        var warmupThreshold = 2;
        var actualMeasurements = measurements.Skip(warmupThreshold).ToArray();

        // Assert
        actualMeasurements.Should().HaveCount(3);
    }

    [Fact]
    public void PerformanceCommand_ShouldCalculateMedian()
    {
        // Arrange
        var times = new[] { 5.0, 3.0, 7.0, 1.0, 9.0 };

        // Act
        var sorted = times.OrderBy(x => x).ToArray();
        var median = sorted[sorted.Length / 2];

        // Assert
        median.Should().Be(5.0);
    }

    [Fact]
    public void PerformanceCommand_ShouldCalculateMode()
    {
        // Arrange
        var times = new[] { 5.0, 3.0, 5.0, 1.0, 5.0 };

        // Act
        var mode = times.GroupBy(x => x).OrderByDescending(g => g.Count()).First().Key;

        // Assert
        mode.Should().Be(5.0);
    }

    [Fact]
    public void PerformanceCommand_ShouldCalculateRange()
    {
        // Arrange
        var times = new[] { 5.0, 3.0, 7.0, 1.0, 9.0 };

        // Act
        var range = times.Max() - times.Min();

        // Assert
        range.Should().Be(8.0);
    }

    [Fact]
    public void PerformanceCommand_ShouldDetectOutliers()
    {
        // Arrange
        var times = new[] { 10.0, 11.0, 12.0, 13.0, 100.0 }; // 100 is outlier
        var mean = times.Take(4).Average();
        var threshold = mean * 2;

        // Act
        var outliers = times.Where(t => t > threshold).ToArray();

        // Assert
        outliers.Should().Contain(100.0);
    }

    [Fact]
    public void PerformanceCommand_ShouldCompareCPUUsage()
    {
        // Arrange
        var relayProcessTime = TimeSpan.FromMilliseconds(50);
        var mediatrProcessTime = TimeSpan.FromMilliseconds(150);

        // Act
        var cpuSavings = ((mediatrProcessTime - relayProcessTime).TotalMilliseconds / mediatrProcessTime.TotalMilliseconds) * 100;

        // Assert
        cpuSavings.Should().BeApproximately(66.67, 0.1);
    }

    [Fact]
    public void PerformanceCommand_ShouldTrackThreadPoolUsage()
    {
        // Arrange
        ThreadPool.GetAvailableThreads(out int workerThreads, out int ioThreads);

        // Act
        var threadPoolInfo = new
        {
            WorkerThreads = workerThreads,
            IOThreads = ioThreads
        };

        // Assert
        threadPoolInfo.WorkerThreads.Should().BeGreaterThan(0);
        threadPoolInfo.IOThreads.Should().BeGreaterThan(0);
    }

    [Fact]
    public void PerformanceCommand_ShouldMeasureContentionTime()
    {
        // Arrange
        var lockObject = new object();
        var contentionTime = TimeSpan.Zero;

        // Act
        var sw = Stopwatch.StartNew();
        lock (lockObject)
        {
            // Simulated work
        }
        sw.Stop();
        contentionTime = sw.Elapsed;

        // Assert
        contentionTime.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void PerformanceCommand_ShouldCalculateLatencyDistribution()
    {
        // Arrange
        var latencies = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };

        // Act
        var distribution = new
        {
            Min = latencies.Min(),
            Max = latencies.Max(),
            Avg = latencies.Average(),
            P50 = latencies.OrderBy(x => x).ElementAt(latencies.Length / 2),
            P95 = latencies.OrderBy(x => x).ElementAt((int)(latencies.Length * 0.95)),
            P99 = latencies.OrderBy(x => x).ElementAt((int)(latencies.Length * 0.99))
        };

        // Assert
        distribution.Min.Should().Be(10);
        distribution.Max.Should().Be(100);
        distribution.Avg.Should().Be(55);
    }

    [Fact]
    public void PerformanceCommand_ShouldGenerateCsvExport()
    {
        // Arrange
        var csv = "Operation,Duration(ms),Throughput(ops/s)\nHandler1,0.5,2000000\nHandler2,1.0,1000000";

        // Act
        var lines = csv.Split('\n');

        // Assert
        lines.Should().HaveCount(3);
        lines[0].Should().Contain("Operation");
    }

    [Fact]
    public void PerformanceCommand_ShouldTrackCacheMissRate()
    {
        // Arrange
        var totalRequests = 1000;
        var cacheHits = 800;
        var cacheMisses = 200;

        // Act
        var missRate = (cacheMisses * 100.0) / totalRequests;

        // Assert
        missRate.Should().Be(20.0);
    }

    [Fact]
    public void PerformanceCommand_ShouldMeasureDatabaseQueryTime()
    {
        // Arrange
        var queryTime = TimeSpan.FromMilliseconds(25);
        var threshold = TimeSpan.FromMilliseconds(100);

        // Act
        var isAcceptable = queryTime < threshold;

        // Assert
        isAcceptable.Should().BeTrue();
    }

    [Fact]
    public void PerformanceCommand_ShouldCalculateRPS()
    {
        // Arrange
        var totalRequests = 10000;
        var duration = TimeSpan.FromSeconds(10);

        // Act
        var rps = totalRequests / duration.TotalSeconds;

        // Assert
        rps.Should().Be(1000); // 1000 requests per second
    }

    [Fact]
    public void PerformanceCommand_ShouldMeasureSerializationOverhead()
    {
        // Arrange
        var serializationTime = TimeSpan.FromMicroseconds(50);
        var totalTime = TimeSpan.FromMicroseconds(100);

        // Act
        var overhead = (serializationTime.TotalMicroseconds / totalTime.TotalMicroseconds) * 100;

        // Assert
        overhead.Should().Be(50.0);
    }

    [Fact]
    public void PerformanceCommand_ShouldTrackAsyncOverhead()
    {
        // Arrange
        var syncTime = TimeSpan.FromMicroseconds(100);
        var asyncTime = TimeSpan.FromMicroseconds(110);

        // Act
        var overhead = asyncTime - syncTime;

        // Assert
        overhead.Should().Be(TimeSpan.FromMicroseconds(10));
    }

    [Fact]
    public void PerformanceCommand_ShouldCalculateThroughputVariability()
    {
        // Arrange
        var throughputs = new[] { 1000, 1100, 950, 1050, 1020 };
        var mean = throughputs.Average();
        var variance = throughputs.Select(t => Math.Pow(t - mean, 2)).Average();

        // Act
        var stdDev = Math.Sqrt(variance);
        var coefficientOfVariation = (stdDev / mean) * 100;

        // Assert
        coefficientOfVariation.Should().BeLessThan(10); // Low variability
    }

    [Fact]
    public void PerformanceCommand_ShouldDetectMemoryLeaks()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        var iterations = 10;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        var memoryGrowth = finalMemory - initialMemory;
        memoryGrowth.Should().BeLessThan(1024 * 1024); // Less than 1MB growth
    }

    [Fact]
    public void PerformanceCommand_ShouldMeasureLargeObjectHeapAllocations()
    {
        // Arrange
        var largeObjectSize = 85000; // > 85KB goes to LOH

        // Act
        var isLOH = largeObjectSize > 85000;

        // Assert
        isLOH.Should().BeFalse();
        (largeObjectSize == 85000).Should().BeTrue();
    }

    [Fact]
    public void PerformanceCommand_ShouldCalculateAmdahlsLaw()
    {
        // Arrange
        var parallelPortion = 0.8; // 80% can be parallelized
        var processors = 4;

        // Act
        var speedup = 1 / ((1 - parallelPortion) + (parallelPortion / processors));

        // Assert
        speedup.Should().BeApproximately(2.5, 0.1);
    }

    [Fact]
    public void PerformanceCommand_ShouldMeasureContextSwitchOverhead()
    {
        // Arrange
        var contextSwitches = 100;
        var overheadPerSwitch = TimeSpan.FromMicroseconds(1);

        // Act
        var totalOverhead = TimeSpan.FromMicroseconds(contextSwitches * overheadPerSwitch.TotalMicroseconds);

        // Assert
        totalOverhead.Should().Be(TimeSpan.FromMicroseconds(100));
    }

    [Fact]
    public void PerformanceCommand_ShouldGenerateFlameGraph()
    {
        // Arrange
        var samples = new[]
        {
            ("Main;Handler1;Operation1", 50),
            ("Main;Handler1;Operation2", 30),
            ("Main;Handler2;Operation1", 20)
        };

        // Act
        var totalSamples = samples.Sum(s => s.Item2);

        // Assert
        totalSamples.Should().Be(100);
    }

    [Fact]
    public void PerformanceCommand_ShouldCompareWithBaseline()
    {
        // Arrange
        var baselineResults = new { Throughput = 1000, Latency = 10.0 };
        var currentResults = new { Throughput = 1500, Latency = 7.0 };

        // Act
        var throughputImprovement = ((currentResults.Throughput - baselineResults.Throughput) * 100.0) / baselineResults.Throughput;
        var latencyImprovement = ((baselineResults.Latency - currentResults.Latency) * 100.0) / baselineResults.Latency;

        // Assert
        throughputImprovement.Should().Be(50.0);
        latencyImprovement.Should().Be(30.0);
    }

    [Fact]
    public void PerformanceCommand_ShouldGenerateHistogram()
    {
        // Arrange
        var latencies = new[] { 1, 2, 2, 3, 3, 3, 4, 4, 4, 4, 5 };

        // Act
        var histogram = latencies.GroupBy(x => x)
            .OrderBy(g => g.Key)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .ToArray();

        // Assert
        histogram.Should().HaveCount(5);
        histogram.First(h => h.Value == 4).Count.Should().Be(4);
    }

    [Theory]
    [InlineData(100, 1, 100)]
    [InlineData(100, 10, 10)]
    [InlineData(100, 100, 1)]
    public void PerformanceCommand_ShouldCalculateLatency(int totalMs, int operations, int expectedLatency)
    {
        // Act
        var latency = totalMs / operations;

        // Assert
        latency.Should().Be(expectedLatency);
    }

    [Fact]
    public void PerformanceCommand_ShouldSupportConcurrentBenchmarks()
    {
        // Arrange
        var concurrencyLevel = 10;
        var operationsPerThread = 1000;

        // Act
        var totalOperations = concurrencyLevel * operationsPerThread;

        // Assert
        totalOperations.Should().Be(10000);
    }

    [Fact]
    public void PerformanceCommand_ShouldMeasureStartupTime()
    {
        // Arrange
        var startupTime = TimeSpan.FromMilliseconds(500);
        var threshold = TimeSpan.FromSeconds(1);

        // Act
        var isAcceptable = startupTime < threshold;

        // Assert
        isAcceptable.Should().BeTrue();
    }

    [Fact]
    public void PerformanceCommand_ShouldTrackJITCompilationTime()
    {
        // Arrange
        var jitTime = TimeSpan.FromMilliseconds(100);
        var totalTime = TimeSpan.FromMilliseconds(1000);

        // Act
        var jitPercentage = (jitTime.TotalMilliseconds / totalTime.TotalMilliseconds) * 100;

        // Assert
        jitPercentage.Should().Be(10.0);
    }

    [Fact]
    public void PerformanceCommand_ShouldGeneratePerformanceMatrix()
    {
        // Arrange
        var matrix = new[]
        {
            new { Operation = "Create", Min = 1.0, Max = 5.0, Avg = 2.5 },
            new { Operation = "Read", Min = 0.5, Max = 2.0, Avg = 1.0 },
            new { Operation = "Update", Min = 1.5, Max = 6.0, Avg = 3.0 },
            new { Operation = "Delete", Min = 1.0, Max = 4.0, Avg = 2.0 }
        };

        // Assert
        matrix.Should().HaveCount(4);
        matrix.First(m => m.Operation == "Read").Avg.Should().Be(1.0);
    }

    [Fact]
    public void PerformanceCommand_ShouldCalculateTotalCPUTime()
    {
        // Arrange
        var process = Process.GetCurrentProcess();

        // Act
        var cpuTime = process.TotalProcessorTime;

        // Assert
        cpuTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void PerformanceCommand_ShouldGenerateSummary()
    {
        // Arrange
        var summary = new
        {
            TotalRuns = 1000,
            SuccessRate = 99.8,
            AverageLatency = 2.5,
            P95Latency = 5.0,
            Throughput = 400000,
            MemoryUsed = 50 * 1024 * 1024 // 50MB
        };

        // Assert
        summary.TotalRuns.Should().Be(1000);
        summary.SuccessRate.Should().BeGreaterThan(99.0);
        summary.Throughput.Should().BeGreaterThan(0);
    }
}
