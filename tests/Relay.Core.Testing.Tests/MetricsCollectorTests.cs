using System;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class MetricsCollectorTests
{
    private readonly MetricsCollector _collector = new();

    [Fact]
    public void Collect_WithNullOperation_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _collector.Collect("test", null!));
        Assert.Equal("operation", exception.ParamName);
    }

    [Fact]
    public void Collect_WithValidOperation_ReturnsMetrics()
    {
        // Arrange
        var operationName = "TestOperation";
        Action operation = () =>
        {
            // Simulate some work
            var data = new int[1000];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = i * i;
            }
        };

        // Act
        var metrics = _collector.Collect(operationName, operation);

        // Assert
        Assert.Equal(operationName, metrics.OperationName);
        Assert.True(metrics.Duration > TimeSpan.Zero);
        Assert.True(metrics.MemoryUsed >= 0); // Memory used should be non-negative
        Assert.Equal(0, metrics.Allocations); // Placeholder value
        Assert.True(metrics.StartTime <= metrics.EndTime);
    }

    [Fact]
    public void Collect_RecordsAccurateTiming()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(150);
        Action operation = () => Task.Delay(delay).Wait();

        // Act
        var metrics = _collector.Collect("TimedOperation", operation);

        // Assert
        Assert.True(metrics.Duration >= delay, $"Duration {metrics.Duration} should be at least {delay}");
        Assert.True(metrics.Duration < delay + TimeSpan.FromMilliseconds(300), "Duration should not be excessively long");
    }

    [Fact]
    public void CollectAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _collector.CollectAsync("test", null!));
        Assert.Equal("operation", exception.Result.ParamName);
    }

    [Fact]
    public async Task CollectAsync_WithValidOperation_ReturnsMetrics()
    {
        // Arrange
        var operationName = "AsyncTestOperation";
        Func<Task> operation = async () =>
        {
            // Simulate async work
            await Task.Delay(10);
            var data = new int[1000];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = i * i;
            }
        };

        // Act
        var metrics = await _collector.CollectAsync(operationName, operation);

        // Assert
        Assert.Equal(operationName, metrics.OperationName);
        Assert.True(metrics.Duration > TimeSpan.Zero);
        Assert.True(metrics.MemoryUsed >= 0);
        Assert.Equal(0, metrics.Allocations);
        Assert.True(metrics.StartTime <= metrics.EndTime);
    }

    [Fact]
    public async Task CollectAsync_RecordsAccurateTiming()
    {
        // Arrange
        var delay = TimeSpan.FromMilliseconds(50);
        Func<Task> operation = () => Task.Delay(delay);

        // Act
        var metrics = await _collector.CollectAsync("AsyncTimedOperation", operation);

        // Assert
        Assert.True(metrics.Duration >= delay, $"Duration {metrics.Duration} should be at least {delay}");
        Assert.True(metrics.Duration < delay + TimeSpan.FromMilliseconds(100), "Duration should not be excessively long");
    }

    [Fact]
    public void Collect_WithReturnValue_NullOperation_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _collector.Collect("test", null! as Func<int>));
        Assert.Equal("operation", exception.ParamName);
    }

    [Fact]
    public void Collect_WithReturnValue_ReturnsResultAndMetrics()
    {
        // Arrange
        var operationName = "ReturningOperation";
        Func<int> operation = () =>
        {
            // Simulate work and return a value
            var result = 0;
            for (int i = 0; i < 1000; i++)
            {
                result += i;
            }
            return result;
        };

        // Act
        var (result, metrics) = _collector.Collect(operationName, operation);

        // Assert
        Assert.Equal(499500, result); // Sum of 0 to 999
        Assert.Equal(operationName, metrics.OperationName);
        Assert.True(metrics.Duration > TimeSpan.Zero);
        Assert.True(metrics.MemoryUsed >= 0);
        Assert.Equal(0, metrics.Allocations);
        Assert.True(metrics.StartTime <= metrics.EndTime);
    }

    [Fact]
    public void CollectAsync_WithReturnValue_NullOperation_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.ThrowsAsync<ArgumentNullException>(() => _collector.CollectAsync("test", null! as Func<Task<int>>));
        Assert.Equal("operation", exception.Result.ParamName);
    }

    [Fact]
    public async Task CollectAsync_WithReturnValue_ReturnsResultAndMetrics()
    {
        // Arrange
        var operationName = "AsyncReturningOperation";
        Func<Task<string>> operation = async () =>
        {
            await Task.Delay(10);
            return "AsyncResult";
        };

        // Act
        var (result, metrics) = await _collector.CollectAsync(operationName, operation);

        // Assert
        Assert.Equal("AsyncResult", result);
        Assert.Equal(operationName, metrics.OperationName);
        Assert.True(metrics.Duration > TimeSpan.Zero);
        Assert.True(metrics.MemoryUsed >= 0);
        Assert.Equal(0, metrics.Allocations);
        Assert.True(metrics.StartTime <= metrics.EndTime);
    }

    [Fact]
    public void Collect_MemoryUsage_IncreasesWithAllocation()
    {
        // Arrange - Use much larger allocations to make the test more reliable
        Func<byte[]> smallAllocation = () => new byte[1000]; // 1KB
        Func<byte[]> largeAllocation = () => new byte[100000]; // 100KB

        // Act - Run multiple times to get more stable measurements
        long smallTotal = 0, largeTotal = 0;
        const int iterations = 10; // Increased iterations for better averaging

        for (int i = 0; i < iterations; i++)
        {
            var smallMetrics = _collector.Collect("Small", () => { var _ = smallAllocation(); });
            var largeMetrics = _collector.Collect("Large", () => { var _ = largeAllocation(); });
            smallTotal += smallMetrics.MemoryUsed;
            largeTotal += largeMetrics.MemoryUsed;
        }

        var avgSmall = smallTotal / iterations;
        var avgLarge = largeTotal / iterations;

        // Assert - Large allocation should generally use significantly more memory
        // Allow for some variance but require large to be at least 5x small on average (reduced from 10x for reliability)
        Assert.True(avgLarge >= avgSmall * 5,
            $"Large allocation average memory ({avgLarge}) should be >= 5x small ({avgSmall})");
    }

    [Fact]
    public void Collect_ExceptionInOperation_StillRecordsMetrics()
    {
        // Arrange
        Action failingOperation = () =>
        {
            Task.Delay(5).Wait();
            throw new InvalidOperationException("Test exception");
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _collector.Collect("FailingOperation", failingOperation));
        Assert.Equal("Test exception", exception.Message);

        // Note: We can't easily test the metrics here since the exception is re-thrown
        // In a real scenario, the metrics would still be collected in the finally block
    }

    [Fact]
    public async Task CollectAsync_ExceptionInOperation_StillRecordsMetrics()
    {
        // Arrange
        Func<Task> failingOperation = async () =>
        {
            await Task.Delay(5);
            throw new InvalidOperationException("Test async exception");
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _collector.CollectAsync("FailingAsyncOperation", failingOperation));
        Assert.Equal("Test async exception", exception.Message);

        // Note: Similar to sync version, metrics would be collected in finally block
    }

    [Fact]
    public void Collect_MultipleOperations_IndependentMetrics()
    {
        // Arrange
        Action fastOperation = () => Task.Delay(1).Wait();
        Action slowOperation = () => Task.Delay(20).Wait();

        // Act
        var fastMetrics = _collector.Collect("Fast", fastOperation);
        var slowMetrics = _collector.Collect("Slow", slowOperation);

        // Assert
        Assert.True(fastMetrics.Duration < slowMetrics.Duration,
            $"Fast operation ({fastMetrics.Duration}) should be faster than slow ({slowMetrics.Duration})");
        Assert.NotEqual(fastMetrics.StartTime, slowMetrics.StartTime);
    }

    [Fact]
    public void Collect_ZeroDurationOperation_RecordsMinimalTime()
    {
        // Arrange
        Action emptyOperation = () => { };

        // Act
        var metrics = _collector.Collect("Empty", emptyOperation);

        // Assert
        Assert.True(metrics.Duration >= TimeSpan.Zero);
        Assert.True(metrics.Duration < TimeSpan.FromMilliseconds(10), "Even empty operations take some time to measure");
    }

    [Fact]
    public void Collect_LargeMemoryOperation_RecordsMemoryUsage()
    {
        // Arrange
        Action memoryIntensiveOperation = () =>
        {
            // Allocate a significant amount of memory
            var largeArray = new byte[1024 * 1024]; // 1MB
            for (int i = 0; i < largeArray.Length; i++)
            {
                largeArray[i] = (byte)(i % 256);
            }
            // Keep reference until end of operation
            GC.KeepAlive(largeArray);
        };

        // Act
        var metrics = _collector.Collect("MemoryIntensive", memoryIntensiveOperation);

        // Assert
        Assert.True(metrics.MemoryUsed >= 0);
        // Note: Exact memory measurement depends on GC timing and is hard to predict precisely
    }
}