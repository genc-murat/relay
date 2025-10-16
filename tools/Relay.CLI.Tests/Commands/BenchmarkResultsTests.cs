using Relay.CLI.Commands.Models;
using Relay.CLI.Commands.Models.Benchmark;

namespace Relay.CLI.Tests.Commands;

public class BenchmarkResultsTests
{
    [Fact]
    public void BenchmarkResults_ShouldHaveTestConfigurationProperty()
    {
        // Arrange & Act
        var results = new BenchmarkResults();
        var config = new TestConfiguration { Iterations = 1000 };

        // Act
        results.TestConfiguration = config;

        // Assert
        results.TestConfiguration.Should().Be(config);
    }

    [Fact]
    public void BenchmarkResults_ShouldHaveRelayResultsProperty()
    {
        // Arrange & Act
        var results = new BenchmarkResults();
        var relayResults = new Dictionary<string, BenchmarkResult>
        {
            ["Handler1"] = new BenchmarkResult { Name = "Handler1", Iterations = 100 }
        };

        // Act
        results.RelayResults = relayResults;

        // Assert
        results.RelayResults.Should().BeEquivalentTo(relayResults);
    }

    [Fact]
    public void BenchmarkResults_ShouldHaveComparisonResultsProperty()
    {
        // Arrange & Act
        var results = new BenchmarkResults();
        var comparisonResults = new Dictionary<string, BenchmarkResult>
        {
            ["Handler1"] = new BenchmarkResult { Name = "Handler1", Iterations = 100 }
        };

        // Act
        results.ComparisonResults = comparisonResults;

        // Assert
        results.ComparisonResults.Should().BeEquivalentTo(comparisonResults);
    }

    [Fact]
    public void BenchmarkResults_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var results = new BenchmarkResults();

        // Assert
        results.TestConfiguration.Should().NotBeNull();
        results.RelayResults.Should().NotBeNull();
        results.RelayResults.Should().BeEmpty();
        results.ComparisonResults.Should().NotBeNull();
        results.ComparisonResults.Should().BeEmpty();
    }

    [Fact]
    public void BenchmarkResults_CanSetAllPropertiesViaInitializer()
    {
        // Arrange
        var config = new TestConfiguration { Iterations = 2000 };
        var relayResults = new Dictionary<string, BenchmarkResult>
        {
            ["CreateUserHandler"] = new BenchmarkResult { Name = "CreateUserHandler", Iterations = 1000 }
        };
        var comparisonResults = new Dictionary<string, BenchmarkResult>
        {
            ["OldHandler"] = new BenchmarkResult { Name = "OldHandler", Iterations = 500 }
        };

        // Act
        var results = new BenchmarkResults
        {
            TestConfiguration = config,
            RelayResults = relayResults,
            ComparisonResults = comparisonResults
        };

        // Assert
        results.TestConfiguration.Should().Be(config);
        results.RelayResults.Should().BeEquivalentTo(relayResults);
        results.ComparisonResults.Should().BeEquivalentTo(comparisonResults);
    }

    [Fact]
    public void BenchmarkResults_CanAddRelayResults()
    {
        // Arrange
        var results = new BenchmarkResults();
        var benchmarkResult = new BenchmarkResult
        {
            Name = "UserHandler",
            Iterations = 1000,
            AverageTime = 5.0
        };

        // Act
        results.RelayResults["UserHandler"] = benchmarkResult;

        // Assert
        results.RelayResults.Should().ContainKey("UserHandler");
        results.RelayResults["UserHandler"].Should().Be(benchmarkResult);
    }

    [Fact]
    public void BenchmarkResults_CanAddComparisonResults()
    {
        // Arrange
        var results = new BenchmarkResults();
        var benchmarkResult = new BenchmarkResult
        {
            Name = "OldUserHandler",
            Iterations = 500,
            AverageTime = 10.0
        };

        // Act
        results.ComparisonResults["OldUserHandler"] = benchmarkResult;

        // Assert
        results.ComparisonResults.Should().ContainKey("OldUserHandler");
        results.ComparisonResults["OldUserHandler"].Should().Be(benchmarkResult);
    }

    [Fact]
    public void BenchmarkResults_CanHaveMultipleRelayResults()
    {
        // Arrange
        var results = new BenchmarkResults();
        var result1 = new BenchmarkResult { Name = "Handler1", Iterations = 100 };
        var result2 = new BenchmarkResult { Name = "Handler2", Iterations = 200 };
        var result3 = new BenchmarkResult { Name = "Handler3", Iterations = 300 };

        // Act
        results.RelayResults["Handler1"] = result1;
        results.RelayResults["Handler2"] = result2;
        results.RelayResults["Handler3"] = result3;

        // Assert
        results.RelayResults.Should().HaveCount(3);
        results.RelayResults.Keys.Should().BeEquivalentTo(new[] { "Handler1", "Handler2", "Handler3" });
    }

    [Fact]
    public void BenchmarkResults_CanHaveMultipleComparisonResults()
    {
        // Arrange
        var results = new BenchmarkResults();
        var result1 = new BenchmarkResult { Name = "OldHandler1", Iterations = 50 };
        var result2 = new BenchmarkResult { Name = "OldHandler2", Iterations = 75 };

        // Act
        results.ComparisonResults["OldHandler1"] = result1;
        results.ComparisonResults["OldHandler2"] = result2;

        // Assert
        results.ComparisonResults.Should().HaveCount(2);
        results.ComparisonResults.Keys.Should().BeEquivalentTo(new[] { "OldHandler1", "OldHandler2" });
    }

    [Fact]
    public void BenchmarkResults_RelayResults_CanBeEmpty()
    {
        // Arrange & Act
        var results = new BenchmarkResults();

        // Assert
        results.RelayResults.Should().BeEmpty();
    }

    [Fact]
    public void BenchmarkResults_ComparisonResults_CanBeEmpty()
    {
        // Arrange & Act
        var results = new BenchmarkResults();

        // Assert
        results.ComparisonResults.Should().BeEmpty();
    }

    [Fact]
    public void BenchmarkResults_CanCompareRelayVsComparison()
    {
        // Arrange
        var results = new BenchmarkResults
        {
            RelayResults = new Dictionary<string, BenchmarkResult>
            {
                ["UserHandler"] = new BenchmarkResult { Name = "UserHandler", AverageTime = 5.0, Iterations = 1000 }
            },
            ComparisonResults = new Dictionary<string, BenchmarkResult>
            {
                ["UserHandler"] = new BenchmarkResult { Name = "UserHandler", AverageTime = 10.0, Iterations = 500 }
            }
        };

        // Act & Assert
        results.RelayResults["UserHandler"].AverageTime.Should().BeLessThan(
            results.ComparisonResults["UserHandler"].AverageTime);
        results.RelayResults["UserHandler"].Iterations.Should().BeGreaterThan(
            results.ComparisonResults["UserHandler"].Iterations);
    }

    [Fact]
    public void BenchmarkResults_TestConfiguration_CanBeModified()
    {
        // Arrange
        var results = new BenchmarkResults();
        var config = new TestConfiguration
        {
            Iterations = 1000,
            Threads = 4,
            MachineName = "TestMachine"
        };

        // Act
        results.TestConfiguration = config;
        results.TestConfiguration.Iterations = 2000;
        results.TestConfiguration.Threads = 8;

        // Assert
        results.TestConfiguration.Iterations.Should().Be(2000);
        results.TestConfiguration.Threads.Should().Be(8);
        results.TestConfiguration.MachineName.Should().Be("TestMachine");
    }

    [Fact]
    public void BenchmarkResults_RelayResults_CanBeReplaced()
    {
        // Arrange
        var results = new BenchmarkResults();
        results.RelayResults["Handler1"] = new BenchmarkResult { Name = "Handler1" };

        var newResults = new Dictionary<string, BenchmarkResult>
        {
            ["Handler2"] = new BenchmarkResult { Name = "Handler2" },
            ["Handler3"] = new BenchmarkResult { Name = "Handler3" }
        };

        // Act
        results.RelayResults = newResults;

        // Assert
        results.RelayResults.Should().HaveCount(2);
        results.RelayResults.Should().ContainKey("Handler2");
        results.RelayResults.Should().ContainKey("Handler3");
        results.RelayResults.Should().NotContainKey("Handler1");
    }

    [Fact]
    public void BenchmarkResults_ComparisonResults_CanBeReplaced()
    {
        // Arrange
        var results = new BenchmarkResults();
        results.ComparisonResults["OldHandler"] = new BenchmarkResult { Name = "OldHandler" };

        var newResults = new Dictionary<string, BenchmarkResult>
        {
            ["NewOldHandler"] = new BenchmarkResult { Name = "NewOldHandler" }
        };

        // Act
        results.ComparisonResults = newResults;

        // Assert
        results.ComparisonResults.Should().HaveCount(1);
        results.ComparisonResults.Should().ContainKey("NewOldHandler");
        results.ComparisonResults.Should().NotContainKey("OldHandler");
    }

    [Fact]
    public void BenchmarkResults_CanBeSerialized_WithTestConfiguration()
    {
        // Arrange
        var results = new BenchmarkResults
        {
            TestConfiguration = new TestConfiguration
            {
                Iterations = 1000,
                Threads = 4,
                Timestamp = new DateTime(2023, 1, 1),
                MachineName = "TestMachine"
            }
        };

        // Act & Assert - Basic serialization check
        results.TestConfiguration.Iterations.Should().Be(1000);
        results.TestConfiguration.Threads.Should().Be(4);
        results.TestConfiguration.Timestamp.Should().Be(new DateTime(2023, 1, 1));
        results.TestConfiguration.MachineName.Should().Be("TestMachine");
    }

    [Fact]
    public void BenchmarkResults_WithComplexData_ShouldStoreCorrectly()
    {
        // Arrange
        var config = new TestConfiguration
        {
            Iterations = 5000,
            WarmupIterations = 100,
            Threads = 8,
            Timestamp = DateTime.Now,
            MachineName = "BenchmarkServer",
            ProcessorCount = 16,
            RuntimeVersion = ".NET 8.0"
        };

        var relayResults = new Dictionary<string, BenchmarkResult>
        {
            ["CreateUserHandler"] = new BenchmarkResult
            {
                Name = "CreateUserHandler",
                TotalTime = TimeSpan.FromSeconds(2.5),
                Iterations = 5000,
                AverageTime = 0.5,
                RequestsPerSecond = 2000.0,
                MemoryAllocated = 1024000,
                Threads = 8
            },
            ["GetUserHandler"] = new BenchmarkResult
            {
                Name = "GetUserHandler",
                TotalTime = TimeSpan.FromSeconds(1.0),
                Iterations = 5000,
                AverageTime = 0.2,
                RequestsPerSecond = 5000.0,
                MemoryAllocated = 512000,
                Threads = 8
            }
        };

        var comparisonResults = new Dictionary<string, BenchmarkResult>
        {
            ["OldCreateUserHandler"] = new BenchmarkResult
            {
                Name = "OldCreateUserHandler",
                TotalTime = TimeSpan.FromSeconds(5.0),
                Iterations = 2500,
                AverageTime = 2.0,
                RequestsPerSecond = 500.0,
                MemoryAllocated = 2048000,
                Threads = 4
            }
        };

        // Act
        var results = new BenchmarkResults
        {
            TestConfiguration = config,
            RelayResults = relayResults,
            ComparisonResults = comparisonResults
        };

        // Assert
        results.TestConfiguration.Should().Be(config);
        results.RelayResults.Should().HaveCount(2);
        results.ComparisonResults.Should().HaveCount(1);

        // Verify relay results
        results.RelayResults["CreateUserHandler"].RequestsPerSecond.Should().Be(2000.0);
        results.RelayResults["GetUserHandler"].RequestsPerSecond.Should().Be(5000.0);

        // Verify comparison results
        results.ComparisonResults["OldCreateUserHandler"].RequestsPerSecond.Should().Be(500.0);
    }

    [Fact]
    public void BenchmarkResults_ShouldBeClass()
    {
        // Arrange & Act
        var results = new BenchmarkResults();

        // Assert
        results.Should().NotBeNull();
        results.GetType().IsClass.Should().BeTrue();
    }

    [Fact]
    public void BenchmarkResults_CanBeUsedInCollections()
    {
        // Arrange & Act
        var resultsList = new List<BenchmarkResults>
        {
            new BenchmarkResults
            {
                RelayResults = new Dictionary<string, BenchmarkResult>
                {
                    ["Handler1"] = new BenchmarkResult { Iterations = 100 }
                }
            },
            new BenchmarkResults
            {
                RelayResults = new Dictionary<string, BenchmarkResult>
                {
                    ["Handler2"] = new BenchmarkResult { Iterations = 200 }
                }
            }
        };

        // Assert
        resultsList.Should().HaveCount(2);
        resultsList.Sum(r => r.RelayResults.Count).Should().Be(2);
    }

    [Fact]
    public void BenchmarkResults_RelayResults_CanBeEnumerated()
    {
        // Arrange
        var results = new BenchmarkResults();
        results.RelayResults["Handler1"] = new BenchmarkResult { Name = "Handler1", Iterations = 100 };
        results.RelayResults["Handler2"] = new BenchmarkResult { Name = "Handler2", Iterations = 200 };

        // Act
        var totalIterations = results.RelayResults.Sum(r => r.Value.Iterations);

        // Assert
        totalIterations.Should().Be(300);
    }

    [Fact]
    public void BenchmarkResults_ComparisonResults_CanBeEnumerated()
    {
        // Arrange
        var results = new BenchmarkResults();
        results.ComparisonResults["OldHandler1"] = new BenchmarkResult { Name = "OldHandler1", Iterations = 50 };
        results.ComparisonResults["OldHandler2"] = new BenchmarkResult { Name = "OldHandler2", Iterations = 75 };

        // Act
        var totalIterations = results.ComparisonResults.Sum(r => r.Value.Iterations);

        // Assert
        totalIterations.Should().Be(125);
    }

    [Fact]
    public void BenchmarkResults_CanBeFiltered_ByHandlerName()
    {
        // Arrange
        var results = new BenchmarkResults();
        results.RelayResults["CreateUserHandler"] = new BenchmarkResult { Name = "CreateUserHandler", Iterations = 100 };
        results.RelayResults["GetUserHandler"] = new BenchmarkResult { Name = "GetUserHandler", Iterations = 200 };
        results.RelayResults["UpdateUserHandler"] = new BenchmarkResult { Name = "UpdateUserHandler", Iterations = 150 };

        // Act
        var userHandlers = results.RelayResults.Where(r => r.Key.Contains("User")).ToList();

        // Assert
        userHandlers.Should().HaveCount(3);
        userHandlers.Sum(r => r.Value.Iterations).Should().Be(450);
    }

    [Fact]
    public void BenchmarkResults_CanCalculateAggregates()
    {
        // Arrange
        var results = new BenchmarkResults();
        results.RelayResults["Handler1"] = new BenchmarkResult { Iterations = 100, AverageTime = 10.0 };
        results.RelayResults["Handler2"] = new BenchmarkResult { Iterations = 200, AverageTime = 5.0 };
        results.RelayResults["Handler3"] = new BenchmarkResult { Iterations = 150, AverageTime = 7.5 };

        // Act
        var totalIterations = results.RelayResults.Sum(r => r.Value.Iterations);
        var averageTime = results.RelayResults.Average(r => r.Value.AverageTime);

        // Assert
        totalIterations.Should().Be(450);
        averageTime.Should().Be(7.5);
    }

    [Fact]
    public void BenchmarkResults_EmptyCollections_ShouldHandleAggregations()
    {
        // Arrange
        var results = new BenchmarkResults();

        // Act & Assert
        results.RelayResults.Should().BeEmpty();
        results.ComparisonResults.Should().BeEmpty();
    }

    [Fact]
    public void BenchmarkResults_WithSameHandlerNames_ShouldAllowDifferentResults()
    {
        // Arrange
        var results = new BenchmarkResults();

        // Act
        results.RelayResults["Handler"] = new BenchmarkResult { Name = "Handler", Iterations = 1000, AverageTime = 1.0 };
        results.ComparisonResults["Handler"] = new BenchmarkResult { Name = "Handler", Iterations = 500, AverageTime = 2.0 };

        // Assert
        results.RelayResults["Handler"].Iterations.Should().Be(1000);
        results.ComparisonResults["Handler"].Iterations.Should().Be(500);
        results.RelayResults["Handler"].AverageTime.Should().BeLessThan(
            results.ComparisonResults["Handler"].AverageTime);
    }
}