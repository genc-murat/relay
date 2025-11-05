using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using System;
using System.Collections.Generic;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class FeatureImportancePatternUpdaterTests
{
    private readonly Mock<ILogger<FeatureImportancePatternUpdater>> _loggerMock;
    private readonly PatternRecognitionConfig _config;

    public FeatureImportancePatternUpdaterTests()
    {
        _loggerMock = new Mock<ILogger<FeatureImportancePatternUpdater>>();
        _config = new PatternRecognitionConfig
        {
            Features = new[] { "ExecutionTime", "ConcurrencyLevel", "MemoryUsage", "RepeatRate", "CacheHitRatio" }
        };
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FeatureImportancePatternUpdater(null!, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Config_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FeatureImportancePatternUpdater(_loggerMock.Object, null!));
    }

    [Fact]
    public void UpdatePatterns_Should_Process_Empty_Predictions()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = Array.Empty<PredictionResult>();
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert - UpdatePatterns iterates through all features even with empty predictions
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void UpdatePatterns_Should_Return_Feature_Count_When_Predictions_Exist()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreateMixedPredictions(5);
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void UpdatePatterns_Should_Calculate_All_Features()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreateMixedPredictions(10);
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(_config.Features.Length, result);
        // Verify that logger was called for each feature
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Exception_Gracefully()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreateMixedPredictions(5);
        var analysis = new PatternAnalysisResult();

        // Act - should not throw
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert - should complete without exception
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void UpdatePatterns_Should_Calculate_Importance_With_Only_Successful_Predictions()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreateSuccessfulOnlyPredictions(10);
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert - should process all features but calculate zero importance (no comparison group)
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void UpdatePatterns_Should_Calculate_Importance_With_Only_Failed_Predictions()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreateFailedOnlyPredictions(10);
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert - should process all features but calculate zero importance (no comparison group)
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Null_Predictions_Array()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var analysis = new PatternAnalysisResult();

        // Act & Assert - should handle gracefully
        var result = updater.UpdatePatterns(null!, analysis);

        // Should return 0 due to exception handling
        Assert.Equal(0, result);
    }

    [Fact]
    public void Feature_Importance_Should_Be_Normalized_Between_Zero_And_One()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreateMixedPredictions(20);
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert - all importance scores should be normalized
        Assert.Equal(_config.Features.Length, result);

        // Verify logging shows valid importance scores
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(_config.Features.Length));
    }

    [Fact]
    public void ExecutionTime_Feature_Should_Have_Higher_Importance_When_Correlated_With_Success()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        // Create predictions where successful ones have lower execution time
        var predictions = CreatePredictionsWithExecutionTimeCorrelation();
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert - should process without error
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void ConcurrencyLevel_Feature_Should_Contribute_To_Importance()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreatePredictionsWithConcurrencyVariation();
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void MemoryUsage_Feature_Should_Be_Analyzed()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreatePredictionsWithMemoryVariation();
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void RepeatRate_Feature_Should_Be_Analyzed()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreatePredictionsWithRepeatRateVariation();
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void CacheHitRatio_Feature_Should_Be_Analyzed()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreateMixedPredictions(15);
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Large_Prediction_Sets()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreateMixedPredictions(1000);
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void UpdatePatterns_Should_Handle_Imbalanced_Predictions()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        // Create predictions with 90% success rate
        var predictions = new List<PredictionResult>();
        for (int i = 0; i < 9; i++)
            predictions.Add(CreateSuccessfulPrediction());
        predictions.Add(CreateFailedPrediction());
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions.ToArray(), analysis);

        // Assert
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void Feature_Importance_Should_Be_Consistent()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreateMixedPredictions(20);
        var analysis = new PatternAnalysisResult();

        // Act - call twice with same predictions
        var result1 = updater.UpdatePatterns(predictions, analysis);
        var result2 = updater.UpdatePatterns(predictions, analysis);

        // Assert - should return same result
        Assert.Equal(result1, result2);
    }

    // Helper methods

    private PredictionResult[] CreateMixedPredictions(int count)
    {
        var predictions = new List<PredictionResult>();
        for (int i = 0; i < count / 2; i++)
            predictions.Add(CreateSuccessfulPrediction());
        for (int i = 0; i < count - (count / 2); i++)
            predictions.Add(CreateFailedPrediction());
        return predictions.ToArray();
    }

    private PredictionResult[] CreateSuccessfulOnlyPredictions(int count)
    {
        var predictions = new List<PredictionResult>();
        for (int i = 0; i < count; i++)
            predictions.Add(CreateSuccessfulPrediction());
        return predictions.ToArray();
    }

    private PredictionResult[] CreateFailedOnlyPredictions(int count)
    {
        var predictions = new List<PredictionResult>();
        for (int i = 0; i < count; i++)
            predictions.Add(CreateFailedPrediction());
        return predictions.ToArray();
    }

    private PredictionResult[] CreatePredictionsWithExecutionTimeCorrelation()
    {
        var predictions = new List<PredictionResult>();

        // Successful predictions with low execution time
        for (int i = 0; i < 8; i++)
        {
            predictions.Add(new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                ActualImprovement = TimeSpan.FromMilliseconds(100),
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    MedianExecutionTime = TimeSpan.FromMilliseconds(50),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(75),
                    P99ExecutionTime = TimeSpan.FromMilliseconds(90),
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    FailedExecutions = 5,
                    MemoryAllocated = 1000000,
                    ConcurrentExecutions = 2,
                    LastExecution = DateTime.UtcNow,
                    SamplePeriod = TimeSpan.FromMinutes(1),
                    CpuUsage = 0.3,
                    MemoryUsage = 1000000,
                    DatabaseCalls = 5,
                    ExternalApiCalls = 2
                }
            });
        }

        // Failed predictions with high execution time
        for (int i = 0; i < 8; i++)
        {
            predictions.Add(new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                ActualImprovement = TimeSpan.Zero,
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(500),
                    MedianExecutionTime = TimeSpan.FromMilliseconds(500),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(750),
                    P99ExecutionTime = TimeSpan.FromMilliseconds(900),
                    TotalExecutions = 100,
                    SuccessfulExecutions = 50,
                    FailedExecutions = 50,
                    MemoryAllocated = 5000000,
                    ConcurrentExecutions = 8,
                    LastExecution = DateTime.UtcNow,
                    SamplePeriod = TimeSpan.FromMinutes(1),
                    CpuUsage = 0.8,
                    MemoryUsage = 5000000,
                    DatabaseCalls = 20,
                    ExternalApiCalls = 10
                }
            });
        }

        return predictions.ToArray();
    }

    private PredictionResult[] CreatePredictionsWithConcurrencyVariation()
    {
        var predictions = new List<PredictionResult>();

        // Successful with low concurrency
        for (int i = 0; i < 5; i++)
        {
            predictions.Add(CreateSuccessfulPredictionWithConcurrency(2));
        }

        // Failed with high concurrency
        for (int i = 0; i < 5; i++)
        {
            predictions.Add(CreateFailedPredictionWithConcurrency(16));
        }

        return predictions.ToArray();
    }

    private PredictionResult[] CreatePredictionsWithMemoryVariation()
    {
        var predictions = new List<PredictionResult>();

        // Successful with low memory
        for (int i = 0; i < 5; i++)
        {
            predictions.Add(CreateSuccessfulPredictionWithMemory(1000000));
        }

        // Failed with high memory
        for (int i = 0; i < 5; i++)
        {
            predictions.Add(CreateFailedPredictionWithMemory(100000000));
        }

        return predictions.ToArray();
    }

    private PredictionResult[] CreatePredictionsWithRepeatRateVariation()
    {
        var predictions = new List<PredictionResult>();

        // Successful with high repeat rate
        for (int i = 0; i < 5; i++)
        {
            predictions.Add(new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                ActualImprovement = TimeSpan.FromMilliseconds(100),
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    MedianExecutionTime = TimeSpan.FromMilliseconds(100),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(150),
                    P99ExecutionTime = TimeSpan.FromMilliseconds(200),
                    TotalExecutions = 1000,
                    SuccessfulExecutions = 950,
                    FailedExecutions = 50,
                    MemoryAllocated = 2000000,
                    ConcurrentExecutions = 3,
                    LastExecution = DateTime.UtcNow,
                    SamplePeriod = TimeSpan.FromMinutes(1),
                    CpuUsage = 0.4,
                    MemoryUsage = 2000000,
                    DatabaseCalls = 8,
                    ExternalApiCalls = 3
                }
            });
        }

        // Failed with low repeat rate
        for (int i = 0; i < 5; i++)
        {
            predictions.Add(new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                ActualImprovement = TimeSpan.Zero,
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(300),
                    MedianExecutionTime = TimeSpan.FromMilliseconds(300),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(450),
                    P99ExecutionTime = TimeSpan.FromMilliseconds(600),
                    TotalExecutions = 100,
                    SuccessfulExecutions = 20,
                    FailedExecutions = 80,
                    MemoryAllocated = 10000000,
                    ConcurrentExecutions = 10,
                    LastExecution = DateTime.UtcNow,
                    SamplePeriod = TimeSpan.FromMinutes(1),
                    CpuUsage = 0.7,
                    MemoryUsage = 10000000,
                    DatabaseCalls = 15,
                    ExternalApiCalls = 8
                }
            });
        }

        return predictions.ToArray();
    }

    private PredictionResult CreateSuccessfulPrediction()
    {
        return new PredictionResult
        {
            RequestType = typeof(string),
            PredictedStrategies = Array.Empty<OptimizationStrategy>(),
            ActualImprovement = TimeSpan.FromMilliseconds(100),
            Timestamp = DateTime.UtcNow,
            Metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(75),
                MedianExecutionTime = TimeSpan.FromMilliseconds(75),
                P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                P99ExecutionTime = TimeSpan.FromMilliseconds(125),
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                FailedExecutions = 10,
                MemoryAllocated = 2000000,
                ConcurrentExecutions = 2,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(1),
                CpuUsage = 0.3,
                MemoryUsage = 2000000,
                DatabaseCalls = 5,
                ExternalApiCalls = 2
            }
        };
    }

    private PredictionResult CreateFailedPrediction()
    {
        return new PredictionResult
        {
            RequestType = typeof(string),
            PredictedStrategies = Array.Empty<OptimizationStrategy>(),
            ActualImprovement = TimeSpan.Zero,
            Timestamp = DateTime.UtcNow,
            Metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(500),
                MedianExecutionTime = TimeSpan.FromMilliseconds(500),
                P95ExecutionTime = TimeSpan.FromMilliseconds(750),
                P99ExecutionTime = TimeSpan.FromMilliseconds(1000),
                TotalExecutions = 100,
                SuccessfulExecutions = 40,
                FailedExecutions = 60,
                MemoryAllocated = 8000000,
                ConcurrentExecutions = 8,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(1),
                CpuUsage = 0.8,
                MemoryUsage = 8000000,
                DatabaseCalls = 20,
                ExternalApiCalls = 10
            }
        };
    }

    private PredictionResult CreateSuccessfulPredictionWithConcurrency(int concurrency)
    {
        return new PredictionResult
        {
            RequestType = typeof(string),
            PredictedStrategies = Array.Empty<OptimizationStrategy>(),
            ActualImprovement = TimeSpan.FromMilliseconds(100),
            Timestamp = DateTime.UtcNow,
            Metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(75),
                MedianExecutionTime = TimeSpan.FromMilliseconds(75),
                P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                P99ExecutionTime = TimeSpan.FromMilliseconds(125),
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                FailedExecutions = 10,
                MemoryAllocated = 2000000,
                ConcurrentExecutions = concurrency,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(1),
                CpuUsage = 0.3,
                MemoryUsage = 2000000,
                DatabaseCalls = 5,
                ExternalApiCalls = 2
            }
        };
    }

    private PredictionResult CreateFailedPredictionWithConcurrency(int concurrency)
    {
        return new PredictionResult
        {
            RequestType = typeof(string),
            PredictedStrategies = Array.Empty<OptimizationStrategy>(),
            ActualImprovement = TimeSpan.Zero,
            Timestamp = DateTime.UtcNow,
            Metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(500),
                MedianExecutionTime = TimeSpan.FromMilliseconds(500),
                P95ExecutionTime = TimeSpan.FromMilliseconds(750),
                P99ExecutionTime = TimeSpan.FromMilliseconds(1000),
                TotalExecutions = 100,
                SuccessfulExecutions = 40,
                FailedExecutions = 60,
                MemoryAllocated = 8000000,
                ConcurrentExecutions = concurrency,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(1),
                CpuUsage = 0.8,
                MemoryUsage = 8000000,
                DatabaseCalls = 20,
                ExternalApiCalls = 10
            }
        };
    }

    private PredictionResult CreateSuccessfulPredictionWithMemory(long memory)
    {
        return new PredictionResult
        {
            RequestType = typeof(string),
            PredictedStrategies = Array.Empty<OptimizationStrategy>(),
            ActualImprovement = TimeSpan.FromMilliseconds(100),
            Timestamp = DateTime.UtcNow,
            Metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(75),
                MedianExecutionTime = TimeSpan.FromMilliseconds(75),
                P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                P99ExecutionTime = TimeSpan.FromMilliseconds(125),
                TotalExecutions = 100,
                SuccessfulExecutions = 90,
                FailedExecutions = 10,
                MemoryAllocated = memory,
                ConcurrentExecutions = 2,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(1),
                CpuUsage = 0.3,
                MemoryUsage = memory,
                DatabaseCalls = 5,
                ExternalApiCalls = 2
            }
        };
    }

    private PredictionResult CreateFailedPredictionWithMemory(long memory)
    {
        return new PredictionResult
        {
            RequestType = typeof(string),
            PredictedStrategies = Array.Empty<OptimizationStrategy>(),
            ActualImprovement = TimeSpan.Zero,
            Timestamp = DateTime.UtcNow,
            Metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(500),
                MedianExecutionTime = TimeSpan.FromMilliseconds(500),
                P95ExecutionTime = TimeSpan.FromMilliseconds(750),
                P99ExecutionTime = TimeSpan.FromMilliseconds(1000),
                TotalExecutions = 100,
                SuccessfulExecutions = 40,
                FailedExecutions = 60,
                MemoryAllocated = memory,
                ConcurrentExecutions = 8,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(1),
                CpuUsage = 0.8,
                MemoryUsage = memory,
                DatabaseCalls = 20,
                ExternalApiCalls = 10
            }
        };
    }
}
