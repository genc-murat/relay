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

public class DefaultAIModelTrainerCancellationTests : IDisposable
{
    private readonly Mock<ILogger<DefaultAIModelTrainer>> _mockLogger;
    private readonly DefaultAIModelTrainer _trainer;

    public DefaultAIModelTrainerCancellationTests()
    {
        _mockLogger = new Mock<ILogger<DefaultAIModelTrainer>>();
        _trainer = new DefaultAIModelTrainer(_mockLogger.Object);
    }

    [Fact]
    public async Task TrainModelAsync_WithImmediateCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _trainer.TrainModelAsync(trainingData, cts.Token).AsTask());
    }

    [Fact]
    public async Task TrainModelAsync_WithCancellationDuringValidation_ThrowsOperationCanceledException()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        var cts = new CancellationTokenSource();

        // Act & Assert
        var task = _trainer.TrainModelAsync(trainingData, cts.Token);
        
        // Cancel after a short delay to hit validation phase
        await Task.Delay(10);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
    }

    [Fact]
    public async Task TrainModelAsync_WithCancellationDuringPerformanceTraining_ThrowsOperationCanceledException()
    {
        // Arrange
        var trainingData = CreateLargeTrainingData(); // Large data to extend training time
        var cts = new CancellationTokenSource();
        var progressReports = new List<TrainingProgress>();

        // Act
        var task = _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
            
            // Cancel when we reach performance models phase
            if (progress.Phase == TrainingPhase.PerformanceModels)
            {
                cts.Cancel();
            }
        }, cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
        
        // Should have reached performance models phase
        Assert.Contains(progressReports, p => p.Phase == TrainingPhase.PerformanceModels);
    }

    [Fact]
    public async Task TrainModelAsync_WithCancellationDuringOptimizationClassifiers_ThrowsOperationCanceledException()
    {
        // Arrange
        var trainingData = CreateLargeTrainingData();
        var cts = new CancellationTokenSource();
        var progressReports = new List<TrainingProgress>();

        // Act
        var task = _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
            
            // Cancel when we reach optimization classifiers phase
            if (progress.Phase == TrainingPhase.OptimizationClassifiers)
            {
                cts.Cancel();
            }
        }, cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
        
        // Should have reached optimization classifiers phase
        Assert.Contains(progressReports, p => p.Phase == TrainingPhase.OptimizationClassifiers);
    }

    [Fact]
    public async Task TrainModelAsync_WithCancellationDuringAnomalyDetection_ThrowsOperationCanceledException()
    {
        // Arrange
        var trainingData = CreateLargeTrainingData();
        var cts = new CancellationTokenSource();
        var progressReports = new List<TrainingProgress>();

        // Act
        var task = _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
            
            // Cancel when we reach anomaly detection phase
            if (progress.Phase == TrainingPhase.AnomalyDetection)
            {
                cts.Cancel();
            }
        }, cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
        
        // Should have reached anomaly detection phase
        Assert.Contains(progressReports, p => p.Phase == TrainingPhase.AnomalyDetection);
    }

    [Fact]
    public async Task TrainModelAsync_WithCancellationDuringForecasting_ThrowsOperationCanceledException()
    {
        // Arrange
        var trainingData = CreateLargeTrainingData();
        var cts = new CancellationTokenSource();
        var progressReports = new List<TrainingProgress>();

        // Act
        var task = _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
            
            // Cancel when we reach forecasting phase
            if (progress.Phase == TrainingPhase.Forecasting)
            {
                cts.Cancel();
            }
        }, cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
        
        // Should have reached forecasting phase
        Assert.Contains(progressReports, p => p.Phase == TrainingPhase.Forecasting);
    }

    [Fact]
    public async Task TrainModelAsync_WithCancellationDuringStatistics_ThrowsOperationCanceledException()
    {
        // Arrange
        var trainingData = CreateLargeTrainingData();
        var cts = new CancellationTokenSource();
        var progressReports = new List<TrainingProgress>();

        // Act
        var task = _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
            
            // Cancel when we reach statistics phase
            if (progress.Phase == TrainingPhase.Statistics)
            {
                cts.Cancel();
            }
        }, cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
        
        // Should have reached statistics phase
        Assert.Contains(progressReports, p => p.Phase == TrainingPhase.Statistics);
    }

    [Fact]
    public async Task TrainModelAsync_WithCancellationAfterCompletion_CompletesSuccessfully()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        var cts = new CancellationTokenSource();

        // Act
        var task = _trainer.TrainModelAsync(trainingData, cts.Token);
        await task; // Wait for completion
        
        // Cancel after completion
        cts.Cancel();

        // Assert - Task should complete successfully
        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task TrainModelAsync_WithMultipleCancellationTokens_RespectsFirstCancellation()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        var cts1 = new CancellationTokenSource();
        var cts2 = new CancellationTokenSource();
        var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cts1.Token, cts2.Token);

        // Act
        var task = _trainer.TrainModelAsync(trainingData, combinedCts.Token);
        
        // Cancel first token
        cts1.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
    }

    [Fact]
    public async Task TrainModelAsync_WithPreCanceledToken_ThrowsImmediately()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var startTime = DateTime.UtcNow;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _trainer.TrainModelAsync(trainingData, cts.Token).AsTask());
        
        // Should throw quickly (within 100ms)
        var elapsed = DateTime.UtcNow - startTime;
        Assert.True(elapsed.TotalMilliseconds < 100, $"Cancellation took {elapsed.TotalMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    public async Task TrainModelAsync_WithCancellationAndProgressCallback_HandlesGracefully()
    {
        // Arrange
        var trainingData = CreateLargeTrainingData();
        var cts = new CancellationTokenSource();
        var progressReports = new List<TrainingProgress>();
        var callbackExceptionThrown = false;

        // Act
        var task = _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
            
            // Cancel and throw exception in callback
            if (progress.Phase == TrainingPhase.OptimizationClassifiers && !callbackExceptionThrown)
            {
                callbackExceptionThrown = true;
                cts.Cancel();
                throw new InvalidOperationException("Callback exception during cancellation");
            }
        }, cts.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
        
        // Should have made progress despite callback exception
        Assert.True(progressReports.Count > 0);
        Assert.Contains(progressReports, p => p.Phase == TrainingPhase.OptimizationClassifiers);
    }

    [Fact]
    public async Task TrainModelAsync_WithCancellationTokenNone_CompletesSuccessfully()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData, CancellationToken.None);

        // Assert - Should complete without throwing
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
    public async Task TrainModelAsync_WithCancellationInDifferentPhases_CancelsAtCorrectPhase()
    {
        // Arrange
        var trainingData = CreateLargeTrainingData();
        var phases = new List<TrainingPhase>();
        
        for (int i = 0; i < 6; i++) // Test each phase
        {
            var cts = new CancellationTokenSource();
            var progressReports = new List<TrainingProgress>();
            var targetPhase = (TrainingPhase)i;

            // Act
            var task = _trainer.TrainModelAsync(trainingData, progress =>
            {
                progressReports.Add(progress);
                
                if (progress.Phase == targetPhase)
                {
                    cts.Cancel();
                }
            }, cts.Token);

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
            phases.Add(targetPhase);
        }

        // Should have tested all phases except Completed
        Assert.Contains(TrainingPhase.Validation, phases);
        Assert.Contains(TrainingPhase.PerformanceModels, phases);
        Assert.Contains(TrainingPhase.OptimizationClassifiers, phases);
        Assert.Contains(TrainingPhase.AnomalyDetection, phases);
        Assert.Contains(TrainingPhase.Forecasting, phases);
        Assert.Contains(TrainingPhase.Statistics, phases);
    }

    private AITrainingData CreateValidTrainingData()
    {
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 15)
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

            OptimizationHistory = Enumerable.Range(0, 10)
                .Select(_ => new AIOptimizationResult
                {
                    Strategy = OptimizationStrategy.EnableCaching,
                    Success = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(50),
                    PerformanceImprovement = 0.2,
                    Timestamp = DateTime.UtcNow
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
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
            ExecutionHistory = Enumerable.Range(0, 100) // Larger dataset
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

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(_ => new AIOptimizationResult
                {
                    Strategy = OptimizationStrategy.EnableCaching,
                    Success = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(50),
                    PerformanceImprovement = 0.2,
                    Timestamp = DateTime.UtcNow
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 200)
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
        _trainer?.Dispose();
    }
}