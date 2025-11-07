using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using System;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class StrategyEffectivenessPatternUpdaterTests
{
    private readonly Mock<ILogger<StrategyEffectivenessPatternUpdater>> _loggerMock;
    private readonly StrategyEffectivenessPatternUpdater _updater;

    public StrategyEffectivenessPatternUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<StrategyEffectivenessPatternUpdater>>();
        _updater = new StrategyEffectivenessPatternUpdater(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new StrategyEffectivenessPatternUpdater(null!));
    }

    [Fact]
    public void UpdatePatterns_Should_Calculate_Effectiveness_For_Single_Strategy()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(30)),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result);
        
        // Verify debug logging with calculated effectiveness
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Strategy EnableCaching effectiveness")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Calculate_Effectiveness_For_Multiple_Strategies()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero),
            CreatePredictionResult(OptimizationStrategy.Parallelization, TimeSpan.FromMilliseconds(30)),
            CreatePredictionResult(OptimizationStrategy.Parallelization, TimeSpan.FromMilliseconds(40))
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(2, result);
        
        // Verify debug logging for both strategies
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Strategy EnableCaching effectiveness")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Strategy ParallelProcessing effectiveness")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Multiple_Strategies_Per_Prediction()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResultWithMultipleStrategies(
                new[] { OptimizationStrategy.Caching, OptimizationStrategy.Parallelization },
                TimeSpan.FromMilliseconds(50)),
            CreatePredictionResultWithMultipleStrategies(
                new[] { OptimizationStrategy.Caching, OptimizationStrategy.Parallelization },
                TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(2, result); // Both strategies should be analyzed
    }

    [Fact]
    public void UpdatePatterns_Should_Calculate_Correct_Success_Rate()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(30)),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result);
        
        // Verify success rate is 50%
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("50.00")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Calculate_Correct_Average_Improvement()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(30)),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result);
        
        // Verify average improvement is 40ms (only successful predictions)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("40")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_All_Successful_Predictions()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(30)),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(40))
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result);
        
        // Verify success rate is 100%
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("100.00")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_All_Failed_Predictions()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result);
        
        // Verify success rate is 0% and average improvement is 0
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("0.00") && v.ToString()!.Contains("0ms")),
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
    public void UpdatePatterns_Should_Calculate_Effectiveness_Score_Correctly()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(100)), // High improvement
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(100)), // High improvement
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero)                    // Failure
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result);
        
        // Verify effectiveness score calculation
        // Success rate: 2/3 ≈ 0.67
        // Average improvement: 100ms
        // Effectiveness score: 0.67 * (1 + log10(100)) ≈ 0.67 * 3 = 2.00
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Score=2.00")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Zero_Average_Improvement_In_Effectiveness_Calculation()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result);

        // Verify effectiveness score with zero improvement
        // Success rate: 0/2 = 0.00
        // Average improvement: 0ms (but Math.Max(1, avgImprovement) ensures at least 1)
        // Effectiveness score: 0.00 * (1 + log10(1)) = 0.00
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Score=0.00")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Ignore_Analysis_Parameter()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero)
        };

        // Act with null analysis
        var result1 = _updater.UpdatePatterns(predictions, null!);

        // Act with valid analysis
        var result2 = _updater.UpdatePatterns(predictions, new PatternAnalysisResult());

        // Assert
        Assert.Equal(result1, result2); // Results should be identical since analysis is not used
        Assert.Equal(1, result1);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Predictions_With_No_Strategies()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResultWithMultipleStrategies(Array.Empty<OptimizationStrategy>(), TimeSpan.FromMilliseconds(50))
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result); // No strategies to update
    }



    [Fact]
    public void UpdatePatterns_Should_Handle_Very_High_Improvement_Values()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(1000000)), // 1e6 ms
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(1000000))
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result);

        // Verify effectiveness score: 1.0 * (1 + log10(1000000)) = 1 + 6 = 7.00
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Score=7.00")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Fractional_Improvement_Values()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(0.5)),
            CreatePredictionResult(OptimizationStrategy.Caching, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result);

        // Verify effectiveness score: 0.5 * (1 + log10(max(1, 0.5))) = 0.5 * (1 + log10(1)) = 0.5 * 1 = 0.50
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Score=0.50")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    private static PredictionResult CreatePredictionResult(OptimizationStrategy strategy, TimeSpan actualImprovement)
    {
        return new PredictionResult
        {
            RequestType = typeof(object),
            PredictedStrategies = new[] { strategy },
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

    private static PredictionResult CreatePredictionResultWithMultipleStrategies(OptimizationStrategy[] strategies, TimeSpan actualImprovement)
    {
        return new PredictionResult
        {
            RequestType = typeof(object),
            PredictedStrategies = strategies,
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