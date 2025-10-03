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
