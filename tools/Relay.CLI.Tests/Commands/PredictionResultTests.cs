using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class PredictionResultTests
{
    [Fact]
    public void PredictionResult_ShouldBeClass()
    {
        // Arrange & Act
        var result = new PredictionResult();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<PredictionResult>();
    }

    [Fact]
    public void PredictionResult_ShouldHaveMetricProperty()
    {
        // Arrange
        var result = new PredictionResult();

        // Act
        result.Metric = "ResponseTime";

        // Assert
        result.Metric.Should().Be("ResponseTime");
    }

    [Fact]
    public void PredictionResult_ShouldHavePredictedValueProperty()
    {
        // Arrange
        var result = new PredictionResult();

        // Act
        result.PredictedValue = "150ms";

        // Assert
        result.PredictedValue.Should().Be("150ms");
    }

    [Fact]
    public void PredictionResult_ShouldHaveConfidenceProperty()
    {
        // Arrange
        var result = new PredictionResult();

        // Act
        result.Confidence = 0.85;

        // Assert
        result.Confidence.Should().Be(0.85);
    }

    [Fact]
    public void PredictionResult_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var result = new PredictionResult();

        // Assert
        result.Metric.Should().BeEmpty();
        result.PredictedValue.Should().BeEmpty();
        result.Confidence.Should().Be(0.0);
    }

    [Fact]
    public void PredictionResult_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var result = new PredictionResult
        {
            Metric = "Throughput",
            PredictedValue = "1000 req/sec",
            Confidence = 0.92
        };

        // Assert
        result.Metric.Should().Be("Throughput");
        result.PredictedValue.Should().Be("1000 req/sec");
        result.Confidence.Should().Be(0.92);
    }

    [Fact]
    public void PredictionResult_CanBeUsedInCollections()
    {
        // Arrange
        var results = new List<PredictionResult>
        {
            new() { Metric = "CPU", PredictedValue = "80%", Confidence = 0.75 },
            new() { Metric = "Memory", PredictedValue = "2GB", Confidence = 0.88 }
        };

        // Act & Assert
        results.Should().HaveCount(2);
        results[0].Metric.Should().Be("CPU");
        results[1].Metric.Should().Be("Memory");
    }

    [Fact]
    public void PredictionResult_CanBeFiltered_ByMetric()
    {
        // Arrange
        var results = new List<PredictionResult>
        {
            new() { Metric = "CPU", PredictedValue = "80%", Confidence = 0.75 },
            new() { Metric = "Memory", PredictedValue = "2GB", Confidence = 0.88 },
            new() { Metric = "CPU", PredictedValue = "85%", Confidence = 0.82 }
        };

        // Act
        var cpuResults = results.Where(r => r.Metric == "CPU").ToList();

        // Assert
        cpuResults.Should().HaveCount(2);
        cpuResults.All(r => r.Metric == "CPU").Should().BeTrue();
    }

    [Fact]
    public void PredictionResult_CanBeSorted_ByConfidence()
    {
        // Arrange
        var results = new List<PredictionResult>
        {
            new() { Metric = "CPU", PredictedValue = "80%", Confidence = 0.75 },
            new() { Metric = "Memory", PredictedValue = "2GB", Confidence = 0.88 },
            new() { Metric = "Disk", PredictedValue = "100MB/s", Confidence = 0.65 }
        };

        // Act
        var sortedResults = results.OrderByDescending(r => r.Confidence).ToList();

        // Assert
        sortedResults[0].Confidence.Should().Be(0.88);
        sortedResults[1].Confidence.Should().Be(0.75);
        sortedResults[2].Confidence.Should().Be(0.65);
    }

    [Fact]
    public void PredictionResult_CanCalculateAggregates()
    {
        // Arrange
        var results = new List<PredictionResult>
        {
            new() { Metric = "CPU", PredictedValue = "80%", Confidence = 0.75 },
            new() { Metric = "Memory", PredictedValue = "2GB", Confidence = 0.88 },
            new() { Metric = "Disk", PredictedValue = "100MB/s", Confidence = 0.65 }
        };

        // Act
        var averageConfidence = results.Average(r => r.Confidence);
        var maxConfidence = results.Max(r => r.Confidence);
        var minConfidence = results.Min(r => r.Confidence);

        // Assert
        averageConfidence.Should().BeApproximately(0.76, 0.01);
        maxConfidence.Should().Be(0.88);
        minConfidence.Should().Be(0.65);
    }

    [Fact]
    public void PredictionResult_WithHighConfidence_ShouldBeReliable()
    {
        // Arrange
        var result = new PredictionResult
        {
            Metric = "ResponseTime",
            PredictedValue = "120ms",
            Confidence = 0.95
        };

        // Act & Assert
        result.Confidence.Should().BeGreaterThan(0.9);
        result.Metric.Should().NotBeEmpty();
        result.PredictedValue.Should().NotBeEmpty();
    }

    [Fact]
    public void PredictionResult_WithLowConfidence_ShouldBeUnreliable()
    {
        // Arrange
        var result = new PredictionResult
        {
            Metric = "ResponseTime",
            PredictedValue = "120ms",
            Confidence = 0.45
        };

        // Act & Assert
        result.Confidence.Should().BeLessThan(0.5);
    }

    [Fact]
    public void PredictionResult_CanBeSerialized_WithJson()
    {
        // Arrange
        var result = new PredictionResult
        {
            Metric = "Throughput",
            PredictedValue = "1500 req/sec",
            Confidence = 0.87
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(result);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<PredictionResult>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Metric.Should().Be(result.Metric);
        deserialized.PredictedValue.Should().Be(result.PredictedValue);
        deserialized.Confidence.Should().Be(result.Confidence);
    }

    [Fact]
    public void PredictionResult_CanHandleEdgeCaseValues()
    {
        // Arrange & Act
        var result = new PredictionResult
        {
            Metric = "",
            PredictedValue = "",
            Confidence = -1.0
        };

        // Assert
        result.Metric.Should().BeEmpty();
        result.PredictedValue.Should().BeEmpty();
        result.Confidence.Should().Be(-1.0);
    }

    [Fact]
    public void PredictionResult_CanHandleVeryHighConfidence()
    {
        // Arrange
        var result = new PredictionResult
        {
            Metric = "Accuracy",
            PredictedValue = "99.9%",
            Confidence = 1.0
        };

        // Act & Assert
        result.Confidence.Should().Be(1.0);
    }

    [Fact]
    public void PredictionResult_CanHandleZeroConfidence()
    {
        // Arrange
        var result = new PredictionResult
        {
            Metric = "Unknown",
            PredictedValue = "N/A",
            Confidence = 0.0
        };

        // Act & Assert
        result.Confidence.Should().Be(0.0);
    }

    [Fact]
    public void PredictionResult_CanBeGrouped_ByMetric()
    {
        // Arrange
        var results = new List<PredictionResult>
        {
            new() { Metric = "CPU", PredictedValue = "80%", Confidence = 0.75 },
            new() { Metric = "Memory", PredictedValue = "2GB", Confidence = 0.88 },
            new() { Metric = "CPU", PredictedValue = "85%", Confidence = 0.82 },
            new() { Metric = "Memory", PredictedValue = "2.5GB", Confidence = 0.91 }
        };

        // Act
        var groupedResults = results.GroupBy(r => r.Metric).ToDictionary(g => g.Key, g => g.ToList());

        // Assert
        groupedResults.Should().ContainKey("CPU");
        groupedResults.Should().ContainKey("Memory");
        groupedResults["CPU"].Should().HaveCount(2);
        groupedResults["Memory"].Should().HaveCount(2);
    }

    [Fact]
    public void PredictionResult_CanBeUsedInLinqQueries()
    {
        // Arrange
        var results = new List<PredictionResult>
        {
            new() { Metric = "CPU", PredictedValue = "80%", Confidence = 0.75 },
            new() { Metric = "Memory", PredictedValue = "2GB", Confidence = 0.88 },
            new() { Metric = "Disk", PredictedValue = "100MB/s", Confidence = 0.65 },
            new() { Metric = "Network", PredictedValue = "1Gbps", Confidence = 0.92 }
        };

        // Act
        var highConfidenceResults = (from r in results
                                    where r.Confidence > 0.8
                                    orderby r.Confidence descending
                                    select r).ToList();

        // Assert
        highConfidenceResults.Should().HaveCount(2);
        highConfidenceResults[0].Metric.Should().Be("Network");
        highConfidenceResults[1].Metric.Should().Be("Memory");
    }

    [Fact]
    public void PredictionResult_WithRealisticData_ShouldBeValid()
    {
        // Arrange
        var results = new List<PredictionResult>
        {
            new() { Metric = "ResponseTime", PredictedValue = "245ms", Confidence = 0.89 },
            new() { Metric = "Throughput", PredictedValue = "1250 req/sec", Confidence = 0.76 },
            new() { Metric = "ErrorRate", PredictedValue = "0.05%", Confidence = 0.94 },
            new() { Metric = "CPUUsage", PredictedValue = "67%", Confidence = 0.82 },
            new() { Metric = "MemoryUsage", PredictedValue = "1.8GB", Confidence = 0.78 }
        };

        // Act & Assert
        results.Should().AllSatisfy(r =>
        {
            r.Metric.Should().NotBeEmpty();
            r.PredictedValue.Should().NotBeEmpty();
            r.Confidence.Should().BeInRange(0.0, 1.0);
        });
    }

    [Fact]
    public void PredictionResult_CanBeCloned()
    {
        // Arrange
        var original = new PredictionResult
        {
            Metric = "Latency",
            PredictedValue = "50ms",
            Confidence = 0.91
        };

        // Act
        var clone = new PredictionResult
        {
            Metric = original.Metric,
            PredictedValue = original.PredictedValue,
            Confidence = original.Confidence
        };

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.Metric.Should().Be(original.Metric);
        clone.PredictedValue.Should().Be(original.PredictedValue);
        clone.Confidence.Should().Be(original.Confidence);
    }

    [Fact]
    public void PredictionResult_EmptyCollections_ShouldHandleAggregations()
    {
        // Arrange
        var results = new List<PredictionResult>();

        // Act & Assert
        results.Should().BeEmpty();
        Assert.Throws<InvalidOperationException>(() => results.Average(r => r.Confidence));
    }

    [Fact]
    public void PredictionResult_CanBeUsedWithAnonymousTypes()
    {
        // Arrange
        var result = new PredictionResult
        {
            Metric = "Performance",
            PredictedValue = "Excellent",
            Confidence = 0.95
        };

        // Act
        var anonymous = new
        {
            result.Metric,
            result.PredictedValue,
            result.Confidence,
            IsHighConfidence = result.Confidence > 0.9
        };

        // Assert
        anonymous.Metric.Should().Be("Performance");
        anonymous.IsHighConfidence.Should().BeTrue();
    }
}