using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class PredictionResultTests
{
    [Fact]
    public void PredictionResult_ShouldHaveMetricProperty()
    {
        // Arrange & Act
        var result = new PredictionResult { Metric = "Throughput" };

        // Assert
        result.Metric.Should().Be("Throughput");
    }

    [Fact]
    public void PredictionResult_ShouldHavePredictedValueProperty()
    {
        // Arrange & Act
        var result = new PredictionResult { PredictedValue = "1,200 req/sec" };

        // Assert
        result.PredictedValue.Should().Be("1,200 req/sec");
    }

    [Fact]
    public void PredictionResult_ShouldHaveConfidenceProperty()
    {
        // Arrange & Act
        var result = new PredictionResult { Confidence = 0.89 };

        // Assert
        result.Confidence.Should().Be(0.89);
    }

    [Fact]
    public void PredictionResult_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var result = new PredictionResult();

        // Assert
        result.Metric.Should().Be("");
        result.PredictedValue.Should().Be("");
        result.Confidence.Should().Be(0.0);
    }

    [Fact]
    public void PredictionResult_ShouldAllowSettingAllProperties()
    {
        // Arrange & Act
        var result = new PredictionResult
        {
            Metric = "Response Time",
            PredictedValue = "95ms avg",
            Confidence = 0.92
        };

        // Assert
        result.Metric.Should().Be("Response Time");
        result.PredictedValue.Should().Be("95ms avg");
        result.Confidence.Should().Be(0.92);
    }

    [Fact]
    public void PredictionResult_ShouldHandleZeroConfidence()
    {
        // Arrange & Act
        var result = new PredictionResult { Confidence = 0.0 };

        // Assert
        result.Confidence.Should().Be(0.0);
    }

    [Fact]
    public void PredictionResult_ShouldHandleHighConfidence()
    {
        // Arrange & Act
        var result = new PredictionResult { Confidence = 1.0 };

        // Assert
        result.Confidence.Should().Be(1.0);
    }

    [Fact]
    public void PredictionResult_ShouldHandleNegativeConfidence()
    {
        // Arrange & Act
        var result = new PredictionResult { Confidence = -0.1 };

        // Assert
        result.Confidence.Should().Be(-0.1);
    }
}