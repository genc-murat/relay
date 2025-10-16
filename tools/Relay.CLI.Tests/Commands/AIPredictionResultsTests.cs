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
        results.ExpectedThroughput.Should().Be(1500.5);
    }

    [Fact]
    public void AIPredictionResults_ShouldHaveExpectedResponseTimeProperty()
    {
        // Arrange & Act
        var results = new AIPredictionResults { ExpectedResponseTime = 250.75 };

        // Assert
        results.ExpectedResponseTime.Should().Be(250.75);
    }

    [Fact]
    public void AIPredictionResults_ShouldHaveExpectedErrorRateProperty()
    {
        // Arrange & Act
        var results = new AIPredictionResults { ExpectedErrorRate = 0.02 };

        // Assert
        results.ExpectedErrorRate.Should().Be(0.02);
    }

    [Fact]
    public void AIPredictionResults_ShouldHaveExpectedCpuUsageProperty()
    {
        // Arrange & Act
        var results = new AIPredictionResults { ExpectedCpuUsage = 75.3 };

        // Assert
        results.ExpectedCpuUsage.Should().Be(75.3);
    }

    [Fact]
    public void AIPredictionResults_ShouldHaveExpectedMemoryUsageProperty()
    {
        // Arrange & Act
        var results = new AIPredictionResults { ExpectedMemoryUsage = 85.7 };

        // Assert
        results.ExpectedMemoryUsage.Should().Be(85.7);
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
        results.Bottlenecks.Should().HaveCount(2);
        results.Bottlenecks[0].Component.Should().Be("Database");
        results.Bottlenecks[1].Component.Should().Be("Cache");
    }

    [Fact]
    public void AIPredictionResults_ShouldHaveRecommendationsProperty()
    {
        // Arrange
        var recommendations = new[] { "Increase connection pool", "Add caching layer" };

        // Act
        var results = new AIPredictionResults { Recommendations = recommendations };

        // Assert
        results.Recommendations.Should().BeEquivalentTo(recommendations);
    }

    [Fact]
    public void AIPredictionResults_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var results = new AIPredictionResults();

        // Assert
        results.ExpectedThroughput.Should().Be(0.0);
        results.ExpectedResponseTime.Should().Be(0.0);
        results.ExpectedErrorRate.Should().Be(0.0);
        results.ExpectedCpuUsage.Should().Be(0.0);
        results.ExpectedMemoryUsage.Should().Be(0.0);
        results.Bottlenecks.Should().BeEmpty();
        results.Recommendations.Should().BeEmpty();
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
        results.ExpectedThroughput.Should().Be(2000.0);
        results.ExpectedResponseTime.Should().Be(150.5);
        results.ExpectedErrorRate.Should().Be(0.01);
        results.ExpectedCpuUsage.Should().Be(65.2);
        results.ExpectedMemoryUsage.Should().Be(78.9);
        results.Bottlenecks.Should().HaveCount(1);
        results.Recommendations.Should().BeEquivalentTo(recommendations);
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
        results.ExpectedThroughput.Should().Be(0.0);
        results.ExpectedResponseTime.Should().Be(0.0);
        results.ExpectedErrorRate.Should().Be(0.0);
        results.ExpectedCpuUsage.Should().Be(0.0);
        results.ExpectedMemoryUsage.Should().Be(0.0);
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
        results.ExpectedThroughput.Should().Be(10000.0);
        results.ExpectedResponseTime.Should().Be(5000.0);
        results.ExpectedErrorRate.Should().Be(1.0);
        results.ExpectedCpuUsage.Should().Be(100.0);
        results.ExpectedMemoryUsage.Should().Be(100.0);
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
            Bottlenecks = new[]
            {
                new PredictedBottleneck { Component = "Database", Probability = 0.8 }
            },
            Recommendations = new[] { "Optimize queries" }
        };

        // Act
        var json = JsonSerializer.Serialize(results);
        var deserialized = JsonSerializer.Deserialize<AIPredictionResults>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.ExpectedThroughput.Should().Be(1500.0);
        deserialized.ExpectedResponseTime.Should().Be(200.0);
        deserialized.ExpectedErrorRate.Should().Be(0.05);
        deserialized.ExpectedCpuUsage.Should().Be(70.0);
        deserialized.ExpectedMemoryUsage.Should().Be(80.0);
        deserialized.Bottlenecks.Should().HaveCount(1);
        deserialized.Recommendations.Should().Contain("Optimize queries");
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
        results.Should().NotBeNull();
        results!.ExpectedThroughput.Should().Be(1200.5);
        results.ExpectedResponseTime.Should().Be(300.25);
        results.ExpectedErrorRate.Should().Be(0.03);
        results.ExpectedCpuUsage.Should().Be(85.5);
        results.ExpectedMemoryUsage.Should().Be(90.2);
        results.Bottlenecks.Should().HaveCount(1);
        results.Bottlenecks[0].Component.Should().Be("Network");
        results.Recommendations.Should().Contain("Upgrade network infrastructure");
    }

    [Fact]
    public void AIPredictionResults_ShouldHandleEmptyArrays()
    {
        // Arrange & Act
        var results = new AIPredictionResults
        {
            Bottlenecks = Array.Empty<PredictedBottleneck>(),
            Recommendations = Array.Empty<string>()
        };

        // Assert
        results.Bottlenecks.Should().BeEmpty();
        results.Recommendations.Should().BeEmpty();
    }

    [Fact]
    public void AIPredictionResults_ShouldHandleNullArraysGracefully()
    {
        // Arrange & Act
        var results = new AIPredictionResults();

        // Assert - Default initialization should provide empty arrays
        results.Bottlenecks.Should().NotBeNull();
        results.Recommendations.Should().NotBeNull();
        results.Bottlenecks.Should().BeEmpty();
        results.Recommendations.Should().BeEmpty();
    }
}