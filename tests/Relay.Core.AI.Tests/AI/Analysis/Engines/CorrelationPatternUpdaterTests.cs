using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using System;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class CorrelationPatternUpdaterTests
{
    private readonly Mock<ILogger<CorrelationPatternUpdater>> _loggerMock;
    private readonly PatternRecognitionConfig _config;
    private readonly CorrelationPatternUpdater _updater;

    public CorrelationPatternUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<CorrelationPatternUpdater>>();
        _config = new PatternRecognitionConfig
        {
            MinimumCorrelationSuccessRate = 0.8,
            MinimumCorrelationCount = 3
        };
        _updater = new CorrelationPatternUpdater(_loggerMock.Object, _config);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CorrelationPatternUpdater(null!, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Config_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CorrelationPatternUpdater(_loggerMock.Object, null!));
    }

    [Fact]
    public void UpdatePatterns_Should_Return_Zero_When_No_Correlations_Meet_Thresholds()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.Zero),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.Zero),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void UpdatePatterns_Should_Return_Count_Of_Strong_Correlations()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(60)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(70)),
            CreatePredictionResult(typeof(int), OptimizationStrategy.Parallelization, TimeSpan.FromMilliseconds(30)),
            CreatePredictionResult(typeof(int), OptimizationStrategy.Parallelization, TimeSpan.FromMilliseconds(40)),
            CreatePredictionResult(typeof(int), OptimizationStrategy.Parallelization, TimeSpan.FromMilliseconds(10))
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(2, result); // Both string + caching and int + parallelization meet 0.8 success rate
    }

    [Fact]
    public void UpdatePatterns_Should_Filter_By_Minimum_Correlation_Count()
    {
        // Arrange
        _config.MinimumCorrelationCount = 5;
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(60)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(70))
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result); // Only 3 instances, below minimum count
    }

    [Fact]
    public void UpdatePatterns_Should_Filter_By_Minimum_Success_Rate()
    {
        // Arrange
        _config.MinimumCorrelationSuccessRate = 0.9;
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(60)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.Zero),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(70))
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result); // 0.75 success rate, below 0.9 threshold
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Multiple_Strategies_Per_Prediction()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResultWithMultipleStrategies(typeof(string), 
                new[] { OptimizationStrategy.Caching, OptimizationStrategy.Parallelization }, 
                TimeSpan.FromMilliseconds(50)),
            CreatePredictionResultWithMultipleStrategies(typeof(string), 
                new[] { OptimizationStrategy.Caching, OptimizationStrategy.Parallelization }, 
                TimeSpan.FromMilliseconds(60)),
            CreatePredictionResultWithMultipleStrategies(typeof(string), 
                new[] { OptimizationStrategy.Caching, OptimizationStrategy.Parallelization }, 
                TimeSpan.FromMilliseconds(70))
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(2, result); // Both strategies have strong correlation with string type
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
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error updating correlation patterns")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void UpdatePatterns_Should_Log_Debug_Messages_For_Strong_Correlations()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(60)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(70))
        };

        var analysis = new PatternAnalysisResult();

        // Act
        _updater.UpdatePatterns(predictions, analysis);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Different_Request_Types_With_Same_Strategy()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(60)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(70)),
            CreatePredictionResult(typeof(int), OptimizationStrategy.Caching, TimeSpan.Zero),
            CreatePredictionResult(typeof(int), OptimizationStrategy.Caching, TimeSpan.Zero),
            CreatePredictionResult(typeof(int), OptimizationStrategy.Caching, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result); // Only string + caching meets success rate threshold
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Null_RequestType()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(null, OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(null, OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(60)),
            CreatePredictionResult(null, OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(70))
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result); // Should return 0 due to exception in RequestType.Name
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Null_PredictedStrategies()
    {
        // Arrange
        var predictions = new[]
        {
            new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = null,
                ActualImprovement = TimeSpan.FromMilliseconds(50),
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
            }
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result); // Should return 0 due to exception
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Empty_PredictedStrategies_Array()
    {
        // Arrange
        var predictions = new[]
        {
            new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                ActualImprovement = TimeSpan.FromMilliseconds(50),
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
            }
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result); // No strategies, no correlations
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Zero_MinimumCorrelationSuccessRate()
    {
        // Arrange
        _config.MinimumCorrelationSuccessRate = 0.0;
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.Zero),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result); // 1/3 ≈0.333 > 0.0, and count >=3
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Zero_MinimumCorrelationCount()
    {
        // Arrange
        _config.MinimumCorrelationCount = 0;
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50))
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result); // Count=1 >=0, success rate=1.0 >0.8
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Negative_Config_Values()
    {
        // Arrange
        _config.MinimumCorrelationSuccessRate = -0.1;
        _config.MinimumCorrelationCount = -1;
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(1, result); // Negative thresholds allow all correlations
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Exact_Success_Rate_Threshold()
    {
        // Arrange
        // Default threshold is 0.8, create 4 predictions: 3 success, 1 fail = 0.75 < 0.8
        // But to test exact, need 4/5 = 0.8 exactly
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(60)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(70)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.FromMilliseconds(80)),
            CreatePredictionResult(typeof(string), OptimizationStrategy.Caching, TimeSpan.Zero)
        };

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result); // 4/5 = 0.8 exactly, but > 0.8 required, so not included
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Large_Number_Of_Predictions()
    {
        // Arrange
        var predictions = Enumerable.Range(0, 1000)
            .Select(i => CreatePredictionResult(
                i % 5 == 0 ? typeof(string) : typeof(int),
                i % 2 == 0 ? OptimizationStrategy.Caching : OptimizationStrategy.Parallelization,
                TimeSpan.FromMilliseconds(i % 3 == 0 ? 50 : 0)))
            .ToArray();

        var analysis = new PatternAnalysisResult();

        // Act
        var result = _updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.True(result >= 0); // Should complete without throwing, result depends on correlations
    }

    private static PredictionResult CreatePredictionResult(Type? requestType, OptimizationStrategy strategy, TimeSpan actualImprovement)
    {
        return new PredictionResult
        {
            RequestType = requestType,
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

    private static PredictionResult CreatePredictionResultWithMultipleStrategies(Type? requestType, OptimizationStrategy[] strategies, TimeSpan actualImprovement)
    {
        return new PredictionResult
        {
            RequestType = requestType,
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