using Relay.CLI.Commands;

using System.Linq;
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
        Assert.NotNull(result);
        Assert.IsType<AILearningResults>(result);
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
        Assert.Equal(15420, result.TrainingSamples);
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
        Assert.Equal(0.94, result.ModelAccuracy);
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
        Assert.Equal(2.3, result.TrainingTime);
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
        Assert.NotNull(result.ImprovementAreas);
        Assert.Equal(2, result.ImprovementAreas.Count());

        var cachingArea = result.ImprovementAreas.FirstOrDefault(a => a.Area == "Caching Predictions");
        Assert.NotNull(cachingArea);
        Assert.Equal("Caching Predictions", cachingArea.Area);
        Assert.Equal(0.12, cachingArea.Improvement);

        var batchArea = result.ImprovementAreas.FirstOrDefault(a => a.Area == "Batch Size Optimization");
        Assert.NotNull(batchArea);
        Assert.Equal("Batch Size Optimization", batchArea.Area);
        Assert.Equal(0.08, batchArea.Improvement);
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
        Assert.True(result.ModelAccuracy >= 0);
        Assert.True(result.ModelAccuracy <= 1);
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
        Assert.True(result.TrainingTime > 0);
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
        Assert.True(result.TrainingSamples > 0);
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
            Assert.True(area.Improvement > 0);
            Assert.True(area.Improvement <= 1);
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
        Assert.Equal(result2.TrainingSamples, result1.TrainingSamples);
        Assert.Equal(result2.ModelAccuracy, result1.ModelAccuracy);
        Assert.Equal(result2.TrainingTime, result1.TrainingTime);
        Assert.Equal(result2.ImprovementAreas.Length, result1.ImprovementAreas.Count());
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
        Assert.NotNull(result);
        // Currently implementation doesn't use metricsPath parameter, so results are the same
        Assert.Equal(15420, result.TrainingSamples);
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
        Assert.NotNull(result);
        // Currently implementation doesn't use path parameter, so results are the same
        Assert.Equal(15420, result.TrainingSamples);
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
        Assert.True(stopwatch.ElapsedMilliseconds < 5000);
        Assert.NotNull(result);
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
        Assert.NotEmpty(result.ImprovementAreas);
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
            Assert.False(string.IsNullOrEmpty(area.Area));
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
        Assert.Contains("Caching Predictions", areaNames);
        Assert.Contains("Batch Size Optimization", areaNames);
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
        Assert.NotSame(result1.ImprovementAreas[0], result2.ImprovementAreas[0]);
        Assert.Equal(result1.ImprovementAreas[0].Area, result2.ImprovementAreas[0].Area);
        Assert.Equal(result1.ImprovementAreas[0].Improvement, result2.ImprovementAreas[0].Improvement);
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
        Assert.NotNull(result);
        // Currently implementation doesn't use updateModel parameter, so results are the same
        Assert.Equal(15420, result.TrainingSamples);
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
        Assert.NotNull(result);
        // Currently implementation doesn't use validate parameter, so results are the same
        Assert.Equal(0.94, result.ModelAccuracy);
    }
}

