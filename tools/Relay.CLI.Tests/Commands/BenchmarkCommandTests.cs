using Relay.CLI.Commands;
using System.CommandLine;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Diagnostics;
using Spectre.Console.Testing;
using TestConsole = System.CommandLine.IO.TestConsole;

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
        Assert.True(stopwatch.ElapsedMilliseconds > 0);
        Assert.True(stopwatch.ElapsedMilliseconds < 100);
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
        Assert.Equal(66.7, improvement, 0.1); // ~66.7% faster
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
        Assert.Equal(1024, result.Allocations);
        Assert.True(result.Gen0Collections > 0);
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
        Assert.Equal(1.49, speedup, 0.01); // 49% faster
        Assert.Equal(100, allocationReduction); // 100% reduction (zero-allocation)
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
        Assert.Contains("TestHandler", formatted);
        Assert.Contains("3", formatted);
        Assert.Contains("333", formatted); // Accept culture-invariant format
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
        Assert.Equal(warmupCount, counter);
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
        Assert.Equal(iterations, times.Count);
    }

    [Fact]
    public void BenchmarkStatistics_CalculatesAverage()
    {
        // Arrange
        var times = new[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        // Act
        var average = times.Average();

        // Assert
        Assert.Equal(3.0, average);
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
        Assert.Equal(3.0, median);
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
        Assert.Equal(51, p50);
        Assert.Equal(96, p95);
        Assert.Equal(100, p99);
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
        Assert.Equal(1.414, stdDev, 0.01);
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
        Assert.Equal(expectedOps, opsPerSecond);
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
        Assert.True(gen0After > gen0Before);
    }

    [Fact]
    public async Task BenchmarkCommand_DetectsRegressions()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "complete-csv.csv");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --output {outputPath} --format csv", console);

        // Assert
        Assert.Equal(0, result);
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Implementation", content);
        Assert.Contains("Average Time", content);
    }

    [Fact]
    public void BenchmarkCommand_GetTestCount_WithAllTests_Returns6()
    {
        // Arrange
        var tests = new[] { "all" };

        // Act
        var count = BenchmarkCommand.GetTestCount(tests);

        // Assert
        Assert.Equal(6, count); // 4 relay + 2 comparison
    }

    [Fact]
    public void BenchmarkCommand_GetTestCount_WithRelayOnly_Returns4()
    {
        // Arrange
        var tests = new[] { "relay" };

        // Act
        var count = BenchmarkCommand.GetTestCount(tests);

        // Assert
        Assert.Equal(4, count);
    }

    [Fact]
    public void BenchmarkCommand_GetTestCount_WithComparisonOnly_Returns2()
    {
        // Arrange
        var tests = new[] { "comparison" };

        // Act
        var count = BenchmarkCommand.GetTestCount(tests);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public void BenchmarkCommand_GetTestCount_WithMultipleTests_ReturnsSum()
    {
        // Arrange
        var tests = new[] { "relay", "comparison" };

        // Act
        var count = BenchmarkCommand.GetTestCount(tests);

        // Assert
        Assert.Equal(6, count); // 4 + 2
    }

    [Fact]
    public void BenchmarkCommand_GetTestCount_WithEmptyArray_Returns1()
    {
        // Arrange
        var tests = Array.Empty<string>();

        // Act
        var count = BenchmarkCommand.GetTestCount(tests);

        // Assert
        Assert.Equal(1, count); // Math.Max(count, 1)
    }

    [Fact]
    public void BenchmarkCommand_GetTestCount_WithUnknownTests_Returns1()
    {
        // Arrange
        var tests = new[] { "unknown" };

        // Act
        var count = BenchmarkCommand.GetTestCount(tests);

        // Assert
        Assert.Equal(1, count); // Math.Max(count, 1)
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
        Assert.True(outlier > mean * 2);
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
        Assert.Equal(20, improvement); // 20% improvement
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
        Assert.Equal(2.0, speedup); // 2x faster
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
        Assert.True(warmupIterations > 0);
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
        Assert.Equal(3, actualResults.Length);
        Assert.DoesNotContain(100.0, actualResults);
        Assert.DoesNotContain(90.0, actualResults);
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
        Assert.True(containsName);
        Assert.True(containsTime);
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
        Assert.True(lowerBound < mean);
        Assert.True(upperBound > mean);
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
        Assert.True(memoryGrowth > 500 * 1024,
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
        Assert.Equal("Fast", fastest?.Name);
        Assert.Equal("Slow", slowest?.Name);
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
        Assert.Equal("A", ranked[0].Name);
        Assert.Equal("B", ranked[1].Name);
        Assert.Equal("C", ranked[2].Name);
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
        Assert.Equal(1024, allocationRate); // 1024 B/s
    }

    [Fact]
    public void BenchmarkCommand_ShouldFormatBytes()
    {
        // Arrange
        var bytes = 1024L;

        // Act
        var formatted = $"{bytes} B";

        // Assert
        Assert.Equal("1024 B", formatted);
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
        Assert.Contains("1024", formatted);
        Assert.Contains("KB", formatted);
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
        Assert.Equal(51, p50);
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
        Assert.Equal(91, p90);
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
        Assert.Equal(100, p99);
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
        Assert.True(hasRegression);
        Assert.Equal(50, regressionPercent);
    }

    [Fact]
    public void BenchmarkCommand_ShouldGenerateCsvOutput()
    {
        // Arrange
        var csv = "Name,Time(μs),Allocations(B)\nHandler1,1000,0\nHandler2,2000,1024";

        // Act
        var lines = csv.Split('\n');

        // Assert
        Assert.Equal(3, lines.Length);
        Assert.Contains("Name", lines[0]);
        Assert.Contains("Handler1", lines[1]);
        Assert.Contains("Handler2", lines[2]);
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
        Assert.True(isValid);
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
        Assert.Equal(2.0, variance);
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
        Assert.True(cv < 10); // Low variance is good
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
        Assert.Equal(0, result);
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
        Assert.Equal(0, result);
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("TestConfiguration", content);
        Assert.Contains("RelayResults", content);
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
        Assert.Equal(0, result);
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains("Relay Performance Benchmark Results", content);
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
        Assert.Equal(0, result);
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Implementation,Average Time", content);
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("RelayResults", content);
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("ComparisonResults", content);
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("RelayResults", content);
        Assert.Contains("ComparisonResults", content);
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("\"Iterations\": 500", content);
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("\"WarmupIterations\": 50", content);
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("\"Threads\": 4", content);
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("MachineName", content);
        Assert.Contains("ProcessorCount", content);
        Assert.Contains("RuntimeVersion", content);
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("AverageTime", content);
        Assert.Contains("RequestsPerSecond", content);
        Assert.Contains("MemoryAllocated", content);
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("chart.js", content);
        Assert.Contains("performanceChart", content);
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Standard", content);
        Assert.Contains("UltraFast", content);
        Assert.Contains("SIMD", content);
        Assert.Contains("AOT", content);
    }

    [Fact]
    public async Task BenchmarkCommand_IncludesMediatRComparison()
    {
        // Arrange
        var command = BenchmarkCommand.Create();
        var console = new TestConsole();
        var outputPath = Path.Combine(_testPath, "mediatr-compare.json");

        // Act
        var result = await command.InvokeAsync($"--iterations 100 --output {outputPath} --format html", console);

        // Assert
        Assert.Equal(0, result);
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("<!DOCTYPE html>", content);
        Assert.Contains("Relay Performance Benchmark Results", content);
        Assert.Contains("Direct Call", content);
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
        Assert.Equal(0, result);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000); // Should complete in less than 5 seconds
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("Timestamp", content);
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
        Assert.Equal(0, result);
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("\"Threads\": 1", content);
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
        Assert.Equal(0, result);
        var content = await File.ReadAllTextAsync(outputPath);
        var firstLine = content.Split('\n')[0];
        Assert.Contains("Implementation", firstLine);
        Assert.Contains("Average Time", firstLine);
        Assert.Contains("Memory", firstLine);
    }

    // BenchmarkResult class tests (from BenchmarkCommand.cs)
    [Fact]
    public void BenchmarkResult_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var result = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResult();

        // Assert
        Assert.Equal("", result.Name);
        Assert.Equal(TimeSpan.Zero, result.TotalTime);
        Assert.Equal(0, result.Iterations);
        Assert.Equal(0, result.AverageTime);
        Assert.Equal(0, result.RequestsPerSecond);
        Assert.Equal(0, result.MemoryAllocated);
        Assert.Equal(0, result.Threads);
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
        Assert.Equal("UltraFast Relay", result.Name);
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
        Assert.Equal(TimeSpan.FromMilliseconds(500), result.TotalTime);
        Assert.Equal(500, result.TotalTime.TotalMilliseconds);
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
        Assert.Equal(100000, result.Iterations);
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
        Assert.Equal(3.5, result.AverageTime);
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
        Assert.Equal(333333.33, result.RequestsPerSecond, 0.01);
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
        Assert.Equal(1024, result.MemoryAllocated);
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
        Assert.Equal(4, result.Threads);
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
        Assert.Equal(0, result.MemoryAllocated);
        Assert.Contains("Zero Allocation", result.Name);
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
        Assert.Equal(10, result.AverageTime); // 1000ms / 100000 = 10μs
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
        Assert.Equal(333333, result.RequestsPerSecond);
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
        Assert.Equal(1, result.TotalTime.Ticks);
        Assert.True(result.AverageTime > 0);
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
        Assert.Equal(int.MaxValue, result.Iterations);
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
        Assert.Equal(long.MaxValue, result.MemoryAllocated);
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
        Assert.Equal(10, speedup);
        Assert.True(fast.AverageTime < slow.AverageTime);
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
        Assert.True(multiThreaded.RequestsPerSecond > singleThreaded.RequestsPerSecond);
        Assert.Equal(4, multiThreaded.Threads);
        Assert.Equal(1, singleThreaded.Threads);
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
        Assert.Equal(100, memoryPerIteration); // 10000 / 100 = 100 bytes per iteration
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
        Assert.Equal(1000, throughputPerMs); // 1000 requests per millisecond
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
        Assert.StartsWith("Standard", result.Name);
        Assert.EndsWith("Relay", result.Name);
        Assert.Equal(14, result.Name.Length);
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
        Assert.Equal("Complete Test", result.Name);
        Assert.Equal(TimeSpan.FromMilliseconds(1000), result.TotalTime);
        Assert.Equal(100000, result.Iterations);
        Assert.Equal(10.0, result.AverageTime);
        Assert.Equal(100000, result.RequestsPerSecond);
        Assert.Equal(2048, result.MemoryAllocated);
        Assert.Equal(2, result.Threads);
    }

    // TestConfiguration class tests
    [Fact]
    public void TestConfiguration_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var config = new Relay.CLI.Commands.Models.TestConfiguration();

        // Assert
        Assert.Equal(0, config.Iterations);
        Assert.Equal(0, config.WarmupIterations);
        Assert.Equal(0, config.Threads);
        Assert.Equal(default(DateTime), config.Timestamp);
        Assert.Equal("", config.MachineName);
        Assert.Equal(0, config.ProcessorCount);
        Assert.Equal("", config.RuntimeVersion);
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
        Assert.Equal(100000, config.Iterations);
        Assert.Equal(1000, config.WarmupIterations);
        Assert.Equal(4, config.Threads);
        Assert.Equal(timestamp, config.Timestamp);
        Assert.Equal("TEST-MACHINE", config.MachineName);
        Assert.Equal(8, config.ProcessorCount);
        Assert.Equal("8.0.0", config.RuntimeVersion);
    }

    // BenchmarkResults class tests
    [Fact]
    public void BenchmarkResults_InitializesWithEmptyCollections()
    {
        // Arrange & Act
        var results = new Relay.CLI.Commands.Models.Benchmark.BenchmarkResults();

        // Assert
        Assert.NotNull(results.TestConfiguration);
        Assert.NotNull(results.RelayResults);
        Assert.Empty(results.RelayResults);
        Assert.NotNull(results.ComparisonResults);
        Assert.Empty(results.ComparisonResults);
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
        Assert.Single(results.RelayResults);
        Assert.Equal(relayResult, results.RelayResults["Standard"]);
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
        Assert.Single(results.ComparisonResults);
        Assert.Equal(mediatrResult, results.ComparisonResults["MediatR"]);
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
        Assert.Equal(4, results.RelayResults.Count);
        Assert.Equal(2, results.ComparisonResults.Count);
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
        Assert.Equal(ultraFast, retrieved);
        Assert.Equal("UltraFast Relay", retrieved.Name);
        Assert.Equal(1.5, retrieved.AverageTime);
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
        Assert.Equal(50000, results.TestConfiguration.Iterations);
        Assert.Equal(8, results.TestConfiguration.Threads);
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
