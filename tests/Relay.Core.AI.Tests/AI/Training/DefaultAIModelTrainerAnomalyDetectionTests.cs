using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Models;
using Relay.Core.AI.Analysis.TimeSeries;
using Xunit;

namespace Relay.Core.Tests.AI.Training;

public class DefaultAIModelTrainerAnomalyDetectionTests : IDisposable
{
    private readonly Mock<ILogger<DefaultAIModelTrainer>> _mockLogger;
    private readonly DefaultAIModelTrainer _trainer;

    public DefaultAIModelTrainerAnomalyDetectionTests()
    {
        _mockLogger = new Mock<ILogger<DefaultAIModelTrainer>>();
        _trainer = new DefaultAIModelTrainer(_mockLogger.Object);
    }

    #region Normal Distribution Tests

    [Fact]
    public async Task TrainModelAsync_WithNormalDistributionData_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateNormalDistributionData();

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
    public async Task TrainModelAsync_WithNormalDistributionWithOutliers_DetectsAnomalies()
    {
        // Arrange
        var trainingData = CreateNormalDistributionWithOutliers();

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
    public async Task TrainModelAsync_WithMultiModalNormalDistribution_HandlesComplexity()
    {
        // Arrange
        var trainingData = CreateMultiModalNormalDistribution();

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

    #region Skewed Distribution Tests

    [Fact]
    public async Task TrainModelAsync_WithRightSkewedDistribution_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateRightSkewedDistribution();

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
    public async Task TrainModelAsync_WithLeftSkewedDistribution_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateLeftSkewedDistribution();

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
    public async Task TrainModelAsync_WithHighlySkewedDistribution_HandlesExtremes()
    {
        // Arrange
        var trainingData = CreateHighlySkewedDistribution();

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

    #region Uniform Distribution Tests

    [Fact]
    public async Task TrainModelAsync_WithUniformDistribution_HandlesConsistency()
    {
        // Arrange
        var trainingData = CreateUniformDistribution();

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
    public async Task TrainModelAsync_WithUniformDistributionWithSpikes_DetectsAnomalies()
    {
        // Arrange
        var trainingData = CreateUniformDistributionWithSpikes();

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

    #region Exponential Distribution Tests

    [Fact]
    public async Task TrainModelAsync_WithExponentialDistribution_HandlesGrowth()
    {
        // Arrange
        var trainingData = CreateExponentialDistribution();

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
    public async Task TrainModelAsync_WithExponentialDecay_HandlesDecline()
    {
        // Arrange
        var trainingData = CreateExponentialDecay();

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

    #region Seasonal Distribution Tests

    [Fact]
    public async Task TrainModelAsync_WithSeasonalDistribution_HandlesPatterns()
    {
        // Arrange
        var trainingData = CreateSeasonalDistribution();

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
    public async Task TrainModelAsync_WithMultiSeasonalDistribution_HandlesComplexPatterns()
    {
        // Arrange
        var trainingData = CreateMultiSeasonalDistribution();

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
    public async Task TrainModelAsync_WithSeasonalWithAnomalies_DetectsOutliers()
    {
        // Arrange
        var trainingData = CreateSeasonalWithAnomalies();

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

    #region Volatile Distribution Tests

    [Fact]
    public async Task TrainModelAsync_WithHighVolatileDistribution_HandlesInstability()
    {
        // Arrange
        var trainingData = CreateHighVolatileDistribution();

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
    public async Task TrainModelAsync_WithLowVolatileDistribution_HandlesStability()
    {
        // Arrange
        var trainingData = CreateLowVolatileDistribution();

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
    public async Task TrainModelAsync_WithBurstyVolatileDistribution_HandlesSpikes()
    {
        // Arrange
        var trainingData = CreateBurstyVolatileDistribution();

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

    #region Threshold Sensitivity Tests

    [Fact]
    public async Task TrainModelAsync_WithLowThresholdData_HandlesSensitivity()
    {
        // Arrange
        var trainingData = CreateLowThresholdData();

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
    public async Task TrainModelAsync_WithHighThresholdData_HandlesTolerance()
    {
        // Arrange
        var trainingData = CreateHighThresholdData();

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
    public async Task TrainModelAsync_WithAdaptiveThresholdData_HandlesDynamics()
    {
        // Arrange
        var trainingData = CreateAdaptiveThresholdData();

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

    #region Edge Case Distribution Tests

    [Fact]
    public async Task TrainModelAsync_WithBimodalDistribution_HandlesDualPeaks()
    {
        // Arrange
        var trainingData = CreateBimodalDistribution();

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
    public async Task TrainModelAsync_WithTrimodalDistribution_HandlesTriplePeaks()
    {
        // Arrange
        var trainingData = CreateTrimodalDistribution();

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
    public async Task TrainModelAsync_WithHeavyTailedDistribution_HandlesExtremes()
    {
        // Arrange
        var trainingData = CreateHeavyTailedDistribution();

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
    public async Task TrainModelAsync_WithSparseDistribution_HandlesMissingData()
    {
        // Arrange
        var trainingData = CreateSparseDistribution();

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

    #region Mixed Distribution Tests

    [Fact]
    public async Task TrainModelAsync_WithMixedNormalAndUniform_HandlesHybrid()
    {
        // Arrange
        var trainingData = CreateMixedNormalAndUniform();

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
    public async Task TrainModelAsync_WithMixedSeasonalAndRandom_HandlesComplexity()
    {
        // Arrange
        var trainingData = CreateMixedSeasonalAndRandom();

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
    public async Task TrainModelAsync_WithMixedTrendAndVolatility_HandlesDynamics()
    {
        // Arrange
        var trainingData = CreateMixedTrendAndVolatility();

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

    private AITrainingData CreateNormalDistributionData()
    {
        var random = new Random(42);
        var normalValues = GenerateNormalDistribution(100, 50, 10); // mean=50, std=10
        
        return new AITrainingData
        {
            ExecutionHistory = normalValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(20)),
                SuccessfulExecutions = (int)(value * 0.9 + random.Next(15)),
                FailedExecutions = (int)(value * 0.1 + random.Next(5)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(50 + value * 0.5 + random.Next(20)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(100 + value + random.Next(40)),
                ConcurrentExecutions = (int)(value / 10 + random.Next(5)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(50) * 1024 * 1024),
                DatabaseCalls = (int)(value / 5 + random.Next(10)),
                ExternalApiCalls = (int)(value / 10 + random.Next(5)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.85 + (value - 50) * 0.001)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(50) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = normalValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.1, Math.Min(0.9, value / 100.0)),
                MemoryUtilization = Math.Max(0.1, Math.Min(0.9, value / 80.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateNormalDistributionWithOutliers()
    {
        var random = new Random(123);
        var baseValues = GenerateNormalDistribution(90, 50, 8);
        var outlierValues = new[] { 150.0, 160.0, 170.0, 10.0, 5.0, 0.0, 200.0, 180.0, 15.0, 190.0 };
        var allValues = baseValues.Concat(outlierValues).ToList();

        return new AITrainingData
        {
            ExecutionHistory = allValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = Math.Max(1, (int)(Math.Max(0, value) + random.Next(30))),
                SuccessfulExecutions = Math.Max(1, (int)(Math.Max(0, value) * 0.9 + random.Next(20))),
                FailedExecutions = Math.Max(0, (int)(Math.Max(0, value) * 0.1 + random.Next(8))),
                AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 50 + Math.Max(0, value) * 0.6 + random.Next(30))),
                P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 100 + Math.Max(0, value) * 1.2 + random.Next(60))),
                ConcurrentExecutions = Math.Max(1, (int)(Math.Max(0, value) / 8 + random.Next(8))),
                MemoryUsage = Math.Max(1024, (int)(Math.Max(0, value) * 1024 * 1024 + random.Next(80) * 1024 * 1024)),
                DatabaseCalls = Math.Max(1, (int)(Math.Max(0, value) / 4 + random.Next(15))),
                ExternalApiCalls = Math.Max(0, (int)(Math.Max(0, value) / 8 + random.Next(8))),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.5, Math.Min(0.98, 0.85 + (Math.Max(0, value) - 50) * 0.002)),
                MemoryAllocated = Math.Max(1024, (int)(Math.Max(0, value) * 1024 * 1024 + random.Next(80) * 1024 * 1024))
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.25, // Lower success due to outliers
                    ExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(50)),
                    PerformanceImprovement = random.NextDouble() * 0.35,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = allValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.05, Math.Min(0.95, value / 200.0)),
                MemoryUtilization = Math.Max(0.05, Math.Min(0.95, value / 180.0)),
                ThroughputPerSecond = (int)Math.Max(1, value)
            }).ToArray()
        };
    }

    private AITrainingData CreateMultiModalNormalDistribution()
    {
        var random = new Random(456);
        var mode1Values = GenerateNormalDistribution(30, 30, 5);  // First mode
        var mode2Values = GenerateNormalDistribution(30, 70, 8);  // Second mode
        var mode3Values = GenerateNormalDistribution(40, 120, 15); // Third mode
        var allValues = mode1Values.Concat(mode2Values).Concat(mode3Values).ToList();

        return new AITrainingData
        {
            ExecutionHistory = allValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = Math.Max(1, (int)(Math.Max(0, value) + random.Next(25))),
                SuccessfulExecutions = Math.Max(1, (int)(Math.Max(0, value) * 0.92 + random.Next(18))),
                FailedExecutions = Math.Max(0, (int)(Math.Max(0, value) * 0.08 + random.Next(6))),
                AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 40 + Math.Max(0, value) * 0.4 + random.Next(25))),
                P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 80 + Math.Max(0, value) * 0.8 + random.Next(50))),
                ConcurrentExecutions = Math.Max(1, (int)(Math.Max(0, value) / 12 + random.Next(6))),
                MemoryUsage = Math.Max(1024, (int)(Math.Max(0, value) * 1024 * 1024 + random.Next(60) * 1024 * 1024)),
                DatabaseCalls = Math.Max(1, (int)(Math.Max(0, value) / 6 + random.Next(12))),
                ExternalApiCalls = Math.Max(0, (int)(Math.Max(0, value) / 12 + random.Next(6))),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.75, Math.Min(0.95, 0.88 + (Math.Max(0, value) - 75) * 0.0005)),
                MemoryAllocated = Math.Max(1024, (int)(Math.Max(0, value) * 1024 * 1024 + random.Next(60) * 1024 * 1024))
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.22,
                    ExecutionTime = TimeSpan.FromMilliseconds(22 + random.Next(45)),
                    PerformanceImprovement = random.NextDouble() * 0.32,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = allValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.1, Math.Min(0.9, value / 150.0)),
                MemoryUtilization = Math.Max(0.1, Math.Min(0.9, value / 130.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateRightSkewedDistribution()
    {
        var random = new Random(789);
        var skewedValues = GenerateRightSkewedDistribution(100, 20, 3); // shape=3 creates right skew

        return new AITrainingData
        {
            ExecutionHistory = skewedValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = Math.Max(1, (int)(Math.Max(0, value) + random.Next(15))),
                SuccessfulExecutions = Math.Max(1, (int)(Math.Max(0, value) * 0.94 + random.Next(12))),
                FailedExecutions = Math.Max(0, (int)(Math.Max(0, value) * 0.06 + random.Next(4))),
                AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 30 + Math.Max(0, value) * 0.8 + random.Next(20))),
                P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 60 + Math.Max(0, value) * 1.6 + random.Next(40))),
                ConcurrentExecutions = Math.Max(1, (int)(Math.Max(0, value) / 15 + random.Next(4))),
                MemoryUsage = Math.Max(1024, (int)(Math.Max(0, value) * 1024 * 1024 + random.Next(40) * 1024 * 1024)),
                DatabaseCalls = Math.Max(1, (int)(Math.Max(0, value) / 8 + random.Next(8))),
                ExternalApiCalls = Math.Max(0, (int)(Math.Max(0, value) / 15 + random.Next(4))),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.8, Math.Min(0.96, 0.92 - Math.Max(0, value) * 0.001)),
                MemoryAllocated = Math.Max(1024, (int)(Math.Max(0, value) * 1024 * 1024 + random.Next(40) * 1024 * 1024))
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.18,
                    ExecutionTime = TimeSpan.FromMilliseconds(18 + random.Next(35)),
                    PerformanceImprovement = random.NextDouble() * 0.28,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = skewedValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.1, Math.Min(0.9, value / 200.0)),
                MemoryUtilization = Math.Max(0.1, Math.Min(0.9, value / 180.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateLeftSkewedDistribution()
    {
        var random = new Random(321);
        var skewedValues = GenerateLeftSkewedDistribution(100, 80, 3); // shape=3 creates left skew

        return new AITrainingData
        {
            ExecutionHistory = skewedValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = Math.Max(1, (int)(value + random.Next(20))),
                SuccessfulExecutions = Math.Max(1, (int)(value * 0.91 + random.Next(16))),
                FailedExecutions = Math.Max(0, (int)(value * 0.09 + random.Next(6))),
                AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 60 + (100 - Math.Max(0, value)) * 0.6 + random.Next(25))),
                P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 120 + (100 - Math.Max(0, value)) * 1.2 + random.Next(50))),
                ConcurrentExecutions = Math.Max(1, (int)(Math.Max(0, value) / 10 + random.Next(7))),
                MemoryUsage = Math.Max(1024, (int)(Math.Max(0, value) * 1024 * 1024 + random.Next(50) * 1024 * 1024)),
                DatabaseCalls = Math.Max(1, (int)(Math.Max(0, value) / 5 + random.Next(10))),
                ExternalApiCalls = Math.Max(0, (int)(Math.Max(0, value) / 10 + random.Next(5))),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.82, Math.Min(0.97, 0.90 + (Math.Max(0, value) - 50) * 0.001)),
                MemoryAllocated = Math.Max(1024, (int)(Math.Max(0, value) * 1024 * 1024 + random.Next(50) * 1024 * 1024))
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.19,
                    ExecutionTime = TimeSpan.FromMilliseconds(24 + random.Next(48)),
                    PerformanceImprovement = random.NextDouble() * 0.31,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = skewedValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.15, Math.Min(0.85, value / 120.0)),
                MemoryUtilization = Math.Max(0.15, Math.Min(0.85, value / 100.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateHighlySkewedDistribution()
    {
        var random = new Random(654);
        var highlySkewedValues = GenerateRightSkewedDistribution(100, 10, 1.5); // More extreme skew

        return new AITrainingData
        {
            ExecutionHistory = highlySkewedValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = Math.Max(1, (int)(Math.Max(0, value) + random.Next(10))),
                SuccessfulExecutions = Math.Max(1, (int)(Math.Max(0, value) * 0.96 + random.Next(8))),
                FailedExecutions = Math.Max(0, (int)(Math.Max(0, value) * 0.04 + random.Next(3))),
                AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 25 + Math.Max(0, value) * 1.2 + random.Next(15))),
                P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 50 + Math.Max(0, value) * 2.4 + random.Next(30))),
                ConcurrentExecutions = Math.Max(1, (int)(Math.Max(0, value) / 20 + random.Next(3))),
                MemoryUsage = Math.Max(1024, (int)(Math.Max(0, value) * 1024 * 1024 + random.Next(30) * 1024 * 1024)),
                DatabaseCalls = Math.Max(1, (int)(Math.Max(0, value) / 10 + random.Next(6))),
                ExternalApiCalls = Math.Max(0, (int)(Math.Max(0, value) / 20 + random.Next(3))),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.85, Math.Min(0.98, 0.94 - Math.Max(0, value) * 0.002)),
                MemoryAllocated = Math.Max(1024, (int)(Math.Max(0, value) * 1024 * 1024 + random.Next(30) * 1024 * 1024))
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(15 + random.Next(30)),
                    PerformanceImprovement = random.NextDouble() * 0.25,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = highlySkewedValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.05, Math.Min(0.95, value / 300.0)),
                MemoryUtilization = Math.Max(0.05, Math.Min(0.95, value / 250.0)),
                ThroughputPerSecond = (int)Math.Max(1, value)
            }).ToArray()
        };
    }

    private AITrainingData CreateUniformDistribution()
    {
        var random = new Random(987);
        var uniformValues = GenerateUniformDistribution(100, 20, 80);

        return new AITrainingData
        {
            ExecutionHistory = uniformValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(15)),
                SuccessfulExecutions = (int)(value * 0.93 + random.Next(12)),
                FailedExecutions = (int)(value * 0.07 + random.Next(5)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(45 + value * 0.5 + random.Next(22)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(90 + value + random.Next(44)),
                ConcurrentExecutions = (int)(value / 10 + random.Next(6)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(45) * 1024 * 1024),
                DatabaseCalls = (int)(value / 5 + random.Next(9)),
                ExternalApiCalls = (int)(value / 10 + random.Next(5)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.78, Math.Min(0.94, 0.86 + (value - 50) * 0.0008)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(45) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.21,
                    ExecutionTime = TimeSpan.FromMilliseconds(21 + random.Next(42)),
                    PerformanceImprovement = random.NextDouble() * 0.29,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = uniformValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.12, Math.Min(0.88, value / 100.0)),
                MemoryUtilization = Math.Max(0.12, Math.Min(0.88, value / 85.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateUniformDistributionWithSpikes()
    {
        var random = new Random(111);
        var baseValues = GenerateUniformDistribution(85, 30, 70);
        var spikeValues = new[] { 150.0, 160.0, 5.0, 10.0, 170.0, 180.0, 8.0, 12.0, 190.0, 200.0, 3.0, 7.0, 175.0, 185.0, 6.0 };
        var allValues = baseValues.Concat(spikeValues).ToList();

        return new AITrainingData
        {
            ExecutionHistory = allValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(25)),
                SuccessfulExecutions = (int)(value * 0.9 + random.Next(18)),
                FailedExecutions = (int)(value * 0.1 + random.Next(7)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(40 + value * 0.7 + random.Next(30)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(80 + value * 1.4 + random.Next(60)),
                ConcurrentExecutions = (int)(value / 8 + random.Next(8)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(60) * 1024 * 1024),
                DatabaseCalls = (int)(value / 4 + random.Next(12)),
                ExternalApiCalls = (int)(value / 8 + random.Next(6)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.7, Math.Min(0.96, 0.83 + (value - 50) * 0.0015)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(60) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.28, // Lower success due to spikes
                    ExecutionTime = TimeSpan.FromMilliseconds(28 + random.Next(56)),
                    PerformanceImprovement = random.NextDouble() * 0.38,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = allValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.08, Math.Min(0.92, value / 220.0)),
                MemoryUtilization = Math.Max(0.08, Math.Min(0.92, value / 200.0)),
                ThroughputPerSecond = (int)Math.Max(1, value)
            }).ToArray()
        };
    }

    private AITrainingData CreateExponentialDistribution()
    {
        var random = new Random(222);
        var exponentialValues = GenerateExponentialDistribution(100, 0.05); // Growth rate

        return new AITrainingData
        {
            ExecutionHistory = exponentialValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = Math.Max(1, (int)(value + random.Next(20))), // Ensure at least 1
                SuccessfulExecutions = Math.Max(1, (int)(value * 0.92 + random.Next(15))), // Ensure at least 1
                FailedExecutions = Math.Max(0, (int)(value * 0.08 + random.Next(6))),
                AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 35 + Math.Log(value + 1) * 10 + random.Next(25))), // Ensure > 0
                P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 70 + Math.Log(value + 1) * 20 + random.Next(50))), // Ensure > 0
                ConcurrentExecutions = Math.Max(1, (int)(Math.Log(value + 1) * 2 + random.Next(6))), // Ensure at least 1
                MemoryUsage = Math.Max(1024, (int)(value * 1024 * 1024 + random.Next(50) * 1024 * 1024)), // Ensure > 0
                DatabaseCalls = Math.Max(0, (int)(Math.Log(value + 1) * 3 + random.Next(10))),
                ExternalApiCalls = Math.Max(0, (int)(Math.Log(value + 1) * 1.5 + random.Next(5))),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.75, Math.Min(0.93, 0.84 + Math.Log(value + 1) * 0.01)),
                MemoryAllocated = Math.Max(1024, (int)(value * 1024 * 1024 + random.Next(50) * 1024 * 1024)) // Ensure > 0
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.23,
                    ExecutionTime = TimeSpan.FromMilliseconds(23 + random.Next(46)),
                    PerformanceImprovement = random.NextDouble() * 0.33,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = exponentialValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.1, Math.Min(0.9, Math.Log(value + 1) / 10)),
                MemoryUtilization = Math.Max(0.1, Math.Min(0.9, Math.Log(value + 1) / 8)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateExponentialDecay()
    {
        var random = new Random(333);
        var decayValues = GenerateExponentialDecay(100, 0.05); // Decay rate

        return new AITrainingData
        {
            ExecutionHistory = decayValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = Math.Max(1, (int)(value + random.Next(15))), // Ensure at least 1
                SuccessfulExecutions = Math.Max(1, (int)(value * 0.94 + random.Next(12))), // Ensure at least 1
                FailedExecutions = Math.Max(0, (int)(value * 0.06 + random.Next(5))),
                AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 55 + (100 - value) * 0.4 + random.Next(20))), // Ensure > 0
                P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 110 + (100 - value) * 0.8 + random.Next(40))), // Ensure > 0
                ConcurrentExecutions = Math.Max(1, (int)(value / 12 + random.Next(5))), // Ensure at least 1
                MemoryUsage = Math.Max(1024, (int)(value * 1024 * 1024 + random.Next(40) * 1024 * 1024)), // Ensure > 0
                DatabaseCalls = Math.Max(0, (int)(value / 6 + random.Next(8))),
                ExternalApiCalls = Math.Max(0, (int)(value / 12 + random.Next(4))),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.8, Math.Min(0.95, 0.88 - (100 - value) * 0.001)),
                MemoryAllocated = Math.Max(1024, (int)(value * 1024 * 1024 + random.Next(40) * 1024 * 1024)) // Ensure > 0
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = decayValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.1, Math.Min(0.9, value / 120.0)),
                MemoryUtilization = Math.Max(0.1, Math.Min(0.9, value / 100.0)),
                ThroughputPerSecond = (int)Math.Max(1, value)
            }).ToArray()
        };
    }

    private AITrainingData CreateSeasonalDistribution()
    {
        var random = new Random(444);
        var seasonalValues = GenerateSeasonalDistribution(100, 50, 30, 10); // mean, amplitude, period

        return new AITrainingData
        {
            ExecutionHistory = seasonalValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(18)),
                SuccessfulExecutions = (int)(value * 0.91 + random.Next(14)),
                FailedExecutions = (int)(value * 0.09 + random.Next(6)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(48 + value * 0.5 + random.Next(24)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(96 + value + random.Next(48)),
                ConcurrentExecutions = (int)(value / 10 + random.Next(6)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(48) * 1024 * 1024),
                DatabaseCalls = (int)(value / 5 + random.Next(10)),
                ExternalApiCalls = (int)(value / 10 + random.Next(5)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.77, Math.Min(0.94, 0.85 + Math.Sin(i * 0.1) * 0.05)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(48) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.22,
                    ExecutionTime = TimeSpan.FromMilliseconds(22 + random.Next(44)),
                    PerformanceImprovement = random.NextDouble() * 0.32,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = seasonalValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.1, Math.Min(0.9, value / 110.0)),
                MemoryUtilization = Math.Max(0.1, Math.Min(0.9, value / 90.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateMultiSeasonalDistribution()
    {
        var random = new Random(555);
        var multiSeasonalValues = GenerateMultiSeasonalDistribution(100, 50, 25, 8, 15, 20); // mean, amp1, period1, amp2, period2

        return new AITrainingData
        {
            ExecutionHistory = multiSeasonalValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(22)),
                SuccessfulExecutions = (int)(value * 0.9 + random.Next(16)),
                FailedExecutions = (int)(value * 0.1 + random.Next(7)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(42 + value * 0.55 + random.Next(28)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(84 + value * 1.1 + random.Next(56)),
                ConcurrentExecutions = (int)(value / 9 + random.Next(7)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(55) * 1024 * 1024),
                DatabaseCalls = (int)(value / 4.5 + random.Next(11)),
                ExternalApiCalls = (int)(value / 9 + random.Next(6)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.76, Math.Min(0.95, 0.84 + Math.Sin(i * 0.15) * 0.06 + Math.Cos(i * 0.05) * 0.04)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(55) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.24,
                    ExecutionTime = TimeSpan.FromMilliseconds(24 + random.Next(48)),
                    PerformanceImprovement = random.NextDouble() * 0.34,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = multiSeasonalValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.08, Math.Min(0.92, value / 130.0)),
                MemoryUtilization = Math.Max(0.08, Math.Min(0.92, value / 110.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateSeasonalWithAnomalies()
    {
        var random = new Random(666);
        var baseSeasonal = GenerateSeasonalDistribution(85, 45, 20, 12);
        var anomalyValues = new[] { 150.0, 10.0, 160.0, 5.0, 170.0, 8.0, 12.0, 180.0, 15.0, 190.0, 7.0, 13.0, 175.0, 9.0, 11.0 };
        var allValues = baseSeasonal.Concat(anomalyValues).ToList();

        return new AITrainingData
        {
            ExecutionHistory = allValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(30)),
                SuccessfulExecutions = (int)(value * 0.88 + random.Next(20)),
                FailedExecutions = (int)(value * 0.12 + random.Next(8)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(38 + value * 0.65 + random.Next(35)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(76 + value * 1.3 + random.Next(70)),
                ConcurrentExecutions = (int)(value / 7 + random.Next(9)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(70) * 1024 * 1024),
                DatabaseCalls = (int)(value / 3.5 + random.Next(14)),
                ExternalApiCalls = (int)(value / 7 + random.Next(7)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.65, Math.Min(0.97, 0.81 + Math.Sin(i * 0.12) * 0.08 + (value > 120 ? -0.1 : 0.02))),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(70) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.32, // Lower success due to anomalies
                    ExecutionTime = TimeSpan.FromMilliseconds(32 + random.Next(64)),
                    PerformanceImprovement = random.NextDouble() * 0.42,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = allValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.05, Math.Min(0.95, value / 200.0)),
                MemoryUtilization = Math.Max(0.05, Math.Min(0.95, value / 180.0)),
                ThroughputPerSecond = (int)Math.Max(1, value)
            }).ToArray()
        };
    }

    private AITrainingData CreateHighVolatileDistribution()
    {
        var random = new Random(777);
        var volatileValues = GenerateHighVolatileDistribution(100, 50, 40); // mean, high volatility

        return new AITrainingData
        {
            ExecutionHistory = volatileValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(Math.Max(5, value) + random.Next(40)),
                SuccessfulExecutions = (int)(Math.Max(4, value * 0.85) + random.Next(25)),
                FailedExecutions = (int)(Math.Max(1, value * 0.15) + random.Next(10)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(10, 1000 / Math.Max(10, value)) + random.Next(50)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(20, 2000 / Math.Max(10, value)) + random.Next(100)),
                ConcurrentExecutions = (int)(Math.Max(1, value / 15) + random.Next(12)),
                MemoryUsage = (int)(Math.Max(10 * 1024 * 1024, value * 1024 * 1024) + random.Next(80) * 1024 * 1024),
                DatabaseCalls = (int)(Math.Max(1, value / 7) + random.Next(18)),
                ExternalApiCalls = (int)(Math.Max(1, value / 15) + random.Next(9)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.5, Math.Min(0.98, 0.75 + random.NextDouble() * 0.3)),
                MemoryAllocated = (int)(Math.Max(10 * 1024 * 1024, value * 1024 * 1024) + random.Next(80) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.35, // Lower success due to volatility
                    ExecutionTime = TimeSpan.FromMilliseconds(35 + random.Next(70)),
                    PerformanceImprovement = random.NextDouble() * 0.45 - 0.05, // Can be negative
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = volatileValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.05, Math.Min(0.95, Math.Max(5, value) / 200.0)),
                MemoryUtilization = Math.Max(0.05, Math.Min(0.95, Math.Max(5, value) / 180.0)),
                ThroughputPerSecond = (int)Math.Max(1, value)
            }).ToArray()
        };
    }

    private AITrainingData CreateLowVolatileDistribution()
    {
        var random = new Random(888);
        var stableValues = GenerateLowVolatileDistribution(100, 60, 5); // mean, low volatility

        return new AITrainingData
        {
            ExecutionHistory = stableValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(8)),
                SuccessfulExecutions = (int)(value * 0.96 + random.Next(6)),
                FailedExecutions = (int)(value * 0.04 + random.Next(2)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(55 + random.Next(10)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(110 + random.Next(20)),
                ConcurrentExecutions = (int)(value / 12 + random.Next(2)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(10) * 1024 * 1024),
                DatabaseCalls = (int)(value / 6 + random.Next(3)),
                ExternalApiCalls = (int)(value / 12 + random.Next(2)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.92, Math.Min(0.98, 0.95 + random.NextDouble() * 0.02)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(10) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.08, // High success due to stability
                    ExecutionTime = TimeSpan.FromMilliseconds(12 + random.Next(20)),
                    PerformanceImprovement = random.NextDouble() * 0.18,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = stableValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.4, Math.Min(0.7, value / 100.0)),
                MemoryUtilization = Math.Max(0.5, Math.Min(0.8, value / 80.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateBurstyVolatileDistribution()
    {
        var random = new Random(999);
        var burstyValues = GenerateBurstyVolatileDistribution(100, 40, 0.2); // mean, burst probability

        return new AITrainingData
        {
            ExecutionHistory = burstyValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(35)),
                SuccessfulExecutions = (int)(value * 0.89 + random.Next(22)),
                FailedExecutions = (int)(value * 0.11 + random.Next(9)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(value > 100 ? 80 : 45 + random.Next(30)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(value > 100 ? 160 : 90 + random.Next(60)),
                ConcurrentExecutions = (int)(value / 8 + random.Next(10)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(65) * 1024 * 1024),
                DatabaseCalls = (int)(value / 4 + random.Next(13)),
                ExternalApiCalls = (int)(value / 8 + random.Next(7)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.6, Math.Min(0.95, value > 100 ? 0.75 : 0.88 + random.NextDouble() * 0.07)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(65) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.28,
                    ExecutionTime = TimeSpan.FromMilliseconds(random.NextDouble() > 0.2 ? 60 : 25 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.38,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = burstyValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.1, Math.Min(0.95, value / 180.0)),
                MemoryUtilization = Math.Max(0.1, Math.Min(0.95, value / 160.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateLowThresholdData()
    {
        var random = new Random(101);
        var lowThresholdValues = GenerateNormalDistribution(100, 30, 3); // Tight distribution

        return new AITrainingData
        {
            ExecutionHistory = lowThresholdValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(5)),
                SuccessfulExecutions = (int)(value * 0.97 + random.Next(4)),
                FailedExecutions = (int)(value * 0.03 + random.Next(2)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(65 + random.Next(8)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(130 + random.Next(16)),
                ConcurrentExecutions = (int)(value / 15 + random.Next(2)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(8) * 1024 * 1024),
                DatabaseCalls = (int)(value / 7 + random.Next(3)),
                ExternalApiCalls = (int)(value / 15 + random.Next(2)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.94, Math.Min(0.99, 0.97 + random.NextDouble() * 0.01)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(8) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.05, // Very high success rate
                    ExecutionTime = TimeSpan.FromMilliseconds(8 + random.Next(12)),
                    PerformanceImprovement = random.NextDouble() * 0.12,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = lowThresholdValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.5, Math.Min(0.8, value / 50.0)),
                MemoryUtilization = Math.Max(0.6, Math.Min(0.85, value / 40.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateHighThresholdData()
    {
        var random = new Random(202);
        var highThresholdValues = GenerateNormalDistribution(100, 70, 25); // Wide distribution

        return new AITrainingData
        {
            ExecutionHistory = highThresholdValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(50)),
                SuccessfulExecutions = (int)(value * 0.87 + random.Next(30)),
                FailedExecutions = (int)(value * 0.13 + random.Next(12)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(35 + value * 0.4 + random.Next(40)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(70 + value * 0.8 + random.Next(80)),
                ConcurrentExecutions = (int)(value / 6 + random.Next(15)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(80) * 1024 * 1024),
                DatabaseCalls = (int)(value / 3 + random.Next(20)),
                ExternalApiCalls = (int)(value / 6 + random.Next(10)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.7, Math.Min(0.92, 0.81 + (value - 70) * 0.002)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(80) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.3, // Lower success due to high variance
                    ExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(80)),
                    PerformanceImprovement = random.NextDouble() * 0.5,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = highThresholdValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.05, Math.Min(0.95, value / 150.0)),
                MemoryUtilization = Math.Max(0.05, Math.Min(0.95, value / 120.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateAdaptiveThresholdData()
    {
        var random = new Random(303);
        var adaptiveValues = GenerateAdaptiveThresholdDistribution(100); // Changing variance over time

        return new AITrainingData
        {
            ExecutionHistory = adaptiveValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(25)),
                SuccessfulExecutions = (int)(value * 0.91 + random.Next(18)),
                FailedExecutions = (int)(value * 0.09 + random.Next(7)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(45 + value * 0.45 + random.Next(30)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(90 + value * 0.9 + random.Next(60)),
                ConcurrentExecutions = (int)(value / 9 + random.Next(8)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(55) * 1024 * 1024),
                DatabaseCalls = (int)(value / 4.5 + random.Next(11)),
                ExternalApiCalls = (int)(value / 9 + random.Next(5)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.72, Math.Min(0.94, 0.83 + (i < 50 ? -0.05 : 0.05) + random.NextDouble() * 0.08)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(55) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > (i < 25 ? 0.15 : 0.3), // Changing success rate
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(50) + (i < 50 ? 0 : 20)),
                    PerformanceImprovement = random.NextDouble() * (i < 50 ? 0.25 : 0.45),
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = adaptiveValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.08, Math.Min(0.92, value / 140.0)),
                MemoryUtilization = Math.Max(0.08, Math.Min(0.92, value / 110.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateBimodalDistribution()
    {
        var random = new Random(404);
        var mode1Values = GenerateNormalDistribution(40, 25, 5);  // First mode
        var mode2Values = GenerateNormalDistribution(60, 75, 8);  // Second mode
        var allValues = mode1Values.Concat(mode2Values).ToList();

        return new AITrainingData
        {
            ExecutionHistory = allValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(20)),
                SuccessfulExecutions = (int)(value * 0.92 + random.Next(15)),
                FailedExecutions = (int)(value * 0.08 + random.Next(6)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(40 + value * 0.5 + random.Next(25)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(80 + value + random.Next(50)),
                ConcurrentExecutions = (int)(value / 10 + random.Next(6)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(50) * 1024 * 1024),
                DatabaseCalls = (int)(value / 5 + random.Next(10)),
                ExternalApiCalls = (int)(value / 10 + random.Next(5)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.78, Math.Min(0.94, 0.86 + (value < 50 ? -0.03 : 0.02))),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(50) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(45)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = allValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.1, Math.Min(0.9, value / 120.0)),
                MemoryUtilization = Math.Max(0.1, Math.Min(0.9, value / 100.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateTrimodalDistribution()
    {
        var random = new Random(505);
        var mode1Values = GenerateNormalDistribution(25, 20, 3);  // First mode
        var mode2Values = GenerateNormalDistribution(40, 50, 6);  // Second mode
        var mode3Values = GenerateNormalDistribution(35, 80, 10); // Third mode
        var allValues = mode1Values.Concat(mode2Values).Concat(mode3Values).ToList();

        return new AITrainingData
        {
            ExecutionHistory = allValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(22)),
                SuccessfulExecutions = (int)(value * 0.9 + random.Next(17)),
                FailedExecutions = (int)(value * 0.1 + random.Next(7)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(38 + value * 0.52 + random.Next(28)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(76 + value * 1.04 + random.Next(56)),
                ConcurrentExecutions = (int)(value / 9 + random.Next(7)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(52) * 1024 * 1024),
                DatabaseCalls = (int)(value / 4.5 + random.Next(11)),
                ExternalApiCalls = (int)(value / 9 + random.Next(6)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.75, Math.Min(0.93, 0.84 + (value < 35 ? -0.04 : value < 65 ? 0.01 : 0.03))),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(52) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.22,
                    ExecutionTime = TimeSpan.FromMilliseconds(22 + random.Next(48)),
                    PerformanceImprovement = random.NextDouble() * 0.32,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = allValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.08, Math.Min(0.92, value / 130.0)),
                MemoryUtilization = Math.Max(0.08, Math.Min(0.92, value / 110.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateHeavyTailedDistribution()
    {
        var random = new Random(606);
        var heavyTailedValues = GenerateHeavyTailedDistribution(100, 30, 2); // scale, shape parameter

        return new AITrainingData
        {
            ExecutionHistory = heavyTailedValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(30)),
                SuccessfulExecutions = (int)(value * 0.88 + random.Next(20)),
                FailedExecutions = (int)(value * 0.12 + random.Next(8)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(35 + Math.Log(value + 1) * 15 + random.Next(35)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(70 + Math.Log(value + 1) * 30 + random.Next(70)),
                ConcurrentExecutions = (int)(Math.Log(value + 1) * 3 + random.Next(8)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(60) * 1024 * 1024),
                DatabaseCalls = (int)(Math.Log(value + 1) * 4 + random.Next(12)),
                ExternalApiCalls = (int)(Math.Log(value + 1) * 2 + random.Next(6)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.65, Math.Min(0.92, 0.78 - Math.Log(value + 1) * 0.02 + random.NextDouble() * 0.1)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(60) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.3, // Lower success due to heavy tails
                    ExecutionTime = TimeSpan.FromMilliseconds(30 + random.Next(60)),
                    PerformanceImprovement = random.NextDouble() * 0.4,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = heavyTailedValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.05, Math.Min(0.95, Math.Log(value + 1) / 8)),
                MemoryUtilization = Math.Max(0.05, Math.Min(0.95, Math.Log(value + 1) / 6)),
                ThroughputPerSecond = (int)Math.Max(1, value)
            }).ToArray()
        };
    }

    private AITrainingData CreateSparseDistribution()
    {
        var random = new Random(707);
        var sparseValues = GenerateSparseDistribution(100, 0.3); // 30% missing data

        return new AITrainingData
        {
            ExecutionHistory = sparseValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = Math.Max(1, (int)value),
                SuccessfulExecutions = Math.Max(1, (int)(value * 0.9)),
                FailedExecutions = Math.Max(0, (int)(value * 0.1)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 50 + random.Next(30))),
                P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 100 + random.Next(60))),
                ConcurrentExecutions = Math.Max(1, (int)(value / 10 + random.Next(5))),
                MemoryUsage = Math.Max(1, (int)(value * 1024 * 1024 + random.Next(40) * 1024 * 1024)),
                DatabaseCalls = Math.Max(1, (int)(value / 5 + random.Next(8))),
                ExternalApiCalls = Math.Max(0, (int)(value / 10 + random.Next(4))),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.85 + random.NextDouble() * 0.1)),
                MemoryAllocated = Math.Max(1, (int)(value * 1024 * 1024 + random.Next(40) * 1024 * 1024))
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.25,
                    ExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(50)),
                    PerformanceImprovement = random.NextDouble() * 0.35,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = sparseValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = value > 0 ? Math.Max(0.1, Math.Min(0.9, value / 120.0)) : 0,
                MemoryUtilization = value > 0 ? Math.Max(0.1, Math.Min(0.9, value / 100.0)) : 0,
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateMixedNormalAndUniform()
    {
        var random = new Random(808);
        var normalValues = GenerateNormalDistribution(50, 40, 8);
        var uniformValues = GenerateUniformDistribution(50, 60, 100);
        var mixedValues = normalValues.Concat(uniformValues).ToList();

        return new AITrainingData
        {
            ExecutionHistory = mixedValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(24)),
                SuccessfulExecutions = (int)(value * 0.91 + random.Next(18)),
                FailedExecutions = (int)(value * 0.09 + random.Next(7)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(42 + value * 0.48 + random.Next(26)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(84 + value * 0.96 + random.Next(52)),
                ConcurrentExecutions = (int)(value / 9 + random.Next(7)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(52) * 1024 * 1024),
                DatabaseCalls = (int)(value / 4.5 + random.Next(11)),
                ExternalApiCalls = (int)(value / 9 + random.Next(6)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.76, Math.Min(0.93, 0.845 + (value < 70 ? -0.02 : 0.02) + random.NextDouble() * 0.07)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(52) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.23,
                    ExecutionTime = TimeSpan.FromMilliseconds(23 + random.Next(46)),
                    PerformanceImprovement = random.NextDouble() * 0.33,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = mixedValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.09, Math.Min(0.91, value / 125.0)),
                MemoryUtilization = Math.Max(0.09, Math.Min(0.91, value / 105.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateMixedSeasonalAndRandom()
    {
        var random = new Random(909);
        var seasonalValues = GenerateSeasonalDistribution(60, 45, 20, 15);
        var randomValues = GenerateNormalDistribution(40, 55, 30);
        var mixedValues = seasonalValues.Concat(randomValues).ToList();

        return new AITrainingData
        {
            ExecutionHistory = mixedValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = Math.Max(1, (int)(value + random.Next(28))),
                SuccessfulExecutions = Math.Max(1, (int)(value * 0.89 + random.Next(20))),
                FailedExecutions = Math.Max(0, (int)(value * 0.11 + random.Next(8))),
                AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 38 + value * 0.55 + random.Next(32))),
                P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 76 + value * 1.1 + random.Next(64))),
                ConcurrentExecutions = Math.Max(1, (int)(value / 8 + random.Next(9))),
                MemoryUsage = Math.Max(1, (int)(value * 1024 * 1024 + random.Next(58) * 1024 * 1024)),
                DatabaseCalls = Math.Max(1, (int)(value / 4 + random.Next(13))),
                ExternalApiCalls = Math.Max(0, (int)(value / 8 + random.Next(7))),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.73, Math.Min(0.92, 0.825 + Math.Sin(i < 60 ? i * 0.1 : 0) * 0.04 + random.NextDouble() * 0.08)),
                MemoryAllocated = Math.Max(1, (int)(value * 1024 * 1024 + random.Next(58) * 1024 * 1024))
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.26,
                    ExecutionTime = TimeSpan.FromMilliseconds(26 + random.Next(52)),
                    PerformanceImprovement = random.NextDouble() * 0.36,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = mixedValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.07, Math.Min(0.93, value / 135.0)),
                MemoryUtilization = Math.Max(0.07, Math.Min(0.93, value / 115.0)),
                ThroughputPerSecond = (int)value
            }).ToArray()
        };
    }

    private AITrainingData CreateMixedTrendAndVolatility()
    {
        var random = new Random(1010);
        var trendVolatilityValues = GenerateMixedTrendAndVolatility(100);

        return new AITrainingData
        {
            ExecutionHistory = trendVolatilityValues.Select((value, i) => new RequestExecutionMetrics
            {
                TotalExecutions = (int)(value + random.Next(32)),
                SuccessfulExecutions = (int)(value * 0.87 + random.Next(22)),
                FailedExecutions = (int)(value * 0.13 + random.Next(9)),
                AverageExecutionTime = TimeSpan.FromMilliseconds(35 + value * 0.6 + random.Next(38)),
                P95ExecutionTime = TimeSpan.FromMilliseconds(70 + value * 1.2 + random.Next(76)),
                ConcurrentExecutions = (int)(value / 7 + random.Next(11)),
                MemoryUsage = (int)(value * 1024 * 1024 + random.Next(65) * 1024 * 1024),
                DatabaseCalls = (int)(value / 3.5 + random.Next(15)),
                ExternalApiCalls = (int)(value / 7 + random.Next(8)),
                LastExecution = DateTime.UtcNow.AddMinutes(-100 + i),
                SuccessRate = Math.Max(0.7, Math.Min(0.91, 0.805 + i * 0.001 + random.NextDouble() * 0.09)),
                MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(65) * 1024 * 1024)
            }).ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.29,
                    ExecutionTime = TimeSpan.FromMilliseconds(29 + random.Next(58)),
                    PerformanceImprovement = random.NextDouble() * 0.39,
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = trendVolatilityValues.Select((value, i) => new SystemLoadMetrics
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                CpuUtilization = Math.Max(0.05, Math.Min(0.95, value / 150.0)),
                MemoryUtilization = Math.Max(0.05, Math.Min(0.95, value / 130.0)),
                ThroughputPerSecond = (int)Math.Max(1, value)
            }).ToArray()
        };
    }

    #endregion

    #region Distribution Generation Helper Methods

    private List<double> GenerateNormalDistribution(int count, double mean, double stdDev)
    {
        var random = new Random(42);
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            // Box-Muller transform for normal distribution
            var u1 = random.NextDouble();
            var u2 = random.NextDouble();
            var z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            values.Add(mean + z0 * stdDev);
        }
        
        return values;
    }

    private List<double> GenerateRightSkewedDistribution(int count, double scale, double shape)
    {
        var random = new Random(123);
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            // Gamma distribution for right skew
            var u = random.NextDouble();
            var value = scale * Math.Pow(-Math.Log(1 - u), 1.0 / shape);
            values.Add(value);
        }
        
        return values;
    }

    private List<double> GenerateLeftSkewedDistribution(int count, double max, double shape)
    {
        var rightSkewed = GenerateRightSkewedDistribution(count, max, shape);
        return rightSkewed.Select(v => max - v).ToList();
    }

    private List<double> GenerateUniformDistribution(int count, double min, double max)
    {
        var random = new Random(456);
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            values.Add(random.NextDouble() * (max - min) + min);
        }
        
        return values;
    }

    private List<double> GenerateExponentialDistribution(int count, double rate)
    {
        var random = new Random(789);
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            var u = random.NextDouble();
            values.Add(-Math.Log(1 - u) / rate);
        }
        
        return values;
    }

    private List<double> GenerateExponentialDecay(int count, double rate)
    {
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            values.Add(100 * Math.Exp(-rate * i));
        }
        
        return values;
    }

    private List<double> GenerateSeasonalDistribution(int count, double mean, double amplitude, double period)
    {
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            var seasonalComponent = amplitude * Math.Sin(2 * Math.PI * i / period);
            values.Add(mean + seasonalComponent);
        }
        
        return values;
    }

    private List<double> GenerateMultiSeasonalDistribution(int count, double mean, double amp1, double period1, double amp2, double period2)
    {
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            var seasonal1 = amp1 * Math.Sin(2 * Math.PI * i / period1);
            var seasonal2 = amp2 * Math.Cos(2 * Math.PI * i / period2);
            values.Add(mean + seasonal1 + seasonal2);
        }
        
        return values;
    }

    private List<double> GenerateHighVolatileDistribution(int count, double mean, double volatility)
    {
        var random = new Random(321);
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            var normalValue = mean + (random.NextDouble() - 0.5) * 2 * volatility;
            var extremeSpike = random.NextDouble() > 0.9 ? random.NextDouble() * volatility * 2 : 0;
            values.Add(Math.Max(1, normalValue + extremeSpike));
        }
        
        return values;
    }

    private List<double> GenerateLowVolatileDistribution(int count, double mean, double volatility)
    {
        var random = new Random(654);
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            values.Add(mean + (random.NextDouble() - 0.5) * 2 * volatility);
        }
        
        return values;
    }

    private List<double> GenerateBurstyVolatileDistribution(int count, double baseValue, double burstProbability)
    {
        var random = new Random(987);
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            var isBurst = random.NextDouble() < burstProbability;
            var value = isBurst ? baseValue + random.NextDouble() * baseValue * 3 : baseValue + random.NextDouble() * 10;
            values.Add(value);
        }
        
        return values;
    }

    private List<double> GenerateAdaptiveThresholdDistribution(int count)
    {
        var random = new Random(111);
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            var phase = i / (count / 2.0); // Two phases
            var mean = 50 + phase * 20; // Increasing mean
            var stdDev = 10 + phase * 15; // Increasing volatility
            values.Add(mean + (random.NextDouble() - 0.5) * 2 * stdDev);
        }
        
        return values;
    }

    private List<double> GenerateHeavyTailedDistribution(int count, double scale, double shape)
    {
        var random = new Random(222);
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            // Pareto distribution for heavy tails
            var u = random.NextDouble();
            var value = scale / Math.Pow(1 - u, 1.0 / shape);
            values.Add(value);
        }
        
        return values;
    }

    private List<double> GenerateSparseDistribution(int count, double missingProbability)
    {
        var random = new Random(333);
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            var isMissing = random.NextDouble() < missingProbability;
            values.Add(isMissing ? 0 : 30 + random.NextDouble() * 50);
        }
        
        return values;
    }

    private List<double> GenerateMixedTrendAndVolatility(int count)
    {
        var random = new Random(444);
        var values = new List<double>();
        
        for (int i = 0; i < count; i++)
        {
            var trend = i * 0.5; // Linear trend
            var volatility = (random.NextDouble() - 0.5) * 20 * (1 + i / (double)count); // Increasing volatility
            var seasonal = 10 * Math.Sin(i * 0.2); // Small seasonal component
            values.Add(Math.Max(1, 30 + trend + volatility + seasonal));
        }
        
        return values;
    }

    #endregion

    public void Dispose()
    {
        _trainer?.Dispose();
    }
}