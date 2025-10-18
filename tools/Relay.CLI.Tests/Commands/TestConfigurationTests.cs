using Relay.CLI.Commands;
using Relay.CLI.Commands.Models;
using Relay.CLI.Commands.Models.Benchmark;

namespace Relay.CLI.Tests.Commands;

public class TestConfigurationTests
{
    [Fact]
    public void TestConfiguration_ShouldHaveIterations()
    {
        // Arrange & Act
        var config = new TestConfiguration { Iterations = 100000 };

        // Assert
        Assert.Equal(100000, config.Iterations);
    }

    [Fact]
    public void TestConfiguration_ShouldHaveWarmupIterations()
    {
        // Arrange & Act
        var config = new TestConfiguration { WarmupIterations = 1000 };

        // Assert
        Assert.Equal(1000, config.WarmupIterations);
    }

    [Fact]
    public void TestConfiguration_ShouldHaveThreads()
    {
        // Arrange & Act
        var config = new TestConfiguration { Threads = 4 };

        // Assert
        Assert.Equal(4, config.Threads);
    }

    [Fact]
    public void TestConfiguration_ShouldHaveTimestamp()
    {
        // Arrange & Act
        var timestamp = DateTime.UtcNow;
        var config = new TestConfiguration { Timestamp = timestamp };

        // Assert
        Assert.Equal(timestamp, config.Timestamp);
    }

    [Fact]
    public void TestConfiguration_ShouldHaveMachineName()
    {
        // Arrange & Act
        var config = new TestConfiguration { MachineName = "TestMachine" };

        // Assert
        Assert.Equal("TestMachine", config.MachineName);
    }

    [Fact]
    public void TestConfiguration_ShouldHaveProcessorCount()
    {
        // Arrange & Act
        var config = new TestConfiguration { ProcessorCount = 8 };

        // Assert
        Assert.Equal(8, config.ProcessorCount);
    }

    [Fact]
    public void TestConfiguration_ShouldHaveRuntimeVersion()
    {
        // Arrange & Act
        var config = new TestConfiguration { RuntimeVersion = "8.0.0" };

        // Assert
        Assert.Equal("8.0.0", config.RuntimeVersion);
    }

    [Fact]
    public void TestConfiguration_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var config = new TestConfiguration();

        // Assert
        Assert.Equal(0, config.Iterations);
        Assert.Equal(0, config.WarmupIterations);
        Assert.Equal(0, config.Threads);
        Assert.Equal(default(DateTime), config.Timestamp);
        Assert.Empty(config.MachineName);
        Assert.Equal(0, config.ProcessorCount);
        Assert.Empty(config.RuntimeVersion);
    }

    [Fact]
    public void TestConfiguration_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var config = new TestConfiguration
        {
            Iterations = 100000,
            WarmupIterations = 1000,
            Threads = 4,
            Timestamp = DateTime.UtcNow,
            MachineName = "TestMachine",
            ProcessorCount = 8,
            RuntimeVersion = "8.0.0"
        };

        // Assert
        Assert.Equal(100000, config.Iterations);
        Assert.Equal(1000, config.WarmupIterations);
        Assert.Equal(4, config.Threads);
        Assert.True(Math.Abs((config.Timestamp - DateTime.UtcNow).TotalSeconds) < 1);
        Assert.Equal("TestMachine", config.MachineName);
        Assert.Equal(8, config.ProcessorCount);
        Assert.Equal("8.0.0", config.RuntimeVersion);
    }

    [Fact]
    public void TestConfiguration_Iterations_CanBeZero()
    {
        // Arrange & Act
        var config = new TestConfiguration { Iterations = 0 };

        // Assert
        Assert.Equal(0, config.Iterations);
    }

    [Fact]
    public void TestConfiguration_Iterations_CanBeLarge()
    {
        // Arrange & Act
        var config = new TestConfiguration { Iterations = 1000000 };

        // Assert
        Assert.Equal(1000000, config.Iterations);
    }

    [Fact]
    public void TestConfiguration_WarmupIterations_CanBeZero()
    {
        // Arrange & Act
        var config = new TestConfiguration { WarmupIterations = 0 };

        // Assert
        Assert.Equal(0, config.WarmupIterations);
    }

    [Fact]
    public void TestConfiguration_Threads_CanBeOne()
    {
        // Arrange & Act
        var config = new TestConfiguration { Threads = 1 };

        // Assert
        Assert.Equal(1, config.Threads);
    }

    [Fact]
    public void TestConfiguration_Threads_CanBeMultiple()
    {
        // Arrange & Act
        var config = new TestConfiguration { Threads = 16 };

        // Assert
        Assert.Equal(16, config.Threads);
    }

    [Fact]
    public void TestConfiguration_Timestamp_CanBeSetToSpecificDate()
    {
        // Arrange & Act
        var specificDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var config = new TestConfiguration { Timestamp = specificDate };

        // Assert
        Assert.Equal(specificDate, config.Timestamp);
        Assert.Equal(2024, config.Timestamp.Year);
        Assert.Equal(1, config.Timestamp.Month);
        Assert.Equal(1, config.Timestamp.Day);
    }

    [Fact]
    public void TestConfiguration_MachineName_CanBeEmpty()
    {
        // Arrange & Act
        var config = new TestConfiguration { MachineName = "" };

        // Assert
        Assert.Empty(config.MachineName);
    }

    [Fact]
    public void TestConfiguration_MachineName_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var config = new TestConfiguration { MachineName = "DESKTOP-ABC123" };

        // Assert
        Assert.Equal("DESKTOP-ABC123", config.MachineName);
    }

    [Fact]
    public void TestConfiguration_ProcessorCount_ReflectsAvailableCores()
    {
        // Arrange & Act
        var config = new TestConfiguration
        {
            ProcessorCount = Environment.ProcessorCount
        };

        // Assert
        Assert.True(config.ProcessorCount > 0);
        Assert.Equal(Environment.ProcessorCount, config.ProcessorCount);
    }

    [Fact]
    public void TestConfiguration_RuntimeVersion_CanBeEmpty()
    {
        // Arrange & Act
        var config = new TestConfiguration { RuntimeVersion = "" };

        // Assert
        Assert.Empty(config.RuntimeVersion);
    }

    [Fact]
    public void TestConfiguration_RuntimeVersion_CanContainDotNetVersion()
    {
        // Arrange & Act
        var config = new TestConfiguration { RuntimeVersion = Environment.Version.ToString() };

        // Assert
        Assert.False(string.IsNullOrEmpty(config.RuntimeVersion));
        Assert.Contains(".", config.RuntimeVersion);
    }

    [Fact]
    public void TestConfiguration_ShouldBeReferenceType()
    {
        // Arrange & Act
        var config1 = new TestConfiguration { Iterations = 1000 };
        var config2 = config1;
        config2.Iterations = 2000;

        // Assert
        Assert.Equal(2000, config1.Iterations);
    }

    [Fact]
    public void TestConfiguration_CanBeUsedInBenchmarkResults()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Iterations = 100000,
            WarmupIterations = 1000,
            Threads = 1
        };

        // Act
        var results = new BenchmarkResults
        {
            TestConfiguration = config
        };

        // Assert
        Assert.Equal(config, results.TestConfiguration);
        Assert.Equal(100000, results.TestConfiguration.Iterations);
    }

    [Fact]
    public void TestConfiguration_WithTypicalBenchmarkSettings_ShouldBeValid()
    {
        // Arrange & Act
        var config = new TestConfiguration
        {
            Iterations = 100000,
            WarmupIterations = 1000,
            Threads = 1,
            Timestamp = DateTime.UtcNow,
            MachineName = Environment.MachineName,
            ProcessorCount = Environment.ProcessorCount,
            RuntimeVersion = Environment.Version.ToString()
        };

        // Assert
        Assert.Equal(100000, config.Iterations);
        Assert.Equal(1000, config.WarmupIterations);
        Assert.Equal(1, config.Threads);
        Assert.NotEmpty(config.MachineName);
        Assert.True(config.ProcessorCount > 0);
        Assert.NotEmpty(config.RuntimeVersion);
    }

    [Fact]
    public void TestConfiguration_WithHighPerformanceSettings_ShouldBeValid()
    {
        // Arrange & Act
        var config = new TestConfiguration
        {
            Iterations = 1000000,
            WarmupIterations = 10000,
            Threads = Environment.ProcessorCount
        };

        // Assert
        Assert.Equal(1000000, config.Iterations);
        Assert.Equal(10000, config.WarmupIterations);
        Assert.True(config.Threads > 0);
    }

    [Fact]
    public void TestConfiguration_WithQuickTestSettings_ShouldBeValid()
    {
        // Arrange & Act
        var config = new TestConfiguration
        {
            Iterations = 1000,
            WarmupIterations = 100,
            Threads = 1
        };

        // Assert
        Assert.Equal(1000, config.Iterations);
        Assert.Equal(100, config.WarmupIterations);
        Assert.Equal(1, config.Threads);
    }

    [Fact]
    public void TestConfiguration_Timestamp_ShouldSupportUtcTime()
    {
        // Arrange & Act
        var utcNow = DateTime.UtcNow;
        var config = new TestConfiguration { Timestamp = utcNow };

        // Assert
        Assert.True(Math.Abs((config.Timestamp - utcNow).TotalMilliseconds) < 100);
    }

    [Fact]
    public void TestConfiguration_MultiThreaded_ShouldSupportParallelExecution()
    {
        // Arrange & Act
        var config = new TestConfiguration
        {
            Iterations = 100000,
            Threads = 8
        };

        // Assert
        Assert.Equal(8, config.Threads);
        Assert.Equal(12500, config.Iterations / config.Threads); // Iterations per thread
    }

    [Fact]
    public void TestConfiguration_WarmupRatio_ShouldBeReasonable()
    {
        // Arrange & Act
        var config = new TestConfiguration
        {
            Iterations = 100000,
            WarmupIterations = 1000
        };

        // Assert
        var warmupRatio = (double)config.WarmupIterations / config.Iterations;
        Assert.True(warmupRatio < 0.1); // Warmup should be < 10% of total
    }

    [Theory]
    [InlineData(100, 10)]
    [InlineData(1000, 100)]
    [InlineData(10000, 1000)]
    [InlineData(100000, 1000)]
    [InlineData(1000000, 10000)]
    public void TestConfiguration_WithVariousIterationCounts_ShouldBeValid(int iterations, int warmup)
    {
        // Arrange & Act
        var config = new TestConfiguration
        {
            Iterations = iterations,
            WarmupIterations = warmup
        };

        // Assert
        Assert.Equal(iterations, config.Iterations);
        Assert.Equal(warmup, config.WarmupIterations);
        Assert.True(config.WarmupIterations <= config.Iterations);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    public void TestConfiguration_WithVariousThreadCounts_ShouldBeValid(int threads)
    {
        // Arrange & Act
        var config = new TestConfiguration { Threads = threads };

        // Assert
        Assert.Equal(threads, config.Threads);
        Assert.True(config.Threads > 0);
    }

    [Fact]
    public void TestConfiguration_StringProperties_ShouldNotBeNull()
    {
        // Arrange & Act
        var config = new TestConfiguration();

        // Assert
        Assert.NotNull(config.MachineName);
        Assert.NotNull(config.RuntimeVersion);
    }

    [Fact]
    public void TestConfiguration_CanBeSerializedToJson()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Iterations = 100000,
            WarmupIterations = 1000,
            Threads = 4,
            Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            MachineName = "TestMachine",
            ProcessorCount = 8,
            RuntimeVersion = "8.0.0"
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(config);

        // Assert
        Assert.Contains("100000", json);
        Assert.Contains("1000", json);
        Assert.Contains("TestMachine", json);
        Assert.Contains("8.0.0", json);
    }

    [Fact]
    public void TestConfiguration_CanBeDeserializedFromJson()
    {
        // Arrange
        var json = @"{
            ""Iterations"": 100000,
            ""WarmupIterations"": 1000,
            ""Threads"": 4,
            ""Timestamp"": ""2024-01-01T00:00:00Z"",
            ""MachineName"": ""TestMachine"",
            ""ProcessorCount"": 8,
            ""RuntimeVersion"": ""8.0.0""
        }";

        // Act
        var config = System.Text.Json.JsonSerializer.Deserialize<TestConfiguration>(json);

        // Assert
        Assert.NotNull(config);
        Assert.Equal(100000, config.Iterations);
        Assert.Equal(1000, config.WarmupIterations);
        Assert.Equal(4, config.Threads);
        Assert.Equal("TestMachine", config.MachineName);
        Assert.Equal(8, config.ProcessorCount);
        Assert.Equal("8.0.0", config.RuntimeVersion);
    }

    [Fact]
    public void TestConfiguration_TotalExecutions_ShouldIncludeWarmup()
    {
        // Arrange & Act
        var config = new TestConfiguration
        {
            Iterations = 100000,
            WarmupIterations = 1000
        };

        var totalExecutions = config.Iterations + config.WarmupIterations;

        // Assert
        Assert.Equal(101000, totalExecutions);
    }

    [Fact]
    public void TestConfiguration_WithSystemInfo_ShouldCaptureEnvironmentDetails()
    {
        // Arrange & Act
        var config = new TestConfiguration
        {
            Timestamp = DateTime.UtcNow,
            MachineName = Environment.MachineName,
            ProcessorCount = Environment.ProcessorCount,
            RuntimeVersion = Environment.Version.ToString()
        };

        // Assert
        Assert.False(string.IsNullOrEmpty(config.MachineName));
        Assert.True(config.ProcessorCount > 0);
        Assert.False(string.IsNullOrEmpty(config.RuntimeVersion));
        Assert.Equal(DateTimeKind.Utc, config.Timestamp.Kind);
    }
}


