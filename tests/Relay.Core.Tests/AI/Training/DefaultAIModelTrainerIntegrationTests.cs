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

public class DefaultAIModelTrainerIntegrationTests : IDisposable
{
    private readonly Mock<ILogger<DefaultAIModelTrainer>> _mockLogger;
    private readonly DefaultAIModelTrainer _trainer;

    public DefaultAIModelTrainerIntegrationTests()
    {
        _mockLogger = new Mock<ILogger<DefaultAIModelTrainer>>();
        _trainer = new DefaultAIModelTrainer(_mockLogger.Object);
    }

    [Fact]
    public async Task TrainModelAsync_WithRealWorldScenario_CompletesSuccessfully()
    {
        // Arrange - Simulate real-world training data
        var trainingData = CreateRealWorldTrainingData();

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
    public async Task TrainModelAsync_WithHighVolumeData_HandlesEfficiently()
    {
        // Arrange - High volume scenario
        var trainingData = CreateHighVolumeTrainingData();

        var startTime = DateTime.UtcNow;

        // Act
        await _trainer.TrainModelAsync(trainingData);

        var duration = DateTime.UtcNow - startTime;

        // Assert - Should complete within reasonable time
        Assert.True(duration.TotalSeconds < 30, $"Training took {duration.TotalSeconds}s, expected < 30s");

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
    public async Task TrainModelAsync_WithVariableLoadPatterns_ProcessesCorrectly()
    {
        // Arrange - Variable load patterns throughout the day
        var trainingData = CreateVariableLoadTrainingData();

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
    public async Task TrainModelAsync_WithSeasonalData_HandlesSeasonality()
    {
        // Arrange - Seasonal patterns (e.g., holiday traffic)
        var trainingData = CreateSeasonalTrainingData();

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
    public async Task TrainModelAsync_WithMixedOptimizationStrategies_LearnsFromAll()
    {
        // Arrange - Multiple optimization strategies applied
        var trainingData = CreateMixedStrategyTrainingData();

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
    public async Task TrainModelAsync_WithProgressiveImprovement_TracksProgress()
    {
        // Arrange - Data showing progressive improvement over time
        var trainingData = CreateProgressiveImprovementTrainingData();

        var progressReports = new List<TrainingProgress>();

        // Act
        await _trainer.TrainModelAsync(trainingData, progress =>
        {
            progressReports.Add(progress);
        });

        // Assert
        Assert.NotEmpty(progressReports);
        Assert.Contains(progressReports, p => p.Phase == TrainingPhase.Completed);
        Assert.Equal(100, progressReports.Last().ProgressPercentage);
    }

    [Fact]
    public async Task TrainModelAsync_WithFailureScenarios_LearnsFromFailures()
    {
        // Arrange - Data including failed optimizations
        var trainingData = CreateFailureScenarioTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData);

        // Assert - Should still complete successfully despite failures
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
    public async Task TrainModelAsync_WithResourceConstraints_HandlesGracefully()
    {
        // Arrange - Simulate resource-constrained environment
        var trainingData = CreateResourceConstrainedTrainingData();

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
    public async Task TrainModelAsync_WithConcurrentLoad_HandlesConcurrency()
    {
        // Arrange - Data from concurrent request scenarios
        var trainingData = CreateConcurrentLoadTrainingData();

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
    public async Task TrainModelAsync_WithBurstTraffic_HandlesSpikes()
    {
        // Arrange - Data with traffic spikes
        var trainingData = CreateBurstTrafficTrainingData();

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
    public async Task TrainModelAsync_WithGradualGrowth_AdaptsToGrowth()
    {
        // Arrange - Data showing gradual system growth
        var trainingData = CreateGradualGrowthTrainingData();

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
    public async Task TrainModelAsync_WithMultipleTrainingSessions_ImprovesOverTime()
    {
        // Arrange - Multiple training sessions with accumulated data
        var trainingData1 = CreateInitialTrainingData();
        var trainingData2 = CreateAccumulatedTrainingData();
        var trainingData3 = CreateMatureTrainingData();

        // Act
        await _trainer.TrainModelAsync(trainingData1);
        await _trainer.TrainModelAsync(trainingData2);
        await _trainer.TrainModelAsync(trainingData3);

        // Assert - All sessions should complete successfully
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
    public async Task TrainModelAsync_WithRealTimeData_ProcessesStreamingData()
    {
        // Arrange - Simulate real-time data stream
        var trainingData = CreateRealTimeTrainingData();

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
    public async Task TrainModelAsync_WithEdgeCaseScenarios_HandlesRobustly()
    {
        // Arrange - Edge case real-world scenarios
        var trainingData = CreateEdgeCaseTrainingData();

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

    private AITrainingData CreateRealWorldTrainingData()
    {
        var random = new Random(42); // Fixed seed for reproducible tests
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 100)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 1000 + random.Next(500),
                    SuccessfulExecutions = 900 + random.Next(100),
                    FailedExecutions = 50 + random.Next(50),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(150)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(200)),
                    ConcurrentExecutions = 5 + random.Next(15),
                    MemoryUsage = (50 + random.Next(100)) * 1024 * 1024,
                    DatabaseCalls = 10 + random.Next(40),
                    ExternalApiCalls = 5 + random.Next(20),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(60)),
                    SuccessRate = 0.85 + random.NextDouble() * 0.1,
                    MemoryAllocated = (50 + random.Next(100)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.3,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(100)),
                    PerformanceImprovement = random.NextDouble() * 0.4,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(24))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 200)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-200 + i),
                    CpuUtilization = 0.2 + random.NextDouble() * 0.6,
                    MemoryUtilization = 0.3 + random.NextDouble() * 0.5,
                    ThroughputPerSecond = 50 + random.Next(200)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateHighVolumeTrainingData()
    {
        var random = new Random(123);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 500) // High volume
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 5000 + random.Next(2000),
                    SuccessfulExecutions = 4500 + random.Next(400),
                    FailedExecutions = 200 + random.Next(100),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(30 + random.Next(100)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(150)),
                    ConcurrentExecutions = 20 + random.Next(30),
                    MemoryUsage = (100 + random.Next(200)) * 1024 * 1024,
                    DatabaseCalls = 50 + random.Next(100),
                    ExternalApiCalls = 20 + random.Next(50),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(60)),
                    SuccessRate = 0.9 + random.NextDouble() * 0.08,
                    MemoryAllocated = (100 + random.Next(200)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 200)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(10 + random.Next(80)),
                    PerformanceImprovement = random.NextDouble() * 0.5,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(48))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 1000)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-1000 + i),
                    CpuUtilization = 0.3 + random.NextDouble() * 0.5,
                    MemoryUtilization = 0.4 + random.NextDouble() * 0.4,
                    ThroughputPerSecond = 100 + random.Next(400)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateVariableLoadTrainingData()
    {
        var random = new Random(456);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 144) // 24 hours of data (6-minute intervals)
                .Select(i => 
                {
                    var hourOfDay = i / 6;
                    var loadMultiplier = GetLoadMultiplier(hourOfDay);
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(100 * loadMultiplier) + random.Next(50),
                        SuccessfulExecutions = (int)(90 * loadMultiplier) + random.Next(20),
                        FailedExecutions = (int)(10 * loadMultiplier) + random.Next(10),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(100)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(200)),
                        ConcurrentExecutions = (int)(5 * loadMultiplier) + random.Next(5),
                        MemoryUsage = (int)(50 * loadMultiplier + random.Next(50)) * 1024 * 1024,
                        DatabaseCalls = (int)(10 * loadMultiplier) + random.Next(20),
                        ExternalApiCalls = (int)(5 * loadMultiplier) + random.Next(10),
                        LastExecution = DateTime.UtcNow.AddHours(-24).AddMinutes(i * 6),
                        SuccessRate = 0.85 + random.NextDouble() * 0.1,
                        MemoryAllocated = (int)(50 * loadMultiplier + random.Next(50)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 72)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.25,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(80)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddHours(-24).AddMinutes(i * 20)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 288) // 24 hours of data (5-minute intervals)
                .Select(i => 
                {
                    var hourOfDay = i / 12;
                    var loadMultiplier = GetLoadMultiplier(hourOfDay);
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddHours(-24).AddMinutes(i * 5),
                        CpuUtilization = (0.2 + random.NextDouble() * 0.3) * loadMultiplier,
                        MemoryUtilization = (0.3 + random.NextDouble() * 0.3) * loadMultiplier,
                        ThroughputPerSecond = (int)(50 * loadMultiplier) + random.Next(100)
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateSeasonalTrainingData()
    {
        var random = new Random(789);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 365) // Daily data for a year
                .Select(i => 
                {
                    var dayOfYear = i;
                    var seasonalMultiplier = GetSeasonalMultiplier(dayOfYear);
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(1000 * seasonalMultiplier) + random.Next(200),
                        SuccessfulExecutions = (int)(900 * seasonalMultiplier) + random.Next(100),
                        FailedExecutions = (int)(100 * seasonalMultiplier) + random.Next(50),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(80)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(160)),
                        ConcurrentExecutions = (int)(10 * seasonalMultiplier) + random.Next(10),
                        MemoryUsage = (int)(100 * seasonalMultiplier + random.Next(100)) * 1024 * 1024,
                        DatabaseCalls = (int)(20 * seasonalMultiplier) + random.Next(30),
                        ExternalApiCalls = (int)(10 * seasonalMultiplier) + random.Next(15),
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
                    ExecutionTime = TimeSpan.FromMilliseconds(15 + random.Next(60)),
                    PerformanceImprovement = random.NextDouble() * 0.35,
                    Timestamp = DateTime.UtcNow.AddDays(-180 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 730) // 2 data points per day for a year
                .Select(i => 
                {
                    var dayOfYear = i / 2;
                    var seasonalMultiplier = GetSeasonalMultiplier(dayOfYear);
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-365 + dayOfYear).AddHours(i % 2 * 12),
                        CpuUtilization = (0.25 + random.NextDouble() * 0.35) * seasonalMultiplier,
                        MemoryUtilization = (0.35 + random.NextDouble() * 0.35) * seasonalMultiplier,
                        ThroughputPerSecond = (int)(100 * seasonalMultiplier) + random.Next(150)
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMixedStrategyTrainingData()
    {
        var random = new Random(321);
        var strategies = Enum.GetValues<OptimizationStrategy>().Where(s => s != OptimizationStrategy.None).ToArray();
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 200)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 800 + random.Next(400),
                    SuccessfulExecutions = 720 + random.Next(200),
                    FailedExecutions = 80 + random.Next(100),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(45 + random.Next(120)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(90 + random.Next(180)),
                    ConcurrentExecutions = 8 + random.Next(12),
                    MemoryUsage = (80 + random.Next(120)) * 1024 * 1024,
                    DatabaseCalls = 15 + random.Next(35),
                    ExternalApiCalls = 8 + random.Next(25),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(120)),
                    SuccessRate = 0.86 + random.NextDouble() * 0.1,
                    MemoryAllocated = (80 + random.Next(120)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 100)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = strategies[random.Next(strategies.Length)],
                    Success = random.NextDouble() > 0.22,
                    ExecutionTime = TimeSpan.FromMilliseconds(18 + random.Next(70)),
                    PerformanceImprovement = random.NextDouble() * 0.38,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(72))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 400)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-400 + i),
                    CpuUtilization = 0.28 + random.NextDouble() * 0.52,
                    MemoryUtilization = 0.38 + random.NextDouble() * 0.42,
                    ThroughputPerSecond = 80 + random.Next(320)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateProgressiveImprovementTrainingData()
    {
        var random = new Random(654);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 150)
                .Select(i => 
                {
                    var improvementFactor = 1.0 - (i * 0.003); // Gradual improvement
                    var baseTime = 100 * improvementFactor;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 1000 + random.Next(200),
                        SuccessfulExecutions = 950 + (int)(i * 0.5) + random.Next(50),
                        FailedExecutions = 50 - (int)(i * 0.2) + random.Next(20),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(baseTime + random.Next(50)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(baseTime * 2 + random.Next(100)),
                        ConcurrentExecutions = 10 + random.Next(10),
                        MemoryUsage = (100 - (int)(i * 0.3) + random.Next(50)) * 1024 * 1024,
                        DatabaseCalls = 25 - (int)(i * 0.1) + random.Next(15),
                        ExternalApiCalls = 12 - (int)(i * 0.05) + random.Next(8),
                        LastExecution = DateTime.UtcNow.AddMinutes(-150 + i),
                        SuccessRate = Math.Min(0.99, 0.85 + (i * 0.001) + random.NextDouble() * 0.05),
                        MemoryAllocated = (100 - (int)(i * 0.3) + random.Next(50)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 75)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = OptimizationStrategy.EnableCaching,
                    Success = random.NextDouble() > (0.3 - i * 0.002), // Improving success rate
                    ExecutionTime = TimeSpan.FromMilliseconds(30 - (i * 0.1) + random.Next(30)),
                    PerformanceImprovement = (0.1 + i * 0.002) * random.NextDouble(),
                    Timestamp = DateTime.UtcNow.AddHours(-75 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 300)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-300 + i),
                    CpuUtilization = Math.Max(0.2, 0.6 - (i * 0.001) + random.NextDouble() * 0.2),
                    MemoryUtilization = Math.Max(0.3, 0.7 - (i * 0.001) + random.NextDouble() * 0.2),
                    ThroughputPerSecond = 150 + (int)(i * 0.5) + random.Next(100)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateFailureScenarioTrainingData()
    {
        var random = new Random(987);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 120)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 800 + random.Next(400),
                    SuccessfulExecutions = 600 + random.Next(200), // Lower success rate
                    FailedExecutions = 200 + random.Next(100), // Higher failure rate
                    AverageExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(120)), // Slower
                    P95ExecutionTime = TimeSpan.FromMilliseconds(160 + random.Next(200)),
                    ConcurrentExecutions = 15 + random.Next(15),
                    MemoryUsage = (120 + random.Next(100)) * 1024 * 1024,
                    DatabaseCalls = 30 + random.Next(40),
                    ExternalApiCalls = 15 + random.Next(25),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(120)),
                    SuccessRate = 0.6 + random.NextDouble() * 0.2, // Lower success rate
                    MemoryAllocated = (120 + random.Next(100)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 60)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.5, // 50% failure rate
                    ExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(100)),
                    PerformanceImprovement = random.NextDouble() > 0.5 ? random.NextDouble() * 0.2 : -random.NextDouble() * 0.1,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(60))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 240)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-240 + i),
                    CpuUtilization = 0.5 + random.NextDouble() * 0.4, // Higher CPU
                    MemoryUtilization = 0.6 + random.NextDouble() * 0.3, // Higher memory
                    ThroughputPerSecond = 60 + random.Next(80) // Lower throughput
                })
                .ToArray()
        };
    }

    private AITrainingData CreateResourceConstrainedTrainingData()
    {
        var random = new Random(111);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 80)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 200 + random.Next(100), // Lower volume
                    SuccessfulExecutions = 180 + random.Next(50),
                    FailedExecutions = 20 + random.Next(30),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(120 + random.Next(80)), // Slower due to constraints
                    P95ExecutionTime = TimeSpan.FromMilliseconds(240 + random.Next(120)),
                    ConcurrentExecutions = 2 + random.Next(3), // Limited concurrency
                    MemoryUsage = (30 + random.Next(20)) * 1024 * 1024, // Limited memory
                    DatabaseCalls = 5 + random.Next(10), // Fewer DB calls
                    ExternalApiCalls = 2 + random.Next(5), // Fewer API calls
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(80)),
                    SuccessRate = 0.85 + random.NextDouble() * 0.1,
                    MemoryAllocated = (30 + random.Next(20)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 40)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = OptimizationStrategy.MemoryPooling, // Focus on memory optimization
                    Success = random.NextDouble() > 0.3,
                    ExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(60)),
                    PerformanceImprovement = random.NextDouble() * 0.25,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(40))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 160)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-160 + i),
                    CpuUtilization = 0.7 + random.NextDouble() * 0.25, // High CPU utilization
                    MemoryUtilization = 0.8 + random.NextDouble() * 0.15, // High memory utilization
                    ThroughputPerSecond = 20 + random.Next(40) // Limited throughput
                })
                .ToArray()
        };
    }

    private AITrainingData CreateConcurrentLoadTrainingData()
    {
        var random = new Random(222);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 100)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 1500 + random.Next(500),
                    SuccessfulExecutions = 1350 + random.Next(300),
                    FailedExecutions = 150 + random.Next(100),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(60 + random.Next(90)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(120 + random.Next(150)),
                    ConcurrentExecutions = 25 + random.Next(25), // High concurrency
                    MemoryUsage = (150 + random.Next(100)) * 1024 * 1024,
                    DatabaseCalls = 40 + random.Next(60),
                    ExternalApiCalls = 20 + random.Next(30),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(100)),
                    SuccessRate = 0.88 + random.NextDouble() * 0.08,
                    MemoryAllocated = (150 + random.Next(100)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = OptimizationStrategy.BatchProcessing, // Focus on batching for concurrency
                    Success = random.NextDouble() > 0.25,
                    ExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(50)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(50))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 200)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-200 + i),
                    CpuUtilization = 0.4 + random.NextDouble() * 0.4,
                    MemoryUtilization = 0.5 + random.NextDouble() * 0.3,
                    ThroughputPerSecond = 200 + random.Next(200) // High throughput
                })
                .ToArray()
        };
    }

    private AITrainingData CreateBurstTrafficTrainingData()
    {
        var random = new Random(333);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 168) // Weekly data with hourly bursts
                .Select(i => 
                {
                    var hourOfWeek = i;
                    var burstMultiplier = GetBurstMultiplier(hourOfWeek);
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(500 * burstMultiplier) + random.Next(200),
                        SuccessfulExecutions = (int)(450 * burstMultiplier) + random.Next(100),
                        FailedExecutions = (int)(50 * burstMultiplier) + random.Next(50),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(80)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(120)),
                        ConcurrentExecutions = (int)(10 * burstMultiplier) + random.Next(10),
                        MemoryUsage = (int)(80 * burstMultiplier + random.Next(80)) * 1024 * 1024,
                        DatabaseCalls = (int)(15 * burstMultiplier) + random.Next(20),
                        ExternalApiCalls = (int)(8 * burstMultiplier) + random.Next(12),
                        LastExecution = DateTime.UtcNow.AddHours(-168 + i),
                        SuccessRate = 0.87 + random.NextDouble() * 0.08,
                        MemoryAllocated = (int)(80 * burstMultiplier + random.Next(80)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 84)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = OptimizationStrategy.CircuitBreaker, // Good for burst handling
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(60)),
                    PerformanceImprovement = random.NextDouble() * 0.35,
                    Timestamp = DateTime.UtcNow.AddHours(-84 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 336) // 30-minute intervals
                .Select(i => 
                {
                    var hourOfWeek = i / 2;
                    var burstMultiplier = GetBurstMultiplier(hourOfWeek);
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddHours(-168 + hourOfWeek).AddMinutes((i % 2) * 30),
                        CpuUtilization = (0.3 + random.NextDouble() * 0.4) * burstMultiplier,
                        MemoryUtilization = (0.4 + random.NextDouble() * 0.3) * burstMultiplier,
                        ThroughputPerSecond = (int)(100 * burstMultiplier) + random.Next(150)
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateGradualGrowthTrainingData()
    {
        var random = new Random(444);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 180) // 6 months of daily data
                .Select(i => 
                {
                    var growthFactor = 1.0 + (i * 0.01); // 1% growth per day
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(500 * growthFactor) + random.Next(100),
                        SuccessfulExecutions = (int)(450 * growthFactor) + random.Next(80),
                        FailedExecutions = (int)(50 * growthFactor) + random.Next(30),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(50)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(80)),
                        ConcurrentExecutions = (int)(5 * growthFactor) + random.Next(5),
                        MemoryUsage = (int)(50 * growthFactor + random.Next(50)) * 1024 * 1024,
                        DatabaseCalls = (int)(10 * growthFactor) + random.Next(15),
                        ExternalApiCalls = (int)(5 * growthFactor) + random.Next(8),
                        LastExecution = DateTime.UtcNow.AddDays(-180 + i),
                        SuccessRate = 0.9 + random.NextDouble() * 0.05,
                        MemoryAllocated = (int)(50 * growthFactor + random.Next(50)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 90)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddDays(-90 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 360) // 2 data points per day
                .Select(i => 
                {
                    var dayIndex = i / 2;
                    var growthFactor = 1.0 + (dayIndex * 0.01);
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-180 + dayIndex).AddHours((i % 2) * 12),
                        CpuUtilization = (0.3 + random.NextDouble() * 0.3) * Math.Min(1.0, growthFactor),
                        MemoryUtilization = (0.4 + random.NextDouble() * 0.3) * Math.Min(1.0, growthFactor),
                        ThroughputPerSecond = (int)(50 * growthFactor) + random.Next(100)
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateInitialTrainingData()
    {
        var random = new Random(555);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 30)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 200 + random.Next(100),
                    SuccessfulExecutions = 180 + random.Next(50),
                    FailedExecutions = 20 + random.Next(30),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(100)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(160 + random.Next(150)),
                    ConcurrentExecutions = 3 + random.Next(5),
                    MemoryUsage = (40 + random.Next(40)) * 1024 * 1024,
                    DatabaseCalls = 8 + random.Next(12),
                    ExternalApiCalls = 4 + random.Next(8),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(30)),
                    SuccessRate = 0.85 + random.NextDouble() * 0.1,
                    MemoryAllocated = (40 + random.Next(40)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 15)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = OptimizationStrategy.EnableCaching,
                    Success = random.NextDouble() > 0.3,
                    ExecutionTime = TimeSpan.FromMilliseconds(30 + random.Next(50)),
                    PerformanceImprovement = random.NextDouble() * 0.2,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(15))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 60)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-60 + i),
                    CpuUtilization = 0.4 + random.NextDouble() * 0.3,
                    MemoryUtilization = 0.5 + random.NextDouble() * 0.3,
                    ThroughputPerSecond = 30 + random.Next(50)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateAccumulatedTrainingData()
    {
        var random = new Random(666);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 60)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 400 + random.Next(200),
                    SuccessfulExecutions = 360 + random.Next(100),
                    FailedExecutions = 40 + random.Next(50),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(60 + random.Next(80)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(120 + random.Next(120)),
                    ConcurrentExecutions = 6 + random.Next(8),
                    MemoryUsage = (60 + random.Next(60)) * 1024 * 1024,
                    DatabaseCalls = 12 + random.Next(20),
                    ExternalApiCalls = 6 + random.Next(12),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(60)),
                    SuccessRate = 0.88 + random.NextDouble() * 0.08,
                    MemoryAllocated = (60 + random.Next(60)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 30)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.25,
                    ExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(45)),
                    PerformanceImprovement = random.NextDouble() * 0.25,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(30))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 120)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-120 + i),
                    CpuUtilization = 0.35 + random.NextDouble() * 0.35,
                    MemoryUtilization = 0.45 + random.NextDouble() * 0.35,
                    ThroughputPerSecond = 50 + random.Next(80)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMatureTrainingData()
    {
        var random = new Random(777);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 100)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 800 + random.Next(400),
                    SuccessfulExecutions = 760 + random.Next(200),
                    FailedExecutions = 40 + random.Next(40),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(60)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(100)),
                    ConcurrentExecutions = 12 + random.Next(12),
                    MemoryUsage = (100 + random.Next(80)) * 1024 * 1024,
                    DatabaseCalls = 20 + random.Next(30),
                    ExternalApiCalls = 10 + random.Next(18),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(100)),
                    SuccessRate = 0.92 + random.NextDouble() * 0.05,
                    MemoryAllocated = (100 + random.Next(80)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(50))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 200)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-200 + i),
                    CpuUtilization = 0.3 + random.NextDouble() * 0.4,
                    MemoryUtilization = 0.4 + random.NextDouble() * 0.4,
                    ThroughputPerSecond = 100 + random.Next(150)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateRealTimeTrainingData()
    {
        var random = new Random(888);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 144) // 6-minute intervals for 24 hours
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 50 + random.Next(50),
                    SuccessfulExecutions = 45 + random.Next(30),
                    FailedExecutions = 5 + random.Next(15),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(30 + random.Next(40)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(60 + random.Next(60)),
                    ConcurrentExecutions = 2 + random.Next(4),
                    MemoryUsage = (20 + random.Next(30)) * 1024 * 1024,
                    DatabaseCalls = 3 + random.Next(7),
                    ExternalApiCalls = 2 + random.Next(4),
                    LastExecution = DateTime.UtcNow.AddMinutes(-144 * 6 + i * 6),
                    SuccessRate = 0.9 + random.NextDouble() * 0.05,
                    MemoryAllocated = (20 + random.Next(30)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 72)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(15 + random.Next(25)),
                    PerformanceImprovement = random.NextDouble() * 0.2,
                    Timestamp = DateTime.UtcNow.AddMinutes(-72 * 20 + i * 20)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 288) // 5-minute intervals
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-288 * 5 + i * 5),
                    CpuUtilization = 0.2 + random.NextDouble() * 0.3,
                    MemoryUtilization = 0.3 + random.NextDouble() * 0.3,
                    ThroughputPerSecond = 20 + random.Next(40)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateEdgeCaseTrainingData()
    {
        var random = new Random(999);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 50)
                .Select(i => 
                {
                    // Mix of normal and edge cases
                    if (i % 10 == 0)
                    {
                        // Edge case: Very high execution time
                        return new RequestExecutionMetrics
                        {
                            TotalExecutions = 100,
                            SuccessfulExecutions = 95,
                            FailedExecutions = 5,
                            AverageExecutionTime = TimeSpan.FromMilliseconds(1000), // Very slow
                            P95ExecutionTime = TimeSpan.FromMilliseconds(2000),
                            ConcurrentExecutions = 1,
                            MemoryUsage = 10 * 1024 * 1024,
                            DatabaseCalls = 1,
                            ExternalApiCalls = 0,
                            LastExecution = DateTime.UtcNow.AddMinutes(-i),
                            SuccessRate = 0.95,
                            MemoryAllocated = 10 * 1024 * 1024
                        };
                    }
                    else if (i % 7 == 0)
                    {
                        // Edge case: Very high concurrency
                        return new RequestExecutionMetrics
                        {
                            TotalExecutions = 5000,
                            SuccessfulExecutions = 4900,
                            FailedExecutions = 100,
                            AverageExecutionTime = TimeSpan.FromMilliseconds(20),
                            P95ExecutionTime = TimeSpan.FromMilliseconds(40),
                            ConcurrentExecutions = 100, // Very high
                            MemoryUsage = 500 * 1024 * 1024,
                            DatabaseCalls = 100,
                            ExternalApiCalls = 50,
                            LastExecution = DateTime.UtcNow.AddMinutes(-i),
                            SuccessRate = 0.98,
                            MemoryAllocated = 500 * 1024 * 1024
                        };
                    }
                    else
                    {
                        // Normal case
                        return new RequestExecutionMetrics
                        {
                            TotalExecutions = 200 + random.Next(300),
                            SuccessfulExecutions = 180 + random.Next(200),
                            FailedExecutions = 20 + random.Next(50),
                            AverageExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(60)),
                            P95ExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(80)),
                            ConcurrentExecutions = 5 + random.Next(10),
                            MemoryUsage = (50 + random.Next(100)) * 1024 * 1024,
                            DatabaseCalls = 10 + random.Next(20),
                            ExternalApiCalls = 5 + random.Next(10),
                            LastExecution = DateTime.UtcNow.AddMinutes(-i),
                            SuccessRate = 0.85 + random.NextDouble() * 0.1,
                            MemoryAllocated = (50 + random.Next(100)) * 1024 * 1024
                        };
                    }
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 25)
                .Select(i => 
                {
                    if (i % 5 == 0)
                    {
                        // Edge case: Failed optimization
                        return new AIOptimizationResult
                        {
                            Strategy = OptimizationStrategy.EnableCaching,
                            Success = false,
                            ExecutionTime = TimeSpan.FromMinutes(1), // Very slow
                            PerformanceImprovement = -0.5, // Negative improvement
                            Timestamp = DateTime.UtcNow.AddHours(-i)
                        };
                    }
                    else
                    {
                        // Normal case
                        return new AIOptimizationResult
                        {
                            Strategy = (OptimizationStrategy)random.Next(1, 8),
                            Success = random.NextDouble() > 0.2,
                            ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                            PerformanceImprovement = random.NextDouble() * 0.3,
                            Timestamp = DateTime.UtcNow.AddHours(-i)
                        };
                    }
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 100)
                .Select(i => 
                {
                    if (i % 20 == 0)
                    {
                        // Edge case: System overload
                        return new SystemLoadMetrics
                        {
                            Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                            CpuUtilization = 0.95, // Near max
                            MemoryUtilization = 0.98, // Near max
                            ThroughputPerSecond = 10 // Very low throughput
                        };
                    }
                    else if (i % 15 == 0)
                    {
                        // Edge case: System idle
                        return new SystemLoadMetrics
                        {
                            Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                            CpuUtilization = 0.05, // Very low
                            MemoryUtilization = 0.1, // Very low
                            ThroughputPerSecond = 5 // Very low
                        };
                    }
                    else
                    {
                        // Normal case
                        return new SystemLoadMetrics
                        {
                            Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                            CpuUtilization = 0.3 + random.NextDouble() * 0.4,
                            MemoryUtilization = 0.4 + random.NextDouble() * 0.3,
                            ThroughputPerSecond = 50 + random.Next(100)
                        };
                    }
                })
                .ToArray()
        };
    }

    private static double GetLoadMultiplier(int hourOfDay)
    {
        // Simulate daily load patterns
        return hourOfDay switch
        {
            >= 0 and < 6 => 0.3,  // Night
            >= 6 and < 9 => 0.8,  // Morning rush
            >= 9 and < 12 => 1.2, // Business hours
            >= 12 and < 14 => 0.7, // Lunch
            >= 14 and < 18 => 1.3, // Afternoon peak
            >= 18 and < 22 => 0.9, // Evening
            _ => 0.4 // Late night
        };
    }

    private static double GetSeasonalMultiplier(int dayOfYear)
    {
        // Simulate seasonal patterns (simplified)
        var dayOfYearNormalized = dayOfYear / 365.0 * 2 * Math.PI;
        return 0.8 + 0.4 * Math.Sin(dayOfYearNormalized - Math.PI / 2); // Peak in summer
    }

    private static double GetBurstMultiplier(int hourOfWeek)
    {
        // Simulate weekly burst patterns
        var dayOfWeek = hourOfWeek / 24;
        var hourOfDay = hourOfWeek % 24;
        
        var baseMultiplier = dayOfWeek switch
        {
            >= 1 and <= 5 => 1.2, // Weekdays
            0 or 6 => 0.6, // Weekends
            _ => 1.0
        };
        
        var hourlyMultiplier = GetLoadMultiplier(hourOfDay);
        
        return baseMultiplier * hourlyMultiplier * (1.0 + (hourOfWeek % 3 == 0 ? 0.5 : 0)); // Occasional bursts
    }

    public void Dispose()
    {
        _trainer?.Dispose();
    }
}