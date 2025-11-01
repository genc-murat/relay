using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Training;

public class DefaultAIModelTrainerDisposalTests : IDisposable
{
    private readonly Mock<ILogger<DefaultAIModelTrainer>> _mockLogger;

    public DefaultAIModelTrainerDisposalTests()
    {
        _mockLogger = new Mock<ILogger<DefaultAIModelTrainer>>();
    }

    [Fact]
    public void Dispose_CalledOnce_LogsDisposalMessage()
    {
        // Arrange
        var trainer = new DefaultAIModelTrainer(_mockLogger.Object);

        // Act
        trainer.Dispose();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("disposed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_LogsDisposalMessageOnce()
    {
        // Arrange
        var trainer = new DefaultAIModelTrainer(_mockLogger.Object);

        // Act
        trainer.Dispose();
        trainer.Dispose();
        trainer.Dispose();

        // Assert - Should only log once
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("disposed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_AfterTraining_LogsSessionCount()
    {
        // Arrange
        var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
        var trainingData = CreateValidTrainingData();

        // Act
        trainer.TrainModelAsync(trainingData).GetAwaiter().GetResult();
        trainer.Dispose();

        // Assert - Should log session count
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Total training sessions: 1")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_AfterMultipleTrainingSessions_LogsCorrectSessionCount()
    {
        // Arrange
        var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
        var trainingData = CreateValidTrainingData();

        // Act
        trainer.TrainModelAsync(trainingData).GetAwaiter().GetResult();
        trainer.TrainModelAsync(trainingData).GetAwaiter().GetResult();
        trainer.TrainModelAsync(trainingData).GetAwaiter().GetResult();
        trainer.Dispose();

        // Assert - Should log correct session count
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Total training sessions: 3")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_WithoutTraining_LogsZeroSessions()
    {
        // Arrange
        var trainer = new DefaultAIModelTrainer(_mockLogger.Object);

        // Act
        trainer.Dispose();

        // Assert - Should log zero sessions
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Total training sessions: 0")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
        var trainingData = CreateValidTrainingData();
        trainer.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            trainer.TrainModelAsync(trainingData).AsTask());
    }

    [Fact]
    public async Task TrainModelAsync_WithProgressCallbackAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
        var trainingData = CreateValidTrainingData();
        trainer.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            trainer.TrainModelAsync(trainingData, progress => { }).AsTask());
    }

    [Fact]
    public async Task TrainModelAsync_WithCancellationTokenAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
        var trainingData = CreateValidTrainingData();
        trainer.Dispose();
        var cts = new CancellationTokenSource();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            trainer.TrainModelAsync(trainingData, cts.Token).AsTask());
    }

    [Fact]
    public async Task TrainModelAsync_WithProgressCallbackAndCancellationTokenAfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
        var trainingData = CreateValidTrainingData();
        trainer.Dispose();
        var cts = new CancellationTokenSource();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            trainer.TrainModelAsync(trainingData, progress => { }, cts.Token).AsTask());
    }

    [Fact]
    public void Dispose_CalledFromDifferentThreads_HandlesGracefully()
    {
        // Arrange
        var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
        var tasks = new Task[10];
        var exceptions = new Exception[10];

        // Act
        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    trainer.Dispose();
                }
                catch (Exception ex)
                {
                    exceptions[index] = ex;
                }
            });
        }

        Task.WaitAll(tasks);

        // Assert - Should not throw any exceptions
        Assert.All(exceptions, ex => Assert.Null(ex));

        // Should only log disposal message once
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("disposed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Dispose_CalledWhileTrainingInProgress_HandlesGracefully()
    {
        // Arrange
        var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
        var trainingData = CreateLargeTrainingData(); // Large dataset for longer training
        var trainingTask = trainer.TrainModelAsync(trainingData);

        // Act - Dispose while training is in progress
        await Task.Delay(10); // Let training start
        trainer.Dispose();

        // Assert - Should handle gracefully
        // Note: The actual behavior depends on implementation
        // This test ensures no unhandled exceptions occur
        try
        {
            await trainingTask;
        }
        catch (ObjectDisposedException)
        {
            // Expected if trainer checks disposed state
        }
        catch (OperationCanceledException)
        {
            // Expected if cancellation is triggered
        }
    }

    [Fact]
    public void Dispose_WithUsingStatement_CallsDispose()
    {
        // Arrange & Act
        using (var trainer = new DefaultAIModelTrainer(_mockLogger.Object))
        {
            // Use trainer
        }

        // Assert - Dispose should be called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("disposed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Dispose_WithMultipleUsingStatements_CallsDisposeEachTime()
    {
        // Arrange & Act
        using (var trainer1 = new DefaultAIModelTrainer(_mockLogger.Object))
        {
            // Use trainer1
        }

        using (var trainer2 = new DefaultAIModelTrainer(_mockLogger.Object))
        {
            // Use trainer2
        }

        // Assert - Dispose should be called for each instance
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("disposed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public void Dispose_AfterFailedTraining_LogsDisposalMessage()
    {
        // Arrange
        var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
        var invalidTrainingData = new AITrainingData
        {
            ExecutionHistory = Array.Empty<RequestExecutionMetrics>(),
            OptimizationHistory = Array.Empty<AIOptimizationResult>(),
            SystemLoadHistory = Array.Empty<SystemLoadMetrics>()
        };

        // Act - Training should fail, but disposal should still work
        Assert.Throws<ArgumentException>(() => 
            trainer.TrainModelAsync(invalidTrainingData).GetAwaiter().GetResult());
        
        trainer.Dispose();

        // Assert - Should still log disposal message even after failed training
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("disposed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private AITrainingData CreateValidTrainingData()
    {
        return new AITrainingData
        {
            ExecutionHistory = System.Linq.Enumerable.Range(0, 15)
                .Select(_ => new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    SuccessRate = 0.9,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    ConcurrentExecutions = 5,
                    MemoryUsage = 1024 * 1024,
                    DatabaseCalls = 5,
                    ExternalApiCalls = 2,
                    LastExecution = DateTime.UtcNow
                })
                .ToArray(),

            OptimizationHistory = System.Linq.Enumerable.Range(0, 10)
                .Select(_ => new AIOptimizationResult
                {
                    Strategy = OptimizationStrategy.EnableCaching,
                    Success = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(50),
                    PerformanceImprovement = 0.2,
                    Timestamp = DateTime.UtcNow
                })
                .ToArray(),

            SystemLoadHistory = System.Linq.Enumerable.Range(0, 50)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-50 + i),
                    CpuUtilization = 0.5,
                    MemoryUtilization = 0.6,
                    ThroughputPerSecond = 100
                })
                .ToArray()
        };
    }

    private AITrainingData CreateLargeTrainingData()
    {
        return new AITrainingData
        {
            ExecutionHistory = System.Linq.Enumerable.Range(0, 100)
                .Select(_ => new RequestExecutionMetrics
                {
                    TotalExecutions = 1000,
                    SuccessfulExecutions = 900,
                    FailedExecutions = 100,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100),
                    ConcurrentExecutions = 5,
                    MemoryUsage = 1024 * 1024,
                    DatabaseCalls = 5,
                    ExternalApiCalls = 2,
                    LastExecution = DateTime.UtcNow
                })
                .ToArray(),

            OptimizationHistory = System.Linq.Enumerable.Range(0, 50)
                .Select(_ => new AIOptimizationResult
                {
                    Strategy = OptimizationStrategy.EnableCaching,
                    Success = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(50),
                    PerformanceImprovement = 0.2,
                    Timestamp = DateTime.UtcNow
                })
                .ToArray(),

            SystemLoadHistory = System.Linq.Enumerable.Range(0, 200)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-200 + i),
                    CpuUtilization = 0.5,
                    MemoryUtilization = 0.6,
                    ThroughputPerSecond = 100
                })
                .ToArray()
        };
    }

    public void Dispose()
    {
        // Test class disposal handled by individual test methods
    }
}