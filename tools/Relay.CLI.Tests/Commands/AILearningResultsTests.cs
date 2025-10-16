using Relay.CLI.Commands;
using FluentAssertions;
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
        results.TrainingSamples.Should().Be(0);
        results.ModelAccuracy.Should().Be(0.0);
        results.TrainingTime.Should().Be(0.0);
        results.ImprovementAreas.Should().NotBeNull();
        results.ImprovementAreas.Should().BeEmpty();
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
        results.TrainingSamples.Should().Be(expectedSamples);
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
        results.ModelAccuracy.Should().Be(expectedAccuracy);
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
        results.TrainingTime.Should().Be(expectedTime);
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
        results.ImprovementAreas.Should().BeEquivalentTo(expectedAreas);
        results.ImprovementAreas.Should().HaveCount(2);
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
        results.TrainingSamples.Should().Be(largeValue);
    }

    [Fact]
    public void AILearningResults_ModelAccuracy_AcceptsValidRange()
    {
        // Arrange
        var results = new AILearningResults();

        // Act & Assert
        results.ModelAccuracy = 0.0;
        results.ModelAccuracy.Should().Be(0.0);

        results.ModelAccuracy = 1.0;
        results.ModelAccuracy.Should().Be(1.0);

        results.ModelAccuracy = 0.5;
        results.ModelAccuracy.Should().Be(0.5);
    }

    [Fact]
    public void AILearningResults_TrainingTime_AcceptsDecimalValues()
    {
        // Arrange
        var results = new AILearningResults();

        // Act
        results.TrainingTime = 2.345;

        // Assert
        results.TrainingTime.Should().Be(2.345);
    }

    [Fact]
    public void AILearningResults_ImprovementAreas_CanBeNullInitially()
    {
        // Arrange
        var results = new AILearningResults();

        // Act
        results.ImprovementAreas = null!;

        // Assert
        results.ImprovementAreas.Should().BeNull();
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
        results.TrainingSamples.Should().Be(15420);
        results.ModelAccuracy.Should().Be(0.94);
        results.TrainingTime.Should().Be(2.3);
        results.ImprovementAreas.Should().HaveCount(2);
        results.ImprovementAreas[0].Area.Should().Be("Caching Predictions");
        results.ImprovementAreas[0].Improvement.Should().Be(0.12);
        results.ImprovementAreas[1].Area.Should().Be("Batch Size Optimization");
        results.ImprovementAreas[1].Improvement.Should().Be(0.08);
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
        results1.ImprovementAreas[0].Area.Should().Be("Test");
        results2.ImprovementAreas[0].Area.Should().Be("Different");
        results1.ImprovementAreas[0].Should().NotBeSameAs(results2.ImprovementAreas[0]);
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
        json.Should().Contain("15420");
        json.Should().Contain("0.94");
        json.Should().Contain("2.3");
        json.Should().Contain("Caching Predictions");
        json.Should().Contain("0.12");
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
        results.Should().NotBeNull();
        results!.TrainingSamples.Should().Be(15420);
        results.ModelAccuracy.Should().Be(0.94);
        results.TrainingTime.Should().Be(2.3);
        results.ImprovementAreas.Should().HaveCount(1);
        results.ImprovementAreas[0].Area.Should().Be("Caching Predictions");
        results.ImprovementAreas[0].Improvement.Should().Be(0.12);
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
        json.Should().Contain("1000");
        json.Should().Contain("0.85");
        json.Should().Contain("1.5");
        json.Should().Contain("[]");
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
        results.TrainingSamples.Should().Be(0);
        results.ModelAccuracy.Should().Be(0.0);
        results.TrainingTime.Should().Be(0.0);
        results.ImprovementAreas.Should().BeEmpty();
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
        results.TrainingSamples.Should().Be(long.MaxValue);
        results.ModelAccuracy.Should().Be(1.0);
        results.TrainingTime.Should().Be(double.MaxValue);
        results.ImprovementAreas[0].Improvement.Should().Be(double.MaxValue);
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
        results.ImprovementAreas.Should().HaveCount(100);
        for (int i = 0; i < areas.Length; i++)
        {
            results.ImprovementAreas[i].Area.Should().Be($"Area{i}");
            results.ImprovementAreas[i].Improvement.Should().Be(i * 0.01);
        }
    }

    [Fact]
    public void AILearningResults_IsReferenceType()
    {
        // Arrange & Act
        var results1 = new AILearningResults();
        var results2 = results1;

        // Assert
        results1.Should().BeSameAs(results2);
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
        results1.TrainingSamples.Should().Be(300);
        results2.TrainingSamples.Should().Be(200);
    }
}