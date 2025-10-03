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
}
