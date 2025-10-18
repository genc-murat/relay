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
        Assert.NotNull(result);
        Assert.IsType<PredictionResult>(result);
    }

    [Fact]
    public void PredictionResult_ShouldHaveMetricProperty()
    {
        // Arrange
        var result = new PredictionResult();

        // Act
        result.Metric = "ResponseTime";

        // Assert
        Assert.Equal("ResponseTime", result.Metric);
    }

    [Fact]
    public void PredictionResult_ShouldHavePredictedValueProperty()
    {
        // Arrange
        var result = new PredictionResult();

        // Act
        result.PredictedValue = "150ms";

        // Assert
        Assert.Equal("150ms", result.PredictedValue);
    }

    [Fact]
    public void PredictionResult_ShouldHaveConfidenceProperty()
    {
        // Arrange
        var result = new PredictionResult();

        // Act
        result.Confidence = 0.85;

        // Assert
        Assert.Equal(0.85, result.Confidence);
    }

    [Fact]
    public void PredictionResult_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var result = new PredictionResult();

        // Assert
        Assert.Equal("", result.Metric);
        Assert.Equal("", result.PredictedValue);
        Assert.Equal(0.0, result.Confidence);
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
        Assert.Equal("Throughput", result.Metric);
        Assert.Equal("1000 req/sec", result.PredictedValue);
        Assert.Equal(0.92, result.Confidence);
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
        Assert.Equal(2, results.Count());
        Assert.Equal("CPU", results[0].Metric);
        Assert.Equal("Memory", results[1].Metric);
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
        Assert.Equal(2, cpuResults.Count());
        Assert.True(cpuResults.All(r => r.Metric == "CPU"));
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
        Assert.Equal(0.88, sortedResults[0].Confidence);
        Assert.Equal(0.75, sortedResults[1].Confidence);
        Assert.Equal(0.65, sortedResults[2].Confidence);
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
        Assert.Equal(0.76, averageConfidence, 0.01);
        Assert.Equal(0.88, maxConfidence);
        Assert.Equal(0.65, minConfidence);
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
        Assert.True(result.Confidence > 0.9);
        Assert.NotEmpty(result.Metric);
        Assert.NotEmpty(result.PredictedValue);
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
        Assert.True(result.Confidence < 0.5);
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
        Assert.NotNull(deserialized);
        Assert.Equal(result.Metric, deserialized.Metric);
        Assert.Equal(result.PredictedValue, deserialized.PredictedValue);
        Assert.Equal(result.Confidence, deserialized.Confidence);
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
        Assert.Equal("", result.Metric);
        Assert.Equal("", result.PredictedValue);
        Assert.Equal(-1.0, result.Confidence);
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
        Assert.Equal(1.0, result.Confidence);
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
        Assert.Equal(0.0, result.Confidence);
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
        Assert.Contains("CPU", groupedResults.Keys);
        Assert.Contains("Memory", groupedResults.Keys);
        Assert.Equal(2, groupedResults["CPU"].Count);
        Assert.Equal(2, groupedResults["Memory"].Count);
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
        Assert.Equal(2, highConfidenceResults.Count());
        Assert.Equal("Network", highConfidenceResults[0].Metric);
        Assert.Equal("Memory", highConfidenceResults[1].Metric);
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
        foreach (var r in results)
        {
            Assert.NotEmpty(r.Metric);
            Assert.NotEmpty(r.PredictedValue);
            Assert.InRange(r.Confidence, 0.0, 1.0);
        }
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
        Assert.NotSame(original, clone);
        Assert.Equal(original.Metric, clone.Metric);
        Assert.Equal(original.PredictedValue, clone.PredictedValue);
        Assert.Equal(original.Confidence, clone.Confidence);
    }

    [Fact]
    public void PredictionResult_EmptyCollections_ShouldHandleAggregations()
    {
        // Arrange
        var results = new List<PredictionResult>();

        // Act & Assert
        Assert.Empty(results);
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
        Assert.Equal("Performance", anonymous.Metric);
        Assert.True(anonymous.IsHighConfidence);
    }
}

