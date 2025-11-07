using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using System;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class LoadBasedPatternUpdaterTests
{
    private readonly Mock<ILogger<LoadBasedPatternUpdater>> _loggerMock;
    private readonly PatternRecognitionConfig _config;
    private readonly LoadBasedPatternUpdater _updater;

    public LoadBasedPatternUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<LoadBasedPatternUpdater>>();
        _config = new PatternRecognitionConfig
        {
            LoadThresholds = new LoadThresholds
            {
                HighLoad = 100,
                MediumLoad = 50
            }
        };
        _updater = new LoadBasedPatternUpdater(_loggerMock.Object, _config);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new LoadBasedPatternUpdater(null!, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Config_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new LoadBasedPatternUpdater(_loggerMock.Object, null!));
    }

    [Fact]
    public void UpdatePatterns_Should_Classify_High_Load_Correctly()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(150, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(120, TimeSpan.FromMilliseconds(30)),
            CreatePredictionResult(200, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result); // One load level (High)
    }

    [Fact]
    public void UpdatePatterns_Should_Classify_Medium_Load_Correctly()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(75, TimeSpan.FromMilliseconds(40)),
            CreatePredictionResult(60, TimeSpan.FromMilliseconds(20)),
            CreatePredictionResult(80, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result); // One load level (Medium)
    }

    [Fact]
    public void UpdatePatterns_Should_Classify_Low_Load_Correctly()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(25, TimeSpan.FromMilliseconds(30)),
            CreatePredictionResult(30, TimeSpan.FromMilliseconds(10)),
            CreatePredictionResult(40, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result); // One load level (Low)
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Multiple_Load_Levels()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(150, TimeSpan.FromMilliseconds(50)), // High
            CreatePredictionResult(75, TimeSpan.FromMilliseconds(30)),  // Medium
            CreatePredictionResult(25, TimeSpan.FromMilliseconds(10)),  // Low
            CreatePredictionResult(120, TimeSpan.Zero),                 // High
            CreatePredictionResult(60, TimeSpan.Zero),                  // Medium
            CreatePredictionResult(30, TimeSpan.Zero)                   // Low
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(3, result); // Three load levels: High, Medium, Low
    }

    [Fact]
    public void UpdatePatterns_Should_Calculate_Success_Rates_Per_Load_Level()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(150, TimeSpan.FromMilliseconds(50)), // High - success
            CreatePredictionResult(120, TimeSpan.Zero),                  // High - failure
            CreatePredictionResult(75, TimeSpan.FromMilliseconds(30)),   // Medium - success
            CreatePredictionResult(60, TimeSpan.Zero),                  // Medium - failure
            CreatePredictionResult(25, TimeSpan.FromMilliseconds(10)),   // Low - success
            CreatePredictionResult(30, TimeSpan.Zero)                    // Low - failure
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(3, result);
        
        // Verify debug logging for each load level with exact success rates
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "Load level High: Success rate = 50.00 % (2 predictions)"),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "Load level Medium: Success rate = 50.00 % (2 predictions)"),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString() == "Load level Low: Success rate = 50.00 % (2 predictions)"),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Empty_Predictions()
    {
        // Arrange
        var predictions = Array.Empty<PredictionResult>();
        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Exception_And_Return_Zero()
    {
        // Arrange
        var predictions = new PredictionResult[] { null! };
        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Respect_Custom_Load_Thresholds()
    {
        // Arrange
        _config.LoadThresholds.HighLoad = 200;
        _config.LoadThresholds.MediumLoad = 100;
        
        var predictions = new[]
        {
            CreatePredictionResult(150, TimeSpan.FromMilliseconds(50)), // Now Medium (was High)
            CreatePredictionResult(250, TimeSpan.FromMilliseconds(30)), // High
            CreatePredictionResult(50, TimeSpan.FromMilliseconds(10))   // Low
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(3, result);
        
        // Verify the load levels are classified correctly with new thresholds
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Medium") && v.ToString()!.Contains("100.00 %")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("High") && v.ToString()!.Contains("100.00 %")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Low") && v.ToString()!.Contains("100.00 %")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Edge_Case_Load_Values()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(50, TimeSpan.FromMilliseconds(10)),   // Exactly Medium threshold
            CreatePredictionResult(100, TimeSpan.FromMilliseconds(20)),  // Exactly High threshold
            CreatePredictionResult(49, TimeSpan.FromMilliseconds(5))     // Just below Medium threshold
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(2, result); // Medium and High (49 is Low)
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Null_Predictions_Array()
    {
        // Arrange
        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(null!, analysis);

        // Assert
        Assert.Equal(0, result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Null_Metrics()
    {
        // Arrange
        var predictions = new[]
        {
            new PredictionResult
            {
                RequestType = typeof(object),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                ActualImprovement = TimeSpan.FromMilliseconds(50),
                Timestamp = DateTime.UtcNow,
                Metrics = null! // Null metrics
            }
        };
        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result); // Exception should cause return 0
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void ClassifyLoad_Should_Handle_Invalid_Thresholds()
    {
        // Arrange - set invalid thresholds where HighLoad <= MediumLoad
        _config.LoadThresholds.HighLoad = 50;
        _config.LoadThresholds.MediumLoad = 50; // Same as HighLoad

        var predictions = new[]
        {
            CreatePredictionResult(60, TimeSpan.FromMilliseconds(50)), // Would be High but thresholds invalid
            CreatePredictionResult(40, TimeSpan.FromMilliseconds(30)), // Would be Medium
            CreatePredictionResult(20, TimeSpan.FromMilliseconds(10))  // Low
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert - should still process without error (logic still works, just thresholds overlap)
        Assert.Equal(2, result); // Two groups: High (60), Low (40 and 20)
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_All_Same_Load_Level()
    {
        // Arrange - all predictions in same load level
        var predictions = new[]
        {
            CreatePredictionResult(150, TimeSpan.FromMilliseconds(50)), // High
            CreatePredictionResult(120, TimeSpan.Zero),                  // High
            CreatePredictionResult(180, TimeSpan.FromMilliseconds(30))  // High
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result); // Only one load level group
    }

    private static PredictionResult CreatePredictionResult(int concurrentExecutions, TimeSpan actualImprovement)
    {
        return new PredictionResult
        {
            RequestType = typeof(object),
            PredictedStrategies = Array.Empty<OptimizationStrategy>(),
            ActualImprovement = actualImprovement,
            Timestamp = DateTime.UtcNow,
            Metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                MemoryUsage = 1024,
                CpuUsage = 50,
                TotalExecutions = 1,
                SuccessfulExecutions = 1,
                FailedExecutions = 0,
                ConcurrentExecutions = concurrentExecutions
            }
        };
    }
}