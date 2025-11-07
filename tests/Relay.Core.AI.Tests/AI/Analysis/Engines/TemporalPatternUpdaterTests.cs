using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using System;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class TemporalPatternUpdaterTests
{
    private readonly Mock<ILogger<TemporalPatternUpdater>> _loggerMock;
    private readonly TemporalPatternUpdater _updater;

    public TemporalPatternUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<TemporalPatternUpdater>>();
        _updater = new TemporalPatternUpdater(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TemporalPatternUpdater(null!));
    }

    [Fact]
    public void UpdatePatterns_Should_Group_By_Hour_Correctly()
    {
        // Arrange
        var baseTime = new DateTime(2023, 1, 1, 10, 0, 0);
        var predictions = new[]
        {
            CreatePredictionResult(baseTime.AddHours(0), TimeSpan.FromMilliseconds(50)),   // 10:00
            CreatePredictionResult(baseTime.AddHours(0), TimeSpan.Zero),                  // 10:00
            CreatePredictionResult(baseTime.AddHours(1), TimeSpan.FromMilliseconds(30)),   // 11:00
            CreatePredictionResult(baseTime.AddHours(1), TimeSpan.FromMilliseconds(40)),   // 11:00
            CreatePredictionResult(baseTime.AddHours(2), TimeSpan.Zero)                    // 12:00
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(4, result); // 2 hours + 2 days
        
        // Verify all success rates are 100%
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("100.00")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once());
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_All_Failed_Predictions()
    {
        // Arrange
        var baseTime = new DateTime(2023, 1, 1, 10, 0, 0);
        var predictions = new[]
        {
            CreatePredictionResult(baseTime, TimeSpan.Zero),
            CreatePredictionResult(baseTime.AddHours(1), TimeSpan.Zero),
            CreatePredictionResult(baseTime.AddDays(1), TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(4, result); // 2 hours + 2 days
        
        // Verify all success rates are 0%
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Trace,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("0.00")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeast(3));
    }

    [Fact]
    public void UpdatePatterns_Should_Ignore_Analysis_Parameter()
    {
        // Arrange
        var baseTime = new DateTime(2023, 1, 1, 10, 0, 0);
        var predictions = new[]
        {
            CreatePredictionResult(baseTime, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(baseTime.AddHours(1), TimeSpan.Zero)
        };

        // Act with null analysis
        var result1 = _updater.UpdatePatterns(predictions, null!);

        // Act with valid analysis
        var result2 = _updater.UpdatePatterns(predictions, new PatternAnalysisResult());

        // Assert
        Assert.Equal(result1, result2); // Results should be identical since analysis is not used
        Assert.Equal(3, result1); // 2 hours + 1 day
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
    public void UpdatePatterns_Should_Handle_Single_Prediction()
    {
        // Arrange
        var baseTime = new DateTime(2023, 1, 1, 10, 0, 0);
        var predictions = new[]
        {
            CreatePredictionResult(baseTime, TimeSpan.FromMilliseconds(50))
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(2, result); // 1 hour + 1 day
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Multiple_Days_And_Hours()
    {
        // Arrange
        var baseTime = new DateTime(2023, 1, 1, 10, 0, 0); // Sunday
        var predictions = new[]
        {
            CreatePredictionResult(baseTime, TimeSpan.FromMilliseconds(50)),                    // Sunday 10:00
            CreatePredictionResult(baseTime.AddHours(1), TimeSpan.Zero),                        // Sunday 11:00
            CreatePredictionResult(baseTime.AddDays(1), TimeSpan.FromMilliseconds(30)),         // Monday 10:00
            CreatePredictionResult(baseTime.AddDays(2), TimeSpan.FromMilliseconds(40)),         // Tuesday 10:00
            CreatePredictionResult(baseTime.AddDays(3), TimeSpan.Zero)                          // Wednesday 10:00
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(6, result); // 2 distinct hours + 4 distinct days
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Exception_With_Null_Predictions_Array()
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

    private static PredictionResult CreatePredictionResult(DateTime timestamp, TimeSpan actualImprovement)
    {
        return new PredictionResult
        {
            RequestType = typeof(object),
            PredictedStrategies = Array.Empty<OptimizationStrategy>(),
            ActualImprovement = actualImprovement,
            Timestamp = timestamp,
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