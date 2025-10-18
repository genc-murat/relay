using Relay.CLI.Commands;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class AILearningResultsTests
{
    [Fact]
    public void AILearningResults_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var results = new AILearningResults();

        // Assert
        Assert.Equal(0L, results.TrainingSamples);
        Assert.Equal(0.0, results.ModelAccuracy);
        Assert.Equal(0.0, results.TrainingTime);
        Assert.NotNull(results.ImprovementAreas);
        Assert.Empty(results.ImprovementAreas);
    }

    [Fact]
    public void AILearningResults_CanSetTrainingSamples()
    {
        // Arrange
        var results = new AILearningResults();
        var expectedSamples = 15420L;

        // Act
        results.TrainingSamples = expectedSamples;

        // Assert
        Assert.Equal(expectedSamples, results.TrainingSamples);
    }

    [Fact]
    public void AILearningResults_CanSetModelAccuracy()
    {
        // Arrange
        var results = new AILearningResults();
        var expectedAccuracy = 0.94;

        // Act
        results.ModelAccuracy = expectedAccuracy;

        // Assert
        Assert.Equal(expectedAccuracy, results.ModelAccuracy);
    }

    [Fact]
    public void AILearningResults_CanSetTrainingTime()
    {
        // Arrange
        var results = new AILearningResults();
        var expectedTime = 2.3;

        // Act
        results.TrainingTime = expectedTime;

        // Assert
        Assert.Equal(expectedTime, results.TrainingTime);
    }

    [Fact]
    public void AILearningResults_CanSetImprovementAreas()
    {
        // Arrange
        var results = new AILearningResults();
        var expectedAreas = new[]
        {
            new ImprovementArea { Area = "Caching Predictions", Improvement = 0.12 },
            new ImprovementArea { Area = "Batch Size Optimization", Improvement = 0.08 }
        };

        // Act
        results.ImprovementAreas = expectedAreas;

        // Assert
        Assert.Equal(expectedAreas, results.ImprovementAreas);
        Assert.Equal(2, results.ImprovementAreas.Length);
    }

    [Fact]
    public void AILearningResults_TrainingSamples_AcceptsLargeValues()
    {
        // Arrange
        var results = new AILearningResults();
        var largeValue = long.MaxValue;

        // Act
        results.TrainingSamples = largeValue;

        // Assert
        Assert.Equal(largeValue, results.TrainingSamples);
    }

    [Fact]
    public void AILearningResults_ModelAccuracy_AcceptsValidRange()
    {
        // Arrange
        var results = new AILearningResults();

        // Act & Assert
        results.ModelAccuracy = 0.0;
        Assert.Equal(0.0, results.ModelAccuracy);

        results.ModelAccuracy = 1.0;
        Assert.Equal(1.0, results.ModelAccuracy);

        results.ModelAccuracy = 0.5;
        Assert.Equal(0.5, results.ModelAccuracy);
    }

    [Fact]
    public void AILearningResults_TrainingTime_AcceptsDecimalValues()
    {
        // Arrange
        var results = new AILearningResults();

        // Act
        results.TrainingTime = 2.345;

        // Assert
        Assert.Equal(2.345, results.TrainingTime);
    }

    [Fact]
    public void AILearningResults_ImprovementAreas_CanBeNullInitially()
    {
        // Arrange
        var results = new AILearningResults();

        // Act
        results.ImprovementAreas = null!;

        // Assert
        Assert.Null(results.ImprovementAreas);
    }

    [Fact]
    public void AILearningResults_WithTypicalValues_HasExpectedStructure()
    {
        // Arrange & Act
        var results = new AILearningResults
        {
            TrainingSamples = 15420,
            ModelAccuracy = 0.94,
            TrainingTime = 2.3,
            ImprovementAreas = new[]
            {
                new ImprovementArea { Area = "Caching Predictions", Improvement = 0.12 },
                new ImprovementArea { Area = "Batch Size Optimization", Improvement = 0.08 }
            }
        };

        // Assert
        Assert.Equal(15420L, results.TrainingSamples);
        Assert.Equal(0.94, results.ModelAccuracy);
        Assert.Equal(2.3, results.TrainingTime);
        Assert.Equal(2, results.ImprovementAreas.Length);
        Assert.Equal("Caching Predictions", results.ImprovementAreas[0].Area);
        Assert.Equal(0.12, results.ImprovementAreas[0].Improvement);
        Assert.Equal("Batch Size Optimization", results.ImprovementAreas[1].Area);
        Assert.Equal(0.08, results.ImprovementAreas[1].Improvement);
    }

    [Fact]
    public void AILearningResults_ImprovementAreas_AreIndependentObjects()
    {
        // Arrange
        var results1 = new AILearningResults
        {
            ImprovementAreas = new[]
            {
                new ImprovementArea { Area = "Test", Improvement = 0.1 }
            }
        };

        var results2 = new AILearningResults
        {
            ImprovementAreas = new[]
            {
                new ImprovementArea { Area = "Different", Improvement = 0.2 }
            }
        };

        // Act & Assert
        Assert.Equal("Test", results1.ImprovementAreas[0].Area);
        Assert.Equal("Different", results2.ImprovementAreas[0].Area);
        Assert.NotSame(results1.ImprovementAreas[0], results2.ImprovementAreas[0]);
    }

    [Fact]
    public void AILearningResults_CanBeSerializedToJson()
    {
        // Arrange
        var results = new AILearningResults
        {
            TrainingSamples = 15420,
            ModelAccuracy = 0.94,
            TrainingTime = 2.3,
            ImprovementAreas = new[]
            {
                new ImprovementArea { Area = "Caching Predictions", Improvement = 0.12 }
            }
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(results);

        // Assert
        Assert.Contains("15420", json);
        Assert.Contains("0.94", json);
        Assert.Contains("2.3", json);
        Assert.Contains("Caching Predictions", json);
        Assert.Contains("0.12", json);
    }

    [Fact]
    public void AILearningResults_CanBeDeserializedFromJson()
    {
        // Arrange
        var json = @"{
            ""TrainingSamples"": 15420,
            ""ModelAccuracy"": 0.94,
            ""TrainingTime"": 2.3,
            ""ImprovementAreas"": [
                {
                    ""Area"": ""Caching Predictions"",
                    ""Improvement"": 0.12
                }
            ]
        }";

        // Act
        var results = System.Text.Json.JsonSerializer.Deserialize<AILearningResults>(json);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(15420L, results!.TrainingSamples);
        Assert.Equal(0.94, results.ModelAccuracy);
        Assert.Equal(2.3, results.TrainingTime);
        Assert.Single(results.ImprovementAreas);
        Assert.Equal("Caching Predictions", results.ImprovementAreas[0].Area);
        Assert.Equal(0.12, results.ImprovementAreas[0].Improvement);
    }

    [Fact]
    public void AILearningResults_EmptyImprovementAreas_SerializesCorrectly()
    {
        // Arrange
        var results = new AILearningResults
        {
            TrainingSamples = 1000,
            ModelAccuracy = 0.85,
            TrainingTime = 1.5,
            ImprovementAreas = Array.Empty<ImprovementArea>()
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(results);

        // Assert
        Assert.Contains("1000", json);
        Assert.Contains("0.85", json);
        Assert.Contains("1.5", json);
        Assert.Contains("[]", json);
    }

    [Fact]
    public void AILearningResults_WithZeroValues_IsValid()
    {
        // Arrange & Act
        var results = new AILearningResults
        {
            TrainingSamples = 0,
            ModelAccuracy = 0.0,
            TrainingTime = 0.0,
            ImprovementAreas = Array.Empty<ImprovementArea>()
        };

        // Assert
        Assert.Equal(0L, results.TrainingSamples);
        Assert.Equal(0.0, results.ModelAccuracy);
        Assert.Equal(0.0, results.TrainingTime);
        Assert.Empty(results.ImprovementAreas);
    }

    [Fact]
    public void AILearningResults_WithMaximumValues_IsValid()
    {
        // Arrange & Act
        var results = new AILearningResults
        {
            TrainingSamples = long.MaxValue,
            ModelAccuracy = 1.0,
            TrainingTime = double.MaxValue,
            ImprovementAreas = new[]
            {
                new ImprovementArea { Area = "Test", Improvement = double.MaxValue }
            }
        };

        // Assert
        Assert.Equal(long.MaxValue, results.TrainingSamples);
        Assert.Equal(1.0, results.ModelAccuracy);
        Assert.Equal(double.MaxValue, results.TrainingTime);
        Assert.Equal(double.MaxValue, results.ImprovementAreas[0].Improvement);
    }

    [Fact]
    public void AILearningResults_ImprovementAreas_CanContainMultipleItems()
    {
        // Arrange
        var areas = new ImprovementArea[100];
        for (int i = 0; i < areas.Length; i++)
        {
            areas[i] = new ImprovementArea { Area = $"Area{i}", Improvement = i * 0.01 };
        }

        var results = new AILearningResults
        {
            ImprovementAreas = areas
        };

        // Act & Assert
        Assert.Equal(100, results.ImprovementAreas.Length);
        for (int i = 0; i < areas.Length; i++)
        {
            Assert.Equal($"Area{i}", results.ImprovementAreas[i].Area);
            Assert.Equal(i * 0.01, results.ImprovementAreas[i].Improvement);
        }
    }

    [Fact]
    public void AILearningResults_IsReferenceType()
    {
        // Arrange & Act
        var results1 = new AILearningResults();
        var results2 = results1;

        // Assert
        Assert.Same(results1, results2);
    }

    [Fact]
    public void AILearningResults_PropertiesAreIndependent()
    {
        // Arrange
        var results1 = new AILearningResults { TrainingSamples = 100 };
        var results2 = new AILearningResults { TrainingSamples = 200 };

        // Act
        results1.TrainingSamples = 300;

        // Assert
        Assert.Equal(300L, results1.TrainingSamples);
        Assert.Equal(200L, results2.TrainingSamples);
    }
}