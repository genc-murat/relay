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

public class DefaultAIModelTrainerPartialDataTests : IDisposable
{
    private readonly Mock<ILogger<DefaultAIModelTrainer>> _mockLogger;
    private readonly DefaultAIModelTrainer _trainer;

    public DefaultAIModelTrainerPartialDataTests()
    {
        _mockLogger = new Mock<ILogger<DefaultAIModelTrainer>>();
        _trainer = new DefaultAIModelTrainer(_mockLogger.Object);
    }

    [Fact]
    public async Task TrainModelAsync_WithNullExecutionHistory_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = null,
            OptimizationHistory = CreateValidOptimizationHistory(),
            SystemLoadHistory = CreateValidSystemLoadHistory()
        };

        // Act & Assert - Should throw validation exception and log warning
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _trainer.TrainModelAsync(trainingData).AsTask());
        
        Assert.Contains("validation failed", exception.Message);
        Assert.Contains("Insufficient execution samples: 0", exception.Message);
        
        // Should log warning about validation failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Training data validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithNullOptimizationHistory_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = CreateValidExecutionHistory(),
            OptimizationHistory = null,
            SystemLoadHistory = CreateValidSystemLoadHistory()
        };

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should log warning about insufficient samples
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithNullSystemLoadHistory_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = CreateValidExecutionHistory(),
            OptimizationHistory = CreateValidOptimizationHistory(),
            SystemLoadHistory = null
        };

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should log warning about insufficient samples
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithEmptyExecutionHistory_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = Array.Empty<RequestExecutionMetrics>(),
            OptimizationHistory = CreateValidOptimizationHistory(),
            SystemLoadHistory = CreateValidSystemLoadHistory()
        };

        // Act & Assert - Should throw validation exception and log warning
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _trainer.TrainModelAsync(trainingData).AsTask());
        
        Assert.Contains("validation failed", exception.Message);
        Assert.Contains("Insufficient execution samples: 0", exception.Message);
        
        // Should log warning about validation failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Training data validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithEmptyOptimizationHistory_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = CreateValidExecutionHistory(),
            OptimizationHistory = Array.Empty<AIOptimizationResult>(),
            SystemLoadHistory = CreateValidSystemLoadHistory()
        };

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should log warning about insufficient samples
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithEmptySystemLoadHistory_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = CreateValidExecutionHistory(),
            OptimizationHistory = CreateValidOptimizationHistory(),
            SystemLoadHistory = Array.Empty<SystemLoadMetrics>()
        };

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should log warning about insufficient samples
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithOnlyExecutionHistory_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = CreateValidExecutionHistory(),
            OptimizationHistory = null,
            SystemLoadHistory = null
        };

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should log warning about insufficient samples
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithOnlyOptimizationHistory_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = null,
            OptimizationHistory = CreateValidOptimizationHistory(),
            SystemLoadHistory = null
        };

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should log warning about insufficient samples
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithOnlySystemLoadHistory_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = null,
            OptimizationHistory = null,
            SystemLoadHistory = CreateValidSystemLoadHistory()
        };

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should log warning about insufficient samples
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithInsufficientExecutionData_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 5) // Less than minimum (10)
                .Select(_ => CreateSampleExecutionMetrics())
                .ToArray(),
            OptimizationHistory = CreateValidOptimizationHistory(),
            SystemLoadHistory = CreateValidSystemLoadHistory()
        };

        // Act & Assert - Should throw validation exception and log warning
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _trainer.TrainModelAsync(trainingData).AsTask());
        
        Assert.Contains("validation failed", exception.Message);
        Assert.Contains("Insufficient execution samples: 5", exception.Message);
        
        // Should log warning about validation failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Training data validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithInsufficientOptimizationData_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = CreateValidExecutionHistory(),
            OptimizationHistory = Enumerable.Range(0, 3) // Less than minimum (5)
                .Select(_ => CreateSampleOptimizationResult())
                .ToArray(),
            SystemLoadHistory = CreateValidSystemLoadHistory()
        };

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should log warning about insufficient optimization samples
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithInsufficientSystemLoadData_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = CreateValidExecutionHistory(),
            OptimizationHistory = CreateValidOptimizationHistory(),
            SystemLoadHistory = Enumerable.Range(0, 5) // Less than minimum (10)
                .Select(i => CreateSampleSystemLoadMetrics(i))
                .ToArray()
        };

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should log warning about insufficient system load samples
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithMixedNullAndValidData_HandlesGracefully()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = CreateValidExecutionHistory(),
            OptimizationHistory = null, // Null
            SystemLoadHistory = CreateValidSystemLoadHistory()
        };

        // Act & Assert - Should not throw
        await _trainer.TrainModelAsync(trainingData);

        // Should log warning about missing optimization data
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithMixedEmptyAndValidData_HandlesGracefully()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = CreateValidExecutionHistory(),
            OptimizationHistory = Array.Empty<AIOptimizationResult>(), // Empty
            SystemLoadHistory = CreateValidSystemLoadHistory()
        };

        // Act & Assert - Should not throw
        await _trainer.TrainModelAsync(trainingData);

        // Should log warning about missing optimization data
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithAllNullData_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = null,
            OptimizationHistory = null,
            SystemLoadHistory = null
        };

        // Act & Assert - Should throw validation exception and log warning
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _trainer.TrainModelAsync(trainingData).AsTask());
        
        Assert.Contains("validation failed", exception.Message);
        Assert.Contains("Insufficient execution samples: 0", exception.Message);
        Assert.Contains("Insufficient optimization samples: 0", exception.Message);
        Assert.Contains("Insufficient system load samples: 0", exception.Message);
        
        // Should log warning about validation failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Training data validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithAllEmptyData_LogsWarning()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = Array.Empty<RequestExecutionMetrics>(),
            OptimizationHistory = Array.Empty<AIOptimizationResult>(),
            SystemLoadHistory = Array.Empty<SystemLoadMetrics>()
        };

        // Act & Assert - Should throw validation exception and log warning
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _trainer.TrainModelAsync(trainingData).AsTask());
        
        Assert.Contains("validation failed", exception.Message);
        Assert.Contains("Insufficient execution samples: 0", exception.Message);
        Assert.Contains("Insufficient optimization samples: 0", exception.Message);
        Assert.Contains("Insufficient system load samples: 0", exception.Message);
        
        // Should log warning about validation failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Training data validation failed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_WithPartialDataAndProgressCallback_ReportsProgress()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = CreateValidExecutionHistory(),
            OptimizationHistory = null, // Missing
            SystemLoadHistory = CreateValidSystemLoadHistory()
        };
        var progressReports = new List<TrainingProgress>();

        // Act
        await _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
        });

        // Assert - Should still report progress even with partial data
        Assert.NotEmpty(progressReports);
        Assert.Contains(progressReports, p => p.Phase == TrainingPhase.Validation);
        Assert.Contains(progressReports, p => p.Phase == TrainingPhase.Completed);
    }

    private RequestExecutionMetrics[] CreateValidExecutionHistory()
    {
        return Enumerable.Range(0, 15)
            .Select(_ => CreateSampleExecutionMetrics())
            .ToArray();
    }

    private AIOptimizationResult[] CreateValidOptimizationHistory()
    {
        return Enumerable.Range(0, 10)
            .Select(_ => CreateSampleOptimizationResult())
            .ToArray();
    }

    private SystemLoadMetrics[] CreateValidSystemLoadHistory()
    {
        return Enumerable.Range(0, 50)
            .Select(i => CreateSampleSystemLoadMetrics(i))
            .ToArray();
    }

    private RequestExecutionMetrics CreateSampleExecutionMetrics()
    {
        var random = new Random();
        var total = random.Next(100, 1000);
        var successful = (long)(total * (0.9 + random.NextDouble() * 0.1));

        return new RequestExecutionMetrics
        {
            TotalExecutions = total,
            SuccessfulExecutions = successful,
            FailedExecutions = total - successful,
            SuccessRate = (double)successful / total,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(200)),
            P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(300)),
            ConcurrentExecutions = random.Next(1, 10),
            MemoryUsage = random.Next(10, 100) * 1024 * 1024,
            DatabaseCalls = random.Next(0, 10),
            ExternalApiCalls = random.Next(0, 5),
            LastExecution = DateTime.UtcNow
        };
    }

    private AIOptimizationResult CreateSampleOptimizationResult()
    {
        var random = new Random();
        return new AIOptimizationResult
        {
            Strategy = OptimizationStrategy.EnableCaching,
            Success = random.NextDouble() > 0.2, // 80% success rate
            ExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(200)),
            PerformanceImprovement = random.NextDouble() * 0.3, // 0-30% improvement
            Timestamp = DateTime.UtcNow
        };
    }

    private SystemLoadMetrics CreateSampleSystemLoadMetrics(int index)
    {
        var random = new Random(index); // Use index as seed for consistent data
        return new SystemLoadMetrics
        {
            Timestamp = DateTime.UtcNow.AddMinutes(-50 + index),
            CpuUtilization = 0.3 + random.NextDouble() * 0.5,
            MemoryUtilization = 0.4 + random.NextDouble() * 0.4,
            ThroughputPerSecond = 100 + random.Next(200)
        };
    }

    public void Dispose()
    {
        _trainer?.Dispose();
    }
}