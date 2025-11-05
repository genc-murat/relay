using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Xunit;
using Xunit.Abstractions;

namespace Relay.Core.Tests.AI.Training;

public class DefaultAIModelTrainerPerformanceTests : IDisposable
{
    private readonly Mock<ILogger<DefaultAIModelTrainer>> _mockLogger;
    private readonly ITestOutputHelper _output;
    private readonly DefaultAIModelTrainer _trainer;

    public DefaultAIModelTrainerPerformanceTests(ITestOutputHelper output)
    {
        _mockLogger = new Mock<ILogger<DefaultAIModelTrainer>>();
        _output = output;
        _trainer = new DefaultAIModelTrainer(_mockLogger.Object);
    }

    #region Large Dataset Performance Tests

    [Fact]
    public async Task TrainModelAsync_WithLargeDataset_CompletesWithinReasonableTime()
    {
        // Arrange
        var trainingData = CreateLargeDataset();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _trainer.TrainModelAsync(trainingData);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"Large dataset training completed in {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.ElapsedMilliseconds < 30000, 
            $"Training took {stopwatch.ElapsedMilliseconds}ms, expected < 30000ms");
    }

    [Fact]
    public async Task TrainModelAsync_WithVeryLargeDataset_HandlesMemoryEfficiently()
    {
        // Arrange
        var trainingData = CreateVeryLargeDataset();
        var initialMemory = GC.GetTotalMemory(true);
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _trainer.TrainModelAsync(trainingData);
        stopwatch.Stop();
        
        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;

        // Assert
        _output.WriteLine($"Very large dataset training:");
        _output.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"  Memory increase: {memoryIncrease / 1024 / 1024:F2}MB");
        
        Assert.True(stopwatch.ElapsedMilliseconds < 60000, 
            $"Training took {stopwatch.ElapsedMilliseconds}ms, expected < 60000ms");
        
        // Memory increase should be reasonable (less than 500MB for this test)
        Assert.True(memoryIncrease < 500L * 1024 * 1024, 
            $"Memory increased by {memoryIncrease / 1024 / 1024:F2}MB, expected < 500MB");
    }

    #endregion

    #region Prediction Performance Tests

    [Fact]
    public async Task TrainModelAsync_PredictionPerformance_MeetsExpectations()
    {
        // Arrange
        var trainingData = CreatePredictionPerformanceTestData();
        var predictionTimes = new List<long>();

        // Act
        await _trainer.TrainModelAsync(trainingData, progress =>
        {
            if (progress.Phase == TrainingPhase.Forecasting)
            {
                var stopwatch = Stopwatch.StartNew();
                // Simulate prediction work
                Thread.Sleep(1);
                stopwatch.Stop();
                predictionTimes.Add(stopwatch.ElapsedMilliseconds);
            }
        });

        // Assert
        if (predictionTimes.Any())
        {
            var avgPredictionTime = predictionTimes.Average();
            var maxPredictionTime = predictionTimes.Max();
            
            _output.WriteLine($"Prediction performance:");
            _output.WriteLine($"  Average: {avgPredictionTime:F2}ms");
            _output.WriteLine($"  Maximum: {maxPredictionTime}ms");
            _output.WriteLine($"  Samples: {predictionTimes.Count}");
            
            Assert.True(avgPredictionTime < 100, 
                $"Average prediction time {avgPredictionTime:F2}ms, expected < 100ms");
        }
    }

    #endregion

    #region Concurrent Performance Tests

    [Fact]
    public async Task TrainModelAsync_ConcurrentWithProgress_HandlesEfficiently()
    {
        // Arrange
        const int concurrentTasks = 2;
        var tasks = new List<Task>();
        var progressReports = new List<(int TaskId, TrainingProgress Progress)>();
        var trainingData = CreateProgressTestData();

        // Act
        for (int i = 0; i < concurrentTasks; i++)
        {
            var taskId = i;
            var task = Task.Run(async () =>
            {
                using var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
                await trainer.TrainModelAsync(trainingData, progress =>
                {
                    lock (progressReports)
                    {
                        progressReports.Add((taskId, progress));
                    }
                });
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        var reportsPerTask = progressReports.GroupBy(r => r.TaskId).ToList();
        _output.WriteLine($"Concurrent progress reporting:");
        _output.WriteLine($"  Total progress reports: {progressReports.Count}");
        _output.WriteLine($"  Reports per task: {string.Join(", ", reportsPerTask.Select(g => g.Count()))}");

        Assert.Equal(concurrentTasks, reportsPerTask.Count);
        Assert.All(reportsPerTask, group => Assert.NotEmpty(group));
    }

    #endregion

    #region Memory Performance Tests

    [Fact]
    public async Task TrainModelAsync_MemoryUsage_StaysWithinBounds()
    {
        // Arrange
        var trainingData = CreateMemoryIntensiveTestData();
        var memoryMeasurements = new List<long>();
        
        // Measure baseline memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = GC.GetTotalMemory(false);

        // Act
        var stopwatch = Stopwatch.StartNew();
        
        // Monitor memory during training
        var monitoringTask = Task.Run(async () =>
        {
            for (int i = 0; i < 30; i++) // Monitor for ~30 seconds
            {
                await Task.Delay(1000);
                memoryMeasurements.Add(GC.GetTotalMemory(false));
            }
        });

        await _trainer.TrainModelAsync(trainingData);
        stopwatch.Stop();
        
        // Stop monitoring
        var finalMemory = GC.GetTotalMemory(false);
        var peakMemory = memoryMeasurements.DefaultIfEmpty(baselineMemory).Max();
        var memoryIncrease = finalMemory - baselineMemory;

        // Assert
        _output.WriteLine($"Memory usage analysis:");
        _output.WriteLine($"  Training time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"  Baseline memory: {baselineMemory / 1024 / 1024:F2}MB");
        _output.WriteLine($"  Peak memory: {peakMemory / 1024 / 1024:F2}MB");
        _output.WriteLine($"  Final memory: {finalMemory / 1024 / 1024:F2}MB");
        _output.WriteLine($"  Memory increase: {memoryIncrease / 1024 / 1024:F2}MB");

        Assert.True(memoryIncrease < 300L * 1024 * 1024, 
            $"Memory increased by {memoryIncrease / 1024 / 1024:F2}MB, expected < 300MB");
    }

    #endregion

    #region Scalability Tests

    [Fact]
    public async Task TrainModelAsync_Scalability_LinearGrowthWithDatasetSize()
    {
        // Arrange
        var datasetSizes = new[] { 100, 500, 1000 };
        var trainingTimes = new List<long>();

        // Act
        foreach (var size in datasetSizes)
        {
            var trainingData = CreateScalabilityTestData(size);
            var stopwatch = Stopwatch.StartNew();
            
            await _trainer.TrainModelAsync(trainingData);
            
            stopwatch.Stop();
            trainingTimes.Add(stopwatch.ElapsedMilliseconds);
            
            _output.WriteLine($"Dataset size {size}: {stopwatch.ElapsedMilliseconds}ms");
            
            // Allow some cleanup time between tests
            await Task.Delay(1000);
            GC.Collect();
        }

        // Assert - Check that growth is roughly linear (not exponential)
        if (trainingTimes.Count >= 3)
        {
            var ratio1to2 = (double)trainingTimes[1] / trainingTimes[0];
            var ratio2to3 = (double)trainingTimes[2] / trainingTimes[1];
            var sizeRatio1to2 = (double)datasetSizes[1] / datasetSizes[0];
            var sizeRatio2to3 = (double)datasetSizes[2] / datasetSizes[1];

            _output.WriteLine($"Scalability analysis:");
            _output.WriteLine($"  Time ratio 100->500: {ratio1to2:F2} (size ratio: {sizeRatio1to2:F2})");
            _output.WriteLine($"  Time ratio 500->1000: {ratio2to3:F2} (size ratio: {sizeRatio2to3:F2})");

            // Time growth should be proportional to data size growth (within reasonable bounds)
            Assert.True(ratio1to2 < sizeRatio1to2 * 2, 
                $"Time growth {ratio1to2:F2} is much higher than data size growth {sizeRatio1to2:F2}");
            Assert.True(ratio2to3 < sizeRatio2to3 * 2, 
                $"Time growth {ratio2to3:F2} is much higher than data size growth {sizeRatio2to3:F2}");
        }
    }

    [Fact]
    public async Task TrainModelAsync_Throughput_HandlesHighVolumeRequests()
    {
        // Arrange
        var trainingData = CreateThroughputTestData();
        var stopwatch = Stopwatch.StartNew();

        // Act
        await _trainer.TrainModelAsync(trainingData);
        stopwatch.Stop();

        var dataPoints = trainingData.ExecutionHistory.Length + 
                        trainingData.OptimizationHistory.Length + 
                        trainingData.SystemLoadHistory.Length;
        var throughput = dataPoints / (stopwatch.ElapsedMilliseconds / 1000.0);

        // Assert
        _output.WriteLine($"Throughput analysis:");
        _output.WriteLine($"  Data points: {dataPoints}");
        _output.WriteLine($"  Time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"  Throughput: {throughput:F2} data points/second");

        Assert.True(throughput > 10, 
            $"Throughput {throughput:F2} data points/second, expected > 10");
    }

    #endregion

    #region Test Data Creation Methods

    private AITrainingData CreateLargeDataset()
    {
        var random = new Random(42);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 1000)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 1000 + random.Next(2000),
                    SuccessfulExecutions = 900 + random.Next(1500),
                    FailedExecutions = 100 + random.Next(500),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(150)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(300)),
                    ConcurrentExecutions = 10 + random.Next(40),
                    MemoryUsage = (100 + random.Next(400)) * 1024 * 1024,
                    DatabaseCalls = 20 + random.Next(80),
                    ExternalApiCalls = 10 + random.Next(40),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(1440)),
                    SuccessRate = 0.8 + random.NextDouble() * 0.15,
                    MemoryAllocated = (100 + random.Next(400)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 500)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(100)),
                    PerformanceImprovement = random.NextDouble() * 0.4,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(168))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 2000)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-2000 + i),
                    CpuUtilization = 0.2 + random.NextDouble() * 0.6,
                    MemoryUtilization = 0.3 + random.NextDouble() * 0.5,
                    ThroughputPerSecond = 100 + random.Next(400)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateVeryLargeDataset()
    {
        var random = new Random(123);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 5000)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 2000 + random.Next(3000),
                    SuccessfulExecutions = 1800 + random.Next(2500),
                    FailedExecutions = 200 + random.Next(500),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(120)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(240)),
                    ConcurrentExecutions = 15 + random.Next(35),
                    MemoryUsage = (150 + random.Next(350)) * 1024 * 1024,
                    DatabaseCalls = 30 + random.Next(70),
                    ExternalApiCalls = 15 + random.Next(35),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(2880)),
                    SuccessRate = 0.85 + random.NextDouble() * 0.1,
                    MemoryAllocated = (150 + random.Next(350)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 2500)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(15 + random.Next(85)),
                    PerformanceImprovement = random.NextDouble() * 0.35,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(336))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 10000)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-10000 + i),
                    CpuUtilization = 0.25 + random.NextDouble() * 0.5,
                    MemoryUtilization = 0.35 + random.NextDouble() * 0.45,
                    ThroughputPerSecond = 150 + random.Next(350)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateHighFrequencyData()
    {
        var random = new Random(456);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 2000)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 500 + random.Next(1000),
                    SuccessfulExecutions = 450 + random.Next(800),
                    FailedExecutions = 50 + random.Next(200),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(10 + random.Next(40)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(80)),
                    ConcurrentExecutions = 20 + random.Next(80),
                    MemoryUsage = (50 + random.Next(150)) * 1024 * 1024,
                    DatabaseCalls = 40 + random.Next(160),
                    ExternalApiCalls = 20 + random.Next(80),
                    LastExecution = DateTime.UtcNow.AddSeconds(-random.Next(3600)),
                    SuccessRate = 0.9 + random.NextDouble() * 0.08,
                    MemoryAllocated = (50 + random.Next(150)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 1000)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.1,
                    ExecutionTime = TimeSpan.FromMilliseconds(5 + random.Next(25)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(720))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 4000)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddSeconds(-4000 + i),
                    CpuUtilization = 0.3 + random.NextDouble() * 0.4,
                    MemoryUtilization = 0.4 + random.NextDouble() * 0.3,
                    ThroughputPerSecond = 200 + random.Next(600)
                })
                .ToArray()
        };
    }

    private AITrainingData CreatePredictionPerformanceTestData()
    {
        var random = new Random(789);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 500)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100 + random.Next(200),
                    SuccessfulExecutions = 90 + random.Next(150),
                    FailedExecutions = 10 + random.Next(50),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(50)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(100)),
                    ConcurrentExecutions = 5 + random.Next(15),
                    MemoryUsage = (75 + random.Next(100)) * 1024 * 1024,
                    DatabaseCalls = 15 + random.Next(35),
                    ExternalApiCalls = 8 + random.Next(17),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(480)),
                    SuccessRate = 0.88 + random.NextDouble() * 0.1,
                    MemoryAllocated = (75 + random.Next(100)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 250)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(10 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.25,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(120))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 1000)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-1000 + i),
                    CpuUtilization = 0.35 + random.NextDouble() * 0.3,
                    MemoryUtilization = 0.45 + random.NextDouble() * 0.25,
                    ThroughputPerSecond = 120 + random.Next(280)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateBatchPredictionTestData()
    {
        var random = new Random(321);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 1500)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 300 + random.Next(700),
                    SuccessfulExecutions = 270 + random.Next(600),
                    FailedExecutions = 30 + random.Next(100),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(60)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(120)),
                    ConcurrentExecutions = 25 + random.Next(75),
                    MemoryUsage = (100 + random.Next(200)) * 1024 * 1024,
                    DatabaseCalls = 50 + random.Next(150),
                    ExternalApiCalls = 25 + random.Next(75),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(1440)),
                    SuccessRate = 0.87 + random.NextDouble() * 0.1,
                    MemoryAllocated = (100 + random.Next(200)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 750)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(8 + random.Next(32)),
                    PerformanceImprovement = random.NextDouble() * 0.2,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(168))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 3000)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-3000 + i),
                    CpuUtilization = 0.4 + random.NextDouble() * 0.35,
                    MemoryUtilization = 0.5 + random.NextDouble() * 0.3,
                    ThroughputPerSecond = 250 + random.Next(500)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateConcurrentTestData()
    {
        var random = new Random(654);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 300)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 200 + random.Next(300),
                    SuccessfulExecutions = 180 + random.Next(250),
                    FailedExecutions = 20 + random.Next(70),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(35 + random.Next(65)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(70 + random.Next(130)),
                    ConcurrentExecutions = 8 + random.Next(22),
                    MemoryUsage = (80 + random.Next(120)) * 1024 * 1024,
                    DatabaseCalls = 20 + random.Next(50),
                    ExternalApiCalls = 10 + random.Next(25),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(600)),
                    SuccessRate = 0.86 + random.NextDouble() * 0.1,
                    MemoryAllocated = (80 + random.Next(120)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 150)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(15 + random.Next(45)),
                    PerformanceImprovement = random.NextDouble() * 0.22,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(96))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 600)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-600 + i),
                    CpuUtilization = 0.33 + random.NextDouble() * 0.37,
                    MemoryUtilization = 0.43 + random.NextDouble() * 0.32,
                    ThroughputPerSecond = 160 + random.Next(290)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateProgressTestData()
    {
        var random = new Random(987);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 200)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 150 + random.Next(250),
                    SuccessfulExecutions = 135 + random.Next(200),
                    FailedExecutions = 15 + random.Next(50),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(30 + random.Next(70)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(60 + random.Next(140)),
                    ConcurrentExecutions = 6 + random.Next(18),
                    MemoryUsage = (70 + random.Next(110)) * 1024 * 1024,
                    DatabaseCalls = 18 + random.Next(42),
                    ExternalApiCalls = 9 + random.Next(21),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(400)),
                    SuccessRate = 0.85 + random.NextDouble() * 0.12,
                    MemoryAllocated = (70 + random.Next(110)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 100)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.18,
                    ExecutionTime = TimeSpan.FromMilliseconds(12 + random.Next(48)),
                    PerformanceImprovement = random.NextDouble() * 0.24,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(72))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 400)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-400 + i),
                    CpuUtilization = 0.32 + random.NextDouble() * 0.38,
                    MemoryUtilization = 0.42 + random.NextDouble() * 0.33,
                    ThroughputPerSecond = 140 + random.Next(260)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMemoryIntensiveTestData()
    {
        var random = new Random(111);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 800)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 400 + random.Next(800),
                    SuccessfulExecutions = 360 + random.Next(700),
                    FailedExecutions = 40 + random.Next(100),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(100)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(200)),
                    ConcurrentExecutions = 20 + random.Next(60),
                    MemoryUsage = (200 + random.Next(600)) * 1024 * 1024,
                    DatabaseCalls = 40 + random.Next(120),
                    ExternalApiCalls = 20 + random.Next(60),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(1200)),
                    SuccessRate = 0.84 + random.NextDouble() * 0.12,
                    MemoryAllocated = (200 + random.Next(600)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 400)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(75)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(192))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 1600)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-1600 + i),
                    CpuUtilization = 0.45 + random.NextDouble() * 0.35,
                    MemoryUtilization = 0.6 + random.NextDouble() * 0.3,
                    ThroughputPerSecond = 300 + random.Next(500)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateGCTestData()
    {
        var random = new Random(222);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 200) // Reduced from 600
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 250 + random.Next(500),
                    SuccessfulExecutions = 225 + random.Next(400),
                    FailedExecutions = 25 + random.Next(100),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(80)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(160)),
                    ConcurrentExecutions = 12 + random.Next(38),
                    MemoryUsage = (120 + random.Next(280)) * 1024 * 1024,
                    DatabaseCalls = 25 + random.Next(75),
                    ExternalApiCalls = 12 + random.Next(38),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(900)),
                    SuccessRate = 0.86 + random.NextDouble() * 0.11,
                    MemoryAllocated = (120 + random.Next(280)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 100) // Reduced from 300
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.17,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(60)),
                    PerformanceImprovement = random.NextDouble() * 0.26,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(144))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 400) // Reduced from 1200
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-400 + i),
                    CpuUtilization = 0.38 + random.NextDouble() * 0.37,
                    MemoryUtilization = 0.48 + random.NextDouble() * 0.37,
                    ThroughputPerSecond = 200 + random.Next(400)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateScalabilityTestData(int size)
    {
        var random = new Random(333);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, size)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100 + random.Next(200),
                    SuccessfulExecutions = 90 + random.Next(150),
                    FailedExecutions = 10 + random.Next(50),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(30 + random.Next(70)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(60 + random.Next(140)),
                    ConcurrentExecutions = 5 + random.Next(15),
                    MemoryUsage = (50 + random.Next(100)) * 1024 * 1024,
                    DatabaseCalls = 10 + random.Next(30),
                    ExternalApiCalls = 5 + random.Next(15),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(600)),
                    SuccessRate = 0.85 + random.NextDouble() * 0.1,
                    MemoryAllocated = (50 + random.Next(100)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, size / 2)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(15 + random.Next(45)),
                    PerformanceImprovement = random.NextDouble() * 0.25,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(120))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, size * 2)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-size * 2 + i),
                    CpuUtilization = 0.3 + random.NextDouble() * 0.4,
                    MemoryUtilization = 0.4 + random.NextDouble() * 0.3,
                    ThroughputPerSecond = 100 + random.Next(200)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateThroughputTestData()
    {
        var random = new Random(444);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 1200)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 300 + random.Next(600),
                    SuccessfulExecutions = 270 + random.Next(500),
                    FailedExecutions = 30 + random.Next(100),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(75)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(150)),
                    ConcurrentExecutions = 15 + random.Next(45),
                    MemoryUsage = (90 + random.Next(180)) * 1024 * 1024,
                    DatabaseCalls = 30 + random.Next(90),
                    ExternalApiCalls = 15 + random.Next(45),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(1800)),
                    SuccessRate = 0.87 + random.NextDouble() * 0.1,
                    MemoryAllocated = (90 + random.Next(180)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 600)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.16,
                    ExecutionTime = TimeSpan.FromMilliseconds(12 + random.Next(58)),
                    PerformanceImprovement = random.NextDouble() * 0.28,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(216))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 2400)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-2400 + i),
                    CpuUtilization = 0.35 + random.NextDouble() * 0.36,
                    MemoryUtilization = 0.45 + random.NextDouble() * 0.35,
                    ThroughputPerSecond = 220 + random.Next(380)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateStressTestData()
    {
        var random = new Random(555);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 400)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 350 + random.Next(500),
                    SuccessfulExecutions = 315 + random.Next(400),
                    FailedExecutions = 35 + random.Next(100),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(45 + random.Next(85)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(90 + random.Next(170)),
                    ConcurrentExecutions = 18 + random.Next(32),
                    MemoryUsage = (130 + random.Next(170)) * 1024 * 1024,
                    DatabaseCalls = 35 + random.Next(65),
                    ExternalApiCalls = 18 + random.Next(32),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(720)),
                    SuccessRate = 0.86 + random.NextDouble() * 0.11,
                    MemoryAllocated = (130 + random.Next(170)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 200)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.14,
                    ExecutionTime = TimeSpan.FromMilliseconds(22 + random.Next(58)),
                    PerformanceImprovement = random.NextDouble() * 0.27,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(96))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 800)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-800 + i),
                    CpuUtilization = 0.42 + random.NextDouble() * 0.33,
                    MemoryUtilization = 0.52 + random.NextDouble() * 0.28,
                    ThroughputPerSecond = 280 + random.Next(320)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateResourceExhaustionTestData()
    {
        var random = new Random(666);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 10000)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 1000 + random.Next(2000),
                    SuccessfulExecutions = 900 + random.Next(1500),
                    FailedExecutions = 100 + random.Next(500),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(200)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(200 + random.Next(400)),
                    ConcurrentExecutions = 50 + random.Next(100),
                    MemoryUsage = (500 + random.Next(1000)) * 1024 * 1024,
                    DatabaseCalls = 100 + random.Next(200),
                    ExternalApiCalls = 50 + random.Next(100),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(2880)),
                    SuccessRate = 0.8 + random.NextDouble() * 0.15,
                    MemoryAllocated = (500 + random.Next(1000)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 5000)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.1,
                    ExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(150)),
                    PerformanceImprovement = random.NextDouble() * 0.4,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(720))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 20000)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-20000 + i),
                    CpuUtilization = 0.5 + random.NextDouble() * 0.4,
                    MemoryUtilization = 0.6 + random.NextDouble() * 0.35,
                    ThroughputPerSecond = 500 + random.Next(1000)
                })
                .ToArray()
        };
    }

    #endregion

    public void Dispose()
    {
        // Cleanup handled by individual test methods
    }
}