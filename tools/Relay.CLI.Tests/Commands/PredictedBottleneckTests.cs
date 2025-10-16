using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class PredictedBottleneckTests
{
    [Fact]
    public void PredictedBottleneck_ShouldBeClass()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck();

        // Assert
        bottleneck.Should().NotBeNull();
        bottleneck.Should().BeOfType<PredictedBottleneck>();
    }

    [Fact]
    public void PredictedBottleneck_ShouldHaveComponentProperty()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck();

        // Act
        bottleneck.Component = "Database";

        // Assert
        bottleneck.Component.Should().Be("Database");
    }

    [Fact]
    public void PredictedBottleneck_ShouldHaveDescriptionProperty()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck();

        // Act
        bottleneck.Description = "High query latency detected";

        // Assert
        bottleneck.Description.Should().Be("High query latency detected");
    }

    [Fact]
    public void PredictedBottleneck_ShouldHaveProbabilityProperty()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck();

        // Act
        bottleneck.Probability = 0.85;

        // Assert
        bottleneck.Probability.Should().Be(0.85);
    }

    [Fact]
    public void PredictedBottleneck_ShouldHaveImpactProperty()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck();

        // Act
        bottleneck.Impact = "High";

        // Assert
        bottleneck.Impact.Should().Be("High");
    }

    [Fact]
    public void PredictedBottleneck_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck();

        // Assert
        bottleneck.Component.Should().BeEmpty();
        bottleneck.Description.Should().BeEmpty();
        bottleneck.Probability.Should().Be(0.0);
        bottleneck.Impact.Should().BeEmpty();
    }

    [Fact]
    public void PredictedBottleneck_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck
        {
            Component = "Cache",
            Description = "Cache miss rate too high",
            Probability = 0.92,
            Impact = "Medium"
        };

        // Assert
        bottleneck.Component.Should().Be("Cache");
        bottleneck.Description.Should().Be("Cache miss rate too high");
        bottleneck.Probability.Should().Be(0.92);
        bottleneck.Impact.Should().Be("Medium");
    }

    [Fact]
    public void PredictedBottleneck_CanBeUsedInCollections()
    {
        // Arrange
        var bottlenecks = new List<PredictedBottleneck>
        {
            new() { Component = "Database", Description = "Slow queries", Probability = 0.8, Impact = "High" },
            new() { Component = "Network", Description = "Latency spikes", Probability = 0.6, Impact = "Medium" }
        };

        // Act & Assert
        bottlenecks.Should().HaveCount(2);
        bottlenecks[0].Component.Should().Be("Database");
        bottlenecks[1].Component.Should().Be("Network");
    }

    [Fact]
    public void PredictedBottleneck_CanBeFiltered_ByImpact()
    {
        // Arrange
        var bottlenecks = new List<PredictedBottleneck>
        {
            new() { Component = "Database", Description = "Slow queries", Probability = 0.8, Impact = "High" },
            new() { Component = "Cache", Description = "Miss rate", Probability = 0.7, Impact = "Medium" },
            new() { Component = "CPU", Description = "High usage", Probability = 0.9, Impact = "High" }
        };

        // Act
        var highImpactBottlenecks = bottlenecks.Where(b => b.Impact == "High").ToList();

        // Assert
        highImpactBottlenecks.Should().HaveCount(2);
        highImpactBottlenecks.All(b => b.Impact == "High").Should().BeTrue();
    }

    [Fact]
    public void PredictedBottleneck_CanBeSorted_ByProbability()
    {
        // Arrange
        var bottlenecks = new List<PredictedBottleneck>
        {
            new() { Component = "Database", Description = "Slow queries", Probability = 0.8, Impact = "High" },
            new() { Component = "Cache", Description = "Miss rate", Probability = 0.7, Impact = "Medium" },
            new() { Component = "CPU", Description = "High usage", Probability = 0.9, Impact = "High" }
        };

        // Act
        var sortedBottlenecks = bottlenecks.OrderByDescending(b => b.Probability).ToList();

        // Assert
        sortedBottlenecks[0].Probability.Should().Be(0.9);
        sortedBottlenecks[1].Probability.Should().Be(0.8);
        sortedBottlenecks[2].Probability.Should().Be(0.7);
    }

    [Fact]
    public void PredictedBottleneck_CanCalculateAggregates()
    {
        // Arrange
        var bottlenecks = new List<PredictedBottleneck>
        {
            new() { Component = "Database", Description = "Slow queries", Probability = 0.8, Impact = "High" },
            new() { Component = "Cache", Description = "Miss rate", Probability = 0.7, Impact = "Medium" },
            new() { Component = "CPU", Description = "High usage", Probability = 0.9, Impact = "High" }
        };

        // Act
        var averageProbability = bottlenecks.Average(b => b.Probability);
        var maxProbability = bottlenecks.Max(b => b.Probability);
        var minProbability = bottlenecks.Min(b => b.Probability);

        // Assert
        averageProbability.Should().BeApproximately(0.8, 0.01);
        maxProbability.Should().Be(0.9);
        minProbability.Should().Be(0.7);
    }

    [Fact]
    public void PredictedBottleneck_WithHighProbability_ShouldBeCritical()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck
        {
            Component = "Database",
            Description = "Connection pool exhausted",
            Probability = 0.95,
            Impact = "Critical"
        };

        // Act & Assert
        bottleneck.Probability.Should().BeGreaterThan(0.9);
        bottleneck.Impact.Should().Be("Critical");
    }

    [Fact]
    public void PredictedBottleneck_WithLowProbability_ShouldBeMinor()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck
        {
            Component = "Disk",
            Description = "Slight I/O delay",
            Probability = 0.35,
            Impact = "Low"
        };

        // Act & Assert
        bottleneck.Probability.Should().BeLessThan(0.5);
        bottleneck.Impact.Should().Be("Low");
    }

    [Fact]
    public void PredictedBottleneck_CanBeSerialized_WithJson()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck
        {
            Component = "Memory",
            Description = "Potential memory leak",
            Probability = 0.78,
            Impact = "Medium"
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(bottleneck);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<PredictedBottleneck>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Component.Should().Be(bottleneck.Component);
        deserialized.Description.Should().Be(bottleneck.Description);
        deserialized.Probability.Should().Be(bottleneck.Probability);
        deserialized.Impact.Should().Be(bottleneck.Impact);
    }

    [Fact]
    public void PredictedBottleneck_CanHandleEdgeCaseValues()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck
        {
            Component = "",
            Description = "",
            Probability = -1.0,
            Impact = ""
        };

        // Assert
        bottleneck.Component.Should().BeEmpty();
        bottleneck.Description.Should().BeEmpty();
        bottleneck.Probability.Should().Be(-1.0);
        bottleneck.Impact.Should().BeEmpty();
    }

    [Fact]
    public void PredictedBottleneck_CanHandleVeryHighProbability()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck
        {
            Component = "Security",
            Description = "Vulnerability detected",
            Probability = 1.0,
            Impact = "Critical"
        };

        // Act & Assert
        bottleneck.Probability.Should().Be(1.0);
    }

    [Fact]
    public void PredictedBottleneck_CanHandleZeroProbability()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck
        {
            Component = "Unknown",
            Description = "Unlikely issue",
            Probability = 0.0,
            Impact = "None"
        };

        // Act & Assert
        bottleneck.Probability.Should().Be(0.0);
    }

    [Fact]
    public void PredictedBottleneck_CanBeGrouped_ByImpact()
    {
        // Arrange
        var bottlenecks = new List<PredictedBottleneck>
        {
            new() { Component = "Database", Description = "Slow queries", Probability = 0.8, Impact = "High" },
            new() { Component = "Cache", Description = "Miss rate", Probability = 0.7, Impact = "Medium" },
            new() { Component = "CPU", Description = "High usage", Probability = 0.9, Impact = "High" },
            new() { Component = "Network", Description = "Latency", Probability = 0.6, Impact = "Medium" }
        };

        // Act
        var groupedBottlenecks = bottlenecks.GroupBy(b => b.Impact).ToDictionary(g => g.Key, g => g.ToList());

        // Assert
        groupedBottlenecks.Should().ContainKey("High");
        groupedBottlenecks.Should().ContainKey("Medium");
        groupedBottlenecks["High"].Should().HaveCount(2);
        groupedBottlenecks["Medium"].Should().HaveCount(2);
    }

    [Fact]
    public void PredictedBottleneck_CanBeUsedInLinqQueries()
    {
        // Arrange
        var bottlenecks = new List<PredictedBottleneck>
        {
            new() { Component = "Database", Description = "Slow queries", Probability = 0.8, Impact = "High" },
            new() { Component = "Cache", Description = "Miss rate", Probability = 0.7, Impact = "Medium" },
            new() { Component = "CPU", Description = "High usage", Probability = 0.9, Impact = "High" },
            new() { Component = "Memory", Description = "Leak", Probability = 0.85, Impact = "High" }
        };

        // Act
        var highImpactHighProbBottlenecks = (from b in bottlenecks
                                             where b.Impact == "High" && b.Probability > 0.8
                                             orderby b.Probability descending
                                             select b).ToList();

        // Assert
        highImpactHighProbBottlenecks.Should().HaveCount(2);
        highImpactHighProbBottlenecks[0].Component.Should().Be("CPU");
        highImpactHighProbBottlenecks[1].Component.Should().Be("Memory");
    }

    [Fact]
    public void PredictedBottleneck_WithRealisticData_ShouldBeValid()
    {
        // Arrange
        var bottlenecks = new List<PredictedBottleneck>
        {
            new() { Component = "Database", Description = "Query optimization needed", Probability = 0.82, Impact = "High" },
            new() { Component = "Cache", Description = "Cache size insufficient", Probability = 0.65, Impact = "Medium" },
            new() { Component = "Network", Description = "Bandwidth bottleneck", Probability = 0.73, Impact = "Medium" },
            new() { Component = "CPU", Description = "Compute-intensive operations", Probability = 0.91, Impact = "High" },
            new() { Component = "Memory", Description = "Memory fragmentation", Probability = 0.58, Impact = "Low" }
        };

        // Act & Assert
        bottlenecks.Should().AllSatisfy(b =>
        {
            b.Component.Should().NotBeEmpty();
            b.Description.Should().NotBeEmpty();
            b.Probability.Should().BeInRange(0.0, 1.0);
            b.Impact.Should().NotBeEmpty();
        });
    }

    [Fact]
    public void PredictedBottleneck_CanBeCloned()
    {
        // Arrange
        var original = new PredictedBottleneck
        {
            Component = "Disk",
            Description = "I/O bottleneck",
            Probability = 0.88,
            Impact = "High"
        };

        // Act
        var clone = new PredictedBottleneck
        {
            Component = original.Component,
            Description = original.Description,
            Probability = original.Probability,
            Impact = original.Impact
        };

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.Component.Should().Be(original.Component);
        clone.Description.Should().Be(original.Description);
        clone.Probability.Should().Be(original.Probability);
        clone.Impact.Should().Be(original.Impact);
    }

    [Fact]
    public void PredictedBottleneck_EmptyCollections_ShouldHandleAggregations()
    {
        // Arrange
        var bottlenecks = new List<PredictedBottleneck>();

        // Act & Assert
        bottlenecks.Should().BeEmpty();
        Assert.Throws<InvalidOperationException>(() => bottlenecks.Average(b => b.Probability));
    }

    [Fact]
    public void PredictedBottleneck_CanBeUsedWithAnonymousTypes()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck
        {
            Component = "API",
            Description = "Rate limiting",
            Probability = 0.76,
            Impact = "Medium"
        };

        // Act
        var anonymous = new
        {
            bottleneck.Component,
            bottleneck.Description,
            bottleneck.Probability,
            IsLikely = bottleneck.Probability > 0.7,
            Severity = bottleneck.Impact
        };

        // Assert
        anonymous.Component.Should().Be("API");
        anonymous.IsLikely.Should().BeTrue();
        anonymous.Severity.Should().Be("Medium");
    }
}