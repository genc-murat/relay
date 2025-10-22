using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using System;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class EnsembleWeightsUpdaterTests
{
#pragma warning disable CS8602, CS8620
    private readonly Mock<ILogger<EnsembleWeightsUpdater>> _loggerMock;
    private readonly PatternRecognitionConfig _config;

    public EnsembleWeightsUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<EnsembleWeightsUpdater>>();
        _config = new PatternRecognitionConfig
        {
            EnsembleModels = new[] { "Model1", "Model2", "Model3" }
        };
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EnsembleWeightsUpdater(null!, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Config_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EnsembleWeightsUpdater(_loggerMock.Object, null!));
    }

    [Fact]
    public void UpdatePatterns_Should_Update_All_Models_And_Return_Count()
    {
        // Arrange
        var updater = new EnsembleWeightsUpdater(_loggerMock.Object, _config);
        var predictions = Array.Empty<PredictionResult>();
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(3, result); // Should return the number of models updated

        // Verify logging for each model
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Model Model1 ensemble weight")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Model Model2 ensemble weight")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Model Model3 ensemble weight")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Exception_And_Return_Zero()
    {
        // Arrange - Create a config that will cause an exception
        var badConfig = new PatternRecognitionConfig
        {
            EnsembleModels = null! // This will cause NullReferenceException
        };
        var updater = new EnsembleWeightsUpdater(_loggerMock.Object, badConfig);
        var predictions = Array.Empty<PredictionResult>();
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result); // Should return 0 on exception

        // Verify warning was logged
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Error updating ensemble weights")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}