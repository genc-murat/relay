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

public class DefaultAIModelTrainerOptimizationGainTests : IDisposable
{
    private readonly Mock<ILogger<DefaultAIModelTrainer>> _mockLogger;
    private readonly DefaultAIModelTrainer _trainer;

    public DefaultAIModelTrainerOptimizationGainTests()
    {
        _mockLogger = new Mock<ILogger<DefaultAIModelTrainer>>();
        _trainer = new DefaultAIModelTrainer(_mockLogger.Object);
    }

    #region Zero and Edge Case Metrics Tests

    [Fact]
    public async Task TrainModelAsync_WithZeroExecutionTime_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateZeroExecutionTimeData();

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
    public async Task TrainModelAsync_WithZeroP95ExecutionTime_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateZeroP95ExecutionTimeData();

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
    public async Task TrainModelAsync_WithZeroSuccessRate_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateZeroSuccessRateData();

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
    public async Task TrainModelAsync_WithPerfectSuccessRate_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreatePerfectSuccessRateData();

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
    public async Task TrainModelAsync_WithMinimalExecutionTime_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateMinimalExecutionTimeData();

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
    public async Task TrainModelAsync_WithMaximumExecutionTime_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateMaximumExecutionTimeData();

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

    #region Negative and Invalid Metrics Tests

    [Fact]
    public async Task TrainModelAsync_WithNegativeExecutionTime_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateNegativeExecutionTimeData();

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
    public async Task TrainModelAsync_WithNegativeSuccessRate_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateNegativeSuccessRateData();

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
    public async Task TrainModelAsync_WithInfiniteSuccessRate_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateInfiniteSuccessRateData();

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
    public async Task TrainModelAsync_WithNaNExecutionTime_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateNaNExecutionTimeData();

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
    public async Task TrainModelAsync_WithNaNSuccessRate_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateNaNSuccessRateData();

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

    #region Extreme Value Tests

    [Fact]
    public async Task TrainModelAsync_WithExtremelyHighExecutionTime_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateExtremelyHighExecutionTimeData();

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
    public async Task TrainModelAsync_WithExtremelyLowExecutionTime_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateExtremelyLowExecutionTimeData();

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
    public async Task TrainModelAsync_WithMicroSecondExecutionTime_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateMicroSecondExecutionTimeData();

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
    public async Task TrainModelAsync_WithHourExecutionTime_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateHourExecutionTimeData();

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

    #region Mixed Edge Cases Tests

    [Fact]
    public async Task TrainModelAsync_WithMixedEdgeCases_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateMixedEdgeCasesData();

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
    public async Task TrainModelAsync_WithAlternatingEdgeCases_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateAlternatingEdgeCasesData();

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
    public async Task TrainModelAsync_WithProgressiveDegradation_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateProgressiveDegradationData();

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
    public async Task TrainModelAsync_WithProgressiveImprovement_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateProgressiveImprovementData();

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

    #region Boundary Condition Tests

    [Fact]
    public async Task TrainModelAsync_WithBoundaryExecutionTime_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateBoundaryExecutionTimeData();

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
    public async Task TrainModelAsync_WithBoundarySuccessRate_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateBoundarySuccessRateData();

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
    public async Task TrainModelAsync_WithFloatingPointEdgeCases_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateFloatingPointEdgeCasesData();

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

    private AITrainingData CreateZeroExecutionTimeData()
    {
        var random = new Random(42);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.Zero, // Zero execution time
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = 0.9,
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.4,
                    MemoryUtilization = 0.5,
                    ThroughputPerSecond = 50
                })
                .ToArray()
        };
    }

    private AITrainingData CreateZeroP95ExecutionTimeData()
    {
        var random = new Random(123);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    P95ExecutionTime = TimeSpan.Zero, // Zero P95 execution time
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = 0.9,
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.4,
                    MemoryUtilization = 0.5,
                    ThroughputPerSecond = 50
                })
                .ToArray()
        };
    }

    private AITrainingData CreateZeroSuccessRateData()
    {
        var random = new Random(456);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 0, // Zero success rate
                    FailedExecutions = 100,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = 0.0, // Zero success rate
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = false, // All optimizations fail
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = 0,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.4,
                    MemoryUtilization = 0.5,
                    ThroughputPerSecond = 50
                })
                .ToArray()
        };
    }

    private AITrainingData CreatePerfectSuccessRateData()
    {
        var random = new Random(789);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 100, // Perfect success rate
                    FailedExecutions = 0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = 1.0, // Perfect success rate
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = true, // All optimizations succeed
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.5,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.4,
                    MemoryUtilization = 0.5,
                    ThroughputPerSecond = 50
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMinimalExecutionTimeData()
    {
        var random = new Random(321);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.FromTicks(1), // Minimal execution time
                    P95ExecutionTime = TimeSpan.FromTicks(1),
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = 0.9,
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.1, // High success rate
                    ExecutionTime = TimeSpan.FromTicks(1),
                    PerformanceImprovement = random.NextDouble() * 0.8,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.1,
                    MemoryUtilization = 0.2,
                    ThroughputPerSecond = 1000
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMaximumExecutionTimeData()
    {
        var random = new Random(654);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.FromDays(1), // Maximum execution time
                    P95ExecutionTime = TimeSpan.FromDays(1),
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = 0.9,
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.8, // Low success rate
                    ExecutionTime = TimeSpan.FromHours(1),
                    PerformanceImprovement = random.NextDouble() * 0.1,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.9,
                    MemoryUtilization = 0.95,
                    ThroughputPerSecond = 1
                })
                .ToArray()
        };
    }

    private AITrainingData CreateNegativeExecutionTimeData()
    {
        var random = new Random(987);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(-50), // Negative execution time
                    P95ExecutionTime = TimeSpan.FromMilliseconds(-100),
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = 0.9,
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.5,
                    ExecutionTime = TimeSpan.FromMilliseconds(-20),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.4,
                    MemoryUtilization = 0.5,
                    ThroughputPerSecond = 50
                })
                .ToArray()
        };
    }

    private AITrainingData CreateNegativeSuccessRateData()
    {
        var random = new Random(111);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = -0.5, // Negative success rate
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.7,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.2,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.4,
                    MemoryUtilization = 0.5,
                    ThroughputPerSecond = 50
                })
                .ToArray()
        };
    }

    private AITrainingData CreateInfiniteSuccessRateData()
    {
        var random = new Random(222);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 0, // Zero total executions
                    SuccessfulExecutions = 100, // But successful executions
                    FailedExecutions = 0,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = double.PositiveInfinity, // Infinite success rate
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.6,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.4,
                    MemoryUtilization = 0.5,
                    ThroughputPerSecond = 50
                })
                .ToArray()
        };
    }

    private AITrainingData CreateNaNExecutionTimeData()
    {
        var random = new Random(333);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.Zero, // Invalid execution time (zero duration)
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = 0.9,
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.3,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = double.NaN,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = double.NaN,
                    MemoryUtilization = 0.5,
                    ThroughputPerSecond = 50
                })
                .ToArray()
        };
    }

    private AITrainingData CreateNaNSuccessRateData()
    {
        var random = new Random(444);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = double.NaN, // NaN success rate
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.4,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.4,
                    MemoryUtilization = 0.5,
                    ThroughputPerSecond = 50
                })
                .ToArray()
        };
    }

    private AITrainingData CreateExtremelyHighExecutionTimeData()
    {
        var random = new Random(555);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(1000000), // 1,000 seconds
                    P95ExecutionTime = TimeSpan.FromMilliseconds(2000000), // 2,000 seconds
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = 0.9,
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.9, // Very low success rate
                    ExecutionTime = TimeSpan.FromMilliseconds(500000),
                    PerformanceImprovement = random.NextDouble() * 0.05,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.95,
                    MemoryUtilization = 0.98,
                    ThroughputPerSecond = 1
                })
                .ToArray()
        };
    }

    private AITrainingData CreateExtremelyLowExecutionTimeData()
    {
        var random = new Random(666);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(0.001), // 1 microsecond
                    P95ExecutionTime = TimeSpan.FromMilliseconds(0.002), // 2 microseconds
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = 0.9,
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.05, // Very high success rate
                    ExecutionTime = TimeSpan.FromMilliseconds(0.001),
                    PerformanceImprovement = random.NextDouble() * 0.9,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.05,
                    MemoryUtilization = 0.1,
                    ThroughputPerSecond = 10000
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMicroSecondExecutionTimeData()
    {
        var random = new Random(777);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.FromTicks(10), // ~1 microsecond
                    P95ExecutionTime = TimeSpan.FromTicks(20), // ~2 microseconds
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = 0.9,
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.1,
                    ExecutionTime = TimeSpan.FromTicks(10),
                    PerformanceImprovement = random.NextDouble() * 0.8,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.1,
                    MemoryUtilization = 0.2,
                    ThroughputPerSecond = 5000
                })
                .ToArray()
        };
    }

    private AITrainingData CreateHourExecutionTimeData()
    {
        var random = new Random(888);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    AverageExecutionTime = TimeSpan.FromHours(1), // 1 hour
                    P95ExecutionTime = TimeSpan.FromHours(2), // 2 hours
                    ConcurrentExecutions = 5,
                    MemoryUsage = 50 * 1024 * 1024,
                    DatabaseCalls = 10,
                    ExternalApiCalls = 5,
                    LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                    SuccessRate = 0.9,
                    MemoryAllocated = 50 * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.85,
                    ExecutionTime = TimeSpan.FromMinutes(30),
                    PerformanceImprovement = random.NextDouble() * 0.08,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.9,
                    MemoryUtilization = 0.95,
                    ThroughputPerSecond = 1
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMixedEdgeCasesData()
    {
        var random = new Random(999);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => 
                {
                    var edgeCase = i % 5;
                    return edgeCase switch
                    {
                        0 => new RequestExecutionMetrics
                        {
                            TotalExecutions = 100,
                            SuccessfulExecutions = 0,
                            FailedExecutions = 100,
                            AverageExecutionTime = TimeSpan.Zero,
                            P95ExecutionTime = TimeSpan.Zero,
                            ConcurrentExecutions = 0,
                            MemoryUsage = 0,
                            DatabaseCalls = 0,
                            ExternalApiCalls = 0,
                            LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                            SuccessRate = 0.0,
                            MemoryAllocated = 0
                        },
                        1 => new RequestExecutionMetrics
                        {
                            TotalExecutions = 100,
                            SuccessfulExecutions = 100,
                            FailedExecutions = 0,
                            AverageExecutionTime = TimeSpan.FromDays(1),
                            P95ExecutionTime = TimeSpan.FromDays(1),
                            ConcurrentExecutions = 100,
                            MemoryUsage = long.MaxValue,
                            DatabaseCalls = 1000,
                            ExternalApiCalls = 500,
                            LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                            SuccessRate = 1.0,
                            MemoryAllocated = long.MaxValue
                        },
                        2 => new RequestExecutionMetrics
                        {
                            TotalExecutions = 0,
                            SuccessfulExecutions = 0,
                            FailedExecutions = 0,
                            AverageExecutionTime = TimeSpan.FromMilliseconds(-100),
                            P95ExecutionTime = TimeSpan.FromMilliseconds(-200),
                            ConcurrentExecutions = -5,
                            MemoryUsage = -1000,
                            DatabaseCalls = -10,
                            ExternalApiCalls = -5,
                            LastExecution = DateTime.MinValue,
                            SuccessRate = double.NaN,
                            MemoryAllocated = -1000
                        },
                        3 => new RequestExecutionMetrics
                        {
                            TotalExecutions = 1,
                            SuccessfulExecutions = 1,
                            FailedExecutions = 0,
                            AverageExecutionTime = TimeSpan.FromTicks(1),
                            P95ExecutionTime = TimeSpan.FromTicks(1),
                            ConcurrentExecutions = 1,
                            MemoryUsage = 1,
                            DatabaseCalls = 1,
                            ExternalApiCalls = 1,
                            LastExecution = DateTime.UtcNow,
                            SuccessRate = 1.0,
                            MemoryAllocated = 1
                        },
                        _ => new RequestExecutionMetrics
                        {
                            TotalExecutions = 100,
                            SuccessfulExecutions = 90,
                            FailedExecutions = 10,
                            AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                            P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                            ConcurrentExecutions = 5,
                            MemoryUsage = 50 * 1024 * 1024,
                            DatabaseCalls = 10,
                            ExternalApiCalls = 5,
                            LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                            SuccessRate = 0.9,
                            MemoryAllocated = 50 * 1024 * 1024
                        }
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.5,
                    ExecutionTime = TimeSpan.FromMilliseconds(random.Next(-100, 1000)),
                    PerformanceImprovement = random.NextDouble() * 2 - 0.5, // Can be negative
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = Math.Max(-1, Math.Min(2, random.NextDouble() * 3 - 0.5)),
                    MemoryUtilization = Math.Max(-1, Math.Min(2, random.NextDouble() * 3 - 0.5)),
                    ThroughputPerSecond = random.Next(-100, 1000)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateAlternatingEdgeCasesData()
    {
        var random = new Random(1010);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => 
                {
                    var isEven = i % 2 == 0;
                    return isEven ? 
                        new RequestExecutionMetrics
                        {
                            TotalExecutions = 100,
                            SuccessfulExecutions = 100,
                            FailedExecutions = 0,
                            AverageExecutionTime = TimeSpan.FromTicks(1),
                            P95ExecutionTime = TimeSpan.FromTicks(1),
                            ConcurrentExecutions = 1,
                            MemoryUsage = 1,
                            DatabaseCalls = 1,
                            ExternalApiCalls = 1,
                            LastExecution = DateTime.UtcNow,
                            SuccessRate = 1.0,
                            MemoryAllocated = 1
                        } :
                        new RequestExecutionMetrics
                        {
                            TotalExecutions = 100,
                            SuccessfulExecutions = 0,
                            FailedExecutions = 100,
                            AverageExecutionTime = TimeSpan.FromDays(1),
                            P95ExecutionTime = TimeSpan.FromDays(1),
                            ConcurrentExecutions = 100,
                            MemoryUsage = long.MaxValue,
                            DatabaseCalls = 1000,
                            ExternalApiCalls = 500,
                            LastExecution = DateTime.UtcNow.AddDays(-1),
                            SuccessRate = 0.0,
                            MemoryAllocated = long.MaxValue
                        };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = i % 2 == 0,
                    ExecutionTime = i % 2 == 0 ? TimeSpan.FromMilliseconds(1) : TimeSpan.FromHours(1),
                    PerformanceImprovement = i % 2 == 0 ? 0.9 : 0.1,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = i % 2 == 0 ? 0.1 : 0.9,
                    MemoryUtilization = i % 2 == 0 ? 0.2 : 0.95,
                    ThroughputPerSecond = i % 2 == 0 ? 1000 : 1
                })
                .ToArray()
        };
    }

    private AITrainingData CreateProgressiveDegradationData()
    {
        var random = new Random(1111);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => 
                {
                    var degradationFactor = 1.0 + (i / 50.0) * 10; // Progressive degradation
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 100,
                        SuccessfulExecutions = Math.Max(0, (int)(90 - i * 1.5)),
                        FailedExecutions = Math.Min(100, (int)(10 + i * 1.5)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(50 * degradationFactor),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(100 * degradationFactor),
                        ConcurrentExecutions = (int)(5 + i * 0.5),
                        MemoryUsage = (int)((50 + i * 10) * 1024 * 1024),
                        DatabaseCalls = (int)(10 + i * 2),
                        ExternalApiCalls = (int)(5 + i),
                        LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                        SuccessRate = Math.Max(0, 0.9 - i * 0.015),
                        MemoryAllocated = (int)((50 + i * 10) * 1024 * 1024)
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > (0.2 + i * 0.02), // Decreasing success rate
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + i * 5),
                    PerformanceImprovement = Math.Max(0, random.NextDouble() * 0.3 - i * 0.01),
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = Math.Min(0.95, 0.4 + i * 0.01),
                    MemoryUtilization = Math.Min(0.95, 0.5 + i * 0.008),
                    ThroughputPerSecond = Math.Max(1, 50 - i)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateProgressiveImprovementData()
    {
        var random = new Random(1212);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => 
                {
                    var improvementFactor = Math.Max(0.1, 1.0 - (i / 50.0) * 0.8); // Progressive improvement
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 100,
                        SuccessfulExecutions = Math.Min(100, (int)(50 + i * 1)),
                        FailedExecutions = Math.Max(0, (int)(50 - i * 1)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(500 * improvementFactor),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(1000 * improvementFactor),
                        ConcurrentExecutions = Math.Max(1, (int)(20 - i * 0.3)),
                        MemoryUsage = (int)((200 - i * 3) * 1024 * 1024),
                        DatabaseCalls = Math.Max(1, (int)(50 - i)),
                        ExternalApiCalls = Math.Max(1, (int)(25 - i * 0.5)),
                        LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                        SuccessRate = Math.Min(1.0, 0.5 + i * 0.01),
                        MemoryAllocated = (int)((200 - i * 3) * 1024 * 1024)
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > (0.5 - i * 0.015), // Increasing success rate
                    ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 100 - i * 3)),
                    PerformanceImprovement = Math.Min(0.8, i * 0.02 + random.NextDouble() * 0.2),
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = Math.Max(0.1, 0.8 - i * 0.01),
                    MemoryUtilization = Math.Max(0.1, 0.85 - i * 0.008),
                    ThroughputPerSecond = Math.Min(200, 10 + i * 3)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateBoundaryExecutionTimeData()
    {
        var random = new Random(1313);
        var boundaryTimes = new[] { 1.0, 999.0, 1000.0, 1001.0, 0.1, 0.01, 0.001 };
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => 
                {
                    var timeValue = boundaryTimes[i % boundaryTimes.Length];
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 100,
                        SuccessfulExecutions = 90,
                        FailedExecutions = 10,
                        AverageExecutionTime = TimeSpan.FromMilliseconds(timeValue),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(timeValue * 2),
                        ConcurrentExecutions = 5,
                        MemoryUsage = 50 * 1024 * 1024,
                        DatabaseCalls = 10,
                        ExternalApiCalls = 5,
                        LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                        SuccessRate = 0.9,
                        MemoryAllocated = 50 * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.3,
                    ExecutionTime = TimeSpan.FromMilliseconds(boundaryTimes[i % boundaryTimes.Length]),
                    PerformanceImprovement = random.NextDouble() * 0.4,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.4,
                    MemoryUtilization = 0.5,
                    ThroughputPerSecond = 50
                })
                .ToArray()
        };
    }

    private AITrainingData CreateBoundarySuccessRateData()
    {
        var random = new Random(1414);
        var boundaryRates = new[] { 0.0, 0.001, 0.01, 0.1, 0.5, 0.9, 0.99, 0.999, 1.0 };
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => 
                {
                    var successRate = boundaryRates[i % boundaryRates.Length];
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 100,
                        SuccessfulExecutions = (int)(100 * successRate),
                        FailedExecutions = (int)(100 * (1 - successRate)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                        ConcurrentExecutions = 5,
                        MemoryUsage = 50 * 1024 * 1024,
                        DatabaseCalls = 10,
                        ExternalApiCalls = 5,
                        LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                        SuccessRate = successRate,
                        MemoryAllocated = 50 * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > (1 - boundaryRates[i % boundaryRates.Length]),
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = boundaryRates[i % boundaryRates.Length] * random.NextDouble() * 0.5,
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.4,
                    MemoryUtilization = 0.5,
                    ThroughputPerSecond = 50
                })
                .ToArray()
        };
    }

    private AITrainingData CreateFloatingPointEdgeCasesData()
    {
        var random = new Random(1515);
        var edgeValues = new[] { double.Epsilon, double.MinValue, double.MaxValue, double.PositiveInfinity, double.NegativeInfinity, double.NaN };
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => 
                {
                    var edgeValue = edgeValues[i % edgeValues.Length];
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 100,
                        SuccessfulExecutions = 90,
                        FailedExecutions = 10,
                        AverageExecutionTime = TimeSpan.FromMilliseconds(double.IsNaN(edgeValue) || double.IsInfinity(edgeValue) ? 50 : Math.Max(1, Math.Abs(edgeValue % 1000))),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(double.IsNaN(edgeValue) || double.IsInfinity(edgeValue) ? 100 : Math.Max(1, Math.Abs(edgeValue % 2000))),
                        ConcurrentExecutions = 5,
                        MemoryUsage = 50 * 1024 * 1024,
                        DatabaseCalls = 10,
                        ExternalApiCalls = 5,
                        LastExecution = DateTime.UtcNow.AddMinutes(-50 + i),
                        SuccessRate = double.IsNaN(edgeValue) || double.IsInfinity(edgeValue) ? 0.9 : Math.Max(0, Math.Min(1, edgeValue)),
                        MemoryAllocated = 50 * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.3,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = double.IsNaN(edgeValues[i % edgeValues.Length]) ? 0.3 : Math.Max(0, Math.Min(1, edgeValues[i % edgeValues.Length] % 1.0)),
                    Timestamp = DateTime.UtcNow.AddMinutes(-25 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.4,
                    MemoryUtilization = 0.5,
                    ThroughputPerSecond = 50
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