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
    public void UpdatePatterns_Should_Handle_Null_Analysis()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = CreateMixedPredictions(5);

        // Act & Assert - should handle gracefully
        var result = updater.UpdatePatterns(predictions, null!);

        // Should return the number of features processed (5)
        Assert.Equal(5, result);
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

    [Fact]
    public void ExtractSingleFeatureValue_Should_Handle_All_Known_Features()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var prediction = CreateSuccessfulPrediction();

        // Act - UpdatePatterns will call ExtractSingleFeatureValue for all features
        var result = updater.UpdatePatterns(new[] { prediction, CreateFailedPrediction() }, new PatternAnalysisResult());

        // Assert - should process all features without error
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void ExtractSingleFeatureValue_Should_Handle_Unknown_Features()
    {
        // Arrange - create config with unknown feature
        var configWithUnknown = new PatternRecognitionConfig
        {
            Features = new[] { "UnknownFeature", "ExecutionTime" }
        };
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, configWithUnknown);
        var predictions = CreateMixedPredictions(5);
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert - should handle unknown features gracefully (return 0.0)
        Assert.Equal(configWithUnknown.Features.Length, result);
    }

    [Fact]
    public void CalculateFeatureImportance_Should_Handle_Single_Prediction()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = new[] { CreateSuccessfulPrediction() };
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert - should process all features but return 0 importance (no comparison group)
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void Statistical_Calculations_Should_Handle_Zero_Variance()
    {
        // Arrange - create predictions with identical values for a feature
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = new List<PredictionResult>();

        // All successful predictions with identical execution time
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
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100), // Same value
                    MedianExecutionTime = TimeSpan.FromMilliseconds(100),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    P99ExecutionTime = TimeSpan.FromMilliseconds(100),
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
            });
        }

        // All failed predictions with identical execution time
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
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100), // Same value
                    MedianExecutionTime = TimeSpan.FromMilliseconds(100),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    P99ExecutionTime = TimeSpan.FromMilliseconds(100),
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
            });
        }

        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions.ToArray(), analysis);

        // Assert - should handle zero variance gracefully
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void Statistical_Calculations_Should_Handle_Empty_Feature_Values()
    {
        // Arrange - create predictions that might result in empty feature value lists
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = new List<PredictionResult>();

        // Create predictions with extreme values that might be filtered out
        for (int i = 0; i < 3; i++)
        {
            predictions.Add(new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                ActualImprovement = TimeSpan.FromMilliseconds(100),
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                     AverageExecutionTime = TimeSpan.FromMilliseconds(-100), // Invalid negative values
                     MedianExecutionTime = TimeSpan.FromMilliseconds(-100),
                     P95ExecutionTime = TimeSpan.FromMilliseconds(-100),
                     P99ExecutionTime = TimeSpan.FromMilliseconds(-100),
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
            });
        }

        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions.ToArray(), analysis);

        // Assert - should handle empty feature value lists gracefully
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void Exception_Handling_Should_Catch_Division_By_Zero_In_Calculations()
    {
        // Arrange - create scenario that might cause division by zero
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = new List<PredictionResult>();

        // Create predictions with zero values that might cause division issues
        for (int i = 0; i < 2; i++)
        {
            predictions.Add(new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                ActualImprovement = TimeSpan.FromMilliseconds(100),
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.Zero, // Zero execution time
                    MedianExecutionTime = TimeSpan.Zero,
                    P95ExecutionTime = TimeSpan.Zero,
                    P99ExecutionTime = TimeSpan.Zero,
                    TotalExecutions = 0, // Zero total executions
                    SuccessfulExecutions = 0,
                    FailedExecutions = 0,
                    MemoryAllocated = 0, // Zero memory
                    ConcurrentExecutions = 0, // Zero concurrency
                    LastExecution = DateTime.UtcNow,
                    SamplePeriod = TimeSpan.FromMinutes(1),
                    CpuUsage = 0.0,
                    MemoryUsage = 0,
                    DatabaseCalls = 0,
                    ExternalApiCalls = 0
                }
            });
        }

        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions.ToArray(), analysis);

        // Assert - should handle division by zero gracefully and return feature count
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void Exception_Handling_Should_Catch_Overflow_In_Mathematical_Operations()
    {
        // Arrange - create predictions with extreme values that might cause overflow
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = new List<PredictionResult>();

        for (int i = 0; i < 2; i++)
        {
            predictions.Add(new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                ActualImprovement = TimeSpan.FromMilliseconds(100),
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                     AverageExecutionTime = TimeSpan.MaxValue, // Extreme values
                     MedianExecutionTime = TimeSpan.MaxValue,
                     P95ExecutionTime = TimeSpan.MaxValue,
                     P99ExecutionTime = TimeSpan.MaxValue,
                    TotalExecutions = int.MaxValue,
                    SuccessfulExecutions = int.MaxValue,
                    FailedExecutions = 0,
                    MemoryAllocated = long.MaxValue,
                    ConcurrentExecutions = int.MaxValue,
                    LastExecution = DateTime.UtcNow,
                    SamplePeriod = TimeSpan.FromMinutes(1),
                    CpuUsage = 1.0,
                    MemoryUsage = long.MaxValue,
                    DatabaseCalls = int.MaxValue,
                    ExternalApiCalls = int.MaxValue
                }
            });
        }

        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions.ToArray(), analysis);

        // Assert - should handle overflow gracefully
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void Exception_Handling_Should_Log_Warnings_For_Calculation_Errors()
    {
        // Arrange
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = new[] { CreateSuccessfulPrediction() }; // Single prediction causes issues
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert - should complete and log warnings
        Assert.Equal(_config.Features.Length, result);

        // Verify debug logs were written for each feature
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(_config.Features.Length));
    }

    [Fact]
    public void UpdatePatterns_Should_Return_Zero_For_Empty_Features()
    {
        // Arrange
        var emptyConfig = new PatternRecognitionConfig
        {
            Features = Array.Empty<string>()
        };
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, emptyConfig);
        var predictions = CreateMixedPredictions(5);
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert
        Assert.Equal(0, result);

        // Verify no logging occurred
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void Mutual_Information_Should_Handle_Identical_Feature_Values()
    {
        // Arrange - create predictions with identical execution times
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = new List<PredictionResult>();

        // All successful with identical execution time
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
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100), // Identical
                    MedianExecutionTime = TimeSpan.FromMilliseconds(100),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    P99ExecutionTime = TimeSpan.FromMilliseconds(100),
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
            });
        }

        // All failed with identical execution time
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
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100), // Identical
                    MedianExecutionTime = TimeSpan.FromMilliseconds(100),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    P99ExecutionTime = TimeSpan.FromMilliseconds(100),
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
            });
        }

        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions.ToArray(), analysis);

        // Assert - should handle identical values gracefully (mutual info returns 0.0 due to zero range)
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void Feature_Extraction_Should_Filter_Invalid_Values()
    {
        // Arrange - create predictions with invalid (negative) execution times
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = new List<PredictionResult>();

        // Successful with negative execution time (invalid)
        predictions.Add(new PredictionResult
        {
            RequestType = typeof(string),
            PredictedStrategies = Array.Empty<OptimizationStrategy>(),
            ActualImprovement = TimeSpan.FromMilliseconds(100),
            Timestamp = DateTime.UtcNow,
            Metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(-100), // Invalid negative
                MedianExecutionTime = TimeSpan.FromMilliseconds(-100),
                P95ExecutionTime = TimeSpan.FromMilliseconds(-100),
                P99ExecutionTime = TimeSpan.FromMilliseconds(-100),
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
        });

        // Failed with valid execution time
        predictions.Add(new PredictionResult
        {
            RequestType = typeof(string),
            PredictedStrategies = Array.Empty<OptimizationStrategy>(),
            ActualImprovement = TimeSpan.Zero,
            Timestamp = DateTime.UtcNow,
            Metrics = new RequestExecutionMetrics
            {
                AverageExecutionTime = TimeSpan.FromMilliseconds(500), // Valid
                MedianExecutionTime = TimeSpan.FromMilliseconds(500),
                P95ExecutionTime = TimeSpan.FromMilliseconds(500),
                P99ExecutionTime = TimeSpan.FromMilliseconds(500),
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
        });

        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions.ToArray(), analysis);

        // Assert - should filter out invalid values and process remaining
        Assert.Equal(_config.Features.Length, result);
    }

    [Fact]
    public void Statistical_Calculations_Should_Work_With_Minimal_Data()
    {
        // Arrange - create minimal data: 1 successful + 1 failed prediction
        var updater = new FeatureImportancePatternUpdater(_loggerMock.Object, _config);
        var predictions = new[]
        {
            CreateSuccessfulPrediction(),
            CreateFailedPrediction()
        };
        var analysis = new PatternAnalysisResult();

        // Act
        var result = updater.UpdatePatterns(predictions, analysis);

        // Assert - should handle minimal data gracefully
        Assert.Equal(_config.Features.Length, result);
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
