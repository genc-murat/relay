using Relay.CLI.Commands;
using System.Text.Json;

namespace Relay.CLI.Tests.Commands;

public class AIPredictionResultsTests
{
    [Fact]
    public void AIPredictionResults_ShouldHaveExpectedThroughputProperty()
    {
        // Arrange & Act
        var results = new AIPredictionResults { ExpectedThroughput = 1500.5 };

        // Assert
        Assert.Equal(1500.5, results.ExpectedThroughput);
    }

    [Fact]
    public void AIPredictionResults_ShouldHaveExpectedResponseTimeProperty()
    {
        // Arrange & Act
        var results = new AIPredictionResults { ExpectedResponseTime = 250.75 };

        // Assert
        Assert.Equal(250.75, results.ExpectedResponseTime);
    }

    [Fact]
    public void AIPredictionResults_ShouldHaveExpectedErrorRateProperty()
    {
        // Arrange & Act
        var results = new AIPredictionResults { ExpectedErrorRate = 0.02 };

        // Assert
        Assert.Equal(0.02, results.ExpectedErrorRate);
    }

    [Fact]
    public void AIPredictionResults_ShouldHaveExpectedCpuUsageProperty()
    {
        // Arrange & Act
        var results = new AIPredictionResults { ExpectedCpuUsage = 75.3 };

        // Assert
        Assert.Equal(75.3, results.ExpectedCpuUsage);
    }

    [Fact]
    public void AIPredictionResults_ShouldHaveExpectedMemoryUsageProperty()
    {
        // Arrange & Act
        var results = new AIPredictionResults { ExpectedMemoryUsage = 85.7 };

        // Assert
        Assert.Equal(85.7, results.ExpectedMemoryUsage);
    }

    [Fact]
    public void AIPredictionResults_ShouldHaveBottlenecksProperty()
    {
        // Arrange
        var bottlenecks = new[]
        {
            new PredictedBottleneck { Component = "Database", Probability = 0.8 },
            new PredictedBottleneck { Component = "Cache", Probability = 0.6 }
        };

        // Act
        var results = new AIPredictionResults { Bottlenecks = bottlenecks };

        // Assert
        Assert.Equal(2, results.Bottlenecks.Length);
        Assert.Equal("Database", results.Bottlenecks[0].Component);
        Assert.Equal("Cache", results.Bottlenecks[1].Component);
    }

    [Fact]
    public void AIPredictionResults_ShouldHaveRecommendationsProperty()
    {
        // Arrange
        var recommendations = new[] { "Increase connection pool", "Add caching layer" };

        // Act
        var results = new AIPredictionResults { Recommendations = recommendations };

        // Assert
        Assert.Equal(recommendations, results.Recommendations);
    }

    [Fact]
    public void AIPredictionResults_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var results = new AIPredictionResults();

        // Assert
        Assert.Equal(0.0, results.ExpectedThroughput);
        Assert.Equal(0.0, results.ExpectedResponseTime);
        Assert.Equal(0.0, results.ExpectedErrorRate);
        Assert.Equal(0.0, results.ExpectedCpuUsage);
        Assert.Equal(0.0, results.ExpectedMemoryUsage);
        Assert.Empty(results.Bottlenecks);
        Assert.Empty(results.Recommendations);
    }

    [Fact]
    public void AIPredictionResults_ShouldAllowSettingAllProperties()
    {
        // Arrange
        var bottlenecks = new[]
        {
            new PredictedBottleneck
            {
                Component = "API Gateway",
                Description = "Rate limiting",
                Probability = 0.85,
                Impact = "High"
            }
        };
        var recommendations = new[] { "Implement rate limiting", "Add load balancer" };

        // Act
        var results = new AIPredictionResults
        {
            ExpectedThroughput = 2000.0,
            ExpectedResponseTime = 150.5,
            ExpectedErrorRate = 0.01,
            ExpectedCpuUsage = 65.2,
            ExpectedMemoryUsage = 78.9,
            Bottlenecks = bottlenecks,
            Recommendations = recommendations
        };

        // Assert
        Assert.Equal(2000.0, results.ExpectedThroughput);
        Assert.Equal(150.5, results.ExpectedResponseTime);
        Assert.Equal(0.01, results.ExpectedErrorRate);
        Assert.Equal(65.2, results.ExpectedCpuUsage);
        Assert.Equal(78.9, results.ExpectedMemoryUsage);
        Assert.Single(results.Bottlenecks);
        Assert.Equal(recommendations, results.Recommendations);
    }

    [Fact]
    public void AIPredictionResults_ShouldHandleZeroValues()
    {
        // Arrange & Act
        var results = new AIPredictionResults
        {
            ExpectedThroughput = 0.0,
            ExpectedResponseTime = 0.0,
            ExpectedErrorRate = 0.0,
            ExpectedCpuUsage = 0.0,
            ExpectedMemoryUsage = 0.0
        };

        // Assert
        Assert.Equal(0.0, results.ExpectedThroughput);
        Assert.Equal(0.0, results.ExpectedResponseTime);
        Assert.Equal(0.0, results.ExpectedErrorRate);
        Assert.Equal(0.0, results.ExpectedCpuUsage);
        Assert.Equal(0.0, results.ExpectedMemoryUsage);
    }

    [Fact]
    public void AIPredictionResults_ShouldHandleHighValues()
    {
        // Arrange & Act
        var results = new AIPredictionResults
        {
            ExpectedThroughput = 10000.0,
            ExpectedResponseTime = 5000.0,
            ExpectedErrorRate = 1.0,
            ExpectedCpuUsage = 100.0,
            ExpectedMemoryUsage = 100.0
        };

        // Assert
        Assert.Equal(10000.0, results.ExpectedThroughput);
        Assert.Equal(5000.0, results.ExpectedResponseTime);
        Assert.Equal(1.0, results.ExpectedErrorRate);
        Assert.Equal(100.0, results.ExpectedCpuUsage);
        Assert.Equal(100.0, results.ExpectedMemoryUsage);
    }

    [Fact]
    public void AIPredictionResults_ShouldSerializeToJson()
    {
        // Arrange
        var results = new AIPredictionResults
        {
            ExpectedThroughput = 1500.0,
            ExpectedResponseTime = 200.0,
            ExpectedErrorRate = 0.05,
            ExpectedCpuUsage = 70.0,
            ExpectedMemoryUsage = 80.0,
            Bottlenecks =
            [
                new PredictedBottleneck { Component = "Database", Probability = 0.8 }
            ],
            Recommendations = ["Optimize queries"]
        };

        // Act
        var json = JsonSerializer.Serialize(results);
        var deserialized = JsonSerializer.Deserialize<AIPredictionResults>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(1500.0, deserialized!.ExpectedThroughput);
        Assert.Equal(200.0, deserialized.ExpectedResponseTime);
        Assert.Equal(0.05, deserialized.ExpectedErrorRate);
        Assert.Equal(70.0, deserialized.ExpectedCpuUsage);
        Assert.Equal(80.0, deserialized.ExpectedMemoryUsage);
        Assert.Single(deserialized.Bottlenecks);
        Assert.Contains("Optimize queries", deserialized.Recommendations);
    }

    [Fact]
    public void AIPredictionResults_ShouldDeserializeFromJson()
    {
        // Arrange
        var json = @"{
            ""ExpectedThroughput"": 1200.5,
            ""ExpectedResponseTime"": 300.25,
            ""ExpectedErrorRate"": 0.03,
            ""ExpectedCpuUsage"": 85.5,
            ""ExpectedMemoryUsage"": 90.2,
            ""Bottlenecks"": [
                {
                    ""Component"": ""Network"",
                    ""Description"": ""Latency issues"",
                    ""Probability"": 0.75,
                    ""Impact"": ""Medium""
                }
            ],
            ""Recommendations"": [""Upgrade network infrastructure""]
        }";

        // Act
        var results = JsonSerializer.Deserialize<AIPredictionResults>(json);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(1200.5, results!.ExpectedThroughput);
        Assert.Equal(300.25, results.ExpectedResponseTime);
        Assert.Equal(0.03, results.ExpectedErrorRate);
        Assert.Equal(85.5, results.ExpectedCpuUsage);
        Assert.Equal(90.2, results.ExpectedMemoryUsage);
        Assert.Single(results.Bottlenecks);
        Assert.Equal("Network", results.Bottlenecks[0].Component);
        Assert.Contains("Upgrade network infrastructure", results.Recommendations);
    }

    [Fact]
    public void AIPredictionResults_ShouldHandleEmptyArrays()
    {
        // Arrange & Act
        var results = new AIPredictionResults
        {
            Bottlenecks = [],
            Recommendations = []
        };

        // Assert
        Assert.Empty(results.Bottlenecks);
        Assert.Empty(results.Recommendations);
    }

    [Fact]
    public void AIPredictionResults_ShouldHandleNullArraysGracefully()
    {
        // Arrange & Act
        var results = new AIPredictionResults();

        // Assert - Default initialization should provide empty arrays
        Assert.NotNull(results.Bottlenecks);
        Assert.NotNull(results.Recommendations);
        Assert.Empty(results.Bottlenecks);
        Assert.Empty(results.Recommendations);
    }
}