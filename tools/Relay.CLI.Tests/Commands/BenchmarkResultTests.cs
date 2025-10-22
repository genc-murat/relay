using Relay.CLI.Commands.Models.Benchmark;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class BenchmarkResultTests
{
    [Fact]
    public void BenchmarkResult_ShouldHaveNameProperty()
    {
        // Arrange & Act
        var result = new BenchmarkResult { Name = "Handler Performance Test" };

        // Assert
        Assert.Equal("Handler Performance Test", result.Name);
    }

    [Fact]
    public void BenchmarkResult_ShouldHaveTotalTimeProperty()
    {
        // Arrange & Act
        var totalTime = TimeSpan.FromSeconds(5.5);
        var result = new BenchmarkResult { TotalTime = totalTime };

        // Assert
        Assert.Equal(totalTime, result.TotalTime);
    }

    [Fact]
    public void BenchmarkResult_ShouldHaveIterationsProperty()
    {
        // Arrange & Act
        var result = new BenchmarkResult { Iterations = 1000 };

        // Assert
        Assert.Equal(1000, result.Iterations);
    }

    [Fact]
    public void BenchmarkResult_ShouldHaveAverageTimeProperty()
    {
        // Arrange & Act
        var result = new BenchmarkResult { AverageTime = 5.5 };

        // Assert
        Assert.Equal(5.5, result.AverageTime);
    }

    [Fact]
    public void BenchmarkResult_ShouldHaveRequestsPerSecondProperty()
    {
        // Arrange & Act
        var result = new BenchmarkResult { RequestsPerSecond = 181.82 };

        // Assert
        Assert.Equal(181.82, result.RequestsPerSecond);
    }

    [Fact]
    public void BenchmarkResult_ShouldHaveMemoryAllocatedProperty()
    {
        // Arrange & Act
        var result = new BenchmarkResult { MemoryAllocated = 1024000 };

        // Assert
        Assert.Equal(1024000, result.MemoryAllocated);
    }

    [Fact]
    public void BenchmarkResult_ShouldHaveThreadsProperty()
    {
        // Arrange & Act
        var result = new BenchmarkResult { Threads = 4 };

        // Assert
        Assert.Equal(4, result.Threads);
    }

    [Fact]
    public void BenchmarkResult_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var result = new BenchmarkResult();

        // Assert
        Assert.Equal("", result.Name);
        Assert.Equal(TimeSpan.Zero, result.TotalTime);
        Assert.Equal(0, result.Iterations);
        Assert.Equal(0.0, result.AverageTime);
        Assert.Equal(0.0, result.RequestsPerSecond);
        Assert.Equal(0, result.MemoryAllocated);
        Assert.Equal(0, result.Threads);
    }

    [Fact]
    public void BenchmarkResult_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var result = new BenchmarkResult
        {
            Name = "CreateUserHandler Benchmark",
            TotalTime = TimeSpan.FromSeconds(10),
            Iterations = 2000,
            AverageTime = 5.0,
            RequestsPerSecond = 200.0,
            MemoryAllocated = 2048000,
            Threads = 8
        };

        // Assert
        Assert.Equal("CreateUserHandler Benchmark", result.Name);
        Assert.Equal(TimeSpan.FromSeconds(10), result.TotalTime);
        Assert.Equal(2000, result.Iterations);
        Assert.Equal(5.0, result.AverageTime);
        Assert.Equal(200.0, result.RequestsPerSecond);
        Assert.Equal(2048000, result.MemoryAllocated);
        Assert.Equal(8, result.Threads);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void BenchmarkResult_ShouldSupportVariousIterationCounts(int iterations)
    {
        // Arrange & Act
        var result = new BenchmarkResult { Iterations = iterations };

        // Assert
        Assert.Equal(iterations, result.Iterations);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(16)]
    public void BenchmarkResult_ShouldSupportVariousThreadCounts(int threads)
    {
        // Arrange & Act
        var result = new BenchmarkResult { Threads = threads };

        // Assert
        Assert.Equal(threads, result.Threads);
    }

    [Fact]
    public void BenchmarkResult_WithTypicalBenchmarkData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new BenchmarkResult
        {
            Name = "UserCreationHandler",
            TotalTime = TimeSpan.FromMilliseconds(2500),
            Iterations = 500,
            AverageTime = 5.0,
            RequestsPerSecond = 200.0,
            MemoryAllocated = 1024000,
            Threads = 4
        };

        // Assert
        Assert.Equal("UserCreationHandler", result.Name);
        Assert.Equal(TimeSpan.FromMilliseconds(2500), result.TotalTime);
        Assert.Equal(500, result.Iterations);
        Assert.Equal(5.0, result.AverageTime);
        Assert.Equal(200.0, result.RequestsPerSecond);
        Assert.Equal(1024000, result.MemoryAllocated);
        Assert.Equal(4, result.Threads);
    }

    [Fact]
    public void BenchmarkResult_NameProperty_CanContainSpacesAndSpecialChars()
    {
        // Arrange & Act
        var result = new BenchmarkResult
        {
            Name = "CreateUserHandler - Performance Test (Async)"
        };

        // Assert
        Assert.Equal("CreateUserHandler - Performance Test (Async)", result.Name);
    }

    [Fact]
    public void BenchmarkResult_TotalTime_CanBeZero()
    {
        // Arrange & Act
        var result = new BenchmarkResult { TotalTime = TimeSpan.Zero };

        // Assert
        Assert.Equal(TimeSpan.Zero, result.TotalTime);
    }

    [Fact]
    public void BenchmarkResult_TotalTime_CanBeLarge()
    {
        // Arrange & Act
        var result = new BenchmarkResult { TotalTime = TimeSpan.FromHours(1) };

        // Assert
        Assert.Equal(TimeSpan.FromHours(1), result.TotalTime);
    }

    [Fact]
    public void BenchmarkResult_Iterations_CanBeZero()
    {
        // Arrange & Act
        var result = new BenchmarkResult { Iterations = 0 };

        // Assert
        Assert.Equal(0, result.Iterations);
    }

    [Fact]
    public void BenchmarkResult_AverageTime_CanBeZero()
    {
        // Arrange & Act
        var result = new BenchmarkResult { AverageTime = 0.0 };

        // Assert
        Assert.Equal(0.0, result.AverageTime);
    }

    [Fact]
    public void BenchmarkResult_AverageTime_CanBeFractional()
    {
        // Arrange & Act
        var result = new BenchmarkResult { AverageTime = 0.00123 };

        // Assert
        Assert.Equal(0.00123, result.AverageTime);
    }

    [Fact]
    public void BenchmarkResult_RequestsPerSecond_CanBeZero()
    {
        // Arrange & Act
        var result = new BenchmarkResult { RequestsPerSecond = 0.0 };

        // Assert
        Assert.Equal(0.0, result.RequestsPerSecond);
    }

    [Fact]
    public void BenchmarkResult_RequestsPerSecond_CanBeHigh()
    {
        // Arrange & Act
        var result = new BenchmarkResult { RequestsPerSecond = 10000.5 };

        // Assert
        Assert.Equal(10000.5, result.RequestsPerSecond);
    }

    [Fact]
    public void BenchmarkResult_MemoryAllocated_CanBeZero()
    {
        // Arrange & Act
        var result = new BenchmarkResult { MemoryAllocated = 0 };

        // Assert
        Assert.Equal(0, result.MemoryAllocated);
    }

    [Fact]
    public void BenchmarkResult_MemoryAllocated_CanBeLarge()
    {
        // Arrange & Act
        var result = new BenchmarkResult { MemoryAllocated = long.MaxValue };

        // Assert
        Assert.Equal(long.MaxValue, result.MemoryAllocated);
    }

    [Fact]
    public void BenchmarkResult_Threads_CanBeZero()
    {
        // Arrange & Act
        var result = new BenchmarkResult { Threads = 0 };

        // Assert
        Assert.Equal(0, result.Threads);
    }

    [Fact]
    public void BenchmarkResult_ShouldBeClass()
    {
        // Arrange & Act
        var result = new BenchmarkResult();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.GetType().IsClass);
    }

    [Fact]
    public void BenchmarkResult_CanBeUsedInList()
    {
        // Arrange & Act
        var results = new List<BenchmarkResult>
        {
            new BenchmarkResult { Name = "Test1", Iterations = 100 },
            new BenchmarkResult { Name = "Test2", Iterations = 200 },
            new BenchmarkResult { Name = "Test3", Iterations = 300 }
        };

        // Assert
        Assert.Equal(3, results.Count());
        Assert.Equal(600, results.Sum(r => r.Iterations));
    }

    [Fact]
    public void BenchmarkResult_CanBeFiltered_ByName()
    {
        // Arrange
        var results = new List<BenchmarkResult>
        {
            new BenchmarkResult { Name = "Handler1", Iterations = 100 },
            new BenchmarkResult { Name = "Handler2", Iterations = 200 },
            new BenchmarkResult { Name = "Handler1", Iterations = 150 }
        };

        // Act
        var handler1Results = results.Where(r => r.Name == "Handler1").ToList();

        // Assert
        Assert.Equal(2, handler1Results.Count());
        Assert.Equal(250, handler1Results.Sum(r => r.Iterations));
    }

    [Fact]
    public void BenchmarkResult_CanBeOrdered_ByAverageTime()
    {
        // Arrange
        var results = new List<BenchmarkResult>
        {
            new BenchmarkResult { Name = "Slow", AverageTime = 10.0 },
            new BenchmarkResult { Name = "Fast", AverageTime = 1.0 },
            new BenchmarkResult { Name = "Medium", AverageTime = 5.0 }
        };

        // Act
        var ordered = results.OrderBy(r => r.AverageTime).ToList();

        // Assert
        Assert.Equal("Fast", ordered[0].Name);
        Assert.Equal("Medium", ordered[1].Name);
        Assert.Equal("Slow", ordered[2].Name);
    }

    [Fact]
    public void BenchmarkResult_CanBeGrouped_ByThreads()
    {
        // Arrange
        var results = new List<BenchmarkResult>
        {
            new BenchmarkResult { Name = "Test1", Threads = 1 },
            new BenchmarkResult { Name = "Test2", Threads = 2 },
            new BenchmarkResult { Name = "Test3", Threads = 1 },
            new BenchmarkResult { Name = "Test4", Threads = 4 }
        };

        // Act
        var grouped = results.GroupBy(r => r.Threads);

        // Assert
        Assert.Equal(3, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key == 1).Count());
        Assert.Single(grouped.First(g => g.Key == 2));
        Assert.Single(grouped.First(g => g.Key == 4));
    }

    [Fact]
    public void BenchmarkResult_PropertiesCanBeModified()
    {
        // Arrange
        var result = new BenchmarkResult
        {
            Name = "Initial",
            Iterations = 100,
            AverageTime = 5.0
        };

        // Act
        result.Name = "Modified";
        result.Iterations = 200;
        result.AverageTime = 10.0;

        // Assert
        Assert.Equal("Modified", result.Name);
        Assert.Equal(200, result.Iterations);
        Assert.Equal(10.0, result.AverageTime);
    }

    [Fact]
    public void BenchmarkResult_WithHighPerformanceData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new BenchmarkResult
        {
            Name = "OptimizedHandler",
            TotalTime = TimeSpan.FromMilliseconds(100),
            Iterations = 10000,
            AverageTime = 0.01,
            RequestsPerSecond = 100000.0,
            MemoryAllocated = 512000,
            Threads = 16
        };

        // Assert
        Assert.Equal("OptimizedHandler", result.Name);
        Assert.Equal(TimeSpan.FromMilliseconds(100), result.TotalTime);
        Assert.Equal(10000, result.Iterations);
        Assert.Equal(0.01, result.AverageTime);
        Assert.Equal(100000.0, result.RequestsPerSecond);
        Assert.Equal(512000, result.MemoryAllocated);
        Assert.Equal(16, result.Threads);
    }

    [Fact]
    public void BenchmarkResult_WithLowPerformanceData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new BenchmarkResult
        {
            Name = "SlowHandler",
            TotalTime = TimeSpan.FromSeconds(30),
            Iterations = 10,
            AverageTime = 3000.0,
            RequestsPerSecond = 0.33,
            MemoryAllocated = 10485760,
            Threads = 1
        };

        // Assert
        Assert.Equal("SlowHandler", result.Name);
        Assert.Equal(TimeSpan.FromSeconds(30), result.TotalTime);
        Assert.Equal(10, result.Iterations);
        Assert.Equal(3000.0, result.AverageTime);
        Assert.Equal(0.33, result.RequestsPerSecond);
        Assert.Equal(10485760, result.MemoryAllocated);
        Assert.Equal(1, result.Threads);
    }

    [Fact]
    public void BenchmarkResult_Name_CanBeEmpty()
    {
        // Arrange & Act
        var result = new BenchmarkResult { Name = "" };

        // Assert
        Assert.Empty(result.Name);
    }

    [Fact]
    public void BenchmarkResult_CalculatedFields_ShouldBeConsistent()
    {
        // Arrange & Act
        var result = new BenchmarkResult
        {
            TotalTime = TimeSpan.FromSeconds(10),
            Iterations = 100,
            AverageTime = 100.0, // 10 seconds / 100 iterations = 100ms per iteration
            RequestsPerSecond = 10.0 // 100 iterations / 10 seconds = 10 RPS
        };

        // Assert
        Assert.Equal(10000, result.TotalTime.TotalMilliseconds);
        Assert.Equal(100, result.Iterations);
        Assert.Equal(100.0, result.AverageTime);
        Assert.Equal(10.0, result.RequestsPerSecond);
    }

    [Fact]
    public void BenchmarkResult_WithMemoryPressure_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new BenchmarkResult
        {
            Name = "MemoryIntensiveHandler",
            TotalTime = TimeSpan.FromSeconds(5),
            Iterations = 50,
            AverageTime = 100.0,
            RequestsPerSecond = 10.0,
            MemoryAllocated = 1073741824, // 1GB
            Threads = 2
        };

        // Assert
        Assert.Equal("MemoryIntensiveHandler", result.Name);
        Assert.Equal(1073741824, result.MemoryAllocated);
        Assert.Equal(2, result.Threads);
    }

    [Fact]
    public void BenchmarkResult_WithSingleThread_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new BenchmarkResult
        {
            Name = "SingleThreadedHandler",
            TotalTime = TimeSpan.FromSeconds(2),
            Iterations = 100,
            AverageTime = 20.0,
            RequestsPerSecond = 50.0,
            MemoryAllocated = 256000,
            Threads = 1
        };

        // Assert
        Assert.Equal("SingleThreadedHandler", result.Name);
        Assert.Equal(1, result.Threads);
        Assert.Equal(50.0, result.RequestsPerSecond);
    }

    [Fact]
    public void BenchmarkResult_WithMultiThread_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new BenchmarkResult
        {
            Name = "MultiThreadedHandler",
            TotalTime = TimeSpan.FromSeconds(1),
            Iterations = 1000,
            AverageTime = 1.0,
            RequestsPerSecond = 1000.0,
            MemoryAllocated = 512000,
            Threads = 8
        };

        // Assert
        Assert.Equal("MultiThreadedHandler", result.Name);
        Assert.Equal(8, result.Threads);
        Assert.Equal(1000.0, result.RequestsPerSecond);
    }
}

