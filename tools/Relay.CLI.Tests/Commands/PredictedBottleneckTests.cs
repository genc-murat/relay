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
        Assert.NotNull(bottleneck);
        Assert.IsType<PredictedBottleneck>(bottleneck);
    }

    [Fact]
    public void PredictedBottleneck_ShouldHaveComponentProperty()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck();

        // Act
        bottleneck.Component = "Database";

        // Assert
        Assert.Equal("Database", bottleneck.Component);
    }

    [Fact]
    public void PredictedBottleneck_ShouldHaveDescriptionProperty()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck();

        // Act
        bottleneck.Description = "High query latency detected";

        // Assert
        Assert.Equal("High query latency detected", bottleneck.Description);
    }

    [Fact]
    public void PredictedBottleneck_ShouldHaveProbabilityProperty()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck();

        // Act
        bottleneck.Probability = 0.85;

        // Assert
        Assert.Equal(0.85, bottleneck.Probability);
    }

    [Fact]
    public void PredictedBottleneck_ShouldHaveImpactProperty()
    {
        // Arrange
        var bottleneck = new PredictedBottleneck();

        // Act
        bottleneck.Impact = "High";

        // Assert
        Assert.Equal("High", bottleneck.Impact);
    }

    [Fact]
    public void PredictedBottleneck_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var bottleneck = new PredictedBottleneck();

        // Assert
        Assert.Empty(bottleneck.Component);
        Assert.Empty(bottleneck.Description);
        Assert.Equal(0.0, bottleneck.Probability);
        Assert.Empty(bottleneck.Impact);
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
        Assert.Equal("Cache", bottleneck.Component);
        Assert.Equal("Cache miss rate too high", bottleneck.Description);
        Assert.Equal(0.92, bottleneck.Probability);
        Assert.Equal("Medium", bottleneck.Impact);
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
        Assert.Equal(2, bottlenecks.Count);
        Assert.Equal("Database", bottlenecks[0].Component);
        Assert.Equal("Network", bottlenecks[1].Component);
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
        Assert.Equal(2, highImpactBottlenecks.Count);
        Assert.True(highImpactBottlenecks.All(b => b.Impact == "High"));
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
        Assert.Equal(0.9, sortedBottlenecks[0].Probability);
        Assert.Equal(0.8, sortedBottlenecks[1].Probability);
        Assert.Equal(0.7, sortedBottlenecks[2].Probability);
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
        Assert.Equal(0.8, averageProbability, 0.01);
        Assert.Equal(0.9, maxProbability);
        Assert.Equal(0.7, minProbability);
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
        Assert.True(bottleneck.Probability > 0.9);
        Assert.Equal("Critical", bottleneck.Impact);
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
        Assert.True(bottleneck.Probability < 0.5);
        Assert.Equal("Low", bottleneck.Impact);
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
        Assert.NotNull(deserialized);
        Assert.Equal(bottleneck.Component, deserialized.Component);
        Assert.Equal(bottleneck.Description, deserialized.Description);
        Assert.Equal(bottleneck.Probability, deserialized.Probability);
        Assert.Equal(bottleneck.Impact, deserialized.Impact);
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
        Assert.Empty(bottleneck.Component);
        Assert.Empty(bottleneck.Description);
        Assert.Equal(-1.0, bottleneck.Probability);
        Assert.Empty(bottleneck.Impact);
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
        Assert.Equal(1.0, bottleneck.Probability);
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
        Assert.Equal(0.0, bottleneck.Probability);
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
        Assert.Contains("High", groupedBottlenecks.Keys);
        Assert.Contains("Medium", groupedBottlenecks.Keys);
        Assert.Equal(2, groupedBottlenecks["High"].Count);
        Assert.Equal(2, groupedBottlenecks["Medium"].Count);
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
        Assert.Equal(2, highImpactHighProbBottlenecks.Count);
        Assert.Equal("CPU", highImpactHighProbBottlenecks[0].Component);
        Assert.Equal("Memory", highImpactHighProbBottlenecks[1].Component);
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
        Assert.All(bottlenecks, b =>
        {
            Assert.NotEmpty(b.Component);
            Assert.NotEmpty(b.Description);
            Assert.InRange(b.Probability, 0.0, 1.0);
            Assert.NotEmpty(b.Impact);
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
        Assert.NotSame(original, clone);
        Assert.Equal(original.Component, clone.Component);
        Assert.Equal(original.Description, clone.Description);
        Assert.Equal(original.Probability, clone.Probability);
        Assert.Equal(original.Impact, clone.Impact);
    }

    [Fact]
    public void PredictedBottleneck_EmptyCollections_ShouldHandleAggregations()
    {
        // Arrange
        var bottlenecks = new List<PredictedBottleneck>();

        // Act & Assert
        Assert.Empty(bottlenecks);
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
        Assert.Equal("API", anonymous.Component);
        Assert.True(anonymous.IsLikely);
        Assert.Equal("Medium", anonymous.Severity);
    }
}

