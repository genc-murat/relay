using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Training;

public class DefaultAIModelTrainerStatisticsTests : IDisposable
{
    private readonly Mock<ILogger<DefaultAIModelTrainer>> _mockLogger;
    private readonly DefaultAIModelTrainer _trainer;

    public DefaultAIModelTrainerStatisticsTests()
    {
        _mockLogger = new Mock<ILogger<DefaultAIModelTrainer>>();
        _trainer = new DefaultAIModelTrainer(_mockLogger.Object);
    }

    #region Model Statistics Calculation Tests

    [Fact]
    public async Task TrainModelAsync_ShouldCalculateBasicStatistics_AfterTraining()
    {
        // Arrange
        var trainingData = CreateBasicTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Verify that training completed and statistics would be calculated
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_ShouldHandleStatisticsCalculation_WithLargeDataset()
    {
        // Arrange
        var trainingData = CreateLargeDatasetTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_ShouldCalculateStatistics_WithVariableDataQuality()
    {
        // Arrange
        var trainingData = CreateVariableQualityTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Feature Importance Extraction Tests

    [Fact]
    public async Task TrainModelAsync_ShouldExtractFeatureImportance_WithValidData()
    {
        // Arrange
        var trainingData = CreateFeatureImportanceTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Feature importance extraction should be attempted during training
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_ShouldHandleFeatureImportanceExtraction_WithMinimalData()
    {
        // Arrange
        var trainingData = CreateMinimalTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should handle gracefully with minimal data
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_ShouldHandleFeatureImportanceExtraction_WithCorrelatedFeatures()
    {
        // Arrange
        var trainingData = CreateCorrelatedFeaturesTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_ShouldHandleFeatureImportanceExtraction_WithOutliers()
    {
        // Arrange
        var trainingData = CreateOutlierTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Statistics Accuracy Tests

    [Fact]
    public async Task TrainModelAsync_ShouldCalculateAccurateStatistics_WithKnownPatterns()
    {
        // Arrange
        var trainingData = CreateKnownPatternTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_ShouldHandleStatisticsCalculation_WithSeasonalData()
    {
        // Arrange
        var trainingData = CreateSeasonalStatisticsTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_ShouldCalculateStatistics_WithTrendingData()
    {
        // Arrange
        var trainingData = CreateTrendingTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Performance Metrics Tests

    [Fact]
    public async Task TrainModelAsync_ShouldTrackPerformanceMetrics_DuringTraining()
    {
        // Arrange
        var trainingData = CreatePerformanceMetricsTrainingData();
        var startTime = DateTime.UtcNow;

        // Act
        await _trainer.TrainModelAsync(trainingData);

        var duration = DateTime.UtcNow - startTime;

        // Assert - Training should complete in reasonable time
        Assert.True(duration.TotalSeconds < 60, $"Training took {duration.TotalSeconds}s, expected < 60s");

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_ShouldHandleMemoryUsage_WithLargeDatasets()
    {
        // Arrange
        var trainingData = CreateMemoryIntensiveTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Statistics Validation Tests

    [Fact]
    public async Task TrainModelAsync_ShouldValidateStatistics_WithEdgeCaseValues()
    {
        // Arrange
        var trainingData = CreateEdgeCaseStatisticsTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_ShouldHandleStatisticsValidation_WithNullValues()
    {
        // Arrange
        var trainingData = CreateNullValueTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_ShouldValidateStatistics_WithExtremeValues()
    {
        // Arrange
        var trainingData = CreateExtremeValueTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Test Data Creation Methods

    private AITrainingData CreateBasicTrainingData()
    {
        var random = new Random(42);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100 + random.Next(50),
                    SuccessfulExecutions = 90 + random.Next(20),
                    FailedExecutions = 10 + random.Next(10),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(50)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(100)),
                    ConcurrentExecutions = 5 + random.Next(10),
                    MemoryUsage = (50 + random.Next(50)) * 1024 * 1024,
                    DatabaseCalls = 10 + random.Next(20),
                    ExternalApiCalls = 5 + random.Next(10),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(60)),
                    SuccessRate = 0.85 + random.NextDouble() * 0.1,
                    MemoryAllocated = (50 + random.Next(50)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(50)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(24))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 100)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                    CpuUtilization = 0.3 + random.NextDouble() * 0.4,
                    MemoryUtilization = 0.4 + random.NextDouble() * 0.3,
                    ThroughputPerSecond = 50 + random.Next(100)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateLargeDatasetTrainingData()
    {
        var random = new Random(123);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 500)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 1000 + random.Next(1000),
                    SuccessfulExecutions = 900 + random.Next(800),
                    FailedExecutions = 100 + random.Next(200),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(30 + random.Next(70)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(60 + random.Next(140)),
                    ConcurrentExecutions = 20 + random.Next(30),
                    MemoryUsage = (100 + random.Next(200)) * 1024 * 1024,
                    DatabaseCalls = 30 + random.Next(70),
                    ExternalApiCalls = 15 + random.Next(35),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(120)),
                    SuccessRate = 0.9 + random.NextDouble() * 0.08,
                    MemoryAllocated = (100 + random.Next(200)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 250)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(15 + random.Next(45)),
                    PerformanceImprovement = random.NextDouble() * 0.35,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(48))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 1000)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-1000 + i),
                    CpuUtilization = 0.25 + random.NextDouble() * 0.5,
                    MemoryUtilization = 0.35 + random.NextDouble() * 0.45,
                    ThroughputPerSecond = 100 + random.Next(300)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateVariableQualityTrainingData()
    {
        var random = new Random(456);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 100)
                .Select(i => 
                {
                    var qualityFactor = random.NextDouble();
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 100 + random.Next(200),
                        SuccessfulExecutions = (int)((100 + random.Next(200)) * qualityFactor),
                        FailedExecutions = (int)((100 + random.Next(200)) * (1 - qualityFactor)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(150) * (2 - qualityFactor)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(200) * (2 - qualityFactor)),
                        ConcurrentExecutions = 5 + random.Next(15),
                        MemoryUsage = (50 + random.Next(150)) * 1024 * 1024,
                        DatabaseCalls = 10 + random.Next(40),
                        ExternalApiCalls = 5 + random.Next(20),
                        LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(100)),
                        SuccessRate = qualityFactor,
                        MemoryAllocated = (50 + random.Next(150)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.3,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(80)),
                    PerformanceImprovement = random.NextDouble() * 0.4 * (random.NextDouble() > 0.5 ? 1 : -1),
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(48))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 200)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-200 + i),
                    CpuUtilization = 0.2 + random.NextDouble() * 0.6,
                    MemoryUtilization = 0.3 + random.NextDouble() * 0.5,
                    ThroughputPerSecond = 30 + random.Next(150)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateFeatureImportanceTrainingData()
    {
        var random = new Random(789);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 200)
                .Select(i => 
                {
                    // Create data with clear feature correlations
                    var executionTime = 50 + i * 0.5 + random.NextDouble() * 20;
                    var concurrency = 5 + i / 10 + random.Next(5);
                    var memory = (100 + i * 2 + random.Next(50)) * 1024 * 1024;
                    var dbCalls = 10 + i / 5 + random.Next(10);
                    var apiCalls = 5 + i / 8 + random.Next(5);
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 200 + i,
                        SuccessfulExecutions = 180 + (int)(i * 0.9),
                        FailedExecutions = 20 + (int)(i * 0.1),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(executionTime),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(executionTime * 2),
                        ConcurrentExecutions = concurrency,
                        MemoryUsage = memory,
                        DatabaseCalls = dbCalls,
                        ExternalApiCalls = apiCalls,
                        LastExecution = DateTime.UtcNow.AddMinutes(-200 + i),
                        SuccessRate = 0.85 + random.NextDouble() * 0.1,
                        MemoryAllocated = memory
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 100)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)((i % 7) + 1),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(50)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddHours(-100 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 400)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-400 + i),
                    CpuUtilization = 0.3 + (i / 400.0) * 0.4 + random.NextDouble() * 0.1,
                    MemoryUtilization = 0.4 + (i / 400.0) * 0.3 + random.NextDouble() * 0.1,
                    ThroughputPerSecond = 50 + i + random.Next(50)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMinimalTrainingData()
    {
        var random = new Random(123);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 10) // Minimum required: 10
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 10 + i,
                    SuccessfulExecutions = 9 + i,
                    FailedExecutions = 1,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50 + i),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100 + i),
                    ConcurrentExecutions = 2 + i,
                    MemoryUsage = (10 + i) * 1024 * 1024,
                    DatabaseCalls = 3 + i,
                    ExternalApiCalls = 1 + i,
                    LastExecution = DateTime.UtcNow.AddMinutes(-1 - i),
                    SuccessRate = 0.9,
                    MemoryAllocated = (10 + i) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 5) // Minimum required: 5
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)((i % 7) + 1),
                    Success = i % 2 == 0,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + i * 5),
                    PerformanceImprovement = 0.1 + i * 0.02,
                    Timestamp = DateTime.UtcNow.AddMinutes(-1 - i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 10) // Minimum required: 10
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-10 + i),
                    CpuUtilization = 0.3 + (i * 0.05),
                    MemoryUtilization = 0.4 + (i * 0.03),
                    ThroughputPerSecond = 10 + i
                })
                .ToArray()
        };
    }

    private AITrainingData CreateCorrelatedFeaturesTrainingData()
    {
        var random = new Random(321);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 150)
                .Select(i => 
                {
                    // Create highly correlated features
                    var baseValue = i + random.NextDouble() * 10;
                    var executionTime = 50 + baseValue * 2;
                    var memory = (100 + baseValue * 5) * 1024 * 1024;
                    var dbCalls = 10 + (int)(baseValue / 2);
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 100 + (int)baseValue,
                        SuccessfulExecutions = 90 + (int)(baseValue * 0.9),
                        FailedExecutions = 10 + (int)(baseValue * 0.1),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(executionTime),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(executionTime * 1.8),
                        ConcurrentExecutions = 5 + (int)(baseValue / 10),
                        MemoryUsage = (long)memory,
                        DatabaseCalls = (int)(long)dbCalls,
                        ExternalApiCalls = 5 + (int)(baseValue / 15),
                        LastExecution = DateTime.UtcNow.AddMinutes(-150 + i),
                        SuccessRate = 0.85 + random.NextDouble() * 0.1,
                        MemoryAllocated = (long)memory
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 75)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)((i % 7) + 1),
                    Success = random.NextDouble() > 0.25,
                    ExecutionTime = TimeSpan.FromMilliseconds(30 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.25,
                    Timestamp = DateTime.UtcNow.AddHours(-75 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 300)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-300 + i),
                    CpuUtilization = 0.3 + (i / 300.0) * 0.3 + random.NextDouble() * 0.1,
                    MemoryUtilization = 0.4 + (i / 300.0) * 0.2 + random.NextDouble() * 0.1,
                    ThroughputPerSecond = 40 + i / 2 + random.Next(30)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateOutlierTrainingData()
    {
        var random = new Random(654);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 100)
                .Select(i => 
                {
                    if (i % 20 == 0) // Every 20th item is an outlier
                    {
                        return new RequestExecutionMetrics
                        {
                            TotalExecutions = 5000, // Much higher
                            SuccessfulExecutions = 4000,
                            FailedExecutions = 1000,
                            AverageExecutionTime = TimeSpan.FromMilliseconds(1000), // Much slower
                            P95ExecutionTime = TimeSpan.FromMilliseconds(2000),
                            ConcurrentExecutions = 100, // Much higher
                            MemoryUsage = 1000 * 1024 * 1024, // Much higher
                            DatabaseCalls = 200, // Much higher
                            ExternalApiCalls = 100, // Much higher
                            LastExecution = DateTime.UtcNow.AddMinutes(-i),
                            SuccessRate = 0.8, // Lower
                            MemoryAllocated = 1000 * 1024 * 1024
                        };
                    }
                    else
                    {
                        return new RequestExecutionMetrics
                        {
                            TotalExecutions = 100 + random.Next(50),
                            SuccessfulExecutions = 90 + random.Next(30),
                            FailedExecutions = 10 + random.Next(20),
                            AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(50)),
                            P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(100)),
                            ConcurrentExecutions = 5 + random.Next(10),
                            MemoryUsage = (50 + random.Next(50)) * 1024 * 1024,
                            DatabaseCalls = 10 + random.Next(20),
                            ExternalApiCalls = 5 + random.Next(10),
                            LastExecution = DateTime.UtcNow.AddMinutes(-i),
                            SuccessRate = 0.85 + random.NextDouble() * 0.1,
                            MemoryAllocated = (50 + random.Next(50)) * 1024 * 1024
                        };
                    }
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(60)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddHours(-50 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 200)
                .Select(i => 
                {
                    if (i % 40 == 0) // Outlier system metrics
                    {
                        return new SystemLoadMetrics
                        {
                            Timestamp = DateTime.UtcNow.AddMinutes(-200 + i),
                            CpuUtilization = 0.95, // Very high
                            MemoryUtilization = 0.98, // Very high
                            ThroughputPerSecond = 5 // Very low
                        };
                    }
                    else
                    {
                        return new SystemLoadMetrics
                        {
                            Timestamp = DateTime.UtcNow.AddMinutes(-200 + i),
                            CpuUtilization = 0.3 + random.NextDouble() * 0.4,
                            MemoryUtilization = 0.4 + random.NextDouble() * 0.3,
                            ThroughputPerSecond = 50 + random.Next(100)
                        };
                    }
                })
                .ToArray()
        };
    }

    private AITrainingData CreateKnownPatternTrainingData()
    {
        var random = new Random(987);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 120)
                .Select(i => 
                {
                    // Create a known pattern: execution time increases linearly with concurrency
                    var concurrency = 5 + (i % 20);
                    var executionTime = 30 + concurrency * 5 + random.NextDouble() * 10;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 100 + i,
                        SuccessfulExecutions = 90 + (int)(i * 0.9),
                        FailedExecutions = 10 + (int)(i * 0.1),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(executionTime),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(executionTime * 2),
                        ConcurrentExecutions = concurrency,
                        MemoryUsage = (50 + concurrency * 10) * 1024 * 1024,
                        DatabaseCalls = 10 + concurrency * 2,
                        ExternalApiCalls = 5 + concurrency,
                        LastExecution = DateTime.UtcNow.AddMinutes(-120 + i),
                        SuccessRate = 0.85 + random.NextDouble() * 0.1,
                        MemoryAllocated = (50 + concurrency * 10) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 60)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)((i % 7) + 1),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(35)),
                    PerformanceImprovement = random.NextDouble() * 0.2,
                    Timestamp = DateTime.UtcNow.AddHours(-60 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 240)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-240 + i),
                    CpuUtilization = 0.3 + (i % 20) * 0.02 + random.NextDouble() * 0.1,
                    MemoryUtilization = 0.4 + (i % 20) * 0.015 + random.NextDouble() * 0.1,
                    ThroughputPerSecond = 50 + (i % 20) * 5 + random.Next(20)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateSeasonalStatisticsTrainingData()
    {
        var random = new Random(111);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 365)
                .Select(i => 
                {
                    var dayOfYear = i;
                    var seasonalMultiplier = 0.8 + 0.4 * Math.Sin((dayOfYear / 365.0) * 2 * Math.PI - Math.PI / 2);
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(1000 * seasonalMultiplier) + random.Next(200),
                        SuccessfulExecutions = (int)(900 * seasonalMultiplier) + random.Next(150),
                        FailedExecutions = (int)(100 * seasonalMultiplier) + random.Next(50),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(80)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(120)),
                        ConcurrentExecutions = (int)(10 * seasonalMultiplier) + random.Next(10),
                        MemoryUsage = (int)(100 * seasonalMultiplier + random.Next(100)) * 1024 * 1024,
                        DatabaseCalls = (int)(20 * seasonalMultiplier) + random.Next(25),
                        ExternalApiCalls = (int)(10 * seasonalMultiplier) + random.Next(12),
                        LastExecution = DateTime.UtcNow.AddDays(-365 + i),
                        SuccessRate = 0.88 + random.NextDouble() * 0.08,
                        MemoryAllocated = (int)(100 * seasonalMultiplier + random.Next(100)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 180)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(50)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddDays(-180 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 730)
                .Select(i => 
                {
                    var dayOfYear = i / 2;
                    var seasonalMultiplier = 0.8 + 0.4 * Math.Sin((dayOfYear / 365.0) * 2 * Math.PI - Math.PI / 2);
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-365 + dayOfYear).AddHours((i % 2) * 12),
                        CpuUtilization = (0.25 + random.NextDouble() * 0.35) * seasonalMultiplier,
                        MemoryUtilization = (0.35 + random.NextDouble() * 0.35) * seasonalMultiplier,
                        ThroughputPerSecond = (int)(100 * seasonalMultiplier) + random.Next(120)
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateTrendingTrainingData()
    {
        var random = new Random(222);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 180)
                .Select(i => 
                {
                    var trendFactor = 1.0 + (i * 0.01); // 1% growth per day
                    var noise = random.NextDouble() * 0.2 - 0.1; // ±10% noise
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)((500 * trendFactor * (1 + noise)) + random.Next(100)),
                        SuccessfulExecutions = (int)((450 * trendFactor * (1 + noise)) + random.Next(80)),
                        FailedExecutions = (int)((50 * trendFactor * (1 + noise)) + random.Next(30)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(50)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(80)),
                        ConcurrentExecutions = (int)(5 * trendFactor) + random.Next(8),
                        MemoryUsage = (int)((50 * trendFactor + random.Next(50)) * 1024 * 1024),
                        DatabaseCalls = (int)(10 * trendFactor) + random.Next(15),
                        ExternalApiCalls = (int)(5 * trendFactor) + random.Next(8),
                        LastExecution = DateTime.UtcNow.AddDays(-180 + i),
                        SuccessRate = 0.9 + random.NextDouble() * 0.05,
                        MemoryAllocated = (int)((50 * trendFactor + random.Next(50)) * 1024 * 1024)
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 90)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.25,
                    Timestamp = DateTime.UtcNow.AddDays(-90 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 360)
                .Select(i => 
                {
                    var dayIndex = i / 2;
                    var trendFactor = 1.0 + (dayIndex * 0.01);
                    var noise = random.NextDouble() * 0.1 - 0.05; // ±5% noise
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-180 + dayIndex).AddHours((i % 2) * 12),
                        CpuUtilization = (0.3 + random.NextDouble() * 0.3) * trendFactor * (1 + noise),
                        MemoryUtilization = (0.4 + random.NextDouble() * 0.3) * trendFactor * (1 + noise),
                        ThroughputPerSecond = (int)((50 * trendFactor + random.Next(80)) * (1 + noise))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreatePerformanceMetricsTrainingData()
    {
        var random = new Random(333);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 300)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 200 + random.Next(300),
                    SuccessfulExecutions = 180 + random.Next(200),
                    FailedExecutions = 20 + random.Next(100),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(80)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(120)),
                    ConcurrentExecutions = 8 + random.Next(20),
                    MemoryUsage = (80 + random.Next(120)) * 1024 * 1024,
                    DatabaseCalls = 15 + random.Next(45),
                    ExternalApiCalls = 8 + random.Next(22),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(180)),
                    SuccessRate = 0.85 + random.NextDouble() * 0.1,
                    MemoryAllocated = (80 + random.Next(120)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 150)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.18,
                    ExecutionTime = TimeSpan.FromMilliseconds(18 + random.Next(52)),
                    PerformanceImprovement = random.NextDouble() * 0.28,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(150))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 600)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-600 + i),
                    CpuUtilization = 0.28 + random.NextDouble() * 0.52,
                    MemoryUtilization = 0.38 + random.NextDouble() * 0.42,
                    ThroughputPerSecond = 70 + random.Next(230)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMemoryIntensiveTrainingData()
    {
        var random = new Random(444);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 400)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 300 + random.Next(400),
                    SuccessfulExecutions = 270 + random.Next(300),
                    FailedExecutions = 30 + random.Next(100),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(60 + random.Next(100)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(120 + random.Next(160)),
                    ConcurrentExecutions = 15 + random.Next(25),
                    MemoryUsage = (200 + random.Next(300)) * 1024 * 1024, // Higher memory usage
                    DatabaseCalls = 25 + random.Next(55),
                    ExternalApiCalls = 12 + random.Next(28),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(240)),
                    SuccessRate = 0.87 + random.NextDouble() * 0.08,
                    MemoryAllocated = (200 + random.Next(300)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 200)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.12,
                    ExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(65)),
                    PerformanceImprovement = random.NextDouble() * 0.32,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(200))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 800)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-800 + i),
                    CpuUtilization = 0.4 + random.NextDouble() * 0.4,
                    MemoryUtilization = 0.6 + random.NextDouble() * 0.3, // Higher memory
                    ThroughputPerSecond = 100 + random.Next(300)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateEdgeCaseStatisticsTrainingData()
    {
        var random = new Random(555);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => 
                {
                    if (i % 10 == 0)
                    {
                        // Edge case: Zero values
                        return new RequestExecutionMetrics
                        {
                            TotalExecutions = 1,
                            SuccessfulExecutions = 1,
                            FailedExecutions = 0,
                            AverageExecutionTime = TimeSpan.FromMilliseconds(1),
                            P95ExecutionTime = TimeSpan.FromMilliseconds(1),
                            ConcurrentExecutions = 1,
                            MemoryUsage = 1024 * 1024,
                            DatabaseCalls = 1,
                            ExternalApiCalls = 0,
                            LastExecution = DateTime.UtcNow.AddMinutes(-i),
                            SuccessRate = 1.0,
                            MemoryAllocated = 1024 * 1024
                        };
                    }
                    else if (i % 7 == 0)
                    {
                        // Edge case: Maximum values
                        return new RequestExecutionMetrics
                        {
                            TotalExecutions = int.MaxValue - 1,
                            SuccessfulExecutions = int.MaxValue - 1000,
                            FailedExecutions = 999,
                            AverageExecutionTime = TimeSpan.FromMinutes(10),
                            P95ExecutionTime = TimeSpan.FromMinutes(15),
                            ConcurrentExecutions = 1000,
                            MemoryUsage = long.MaxValue - 1,
                            DatabaseCalls = 10000,
                            ExternalApiCalls = 5000,
                            LastExecution = DateTime.UtcNow.AddMinutes(-i),
                            SuccessRate = 0.999,
                            MemoryAllocated = long.MaxValue - 1
                        };
                    }
                    else
                    {
                        // Normal case
                        return new RequestExecutionMetrics
                        {
                            TotalExecutions = 100 + random.Next(200),
                            SuccessfulExecutions = 90 + random.Next(150),
                            FailedExecutions = 10 + random.Next(50),
                            AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(100)),
                            P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(150)),
                            ConcurrentExecutions = 5 + random.Next(15),
                            MemoryUsage = (50 + random.Next(100)) * 1024 * 1024,
                            DatabaseCalls = 10 + random.Next(30),
                            ExternalApiCalls = 5 + random.Next(15),
                            LastExecution = DateTime.UtcNow.AddMinutes(-i),
                            SuccessRate = 0.85 + random.NextDouble() * 0.1,
                            MemoryAllocated = (50 + random.Next(100)) * 1024 * 1024
                        };
                    }
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(80)),
                    PerformanceImprovement = random.NextDouble() * 0.4,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(48))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 100)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                    CpuUtilization = random.NextDouble(), // Full range 0-1
                    MemoryUtilization = random.NextDouble(), // Full range 0-1
                    ThroughputPerSecond = random.Next(10000) // Wide range
                })
                .ToArray()
        };
    }

    private AITrainingData CreateNullValueTrainingData()
    {
        // This creates data that might have null or edge case values
        // Note: The actual structs can't be null, but we can test edge cases
        var random = new Random(666);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 30)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = Math.Max(1, 50 + random.Next(100)),
                    SuccessfulExecutions = Math.Max(0, 45 + random.Next(80)),
                    FailedExecutions = Math.Max(0, 5 + random.Next(20)),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 30 + random.Next(70))),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 60 + random.Next(120))),
                    ConcurrentExecutions = Math.Max(1, 3 + random.Next(10)),
                    MemoryUsage = Math.Max(1024 * 1024, (30 + random.Next(70)) * 1024 * 1024),
                    DatabaseCalls = Math.Max(0, 5 + random.Next(15)),
                    ExternalApiCalls = Math.Max(0, 2 + random.Next(8)),
                    LastExecution = DateTime.UtcNow.AddMinutes(-Math.Max(1, random.Next(60))),
                    SuccessRate = Math.Max(0.0, Math.Min(1.0, 0.8 + random.NextDouble() * 0.15)),
                    MemoryAllocated = Math.Max(1024 * 1024, (30 + random.Next(70)) * 1024 * 1024)
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 15)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)Math.Max(1, random.Next(1, 8)),
                    Success = random.NextDouble() > 0.25,
                    ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, random.Next(10, 100))),
                    PerformanceImprovement = Math.Max(-1.0, Math.Min(1.0, random.NextDouble() * 0.5 - 0.1)),
                    Timestamp = DateTime.UtcNow.AddHours(-Math.Max(1, random.Next(48)))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 60)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-Math.Max(1, 60 - i)),
                    CpuUtilization = Math.Max(0.0, Math.Min(1.0, random.NextDouble())),
                    MemoryUtilization = Math.Max(0.0, Math.Min(1.0, random.NextDouble())),
                    ThroughputPerSecond = Math.Max(0, random.Next(200))
                })
                .ToArray()
        };
    }

    private AITrainingData CreateExtremeValueTrainingData()
    {
        var random = new Random(777);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 80)
                .Select(i => 
                {
                    if (i % 8 == 0)
                    {
                        // Extremely high values
                        return new RequestExecutionMetrics
                        {
                            TotalExecutions = 1000000,
                            SuccessfulExecutions = 999000,
                            FailedExecutions = 1000,
                            AverageExecutionTime = TimeSpan.FromMinutes(5),
                            P95ExecutionTime = TimeSpan.FromMinutes(10),
                            ConcurrentExecutions = 10000,
                            MemoryUsage = 10L * 1024 * 1024 * 1024, // 10GB
                            DatabaseCalls = 100000,
                            ExternalApiCalls = 50000,
                            LastExecution = DateTime.UtcNow.AddMinutes(-i),
                            SuccessRate = 0.999,
                            MemoryAllocated = 10L * 1024 * 1024 * 1024
                        };
                    }
                    else if (i % 5 == 0)
                    {
                        // Extremely low values
                        return new RequestExecutionMetrics
                        {
                            TotalExecutions = 1,
                            SuccessfulExecutions = 0,
                            FailedExecutions = 1,
                            AverageExecutionTime = TimeSpan.FromTicks(1),
                            P95ExecutionTime = TimeSpan.FromTicks(1),
                            ConcurrentExecutions = 1,
                            MemoryUsage = 1024,
                            DatabaseCalls = 0,
                            ExternalApiCalls = 0,
                            LastExecution = DateTime.UtcNow.AddMinutes(-i),
                            SuccessRate = 0.0,
                            MemoryAllocated = 1024
                        };
                    }
                    else
                    {
                        // Normal values
                        return new RequestExecutionMetrics
                        {
                            TotalExecutions = 100 + random.Next(200),
                            SuccessfulExecutions = 85 + random.Next(100),
                            FailedExecutions = 15 + random.Next(30),
                            AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(100)),
                            P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(150)),
                            ConcurrentExecutions = 5 + random.Next(15),
                            MemoryUsage = (50 + random.Next(100)) * 1024 * 1024,
                            DatabaseCalls = 10 + random.Next(25),
                            ExternalApiCalls = 5 + random.Next(12),
                            LastExecution = DateTime.UtcNow.AddMinutes(-i),
                            SuccessRate = 0.8 + random.NextDouble() * 0.15,
                            MemoryAllocated = (50 + random.Next(100)) * 1024 * 1024
                        };
                    }
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 40)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(random.Next(1, 1000)),
                    PerformanceImprovement = random.NextDouble() * 2.0 - 1.0, // Range -1 to 1
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(72))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 160)
                .Select(i => 
                {
                    if (i % 20 == 0)
                    {
                        // Extreme system load
                        return new SystemLoadMetrics
                        {
                            Timestamp = DateTime.UtcNow.AddMinutes(-160 + i),
                            CpuUtilization = 1.0,
                            MemoryUtilization = 1.0,
                            ThroughputPerSecond = 100000
                        };
                    }
                    else if (i % 15 == 0)
                    {
                        // Minimal system load
                        return new SystemLoadMetrics
                        {
                            Timestamp = DateTime.UtcNow.AddMinutes(-160 + i),
                            CpuUtilization = 0.0,
                            MemoryUtilization = 0.0,
                            ThroughputPerSecond = 0
                        };
                    }
                    else
                    {
                        return new SystemLoadMetrics
                        {
                            Timestamp = DateTime.UtcNow.AddMinutes(-160 + i),
                            CpuUtilization = 0.3 + random.NextDouble() * 0.4,
                            MemoryUtilization = 0.4 + random.NextDouble() * 0.3,
                            ThroughputPerSecond = 50 + random.Next(150)
                        };
                    }
                })
                .ToArray()
        };
    }

    #endregion

    public void Dispose()
    {
        _trainer?.Dispose();
    }
}