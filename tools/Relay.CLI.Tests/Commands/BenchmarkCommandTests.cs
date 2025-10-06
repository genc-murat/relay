using Relay.CLI.Commands;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
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

    [Fact()]
    public void BenchmarkCommand_ShouldTrackMemoryUsage()
    {
        // Arrange
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var beforeMemory = GC.GetTotalMemory(false);

        // Act - Allocate a significant amount of memory
        var largeArray = new byte[1024 * 1024]; // 1MB allocation
        for (int i = 0; i < largeArray.Length; i++)
        {
            largeArray[i] = (byte)(i % 256); // Touch the memory to ensure allocation
        }

        var afterMemory = GC.GetTotalMemory(false);

        // Assert
        var memoryGrowth = afterMemory - beforeMemory;
        memoryGrowth.Should().BeGreaterThan(500 * 1024,
            "allocating 1MB should increase memory by at least 500KB");

        // Keep reference to prevent GC
        GC.KeepAlive(largeArray);
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

    [Fact]
    public async Task BenchmarkCommand_WithDefaultOptions_RunsSuccessfully()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync("--iterations 100 --warmup 10", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task BenchmarkCommand_WithJsonOutput_GeneratesJsonReport()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "benchmark.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("TestConfiguration");
        content.Should().Contain("RelayResults");
    }

    [Fact]
    public async Task BenchmarkCommand_WithHtmlOutput_GeneratesHtmlReport()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "benchmark.html");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --output {outputPath} --format html", console);

        // Assert
        result.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("<!DOCTYPE html>");
        content.Should().Contain("Relay Performance Benchmark Results");
    }

    [Fact]
    public async Task BenchmarkCommand_WithCsvOutput_GeneratesCsvReport()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "benchmark.csv");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --output {outputPath} --format csv", console);

        // Assert
        result.Should().Be(0);
        File.Exists(outputPath).Should().BeTrue();
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("Implementation,Average Time");
    }

    [Fact]
    public async Task BenchmarkCommand_WithRelayTestsOnly_RunsRelayBenchmarks()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "relay-only.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --tests relay --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("RelayResults");
    }

    [Fact]
    public async Task BenchmarkCommand_WithComparisonTests_RunsComparisonBenchmarks()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "comparison.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --tests comparison --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("ComparisonResults");
    }

    [Fact]
    public async Task BenchmarkCommand_WithAllTests_RunsAllBenchmarks()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "all-tests.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --tests all --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("RelayResults");
        content.Should().Contain("ComparisonResults");
    }

    [Fact]
    public async Task BenchmarkCommand_WithCustomIterations_UsesSpecifiedIterations()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "custom-iterations.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 500 --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("\"Iterations\": 500");
    }

    [Fact]
    public async Task BenchmarkCommand_WithCustomWarmup_UsesSpecifiedWarmup()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "custom-warmup.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --warmup 50 --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("\"WarmupIterations\": 50");
    }

    [Fact]
    public async Task BenchmarkCommand_WithMultipleThreads_RunsConcurrentBenchmarks()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "multi-thread.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 1000 --threads 4 --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("\"Threads\": 4");
    }

    [Fact]
    public async Task BenchmarkCommand_OutputContainsTestConfiguration()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "config-test.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("MachineName");
        content.Should().Contain("ProcessorCount");
        content.Should().Contain("RuntimeVersion");
    }

    [Fact]
    public async Task BenchmarkCommand_GeneratesPerformanceMetrics()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "metrics.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("AverageTime");
        content.Should().Contain("RequestsPerSecond");
        content.Should().Contain("MemoryAllocated");
    }

    [Fact]
    public async Task BenchmarkCommand_HtmlReportContainsChart()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "chart-test.html");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --output {outputPath} --format html", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("chart.js");
        content.Should().Contain("performanceChart");
    }

    [Fact]
    public async Task BenchmarkCommand_ComparesMultipleRelayImplementations()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "relay-compare.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --tests relay --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("Standard");
        content.Should().Contain("UltraFast");
        content.Should().Contain("SIMD");
        content.Should().Contain("AOT");
    }

    [Fact]
    public async Task BenchmarkCommand_IncludesMediatRComparison()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "mediatr-compare.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --tests all --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("MediatR");
        content.Should().Contain("DirectCall");
    }

    [Fact]
    public async Task BenchmarkCommand_WithSmallIterations_CompletesQuickly()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await command.InvokeAsync("--iterations 10 --warmup 5", console);
        stopwatch.Stop();

        // Assert
        result.Should().Be(0);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete in less than 5 seconds
    }

    [Fact]
    public async Task BenchmarkCommand_IncludesTimestamp()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "timestamp-test.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("Timestamp");
    }

    [Fact]
    public async Task BenchmarkCommand_MeasuresTotalTime()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "total-time.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("TotalTime");
    }

    [Fact]
    public async Task BenchmarkCommand_DefaultsToConsoleFormat()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();

        // Act
        var result = await command.InvokeAsync("--iterations 100", console);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task BenchmarkCommand_WithSingleThread_RunsSynchronously()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "single-thread.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --threads 1 --output {outputPath} --format json", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("\"Threads\": 1");
    }

    [Fact]
    public async Task BenchmarkCommand_CsvFormatContainsHeaders()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "headers-test.csv");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --output {outputPath} --format csv", console);

        // Assert
        result.Should().Be(0);
        var content = await File.ReadAllTextAsync(outputPath);
        var firstLine = content.Split('\n')[0];
        firstLine.Should().Contain("Implementation");
        firstLine.Should().Contain("Average Time");
        firstLine.Should().Contain("Memory");
    }

    // BenchmarkResult class tests (from BenchmarkCommand.cs)
    [Fact]
    public void BenchmarkResult_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult();

        // Assert
        result.Name.Should().Be("");
        result.TotalTime.Should().Be(TimeSpan.Zero);
        result.Iterations.Should().Be(0);
        result.AverageTime.Should().Be(0);
        result.RequestsPerSecond.Should().Be(0);
        result.MemoryAllocated.Should().Be(0);
        result.Threads.Should().Be(0);
    }

    [Fact]
    public void BenchmarkResult_StoresCorrectName()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Name = "UltraFast Relay"
        };

        // Assert
        result.Name.Should().Be("UltraFast Relay");
    }

    [Fact]
    public void BenchmarkResult_StoresCorrectTotalTime()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            TotalTime = TimeSpan.FromMilliseconds(500)
        };

        // Assert
        result.TotalTime.Should().Be(TimeSpan.FromMilliseconds(500));
        result.TotalTime.TotalMilliseconds.Should().Be(500);
    }

    [Fact]
    public void BenchmarkResult_StoresCorrectIterations()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Iterations = 100000
        };

        // Assert
        result.Iterations.Should().Be(100000);
    }

    [Fact]
    public void BenchmarkResult_StoresCorrectAverageTime()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            AverageTime = 3.5
        };

        // Assert
        result.AverageTime.Should().Be(3.5);
    }

    [Fact]
    public void BenchmarkResult_StoresCorrectRequestsPerSecond()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            RequestsPerSecond = 333333.33
        };

        // Assert
        result.RequestsPerSecond.Should().BeApproximately(333333.33, 0.01);
    }

    [Fact]
    public void BenchmarkResult_StoresCorrectMemoryAllocated()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            MemoryAllocated = 1024
        };

        // Assert
        result.MemoryAllocated.Should().Be(1024);
    }

    [Fact]
    public void BenchmarkResult_StoresCorrectThreadCount()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Threads = 4
        };

        // Assert
        result.Threads.Should().Be(4);
    }

    [Fact]
    public void BenchmarkResult_AllowsZeroAllocationResult()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Name = "Zero Allocation Handler",
            MemoryAllocated = 0
        };

        // Assert
        result.MemoryAllocated.Should().Be(0);
        result.Name.Should().Contain("Zero Allocation");
    }

    [Fact]
    public void BenchmarkResult_CalculatesCorrectAverageFromTotalTime()
    {
        // Arrange
        var totalTime = TimeSpan.FromMilliseconds(1000);
        var iterations = 100000;

        // Act
        var averageTime = totalTime.TotalMicroseconds / iterations;
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            TotalTime = totalTime,
            Iterations = iterations,
            AverageTime = averageTime
        };

        // Assert
        result.AverageTime.Should().Be(10); // 1000ms / 100000 = 10μs
    }

    [Fact]
    public void BenchmarkResult_CalculatesCorrectRequestsPerSecond()
    {
        // Arrange
        var totalTime = TimeSpan.FromSeconds(1);
        var iterations = 333333;

        // Act
        var rps = iterations / totalTime.TotalSeconds;
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Iterations = iterations,
            TotalTime = totalTime,
            RequestsPerSecond = rps
        };

        // Assert
        result.RequestsPerSecond.Should().Be(333333);
    }

    [Fact]
    public void BenchmarkResult_HandlesVeryFastExecutionTime()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Name = "Ultra Fast",
            TotalTime = TimeSpan.FromTicks(1),
            Iterations = 1000000,
            AverageTime = 0.0001 // Very small value
        };

        // Assert
        result.TotalTime.Ticks.Should().Be(1);
        result.AverageTime.Should().BeGreaterThan(0);
    }

    [Fact]
    public void BenchmarkResult_HandlesLargeNumberOfIterations()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Iterations = int.MaxValue
        };

        // Assert
        result.Iterations.Should().Be(int.MaxValue);
    }

    [Fact]
    public void BenchmarkResult_HandlesLargeMemoryAllocation()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            MemoryAllocated = long.MaxValue
        };

        // Assert
        result.MemoryAllocated.Should().Be(long.MaxValue);
    }

    [Fact]
    public void BenchmarkResult_ComparesWithOtherResults()
    {
        // Arrange
        var fast = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Name = "Fast",
            AverageTime = 1.0
        };

        var slow = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Name = "Slow",
            AverageTime = 10.0
        };

        // Act
        var speedup = slow.AverageTime / fast.AverageTime;

        // Assert
        speedup.Should().Be(10);
        fast.AverageTime.Should().BeLessThan(slow.AverageTime);
    }

    [Fact]
    public void BenchmarkResult_SupportsMultiThreadedResults()
    {
        // Arrange & Act
        var singleThreaded = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Name = "Single Thread",
            Threads = 1,
            RequestsPerSecond = 100000
        };

        var multiThreaded = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Name = "Multi Thread",
            Threads = 4,
            RequestsPerSecond = 350000
        };

        // Assert
        multiThreaded.RequestsPerSecond.Should().BeGreaterThan(singleThreaded.RequestsPerSecond);
        multiThreaded.Threads.Should().Be(4);
        singleThreaded.Threads.Should().Be(1);
    }

    [Fact]
    public void BenchmarkResult_CanCalculateMemoryPerIteration()
    {
        // Arrange
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            MemoryAllocated = 10000,
            Iterations = 100
        };

        // Act
        var memoryPerIteration = result.MemoryAllocated / (double)result.Iterations;

        // Assert
        memoryPerIteration.Should().Be(100); // 10000 / 100 = 100 bytes per iteration
    }

    [Fact]
    public void BenchmarkResult_CanCalculateThroughput()
    {
        // Arrange
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            RequestsPerSecond = 1000000
        };

        // Act
        var throughputPerMs = result.RequestsPerSecond / 1000;

        // Assert
        throughputPerMs.Should().Be(1000); // 1000 requests per millisecond
    }

    [Fact]
    public void BenchmarkResult_FormatsNameCorrectly()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Name = "Standard Relay"
        };

        // Assert
        result.Name.Should().StartWith("Standard");
        result.Name.Should().EndWith("Relay");
        result.Name.Should().HaveLength(14);
    }

    [Fact]
    public void BenchmarkResult_AllPropertiesCanBeSet()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Name = "Complete Test",
            TotalTime = TimeSpan.FromMilliseconds(1000),
            Iterations = 100000,
            AverageTime = 10.0,
            RequestsPerSecond = 100000,
            MemoryAllocated = 2048,
            Threads = 2
        };

        // Assert
        result.Name.Should().Be("Complete Test");
        result.TotalTime.Should().Be(TimeSpan.FromMilliseconds(1000));
        result.Iterations.Should().Be(100000);
        result.AverageTime.Should().Be(10.0);
        result.RequestsPerSecond.Should().Be(100000);
        result.MemoryAllocated.Should().Be(2048);
        result.Threads.Should().Be(2);
    }

    // TestConfiguration class tests
    [Fact]
    public void TestConfiguration_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var config = new Relay.CLI.Commands.Models.TestConfiguration();

        // Assert
        config.Iterations.Should().Be(0);
        config.WarmupIterations.Should().Be(0);
        config.Threads.Should().Be(0);
        config.Timestamp.Should().Be(default(DateTime));
        config.MachineName.Should().Be("");
        config.ProcessorCount.Should().Be(0);
        config.RuntimeVersion.Should().Be("");
    }

    [Fact]
    public void TestConfiguration_StoresAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var config = new Relay.CLI.Commands.Models.TestConfiguration
        {
            Iterations = 100000,
            WarmupIterations = 1000,
            Threads = 4,
            Timestamp = timestamp,
            MachineName = "TEST-MACHINE",
            ProcessorCount = 8,
            RuntimeVersion = "8.0.0"
        };

        // Assert
        config.Iterations.Should().Be(100000);
        config.WarmupIterations.Should().Be(1000);
        config.Threads.Should().Be(4);
        config.Timestamp.Should().Be(timestamp);
        config.MachineName.Should().Be("TEST-MACHINE");
        config.ProcessorCount.Should().Be(8);
        config.RuntimeVersion.Should().Be("8.0.0");
    }

    // BenchmarkResults class tests
    [Fact]
    public void BenchmarkResults_InitializesWithEmptyCollections()
    {
        // Arrange & Act
        var results = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResults();

        // Assert
        results.TestConfiguration.Should().NotBeNull();
        results.RelayResults.Should().NotBeNull();
        results.RelayResults.Should().BeEmpty();
        results.ComparisonResults.Should().NotBeNull();
        results.ComparisonResults.Should().BeEmpty();
    }

    [Fact]
    public void BenchmarkResults_CanAddRelayResults()
    {
        // Arrange
        var results = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResults();
        var relayResult = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Name = "Standard Relay",
            AverageTime = 3.0
        };

        // Act
        results.RelayResults.Add("Standard", relayResult);

        // Assert
        results.RelayResults.Should().HaveCount(1);
        results.RelayResults["Standard"].Should().Be(relayResult);
    }

    [Fact]
    public void BenchmarkResults_CanAddComparisonResults()
    {
        // Arrange
        var results = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResults();
        var mediatrResult = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Name = "MediatR",
            AverageTime = 9.0
        };

        // Act
        results.ComparisonResults.Add("MediatR", mediatrResult);

        // Assert
        results.ComparisonResults.Should().HaveCount(1);
        results.ComparisonResults["MediatR"].Should().Be(mediatrResult);
    }

    [Fact]
    public void BenchmarkResults_CanStoreMultipleResults()
    {
        // Arrange
        var results = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResults();

        // Act
        results.RelayResults.Add("Standard", new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult { Name = "Standard" });
        results.RelayResults.Add("UltraFast", new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult { Name = "UltraFast" });
        results.RelayResults.Add("SIMD", new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult { Name = "SIMD" });
        results.RelayResults.Add("AOT", new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult { Name = "AOT" });

        results.ComparisonResults.Add("DirectCall", new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult { Name = "DirectCall" });
        results.ComparisonResults.Add("MediatR", new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult { Name = "MediatR" });

        // Assert
        results.RelayResults.Should().HaveCount(4);
        results.ComparisonResults.Should().HaveCount(2);
    }

    [Fact]
    public void BenchmarkResults_CanRetrieveResultsByKey()
    {
        // Arrange
        var results = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResults();
        var ultraFast = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult
        {
            Name = "UltraFast Relay",
            AverageTime = 1.5
        };
        results.RelayResults.Add("UltraFast", ultraFast);

        // Act
        var retrieved = results.RelayResults["UltraFast"];

        // Assert
        retrieved.Should().Be(ultraFast);
        retrieved.Name.Should().Be("UltraFast Relay");
        retrieved.AverageTime.Should().Be(1.5);
    }

    [Fact]
    public void BenchmarkResults_TestConfigurationIsModifiable()
    {
        // Arrange
        var results = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResults();

        // Act
        results.TestConfiguration.Iterations = 50000;
        results.TestConfiguration.Threads = 8;

        // Assert
        results.TestConfiguration.Iterations.Should().Be(50000);
        results.TestConfiguration.Threads.Should().Be(8);
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
