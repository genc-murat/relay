using Relay.CLI.Commands;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class AIModelLearnerTests
{
    private readonly AIModelLearner _learner;

    public AIModelLearnerTests()
    {
        _learner = new AIModelLearner();
    }

    [Fact]
    public async Task LearnAsync_WithValidParameters_ReturnsLearningResults()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = @"C:\test\metrics.json";
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<AILearningResults>();
    }

    [Fact]
    public async Task LearnAsync_ReturnsExpectedTrainingSamples()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.TrainingSamples.Should().Be(15420);
    }

    [Fact]
    public async Task LearnAsync_ReturnsExpectedModelAccuracy()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.ModelAccuracy.Should().Be(0.94);
    }

    [Fact]
    public async Task LearnAsync_ReturnsExpectedTrainingTime()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.TrainingTime.Should().Be(2.3);
    }

    [Fact]
    public async Task LearnAsync_ReturnsImprovementAreas()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.ImprovementAreas.Should().NotBeNull();
        result.ImprovementAreas.Should().HaveCount(2);

        var cachingArea = result.ImprovementAreas.FirstOrDefault(a => a.Area == "Caching Predictions");
        cachingArea.Should().NotBeNull();
        cachingArea.Area.Should().Be("Caching Predictions");
        cachingArea.Improvement.Should().Be(0.12);

        var batchArea = result.ImprovementAreas.FirstOrDefault(a => a.Area == "Batch Size Optimization");
        batchArea.Should().NotBeNull();
        batchArea.Area.Should().Be("Batch Size Optimization");
        batchArea.Improvement.Should().Be(0.08);
    }

    [Fact]
    public async Task LearnAsync_ModelAccuracyIsWithinValidRange()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.ModelAccuracy.Should().BeGreaterThanOrEqualTo(0);
        result.ModelAccuracy.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task LearnAsync_TrainingTimeIsPositive()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.TrainingTime.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LearnAsync_TrainingSamplesIsPositive()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.TrainingSamples.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LearnAsync_ImprovementAreasHaveValidImprovements()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        foreach (var area in result.ImprovementAreas)
        {
            area.Improvement.Should().BeGreaterThan(0);
            area.Improvement.Should().BeLessThanOrEqualTo(1);
        }
    }

    [Fact]
    public async Task LearnAsync_WithDifferentParameters_ReturnsSameResults()
    {
        // Arrange
        var path1 = @"C:\test\project1";
        var path2 = @"C:\test\project2";
        string? metricsPath1 = @"C:\metrics1.json";
        string? metricsPath2 = @"C:\metrics2.json";
        var updateModel1 = true;
        var updateModel2 = false;
        var validate1 = true;
        var validate2 = false;

        // Act
        var result1 = await _learner.LearnAsync(path1, metricsPath1, updateModel1, validate1);
        var result2 = await _learner.LearnAsync(path2, metricsPath2, updateModel2, validate2);

        // Assert
        // Currently implementation doesn't use the parameters, so results are the same
        result1.TrainingSamples.Should().Be(result2.TrainingSamples);
        result1.ModelAccuracy.Should().Be(result2.ModelAccuracy);
        result1.TrainingTime.Should().Be(result2.TrainingTime);
        result1.ImprovementAreas.Should().HaveCount(result2.ImprovementAreas.Length);
    }

    [Fact]
    public async Task LearnAsync_WithNullMetricsPath_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.Should().NotBeNull();
        // Currently implementation doesn't use metricsPath parameter, so results are the same
        result.TrainingSamples.Should().Be(15420);
    }

    [Fact]
    public async Task LearnAsync_WithEmptyPath_ReturnsSameResults()
    {
        // Arrange
        var path = "";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.Should().NotBeNull();
        // Currently implementation doesn't use path parameter, so results are the same
        result.TrainingSamples.Should().Be(15420);
    }

    [Fact]
    public async Task LearnAsync_CompletesWithinReasonableTime()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);
        stopwatch.Stop();

        // Assert
        // Should complete in less than 5 seconds (simulated delay is 4.5 seconds)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task LearnAsync_ReturnsNonEmptyImprovementAreas()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.ImprovementAreas.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LearnAsync_ImprovementAreasHaveNonEmptyNames()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        foreach (var area in result.ImprovementAreas)
        {
            area.Area.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task LearnAsync_ReturnsExpectedImprovementAreaNames()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        var areaNames = result.ImprovementAreas.Select(a => a.Area).ToArray();
        areaNames.Should().Contain("Caching Predictions");
        areaNames.Should().Contain("Batch Size Optimization");
    }

    [Fact]
    public async Task LearnAsync_ImprovementAreasAreIndependentObjects()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = true;

        // Act
        var result1 = await _learner.LearnAsync(path, metricsPath, updateModel, validate);
        var result2 = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result1.ImprovementAreas[0].Should().NotBeSameAs(result2.ImprovementAreas[0]);
        result1.ImprovementAreas[0].Area.Should().Be(result2.ImprovementAreas[0].Area);
        result1.ImprovementAreas[0].Improvement.Should().Be(result2.ImprovementAreas[0].Improvement);
    }

    [Fact]
    public async Task LearnAsync_WithUpdateModelFalse_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = false;
        var validate = true;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.Should().NotBeNull();
        // Currently implementation doesn't use updateModel parameter, so results are the same
        result.TrainingSamples.Should().Be(15420);
    }

    [Fact]
    public async Task LearnAsync_WithValidateFalse_ReturnsSameResults()
    {
        // Arrange
        var path = @"C:\test\project";
        string? metricsPath = null;
        var updateModel = true;
        var validate = false;

        // Act
        var result = await _learner.LearnAsync(path, metricsPath, updateModel, validate);

        // Assert
        result.Should().NotBeNull();
        // Currently implementation doesn't use validate parameter, so results are the same
        result.ModelAccuracy.Should().Be(0.94);
    }
}