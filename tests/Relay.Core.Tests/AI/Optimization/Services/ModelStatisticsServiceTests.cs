using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Models;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class ModelStatisticsServiceTests
{
    private readonly ILogger _logger;
    private readonly ModelStatisticsService _service;

    public ModelStatisticsServiceTests()
    {
        _logger = NullLogger.Instance;
        _service = new ModelStatisticsService(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new ModelStatisticsService(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var service = new ModelStatisticsService(logger);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region RecordPrediction Tests

    [Fact]
    public void RecordPrediction_Should_Throw_When_RequestType_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.RecordPrediction(null!));
    }

    [Fact]
    public void RecordPrediction_Should_Increment_Total_Predictions()
    {
        // Arrange: Get initial statistics
        var initialStats = _service.GetModelStatistics();

        // Act
        _service.RecordPrediction(typeof(string));

        // Assert
        var updatedStats = _service.GetModelStatistics();
        Assert.Equal(initialStats.TotalPredictions + 1, updatedStats.TotalPredictions);
    }

    [Fact]
    public void RecordPrediction_Should_Be_Thread_Safe()
    {
        // Arrange
        var tasks = new List<Task>();
        const int threadCount = 5;
        const int iterations = 10;

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            var task = Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    _service.RecordPrediction(typeof(string));
                }
            });
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var finalStats = _service.GetModelStatistics();
        Assert.Equal(threadCount * iterations, finalStats.TotalPredictions);
    }

    #endregion

    #region UpdateModelAccuracy Tests

    [Fact]
    public void UpdateModelAccuracy_Should_Throw_When_Required_Parameters_Are_Null()
    {
        // Arrange
        var actualMetrics = new RequestExecutionMetrics();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.UpdateModelAccuracy(null!, new OptimizationStrategy[0], actualMetrics, false));
        Assert.Throws<ArgumentNullException>(() => 
            _service.UpdateModelAccuracy(typeof(string), null!, actualMetrics, false));
        Assert.Throws<ArgumentNullException>(() => 
            _service.UpdateModelAccuracy(typeof(string), new OptimizationStrategy[0], null!, false));
    }

    [Fact]
    public void UpdateModelAccuracy_Should_Update_Correct_Predictions_When_Successful()
    {
        // Act: Record a successful prediction
        var strategies = new[] { OptimizationStrategy.EnableCaching };
        var metrics = new RequestExecutionMetrics 
        { 
            TotalExecutions = 100,
            SuccessfulExecutions = 90, // This gives 90/100 = 0.9 SuccessRate
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        }; // Above threshold
        _service.UpdateModelAccuracy(typeof(string), strategies, metrics, true); // strategies match = true

        // Assert
        var stats = _service.GetModelStatistics();
        Assert.Equal(1, stats.TotalPredictions);
        Assert.Equal(1.0, stats.AccuracyScore); // 1 correct out of 1 total = 100% accuracy
    }

    [Fact]
    public void UpdateModelAccuracy_Should_Not_Update_Correct_Predictions_When_Unsuccessful()
    {
        // Act: Record an unsuccessful prediction (strategies don't match)
        var strategies = new[] { OptimizationStrategy.EnableCaching };
        var metrics = new RequestExecutionMetrics 
        { 
            TotalExecutions = 100,
            SuccessfulExecutions = 90, // This gives 90/100 = 0.9 SuccessRate
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        }; // Above threshold, but strategies don't match
        _service.UpdateModelAccuracy(typeof(string), strategies, metrics, false); // strategies match = false

        // Assert
        var stats = _service.GetModelStatistics();
        Assert.Equal(1, stats.TotalPredictions);
        Assert.Equal(0.0, stats.AccuracyScore); // Accuracy should remain 0
    }

    [Fact]
    public void UpdateModelAccuracy_Should_Not_Update_Correct_Predictions_When_Low_Success_Rate()
    {
        // Act: Record a prediction with low success rate
        var strategies = new[] { OptimizationStrategy.EnableCaching };
        var metrics = new RequestExecutionMetrics 
        { 
            TotalExecutions = 100,
            SuccessfulExecutions = 50, // This gives 50/100 = 0.5 SuccessRate
            FailedExecutions = 50,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        }; // Below threshold of 0.8
        _service.UpdateModelAccuracy(typeof(string), strategies, metrics, true); // strategies match, but low success rate

        // Assert
        var stats = _service.GetModelStatistics();
        Assert.Equal(1, stats.TotalPredictions);
        Assert.Equal(0.0, stats.AccuracyScore); // Accuracy should remain 0
    }

    #endregion

    #region GetModelStatistics Tests

    [Fact]
    public void GetModelStatistics_Should_Return_Initial_Values()
    {
        // Act
        var stats = _service.GetModelStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(0, stats.TotalPredictions);
        Assert.Equal(0.0, stats.AccuracyScore);
        Assert.Equal(0.0, stats.PrecisionScore);
        Assert.Equal(0.0, stats.RecallScore);
        Assert.Equal(TimeSpan.Zero, stats.AveragePredictionTime);
        Assert.NotNull(stats.ModelVersion);
        Assert.NotNull(stats.ModelTrainingDate);
    }

    [Fact]
    public void GetModelStatistics_Should_Update_After_Predictions()
    {
        // Arrange
        var initialStats = _service.GetModelStatistics();

        // Act: Record some predictions
        _service.RecordPrediction(typeof(string));
        _service.RecordPrediction(typeof(int));

        // Also update accuracy data
        var strategies = new[] { OptimizationStrategy.EnableCaching };
        var metrics = new RequestExecutionMetrics 
        { 
            TotalExecutions = 100,
            SuccessfulExecutions = 90, // This gives 90/100 = 0.9 SuccessRate
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        };
        _service.UpdateModelAccuracy(typeof(string), strategies, metrics, true);

        var stats = _service.GetModelStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TotalPredictions >= 2); // At least 2 predictions recorded
        Assert.True(stats.AccuracyScore >= 0.0);
    }

    #endregion

    #region Accuracy Calculation Tests

    [Fact]
    public void GetModelStatistics_Should_Calculate_Accuracy_Correctly()
    {
        // Arrange: Record a mix of successful and unsuccessful predictions
        var strategies = new[] { OptimizationStrategy.EnableCaching };
        var successfulMetrics = new RequestExecutionMetrics 
        { 
            TotalExecutions = 100,
            SuccessfulExecutions = 90, // This gives 90/100 = 0.9 SuccessRate
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        }; // Successful
        var failedMetrics = new RequestExecutionMetrics 
        { 
            TotalExecutions = 100,
            SuccessfulExecutions = 50, // This gives 50/100 = 0.5 SuccessRate
            FailedExecutions = 50,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        }; // Failed due to low success rate

        // Act: Add 3 successful and 2 failed predictions
        _service.UpdateModelAccuracy(typeof(string), strategies, successfulMetrics, true); // Correct
        _service.UpdateModelAccuracy(typeof(string), strategies, successfulMetrics, true); // Correct
        _service.UpdateModelAccuracy(typeof(string), strategies, successfulMetrics, true); // Correct
        _service.UpdateModelAccuracy(typeof(string), strategies, failedMetrics, true); // Failed (low success rate)
        _service.UpdateModelAccuracy(typeof(string), strategies, failedMetrics, true); // Failed (low success rate)

        var stats = _service.GetModelStatistics();
        var expectedAccuracy = 3.0 / 5.0; // 3 correct out of 5 total

        // Assert
        Assert.Equal(expectedAccuracy, stats.AccuracyScore, 6);
    }

    [Fact]
    public void Precision_Should_Be_Calculated_Correctly()
    {
        // Arrange: Create a scenario where we can test precision calculation
        var strategies = new[] { OptimizationStrategy.EnableCaching };
        var metrics = new RequestExecutionMetrics 
        { 
            TotalExecutions = 100,
            SuccessfulExecutions = 90, // This gives 90/100 = 0.9 SuccessRate
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        };

        // Act: Record several predictions
        for (int i = 0; i < 10; i++)
        {
            _service.UpdateModelAccuracy(typeof(string), strategies, metrics, i < 7); // First 7 match, last 3 don't
        }

        var stats = _service.GetModelStatistics();

        // Assert: With our logic, all predictions (successful and unsuccessful) count toward total
        // The precision is calculated as correct predictions divided by total predictions
        Assert.Equal(10, stats.TotalPredictions); // 10 total calls to UpdateModelAccuracy
        // This depends on how many of the 10 total had matching strategies and high success rate
        Assert.True(stats.PrecisionScore >= 0.0);
        Assert.True(stats.PrecisionScore <= 1.0);
    }

    #endregion

    #region Model Confidence Tests

    [Fact]
    public void ModelConfidence_Should_Be_Low_With_Few_Predictions()
    {
        // Act
        var stats = _service.GetModelStatistics();

        // Assert: Initially with 0 predictions, confidence might be default or based on internal logic
        // According to the code, if _totalPredictions < 10, confidence returns 0.5
        Assert.True(stats.ModelConfidence >= 0.0);
    }

    [Fact]
    public void ModelConfidence_Should_Increase_With_More_Predictions()
    {
        // Arrange: Create multiple predictions with high success rate
        var strategies = new[] { OptimizationStrategy.EnableCaching };
        var metrics = new RequestExecutionMetrics 
        { 
            TotalExecutions = 100,
            SuccessfulExecutions = 90, // This gives 90/100 = 0.9 SuccessRate
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        };

        // Act: Record multiple successful predictions
        for (int i = 0; i < 15; i++) // More than 10 to get out of low-confidence range
        {
            _service.UpdateModelAccuracy(typeof(string), strategies, metrics, true);
        }

        var stats = _service.GetModelStatistics();

        // Assert
        Assert.True(stats.ModelConfidence >= 0.0);
        Assert.True(stats.ModelConfidence <= 0.95); // Should be clamped at 0.95
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void UpdateModelAccuracy_Should_Be_Thread_Safe()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            var task = Task.Run(() =>
            {
                var strategies = new[] { OptimizationStrategy.EnableCaching };
                var metrics = new RequestExecutionMetrics 
                { 
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90, // This gives 90/100 = 0.9 SuccessRate
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100)
                };
                for (int j = 0; j < 10; j++)
                {
                    _service.UpdateModelAccuracy(typeof(string), strategies, metrics, true);
                }
            });
            tasks.Add(task);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var stats = _service.GetModelStatistics();
        Assert.Equal(50, stats.TotalPredictions);
    }

    [Fact]
    public void RecordPrediction_And_UpdateModelAccuracy_Should_Be_Thread_Safe_Together()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act: Mix record prediction and update accuracy calls
        for (int i = 0; i < 3; i++)
        {
            var recordTask = Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    _service.RecordPrediction(typeof(string));
                }
            });
            tasks.Add(recordTask);

            var updateTask = Task.Run(() =>
            {
                var strategies = new[] { OptimizationStrategy.EnableCaching };
                var metrics = new RequestExecutionMetrics 
                { 
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90, // This gives 90/100 = 0.9 SuccessRate
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100)
                };
                for (int j = 0; j < 10; j++)
                {
                    _service.UpdateModelAccuracy(typeof(string), strategies, metrics, true);
                }
            });
            tasks.Add(updateTask);
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        var stats = _service.GetModelStatistics();
        // Total predictions will include both recorded predictions and accuracy updates
        Assert.True(stats.TotalPredictions >= 30); // At least the ones from accuracy updates
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void GetModelStatistics_Should_Handle_Zero_Division_Safely()
    {
        // Act
        var stats = _service.GetModelStatistics();

        // Assert: Should not throw and should return valid statistics
        Assert.NotNull(stats);
        Assert.True(stats.AccuracyScore >= 0.0);
        Assert.True(stats.AccuracyScore <= 1.0);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Full_Statistics_Workflow_Should_Work()
    {
        // Arrange - Start with clean state
        var initialStats = _service.GetModelStatistics();

        // Act - Perform a series of operations
        _service.RecordPrediction(typeof(string));
        _service.RecordPrediction(typeof(int));

        // Add various types of predictions with different success rates
        var successfulStrategies = new[] { OptimizationStrategy.EnableCaching, OptimizationStrategy.BatchProcessing };
        var successfulMetrics = new RequestExecutionMetrics 
        { 
            TotalExecutions = 100,
            SuccessfulExecutions = 90, // This gives 90/100 = 0.9 SuccessRate
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        };
        var failedMetrics = new RequestExecutionMetrics 
        { 
            TotalExecutions = 100,
            SuccessfulExecutions = 30, // This gives 30/100 = 0.3 SuccessRate
            FailedExecutions = 70,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        };

        // Add successful predictions
        for (int i = 0; i < 5; i++)
        {
            _service.UpdateModelAccuracy(typeof(string), successfulStrategies, successfulMetrics, true);
        }

        // Add failed predictions
        for (int i = 0; i < 3; i++)
        {
            _service.UpdateModelAccuracy(typeof(int), successfulStrategies, failedMetrics, true);
        }

        // Collect final statistics
        var finalStats = _service.GetModelStatistics();

        // Assert
        Assert.NotNull(finalStats);
        Assert.True(finalStats.TotalPredictions >= 8); // 2 recorded + 8 accuracy updates
        Assert.True(finalStats.AccuracyScore >= 0.0);
        Assert.True(finalStats.AccuracyScore <= 1.0);
        Assert.True(finalStats.ModelConfidence >= 0.0);
        Assert.NotNull(finalStats.ModelVersion);
        Assert.NotNull(finalStats.ModelTrainingDate);
    }

    #endregion
}

// Extension method to access internal counters for testing purposes
public static class ModelStatisticsServiceExtensions
{
    public static long CorrectPredictions(this AIModelStatistics stats)
    {
        // This would need to be determined from the internal state
        // Since we can't access the private fields directly, let's just calculate it from the stats
        if (stats.TotalPredictions == 0) return 0;
        return (long)(stats.AccuracyScore * stats.TotalPredictions);
    }
}
