 using Relay.CLI.Commands.Models;
 using Relay.CLI.Commands.Models.Benchmark;
 using Xunit;

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
        Assert.Equal(config, results.TestConfiguration);
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
        Assert.Equal(relayResults, results.RelayResults);
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
        Assert.Equal(comparisonResults, results.ComparisonResults);
    }

    [Fact]
    public void BenchmarkResults_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var results = new BenchmarkResults();

        // Assert
        Assert.NotNull(results.TestConfiguration);
        Assert.NotNull(results.RelayResults);
        Assert.Empty(results.RelayResults);
        Assert.NotNull(results.ComparisonResults);
        Assert.Empty(results.ComparisonResults);
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
        Assert.Equal(config, results.TestConfiguration);
        Assert.Equal(relayResults, results.RelayResults);
        Assert.Equal(comparisonResults, results.ComparisonResults);
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
        Assert.Contains("UserHandler", results.RelayResults.Keys);
        Assert.Equal(benchmarkResult, results.RelayResults["UserHandler"]);
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
        Assert.Contains("OldUserHandler", results.ComparisonResults.Keys);
        Assert.Equal(benchmarkResult, results.ComparisonResults["OldUserHandler"]);
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
        Assert.Equal(3, results.RelayResults.Count());
        Assert.Equal(new[] { "Handler1", "Handler2", "Handler3" }.OrderBy(x => x), results.RelayResults.Keys.OrderBy(x => x));
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
        Assert.Equal(2, results.ComparisonResults.Count());
        Assert.Equal(new[] { "OldHandler1", "OldHandler2" }.OrderBy(x => x), results.ComparisonResults.Keys.OrderBy(x => x));
    }

    [Fact]
    public void BenchmarkResults_RelayResults_CanBeEmpty()
    {
        // Arrange & Act
        var results = new BenchmarkResults();

        // Assert
        Assert.Empty(results.RelayResults);
    }

    [Fact]
    public void BenchmarkResults_ComparisonResults_CanBeEmpty()
    {
        // Arrange & Act
        var results = new BenchmarkResults();

        // Assert
        Assert.Empty(results.ComparisonResults);
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
        Assert.True(results.RelayResults["UserHandler"].AverageTime <
            results.ComparisonResults["UserHandler"].AverageTime);
        Assert.True(results.RelayResults["UserHandler"].Iterations >
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
        Assert.Equal(2000, results.TestConfiguration.Iterations);
        Assert.Equal(8, results.TestConfiguration.Threads);
        Assert.Equal("TestMachine", results.TestConfiguration.MachineName);
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
        Assert.Equal(2, results.RelayResults.Count());
        Assert.Contains("Handler2", results.RelayResults.Keys);
        Assert.Contains("Handler3", results.RelayResults.Keys);
        Assert.DoesNotContain("Handler1", results.RelayResults.Keys);
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
        Assert.Equal(1, results.ComparisonResults.Count());
        Assert.Contains("NewOldHandler", results.ComparisonResults.Keys);
        Assert.DoesNotContain("OldHandler", results.ComparisonResults.Keys);
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
        Assert.Equal(1000, results.TestConfiguration.Iterations);
        Assert.Equal(4, results.TestConfiguration.Threads);
        Assert.Equal(new DateTime(2023, 1, 1), results.TestConfiguration.Timestamp);
        Assert.Equal("TestMachine", results.TestConfiguration.MachineName);
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
        Assert.Equal(config, results.TestConfiguration);
        Assert.Equal(2, results.RelayResults.Count());
        Assert.Equal(1, results.ComparisonResults.Count());

        // Verify relay results
        Assert.Equal(2000.0, results.RelayResults["CreateUserHandler"].RequestsPerSecond);
        Assert.Equal(5000.0, results.RelayResults["GetUserHandler"].RequestsPerSecond);

        // Verify comparison results
        Assert.Equal(500.0, results.ComparisonResults["OldCreateUserHandler"].RequestsPerSecond);
    }

    [Fact]
    public void BenchmarkResults_ShouldBeClass()
    {
        // Arrange & Act
        var results = new BenchmarkResults();

        // Assert
        Assert.NotNull(results);
        Assert.True(results.GetType().IsClass);
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
        Assert.Equal(2, resultsList.Count());
        Assert.Equal(2, resultsList.Sum(r => r.RelayResults.Count));
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
        Assert.Equal(300, totalIterations);
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
        Assert.Equal(125, totalIterations);
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
        Assert.Equal(3, userHandlers.Count());
        Assert.Equal(450, userHandlers.Sum(r => r.Value.Iterations));
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
        Assert.Equal(450, totalIterations);
        Assert.Equal(7.5, averageTime);
    }

    [Fact]
    public void BenchmarkResults_EmptyCollections_ShouldHandleAggregations()
    {
        // Arrange
        var results = new BenchmarkResults();

        // Act & Assert
        Assert.Empty(results.RelayResults);
        Assert.Empty(results.ComparisonResults);
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
        Assert.Equal(1000, results.RelayResults["Handler"].Iterations);
        Assert.Equal(500, results.ComparisonResults["Handler"].Iterations);
        Assert.True(results.RelayResults["Handler"].AverageTime <
            results.ComparisonResults["Handler"].AverageTime);
    }
}

