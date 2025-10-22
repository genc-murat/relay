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

public class DefaultAIModelTrainerTests : IDisposable
{
    private readonly Mock<ILogger<DefaultAIModelTrainer>> _mockLogger;
    private readonly DefaultAIModelTrainer _trainer;

    public DefaultAIModelTrainerTests()
    {
        _mockLogger = new Mock<ILogger<DefaultAIModelTrainer>>();
        _trainer = new DefaultAIModelTrainer(_mockLogger.Object);
    }

    [Fact]
    public async Task TrainModelAsync_WithValidData_Succeeds()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

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
    public async Task TrainModelAsync_WithNullData_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _trainer.TrainModelAsync(null!).AsTask());
    }

    [Fact]
    public async Task TrainModelAsync_WithInsufficientData_LogsWarning()
    {
        // Arrange - Create data with insufficient samples
        var trainingData = new AITrainingData
        {
            ExecutionHistory = new[] { CreateSampleExecutionMetrics() }, // Only 1 sample (need 10)
            OptimizationHistory = Array.Empty<AIOptimizationResult>(),
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
    public async Task TrainModelAsync_WithProgressCallback_ReportsProgress()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        var progressReports = new List<TrainingProgress>();

        // Act
        await _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
        });

        // Assert
        Assert.NotEmpty(progressReports);

        // Should have reports for all phases
        var phases = progressReports.Select(p => p.Phase).Distinct().ToList();
        Assert.Contains(TrainingPhase.Validation, phases);
        Assert.Contains(TrainingPhase.PerformanceModels, phases);
        Assert.Contains(TrainingPhase.OptimizationClassifiers, phases);
        Assert.Contains(TrainingPhase.AnomalyDetection, phases);
        Assert.Contains(TrainingPhase.Forecasting, phases);
        Assert.Contains(TrainingPhase.Statistics, phases);
        Assert.Contains(TrainingPhase.Completed, phases);
    }

    [Fact]
    public async Task TrainModelAsync_WithProgressCallback_ReportsIncreasingProgress()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        var progressReports = new List<TrainingProgress>();

        // Act
        await _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
        });

        // Assert
        Assert.NotEmpty(progressReports);

        // Progress should generally increase (allowing for same values at phase boundaries)
        for (int i = 1; i < progressReports.Count; i++)
        {
            Assert.True(progressReports[i].ProgressPercentage >= progressReports[i - 1].ProgressPercentage,
                $"Progress decreased from {progressReports[i - 1].ProgressPercentage}% to {progressReports[i].ProgressPercentage}%");
        }

        // Final progress should be 100%
        Assert.Equal(100, progressReports.Last().ProgressPercentage);
        Assert.Equal(TrainingPhase.Completed, progressReports.Last().Phase);
    }

    [Fact]
    public async Task TrainModelAsync_WithProgressCallback_ReportsElapsedTime()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        var progressReports = new List<TrainingProgress>();

        // Act
        await _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
        });

        // Assert
        Assert.NotEmpty(progressReports);

        // Elapsed time should increase
        for (int i = 1; i < progressReports.Count; i++)
        {
            Assert.True(progressReports[i].ElapsedTime >= progressReports[i - 1].ElapsedTime);
        }

        // Final elapsed time should be positive
        Assert.True(progressReports.Last().ElapsedTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task TrainModelAsync_WithProgressCallback_ReportsSampleCounts()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        var progressReports = new List<TrainingProgress>();

        // Act
        await _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
        });

        // Assert
        Assert.NotEmpty(progressReports);

        var expectedTotalSamples = trainingData.ExecutionHistory!.Length +
                                  trainingData.OptimizationHistory!.Length +
                                  trainingData.SystemLoadHistory!.Length;

        // All reports should have the same total samples
        Assert.All(progressReports, p => Assert.Equal(expectedTotalSamples, p.TotalSamples));

        // Samples processed should increase with progress
        var completedReport = progressReports.Last();
        Assert.Equal(expectedTotalSamples, completedReport.SamplesProcessed);
    }

    [Fact]
    public async Task TrainModelAsync_WithProgressCallback_ReportsStatusMessages()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        var statusMessages = new List<string>();

        // Act
        await _trainer.TrainModelAsync(trainingData, progress =>
        {
            statusMessages.Add(progress.StatusMessage);
        });

        // Assert
        Assert.NotEmpty(statusMessages);
        Assert.All(statusMessages, msg => Assert.False(string.IsNullOrWhiteSpace(msg)));

        // Should contain expected messages
        Assert.Contains(statusMessages, msg => msg.Contains("Validating"));
        Assert.Contains(statusMessages, msg => msg.Contains("performance"));
        Assert.Contains(statusMessages, msg => msg.Contains("completed"));
    }

    [Fact]
    public async Task TrainModelAsync_WithProgressCallback_Reports_CurrentMetrics()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();
        var progressReports = new List<TrainingProgress>();

        // Act
        await _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
        });

        // Assert
        Assert.NotEmpty(progressReports);

        // Check that metrics are reported for relevant phases
        var performancePhaseReport = progressReports.FirstOrDefault(p => p.Phase == TrainingPhase.PerformanceModels);
        Assert.NotNull(performancePhaseReport);
        Assert.NotNull(performancePhaseReport.CurrentMetrics);
        Assert.NotNull(performancePhaseReport.CurrentMetrics.RSquared);
        Assert.NotNull(performancePhaseReport.CurrentMetrics.MAE);
        Assert.NotNull(performancePhaseReport.CurrentMetrics.RMSE);

        var classifierPhaseReport = progressReports.FirstOrDefault(p => p.Phase == TrainingPhase.OptimizationClassifiers);
        Assert.NotNull(classifierPhaseReport);
        Assert.NotNull(classifierPhaseReport.CurrentMetrics);
        Assert.NotNull(classifierPhaseReport.CurrentMetrics.Accuracy);
        Assert.NotNull(classifierPhaseReport.CurrentMetrics.AUC);
        Assert.NotNull(classifierPhaseReport.CurrentMetrics.F1Score);

        // Validation and Completed phases should not have metrics
        var validationReport = progressReports.FirstOrDefault(p => p.Phase == TrainingPhase.Validation);
        Assert.NotNull(validationReport);
        Assert.Null(validationReport.CurrentMetrics);

        var completedReport = progressReports.FirstOrDefault(p => p.Phase == TrainingPhase.Completed);
        Assert.NotNull(completedReport);
        Assert.Null(completedReport.CurrentMetrics);
    }

    [Fact]
    public async Task TrainModelAsync_WithCancellationToken_CanBeCancelled()
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
    public async Task TrainModelAsync_WithoutProgressCallback_StillSucceeds()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData, progressCallback: null);

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
    public async Task TrainModelAsync_WithValidationFailure_ReportsCompletedWithError()
    {
        // Arrange
        var trainingData = new AITrainingData
        {
            ExecutionHistory = new[] { CreateSampleExecutionMetrics() },
            OptimizationHistory = Array.Empty<AIOptimizationResult>(),
            SystemLoadHistory = Array.Empty<SystemLoadMetrics>()
        };

        var progressReports = new List<TrainingProgress>();

        // Act
        await _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
        });

        // Assert
        var finalReport = progressReports.Last();
        Assert.Equal(TrainingPhase.Completed, finalReport.Phase);
        Assert.Equal(100, finalReport.ProgressPercentage);
        Assert.Contains("failed", finalReport.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TrainModelAsync_MultipleInvocations_EachSucceeds()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();

        // Act - Train multiple times
        await _trainer.TrainModelAsync(trainingData);
        await _trainer.TrainModelAsync(trainingData);
        await _trainer.TrainModelAsync(trainingData);

        // Assert - All should succeed
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("completed successfully")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task TrainModelAsync_LogsSessionId()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should log with session ID
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("session #")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task TrainModelAsync_LogsSampleCounts()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should log sample counts
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) =>
                    o.ToString()!.Contains("execution samples") &&
                    o.ToString()!.Contains("optimization samples") &&
                    o.ToString()!.Contains("system load samples")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TrainModelAsync_LogsQualityScore()
    {
        // Arrange
        var trainingData = CreateValidTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should log quality score
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("quality score")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Helper methods
    private AITrainingData CreateValidTrainingData()
    {
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 15)
                .Select(_ => CreateSampleExecutionMetrics())
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 10)
                .Select(_ => CreateSampleOptimizationResult())
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 50)
                .Select(i => CreateSampleSystemLoadMetrics(i))
                .ToArray()
        };
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
