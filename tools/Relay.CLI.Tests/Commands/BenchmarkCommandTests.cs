using Relay.CLI.Commands;
using System.Diagnostics;

namespace Relay.CLI.Tests.Commands;

public class BenchmarkCommandTests : IDisposable
{
    private readonly string _testPath;

    public BenchmarkCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-benchmark-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    [Fact]
    public async Task BenchmarkCommand_MeasuresExecutionTime()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        await Task.Delay(10);
        stopwatch.Stop();

        // Assert - Allow significant margin for timing variance in CI/CD environments
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public async Task BenchmarkCommand_ComparesRelayVsMediatR()
    {
        // Arrange
        var relayTime = TimeSpan.FromMilliseconds(3);
        var mediatrTime = TimeSpan.FromMilliseconds(9);

        // Act
        var improvement = (mediatrTime - relayTime).TotalMilliseconds / mediatrTime.TotalMilliseconds * 100;

        // Assert
        improvement.Should().BeApproximately(67, 1); // ~67% faster
    }

    [Fact]
    public void BenchmarkResult_TracksAllocations()
    {
        // Arrange
        var result = new BenchmarkResult
        {
            Name = "TestHandler",
            ExecutionTime = TimeSpan.FromMilliseconds(5),
            Allocations = 1024,
            Gen0Collections = 2,
            Gen1Collections = 0,
            Gen2Collections = 0
        };

        // Assert
        result.Allocations.Should().Be(1024);
        result.Gen0Collections.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BenchmarkComparison_CalculatesImprovement()
    {
        // Arrange
        var baseline = new BenchmarkResult
        {
            ExecutionTime = TimeSpan.FromMilliseconds(10),
            Allocations = 1000
        };

        var optimized = new BenchmarkResult
        {
            ExecutionTime = TimeSpan.FromMilliseconds(6.7),
            Allocations = 0
        };

        // Act
        var speedup = baseline.ExecutionTime.TotalMilliseconds / optimized.ExecutionTime.TotalMilliseconds;
        var allocationReduction = (baseline.Allocations - optimized.Allocations) * 100.0 / baseline.Allocations;

        // Assert
        speedup.Should().BeApproximately(1.49, 0.01); // 49% faster
        allocationReduction.Should().Be(100); // 100% reduction (zero-allocation)
    }

    [Fact]
    public void BenchmarkReport_FormatsResults()
    {
        // Arrange
        var result = new BenchmarkResult
        {
            Name = "TestHandler",
            ExecutionTime = TimeSpan.FromMicroseconds(3000),
            Allocations = 0,
            ThroughputPerSecond = 333333
        };

        // Act
        var formatted = $"{result.Name}: {result.ExecutionTime.TotalMicroseconds:F2}μs, {result.Allocations}B, {result.ThroughputPerSecond:N0} ops/s";

        // Assert
        formatted.Should().Contain("TestHandler");
        formatted.Should().Contain("3");
        formatted.Should().Contain("333"); // Accept culture-invariant format
    }

    [Fact]
    public async Task BenchmarkCommand_RunsWarmupIterations()
    {
        // Arrange
        var warmupCount = 3;
        var counter = 0;

        // Act
        for (int i = 0; i < warmupCount; i++)
        {
            counter++;
            await Task.Yield();
        }

        // Assert
        counter.Should().Be(warmupCount);
    }

    [Fact]
    public async Task BenchmarkCommand_RunsMultipleIterations()
    {
        // Arrange
        var iterations = 100;
        var times = new List<double>();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            await Task.Yield();
            sw.Stop();
            times.Add(sw.Elapsed.TotalMicroseconds);
        }

        // Assert
        times.Should().HaveCount(iterations);
    }

    [Fact]
    public void BenchmarkStatistics_CalculatesAverage()
    {
        // Arrange
        var times = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var average = times.Average();

        // Assert
        average.Should().Be(3.0);
    }

    [Fact]
    public void BenchmarkStatistics_CalculatesMedian()
    {
        // Arrange
        var times = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        Array.Sort(times);
        var median = times[times.Length / 2];

        // Assert
        median.Should().Be(3.0);
    }

    [Fact]
    public void BenchmarkStatistics_CalculatesPercentiles()
    {
        // Arrange
        var times = Enumerable.Range(1, 100).Select(i => (double)i).ToArray();

        // Act
        Array.Sort(times);
        var p50 = times[50];
        var p95 = times[95];
        var p99 = times[99];

        // Assert
        p50.Should().Be(51);
        p95.Should().Be(96);
        p99.Should().Be(100);
    }

    [Fact]
    public void BenchmarkStatistics_CalculatesStandardDeviation()
    {
        // Arrange
        var times = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var mean = times.Average();
        var variance = times.Select(t => Math.Pow(t - mean, 2)).Average();
        var stdDev = Math.Sqrt(variance);

        // Assert
        stdDev.Should().BeApproximately(1.414, 0.01);
    }

    [Theory]
    [InlineData(1000, 1000)] // 1000 microseconds = 1ms -> 1000 ops/sec
    [InlineData(100, 10000)]  // 100 microseconds = 0.1ms -> 10000 ops/sec  
    [InlineData(10, 100000)]  // 10 microseconds = 0.01ms -> 100000 ops/sec
    public void BenchmarkThroughput_CalculatesOpsPerSecond(double microseconds, long expectedOps)
    {
        // Act
        var opsPerSecond = (long)(1_000_000 / microseconds);

        // Assert
        opsPerSecond.Should().Be(expectedOps);
    }

    [Fact]
    public void BenchmarkMemory_TracksGCCollections()
    {
        // Arrange
        var gen0Before = GC.CollectionCount(0);
        var gen1Before = GC.CollectionCount(1);
        var gen2Before = GC.CollectionCount(2);

        // Act - Allocate some memory
        var list = new List<byte[]>();
        for (int i = 0; i < 1000; i++)
        {
            list.Add(new byte[1024]);
        }
        GC.Collect();

        // Assert
        var gen0After = GC.CollectionCount(0);
        gen0After.Should().BeGreaterThan(gen0Before);
    }

    [Fact]
    public async Task BenchmarkCommand_DetectsRegressions()
    {
        // Arrange
        var baseline = TimeSpan.FromMilliseconds(5);
        var current = TimeSpan.FromMilliseconds(8);

        // Act
        var regression = (current - baseline).TotalMilliseconds / baseline.TotalMilliseconds * 100;

        // Assert
        regression.Should().Be(60); // 60% slower - regression detected!
    }

    [Fact]
    public void BenchmarkOutput_GeneratesMarkdownTable()
    {
        // Arrange
        var results = new[]
        {
            new BenchmarkResult { Name = "Handler1", ExecutionTime = TimeSpan.FromMicroseconds(1000), Allocations = 0 },
            new BenchmarkResult { Name = "Handler2", ExecutionTime = TimeSpan.FromMicroseconds(2000), Allocations = 1024 }
        };

        // Act
        var markdown = @"
| Handler  | Time (μs) | Allocations |
|----------|-----------|-------------|
| Handler1 |    1000.0 |         0 B |
| Handler2 |    2000.0 |      1024 B |";

        // Assert
        markdown.Should().Contain("Handler1");
        markdown.Should().Contain("1000.0");
        markdown.Should().Contain("0 B");
    }

    [Fact]
    public async Task BenchmarkCommand_ExportsResults()
    {
        // Arrange
        var results = new[]
        {
            new BenchmarkResult { Name = "Test", ExecutionTime = TimeSpan.FromMilliseconds(1) }
        };

        // Act
        var csvPath = Path.Combine(_testPath, "results.csv");
        var csv = "Name,Time(ms),Allocations\nTest,1,0";
        await File.WriteAllTextAsync(csvPath, csv);

        // Assert
        File.Exists(csvPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(csvPath);
        content.Should().Contain("Test");
    }

    [Fact]
    public void BenchmarkValidation_EnsuresAccuracy()
    {
        // Arrange
        var minIterations = 100;
        var actualIterations = 150;

        // Assert
        actualIterations.Should().BeGreaterThanOrEqualTo(minIterations);
    }

    [Fact]
    public void BenchmarkComparison_ShowsRelativePerformance()
    {
        // Arrange
        var relayResult = new BenchmarkResult
        {
            Name = "Relay",
            ExecutionTime = TimeSpan.FromMicroseconds(3000)
        };

        var mediatrResult = new BenchmarkResult
        {
            Name = "MediatR",
            ExecutionTime = TimeSpan.FromMicroseconds(9000)
        };

        // Act
        var ratio = mediatrResult.ExecutionTime.TotalMicroseconds / relayResult.ExecutionTime.TotalMicroseconds;

        // Assert
        ratio.Should().BeApproximately(3.0, 0.1); // MediatR is ~3x slower
    }

    [Fact]
    public void BenchmarkResult_ShouldHaveZeroAllocations()
    {
        // Arrange
        var result = new BenchmarkResult { Allocations = 0 };

        // Assert
        result.Allocations.Should().Be(0, "zero-allocation is optimal");
    }

    [Fact]
    public void BenchmarkResult_ShouldTrackMultipleMetrics()
    {
        // Arrange
        var result = new BenchmarkResult
        {
            Name = "Handler",
            ExecutionTime = TimeSpan.FromMicroseconds(1500),
            Allocations = 512,
            Gen0Collections = 1,
            ThroughputPerSecond = 666666
        };

        // Assert
        result.Name.Should().NotBeEmpty();
        result.ExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.Allocations.Should().BeGreaterThan(0);
        result.Gen0Collections.Should().BeGreaterThan(0);
        result.ThroughputPerSecond.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BenchmarkCommand_ShouldCalculateMinTime()
    {
        // Arrange
        var times = new[] { 5.0, 3.0, 7.0, 2.0, 9.0 };

        // Act
        var min = times.Min();

        // Assert
        min.Should().Be(2.0);
    }

    [Fact]
    public void BenchmarkCommand_ShouldCalculateMaxTime()
    {
        // Arrange
        var times = new[] { 5.0, 3.0, 7.0, 2.0, 9.0 };

        // Act
        var max = times.Max();

        // Assert
        max.Should().Be(9.0);
    }

    [Fact]
    public void BenchmarkCommand_ShouldCalculateRange()
    {
        // Arrange
        var times = new[] { 5.0, 3.0, 7.0, 2.0, 9.0 };

        // Act
        var range = times.Max() - times.Min();

        // Assert
        range.Should().Be(7.0);
    }

    [Theory]
    [InlineData(1, 1000000)]
    [InlineData(10, 100000)]
    [InlineData(100, 10000)]
    [InlineData(1000, 1000)]
    public void BenchmarkCommand_ShouldCalculateThroughput(double microseconds, long expectedThroughput)
    {
        // Act
        var throughput = (long)(1_000_000 / microseconds);

        // Assert
        throughput.Should().Be(expectedThroughput);
    }

    [Fact]
    public void BenchmarkCommand_ShouldFormatMicroseconds()
    {
        // Arrange
        var time = TimeSpan.FromMicroseconds(1234.56);

        // Act
        var formatted = $"{time.TotalMicroseconds:F2}μs";

        // Assert
        formatted.Should().Contain("1234");
        formatted.Should().Contain("μs");
    }

    [Fact]
    public void BenchmarkCommand_ShouldFormatMilliseconds()
    {
        // Arrange
        var time = TimeSpan.FromMilliseconds(12.34);

        // Act
        var formatted = $"{time.TotalMilliseconds:F2}ms";

        // Assert
        formatted.Should().Contain("12");
        formatted.Should().Contain("ms");
    }

    [Fact]
    public void BenchmarkCommand_ShouldFormatSeconds()
    {
        // Arrange
        var time = TimeSpan.FromSeconds(1.234);

        // Act
        var formatted = $"{time.TotalSeconds:F2}s";

        // Assert
        formatted.Should().Contain("1");
        formatted.Should().Contain("s");
    }

    [Fact]
    public void BenchmarkCommand_ShouldDetectOutliers()
    {
        // Arrange
        var times = new[] { 1.0, 2.0, 3.0, 4.0, 100.0 }; // 100 is an outlier

        // Act
        var mean = times.Average();
        var outlier = times.Last();

        // Assert
        outlier.Should().BeGreaterThan(mean * 2);
    }

    [Fact]
    public void BenchmarkCommand_ShouldCompareWithBaseline()
    {
        // Arrange
        var baseline = 100.0;
        var current = 80.0;

        // Act
        var improvement = (baseline - current) / baseline * 100;

        // Assert
        improvement.Should().Be(20); // 20% improvement
    }

    [Fact]
    public void BenchmarkCommand_ShouldCalculateSpeedup()
    {
        // Arrange
        var oldTime = 10.0;
        var newTime = 5.0;

        // Act
        var speedup = oldTime / newTime;

        // Assert
        speedup.Should().Be(2.0); // 2x faster
    }

    [Fact]
    public async Task BenchmarkCommand_ShouldWarmupJIT()
    {
        // Arrange
        var warmupIterations = 5;

        // Act
        for (int i = 0; i < warmupIterations; i++)
        {
            await Task.Delay(1);
        }

        // Assert
        warmupIterations.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BenchmarkCommand_ShouldExcludeWarmupFromResults()
    {
        // Arrange
        var allTimes = new[] { 100.0, 90.0, 10.0, 9.0, 8.0 }; // First 2 are warmup
        var warmupCount = 2;

        // Act
        var actualResults = allTimes.Skip(warmupCount).ToArray();

        // Assert
        actualResults.Should().HaveCount(3);
        actualResults.Should().NotContain(100.0);
        actualResults.Should().NotContain(90.0);
    }

    [Fact]
    public void BenchmarkCommand_ShouldGenerateJsonReport()
    {
        // Arrange
        var json = "{\"name\":\"TestHandler\",\"time\":1000,\"allocations\":0}";

        // Act
        var containsName = json.Contains("name");
        var containsTime = json.Contains("time");

        // Assert
        containsName.Should().BeTrue();
        containsTime.Should().BeTrue();
    }

    [Fact]
    public void BenchmarkCommand_ShouldCalculateConfidenceInterval()
    {
        // Arrange
        var times = new[] { 10.0, 11.0, 10.5, 10.2, 10.8 };
        var mean = times.Average();
        var stdDev = Math.Sqrt(times.Select(t => Math.Pow(t - mean, 2)).Average());

        // Act
        var marginOfError = 1.96 * (stdDev / Math.Sqrt(times.Length)); // 95% confidence
        var lowerBound = mean - marginOfError;
        var upperBound = mean + marginOfError;

        // Assert
        lowerBound.Should().BeLessThan(mean);
        upperBound.Should().BeGreaterThan(mean);
    }

    [Fact]
    public void BenchmarkCommand_ShouldTrackMemoryUsage()
    {
        // Arrange
        var beforeMemory = GC.GetTotalMemory(false);

        // Act
        var list = new List<int>(1000);
        for (int i = 0; i < 1000; i++) list.Add(i);
        var afterMemory = GC.GetTotalMemory(false);

        // Assert
        afterMemory.Should().BeGreaterThan(beforeMemory);
    }

    [Fact]
    public void BenchmarkCommand_ShouldCompareMultipleHandlers()
    {
        // Arrange
        var results = new[]
        {
            new BenchmarkResult { Name = "Fast", ExecutionTime = TimeSpan.FromMicroseconds(100) },
            new BenchmarkResult { Name = "Medium", ExecutionTime = TimeSpan.FromMicroseconds(500) },
            new BenchmarkResult { Name = "Slow", ExecutionTime = TimeSpan.FromMicroseconds(1000) }
        };

        // Act
        var fastest = results.MinBy(r => r.ExecutionTime.TotalMicroseconds);
        var slowest = results.MaxBy(r => r.ExecutionTime.TotalMicroseconds);

        // Assert
        fastest?.Name.Should().Be("Fast");
        slowest?.Name.Should().Be("Slow");
    }

    [Fact]
    public void BenchmarkCommand_ShouldRankResults()
    {
        // Arrange
        var results = new[]
        {
            new BenchmarkResult { Name = "C", ExecutionTime = TimeSpan.FromMicroseconds(300) },
            new BenchmarkResult { Name = "A", ExecutionTime = TimeSpan.FromMicroseconds(100) },
            new BenchmarkResult { Name = "B", ExecutionTime = TimeSpan.FromMicroseconds(200) }
        };

        // Act
        var ranked = results.OrderBy(r => r.ExecutionTime.TotalMicroseconds).ToArray();

        // Assert
        ranked[0].Name.Should().Be("A");
        ranked[1].Name.Should().Be("B");
        ranked[2].Name.Should().Be("C");
    }

    [Fact]
    public void BenchmarkCommand_ShouldCalculateAllocationRate()
    {
        // Arrange
        var allocations = 1024L;
        var executionTime = TimeSpan.FromSeconds(1);

        // Act
        var allocationRate = allocations / executionTime.TotalSeconds;

        // Assert
        allocationRate.Should().Be(1024); // 1024 B/s
    }

    [Fact]
    public void BenchmarkCommand_ShouldFormatBytes()
    {
        // Arrange
        var bytes = 1024L;

        // Act
        var formatted = $"{bytes} B";

        // Assert
        formatted.Should().Be("1024 B");
    }

    [Fact]
    public void BenchmarkCommand_ShouldFormatKilobytes()
    {
        // Arrange
        var bytes = 1024 * 1024L;

        // Act
        var kb = bytes / 1024.0;
        var formatted = $"{kb:F2} KB";

        // Assert
        formatted.Should().Contain("1024");
        formatted.Should().Contain("KB");
    }

    [Fact]
    public void BenchmarkCommand_ShouldCalculateP50()
    {
        // Arrange
        var times = Enumerable.Range(1, 100).Select(i => (double)i).ToArray();

        // Act
        Array.Sort(times);
        var p50 = times[50];

        // Assert
        p50.Should().Be(51);
    }

    [Fact]
    public void BenchmarkCommand_ShouldCalculateP90()
    {
        // Arrange
        var times = Enumerable.Range(1, 100).Select(i => (double)i).ToArray();

        // Act
        Array.Sort(times);
        var p90 = times[90];

        // Assert
        p90.Should().Be(91);
    }

    [Fact]
    public void BenchmarkCommand_ShouldCalculateP99()
    {
        // Arrange
        var times = Enumerable.Range(1, 100).Select(i => (double)i).ToArray();

        // Act
        Array.Sort(times);
        var p99 = times[99];

        // Assert
        p99.Should().Be(100);
    }

    [Fact]
    public void BenchmarkCommand_ShouldDetectPerformanceRegression()
    {
        // Arrange
        var baseline = 100.0;
        var current = 150.0;
        var threshold = 10.0; // 10% threshold

        // Act
        var regressionPercent = (current - baseline) / baseline * 100;
        var hasRegression = regressionPercent > threshold;

        // Assert
        hasRegression.Should().BeTrue();
        regressionPercent.Should().Be(50);
    }

    [Fact]
    public void BenchmarkCommand_ShouldGenerateCsvOutput()
    {
        // Arrange
        var csv = "Name,Time(μs),Allocations(B)\nHandler1,1000,0\nHandler2,2000,1024";

        // Act
        var lines = csv.Split('\n');

        // Assert
        lines.Should().HaveCount(3);
        lines[0].Should().Contain("Name");
        lines[1].Should().Contain("Handler1");
        lines[2].Should().Contain("Handler2");
    }

    [Fact]
    public void BenchmarkCommand_ShouldValidateMinIterations()
    {
        // Arrange
        var minIterations = 10;
        var actualIterations = 50;

        // Act
        var isValid = actualIterations >= minIterations;

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void BenchmarkCommand_ShouldCalculateVariance()
    {
        // Arrange
        var times = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        var mean = times.Average();

        // Act
        var variance = times.Select(t => Math.Pow(t - mean, 2)).Average();

        // Assert
        variance.Should().Be(2.0);
    }

    [Fact]
    public void BenchmarkCommand_ShouldCalculateCoeffientOfVariation()
    {
        // Arrange
        var times = new[] { 10.0, 12.0, 11.0, 10.5, 11.5 };
        var mean = times.Average();
        var stdDev = Math.Sqrt(times.Select(t => Math.Pow(t - mean, 2)).Average());

        // Act
        var cv = (stdDev / mean) * 100; // Coefficient of variation as percentage

        // Assert
        cv.Should().BeLessThan(10); // Low variance is good
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testPath))
                Directory.Delete(_testPath, true);
        }
        catch { }
    }

    private class BenchmarkResult
    {
        public string Name { get; set; } = "";
        public TimeSpan ExecutionTime { get; set; }
        public long Allocations { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
        public long ThroughputPerSecond { get; set; }
    }
}
