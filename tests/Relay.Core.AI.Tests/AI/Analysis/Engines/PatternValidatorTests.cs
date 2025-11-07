using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using System;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class PatternValidatorTests
{
    private readonly Mock<ILogger<PatternValidator>> _loggerMock;
    private readonly PatternRecognitionConfig _config;
    private readonly PatternValidator _validator;

    public PatternValidatorTests()
    {
        _loggerMock = new Mock<ILogger<PatternValidator>>();
        _config = new PatternRecognitionConfig
        {
            MinimumOverallAccuracy = 0.7
        };
        _validator = new PatternValidator(_loggerMock.Object, _config);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PatternValidator(null!, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Config_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PatternValidator(_loggerMock.Object, null!));
    }

    [Fact]
    public void UpdatePatterns_Should_Pass_Validation_When_All_Metrics_Are_Acceptable()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(TimeSpan.FromMilliseconds(30)),
            CreatePredictionResult(TimeSpan.FromMilliseconds(40))
        };

        var analysis = new PatternAnalysisResult
        {
            OverallAccuracy = 0.8,
            PatternsUpdated = 5
        };

        // Act
        var result = _validator.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result); // Validator always returns 0
        
        // Verify debug logging for successful validation
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("All retrained patterns validated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Fail_Validation_When_Overall_Accuracy_Is_Too_Low()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(TimeSpan.Zero),
            CreatePredictionResult(TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult
        {
            OverallAccuracy = 0.33, // Below 0.7 threshold
            PatternsUpdated = 5
        };

        // Act
        var result = _validator.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);
        
        // Verify warning logging for accuracy issue
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Overall accuracy below acceptable threshold: 33.00%")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Fail_Validation_When_No_Patterns_Updated()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(TimeSpan.FromMilliseconds(30))
        };

        var analysis = new PatternAnalysisResult
        {
            OverallAccuracy = 0.8,
            PatternsUpdated = 0
        };

        // Act
        var result = _validator.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);
        
        // Verify warning logging for no patterns updated
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No patterns were updated during retraining")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Fail_Validation_When_Multiple_Issues_Exist()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(TimeSpan.Zero),
            CreatePredictionResult(TimeSpan.Zero),
            CreatePredictionResult(TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult
        {
            OverallAccuracy = 0.0, // Below threshold
            PatternsUpdated = 0     // No patterns updated
        };

        // Act
        var result = _validator.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);
        
        // Verify warning logging for both issues
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Pattern validation found 2 issues")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Overall accuracy below acceptable threshold: 0.00%")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No patterns were updated during retraining")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Empty_Predictions()
    {
        // Arrange
        var predictions = Array.Empty<PredictionResult>();
        var analysis = new PatternAnalysisResult
        {
            OverallAccuracy = 0.8,
            PatternsUpdated = 5
        };

        // Act
        var result = _validator.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);
        
        // Verify debug logging for successful validation
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("All retrained patterns validated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Exception_And_Return_Zero()
    {
        // Arrange
        var predictions = new PredictionResult[] { null! };
        var analysis = new PatternAnalysisResult();

        // Act
        var result = _validator.UpdatePatterns(predictions, analysis);

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
    public void UpdatePatterns_Should_Respect_Custom_Minimum_Accuracy_Threshold()
    {
        // Arrange
        _config.MinimumOverallAccuracy = 0.5;
        var predictions = new[]
        {
            CreatePredictionResult(TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult
        {
            OverallAccuracy = 0.5, // Exactly at threshold
            PatternsUpdated = 5
        };

        // Act
        var result = _validator.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);
        
        // Verify debug logging for successful validation (0.5 is acceptable with 0.5 threshold)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("All retrained patterns validated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Fail_When_Just_Below_Threshold()
    {
        // Arrange
        _config.MinimumOverallAccuracy = 0.5;
        var predictions = new[]
        {
            CreatePredictionResult(TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult
        {
            OverallAccuracy = 0.49, // Just below threshold
            PatternsUpdated = 5
        };

        // Act
        var result = _validator.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);
        
        // Verify warning logging for accuracy issue
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Overall accuracy below acceptable threshold: 49.00%")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Zero_Minimum_Accuracy_Threshold()
    {
        // Arrange
        _config.MinimumOverallAccuracy = 0.0;
        var predictions = new[]
        {
            CreatePredictionResult(TimeSpan.Zero),
            CreatePredictionResult(TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult
        {
            OverallAccuracy = 0.0, // At threshold
            PatternsUpdated = 5
        };

        // Act
        var result = _validator.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);
        
        // Verify debug logging for successful validation (0.0 is acceptable with 0.0 threshold)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("All retrained patterns validated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Null_Analysis_Parameter()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(TimeSpan.FromMilliseconds(50))
        };

        // Act
        var result = _validator.UpdatePatterns(predictions, null!);

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
    public void UpdatePatterns_Should_Ignore_Predictions_Parameter()
    {
        // Arrange
        var analysis = new PatternAnalysisResult
        {
            OverallAccuracy = 0.8,
            PatternsUpdated = 5
        };

        // Act with null predictions
        var result1 = _validator.UpdatePatterns(null!, analysis);

        // Act with valid predictions
        var result2 = _validator.UpdatePatterns(new[] { CreatePredictionResult(TimeSpan.FromMilliseconds(50)) }, analysis);

        // Assert
        Assert.Equal(result1, result2); // Results should be identical since predictions are not used
        Assert.Equal(0, result1);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Negative_Overall_Accuracy()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult
        {
            OverallAccuracy = -0.1, // Negative accuracy
            PatternsUpdated = 5
        };

        // Act
        var result = _validator.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);

        // Verify warning logging for accuracy issue
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Overall accuracy below acceptable threshold: -10.00%")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Negative_Patterns_Updated()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(TimeSpan.FromMilliseconds(50))
        };

        var analysis = new PatternAnalysisResult
        {
            OverallAccuracy = 0.8,
            PatternsUpdated = -1 // Negative patterns updated
        };

        // Act
        var result = _validator.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);

        // Verify debug logging for successful validation (negative patterns updated doesn't trigger the == 0 check)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("All retrained patterns validated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Negative_Minimum_Accuracy_Threshold()
    {
        // Arrange
        _config.MinimumOverallAccuracy = -0.1; // Negative threshold
        var predictions = new[]
        {
            CreatePredictionResult(TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult
        {
            OverallAccuracy = -0.05, // Less negative than threshold
            PatternsUpdated = 5
        };

        // Act
        var result = _validator.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);

        // Verify debug logging for successful validation (since -0.05 >= -0.1)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("All retrained patterns validated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private static PredictionResult CreatePredictionResult(TimeSpan actualImprovement)
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
                FailedExecutions = 0
            }
        };
    }
}