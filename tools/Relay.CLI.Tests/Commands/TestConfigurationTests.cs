using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class TestConfigurationTests
{
    [Fact]
    public void TestConfiguration_ShouldHaveIterations()
    {
        // Arrange & Act
        var config = new TestConfiguration { Iterations = 100000 };

        // Assert
        config.Iterations.Should().Be(100000);
    }

    [Fact]
    public void TestConfiguration_ShouldHaveWarmupIterations()
    {
        // Arrange & Act
        var config = new TestConfiguration { WarmupIterations = 1000 };

        // Assert
        config.WarmupIterations.Should().Be(1000);
    }

    [Fact]
    public void TestConfiguration_ShouldHaveThreads()
    {
        // Arrange & Act
        var config = new TestConfiguration { Threads = 4 };

        // Assert
        config.Threads.Should().Be(4);
    }

    [Fact]
    public void TestConfiguration_ShouldHaveTimestamp()
    {
        // Arrange & Act
        var timestamp = DateTime.UtcNow;
        var config = new TestConfiguration { Timestamp = timestamp };

        // Assert
        config.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void TestConfiguration_ShouldHaveMachineName()
    {
        // Arrange & Act
        var config = new TestConfiguration { MachineName = "TestMachine" };

        // Assert
        config.MachineName.Should().Be("TestMachine");
    }

    [Fact]
    public void TestConfiguration_ShouldHaveProcessorCount()
    {
        // Arrange & Act
        var config = new TestConfiguration { ProcessorCount = 8 };

        // Assert
        config.ProcessorCount.Should().Be(8);
    }

    [Fact]
    public void TestConfiguration_ShouldHaveRuntimeVersion()
    {
        // Arrange & Act
        var config = new TestConfiguration { RuntimeVersion = "8.0.0" };

        // Assert
        config.RuntimeVersion.Should().Be("8.0.0");
    }

    [Fact]
    public void TestConfiguration_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var config = new TestConfiguration();

        // Assert
        config.Iterations.Should().Be(0);
        config.WarmupIterations.Should().Be(0);
        config.Threads.Should().Be(0);
        config.Timestamp.Should().Be(default(DateTime));
        config.MachineName.Should().BeEmpty();
        config.ProcessorCount.Should().Be(0);
        config.RuntimeVersion.Should().BeEmpty();
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
        config.Iterations.Should().Be(100000);
        config.WarmupIterations.Should().Be(1000);
        config.Threads.Should().Be(4);
        config.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        config.MachineName.Should().Be("TestMachine");
        config.ProcessorCount.Should().Be(8);
        config.RuntimeVersion.Should().Be("8.0.0");
    }

    [Fact]
    public void TestConfiguration_Iterations_CanBeZero()
    {
        // Arrange & Act
        var config = new TestConfiguration { Iterations = 0 };

        // Assert
        config.Iterations.Should().Be(0);
    }

    [Fact]
    public void TestConfiguration_Iterations_CanBeLarge()
    {
        // Arrange & Act
        var config = new TestConfiguration { Iterations = 1000000 };

        // Assert
        config.Iterations.Should().Be(1000000);
    }

    [Fact]
    public void TestConfiguration_WarmupIterations_CanBeZero()
    {
        // Arrange & Act
        var config = new TestConfiguration { WarmupIterations = 0 };

        // Assert
        config.WarmupIterations.Should().Be(0);
    }

    [Fact]
    public void TestConfiguration_Threads_CanBeOne()
    {
        // Arrange & Act
        var config = new TestConfiguration { Threads = 1 };

        // Assert
        config.Threads.Should().Be(1);
    }

    [Fact]
    public void TestConfiguration_Threads_CanBeMultiple()
    {
        // Arrange & Act
        var config = new TestConfiguration { Threads = 16 };

        // Assert
        config.Threads.Should().Be(16);
    }

    [Fact]
    public void TestConfiguration_Timestamp_CanBeSetToSpecificDate()
    {
        // Arrange & Act
        var specificDate = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var config = new TestConfiguration { Timestamp = specificDate };

        // Assert
        config.Timestamp.Should().Be(specificDate);
        config.Timestamp.Year.Should().Be(2024);
        config.Timestamp.Month.Should().Be(1);
        config.Timestamp.Day.Should().Be(1);
    }

    [Fact]
    public void TestConfiguration_MachineName_CanBeEmpty()
    {
        // Arrange & Act
        var config = new TestConfiguration { MachineName = "" };

        // Assert
        config.MachineName.Should().BeEmpty();
    }

    [Fact]
    public void TestConfiguration_MachineName_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var config = new TestConfiguration { MachineName = "DESKTOP-ABC123" };

        // Assert
        config.MachineName.Should().Be("DESKTOP-ABC123");
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
        config.ProcessorCount.Should().BeGreaterThan(0);
        config.ProcessorCount.Should().Be(Environment.ProcessorCount);
    }

    [Fact]
    public void TestConfiguration_RuntimeVersion_CanBeEmpty()
    {
        // Arrange & Act
        var config = new TestConfiguration { RuntimeVersion = "" };

        // Assert
        config.RuntimeVersion.Should().BeEmpty();
    }

    [Fact]
    public void TestConfiguration_RuntimeVersion_CanContainDotNetVersion()
    {
        // Arrange & Act
        var config = new TestConfiguration { RuntimeVersion = Environment.Version.ToString() };

        // Assert
        config.RuntimeVersion.Should().NotBeNullOrEmpty();
        config.RuntimeVersion.Should().Contain(".");
    }

    [Fact]
    public void TestConfiguration_ShouldBeReferenceType()
    {
        // Arrange & Act
        var config1 = new TestConfiguration { Iterations = 1000 };
        var config2 = config1;
        config2.Iterations = 2000;

        // Assert
        config1.Iterations.Should().Be(2000);
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
        results.TestConfiguration.Should().Be(config);
        results.TestConfiguration.Iterations.Should().Be(100000);
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
        config.Iterations.Should().Be(100000);
        config.WarmupIterations.Should().Be(1000);
        config.Threads.Should().Be(1);
        config.MachineName.Should().NotBeEmpty();
        config.ProcessorCount.Should().BeGreaterThan(0);
        config.RuntimeVersion.Should().NotBeEmpty();
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
        config.Iterations.Should().Be(1000000);
        config.WarmupIterations.Should().Be(10000);
        config.Threads.Should().BeGreaterThan(0);
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
        config.Iterations.Should().Be(1000);
        config.WarmupIterations.Should().Be(100);
        config.Threads.Should().Be(1);
    }

    [Fact]
    public void TestConfiguration_Timestamp_ShouldSupportUtcTime()
    {
        // Arrange & Act
        var utcNow = DateTime.UtcNow;
        var config = new TestConfiguration { Timestamp = utcNow };

        // Assert
        config.Timestamp.Should().BeCloseTo(utcNow, TimeSpan.FromMilliseconds(100));
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
        config.Threads.Should().Be(8);
        (config.Iterations / config.Threads).Should().Be(12500); // Iterations per thread
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
        warmupRatio.Should().BeLessThan(0.1); // Warmup should be < 10% of total
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
        config.Iterations.Should().Be(iterations);
        config.WarmupIterations.Should().Be(warmup);
        config.WarmupIterations.Should().BeLessThanOrEqualTo(config.Iterations);
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
        config.Threads.Should().Be(threads);
        config.Threads.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TestConfiguration_StringProperties_ShouldNotBeNull()
    {
        // Arrange & Act
        var config = new TestConfiguration();

        // Assert
        config.MachineName.Should().NotBeNull();
        config.RuntimeVersion.Should().NotBeNull();
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
        json.Should().Contain("100000");
        json.Should().Contain("1000");
        json.Should().Contain("TestMachine");
        json.Should().Contain("8.0.0");
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
        config.Should().NotBeNull();
        config!.Iterations.Should().Be(100000);
        config.WarmupIterations.Should().Be(1000);
        config.Threads.Should().Be(4);
        config.MachineName.Should().Be("TestMachine");
        config.ProcessorCount.Should().Be(8);
        config.RuntimeVersion.Should().Be("8.0.0");
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
        totalExecutions.Should().Be(101000);
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
        config.MachineName.Should().NotBeNullOrEmpty();
        config.ProcessorCount.Should().BeGreaterThan(0);
        config.RuntimeVersion.Should().NotBeNullOrEmpty();
        config.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }
}
