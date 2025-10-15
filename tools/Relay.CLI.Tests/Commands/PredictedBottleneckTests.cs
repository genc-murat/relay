using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class PredictedBottleneckTests
{
    [Fact]
    public void PredictedBottleneck_ShouldHaveComponentProperty()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck { Component = "Database" };

        // Assert
        bottleneck.Component.Should().Be("Database");
    }

    [Fact]
    public void PredictedBottleneck_ShouldHaveDescriptionProperty()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck { Description = "Connection pool exhaustion" };

        // Assert
        bottleneck.Description.Should().Be("Connection pool exhaustion");
    }

    [Fact]
    public void PredictedBottleneck_ShouldHaveProbabilityProperty()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck { Probability = 0.85 };

        // Assert
        bottleneck.Probability.Should().Be(0.85);
    }

    [Fact]
    public void PredictedBottleneck_ShouldHaveImpactProperty()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck { Impact = "High" };

        // Assert
        bottleneck.Impact.Should().Be("High");
    }

    [Fact]
    public void PredictedBottleneck_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck();

        // Assert
        bottleneck.Component.Should().Be("");
        bottleneck.Description.Should().Be("");
        bottleneck.Probability.Should().Be(0.0);
        bottleneck.Impact.Should().Be("");
    }

    [Fact]
    public void PredictedBottleneck_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck
        {
            Component = "API Gateway",
            Description = "Rate limiting exceeded",
            Probability = 0.72,
            Impact = "Medium"
        };

        // Assert
        bottleneck.Component.Should().Be("API Gateway");
        bottleneck.Description.Should().Be("Rate limiting exceeded");
        bottleneck.Probability.Should().Be(0.72);
        bottleneck.Impact.Should().Be("Medium");
    }

    [Fact]
    public void PredictedBottleneck_ShouldHandleZeroProbability()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck { Probability = 0.0 };

        // Assert
        bottleneck.Probability.Should().Be(0.0);
    }

    [Fact]
    public void PredictedBottleneck_ShouldHandleHighProbability()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck { Probability = 1.0 };

        // Assert
        bottleneck.Probability.Should().Be(1.0);
    }
}