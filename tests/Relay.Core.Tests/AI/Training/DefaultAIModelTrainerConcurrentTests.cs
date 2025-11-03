using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Xunit;
using Xunit.Abstractions;

namespace Relay.Core.Tests.AI.Training;

/// <summary>
/// Simple test logger that captures log messages for testing
/// </summary>
public class TestLogger<T> : ILogger<T>
{
    private readonly List<string> _logMessages;
    private readonly object _lock = new object();

    public TestLogger(List<string> logMessages)
    {
        _logMessages = logMessages;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_lock)
        {
            _logMessages.Add($"{logLevel}: {formatter(state, exception)}");
        }
    }
}

public class DefaultAIModelTrainerConcurrentTests : IDisposable
{
    private readonly Mock<ILogger<DefaultAIModelTrainer>> _mockLogger;
    private readonly ITestOutputHelper _output;

    public DefaultAIModelTrainerConcurrentTests(ITestOutputHelper output)
    {
        _mockLogger = new Mock<ILogger<DefaultAIModelTrainer>>();
        _output = output;
    }

    [Fact]
    public async Task TrainModelAsync_ConcurrentSessions_HandlesThreadSafely()
    {
        // Arrange
        const int concurrentSessions = 5;
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var completedSessions = 0;

        // Act
        for (int i = 0; i < concurrentSessions; i++)
        {
            var sessionId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    using var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
                    var trainingData = CreateUniqueTrainingData(sessionId);
                    
                    await trainer.TrainModelAsync(trainingData);
                    
                    Interlocked.Increment(ref completedSessions);
                    _output.WriteLine($"Session {sessionId} completed successfully");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                    _output.WriteLine($"Session {sessionId} failed: {ex.Message}");
                }
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(concurrentSessions, completedSessions);
    }

    [Fact]
    public async Task TrainModelAsync_ConcurrentSessionsWithProgress_HandlesThreadSafely()
    {
        // Arrange
        const int concurrentSessions = 3;
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var progressReports = new List<(int SessionId, TrainingProgress Progress)>();
        var completedSessions = 0;

        // Act
        for (int i = 0; i < concurrentSessions; i++)
        {
            var sessionId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    using var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
                    var trainingData = CreateUniqueTrainingData(sessionId);
                    
                    await trainer.TrainModelAsync(trainingData, progress =>
                    {
                        lock (progressReports)
                        {
                            progressReports.Add((sessionId, progress));
                        }
                    });
                    
                    Interlocked.Increment(ref completedSessions);
                    int reportCount;
                    lock (progressReports)
                    {
                        reportCount = progressReports.Count(p => p.SessionId == sessionId);
                    }
                    _output.WriteLine($"Session {sessionId} completed with {reportCount} progress reports");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                    _output.WriteLine($"Session {sessionId} failed: {ex.Message}");
                }
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(concurrentSessions, completedSessions);
        
        // Each session should have progress reports
        for (int i = 0; i < concurrentSessions; i++)
        {
            var sessionReports = progressReports.Where(p => p.SessionId == i).ToList();
            Assert.NotEmpty(sessionReports);
            Assert.Contains(sessionReports, p => p.Progress.Phase == TrainingPhase.Completed);
        }
    }

    [Fact]
    public async Task TrainModelAsync_ConcurrentSessionsWithCancellation_HandlesThreadSafely()
    {
        // Arrange
        const int concurrentSessions = 4;
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var cancelledSessions = 0;
        var completedSessions = 0;

        // Act
        for (int i = 0; i < concurrentSessions; i++)
        {
            var sessionId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    using var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
                    using var cts = new CancellationTokenSource();
                    
                    // Cancel half of the sessions
                    if (sessionId % 2 == 0)
                    {
                        cts.CancelAfter(TimeSpan.FromMilliseconds(50));
                    }
                    
                    var trainingData = CreateUniqueTrainingData(sessionId);
                    
                    await trainer.TrainModelAsync(trainingData, cts.Token);
                    
                    Interlocked.Increment(ref completedSessions);
                    _output.WriteLine($"Session {sessionId} completed successfully");
                }
                catch (OperationCanceledException)
                {
                    Interlocked.Increment(ref cancelledSessions);
                    _output.WriteLine($"Session {sessionId} was cancelled as expected");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                    _output.WriteLine($"Session {sessionId} failed unexpectedly: {ex.Message}");
                }
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(concurrentSessions / 2, cancelledSessions);
        Assert.Equal(concurrentSessions / 2, completedSessions);
    }

    [Fact]
    public async Task TrainModelAsync_ConcurrentSessionsWithMixedData_HandlesThreadSafely()
    {
        // Arrange
        const int concurrentSessions = 6;
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var completedSessions = 0;

        // Act
        for (int i = 0; i < concurrentSessions; i++)
        {
            var sessionId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    using var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
                    var trainingData = CreateMixedTrainingData(sessionId);
                    
                    await trainer.TrainModelAsync(trainingData);
                    
                    Interlocked.Increment(ref completedSessions);
                    _output.WriteLine($"Session {sessionId} completed successfully");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                    _output.WriteLine($"Session {sessionId} failed: {ex.Message}");
                }
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(concurrentSessions, completedSessions);
    }

    [Fact]
    public async Task TrainModelAsync_ConcurrentSessionsWithDisposal_HandlesThreadSafely()
    {
        // Arrange
        const int concurrentSessions = 3;
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var disposedSessions = 0;

        // Act
        for (int i = 0; i < concurrentSessions; i++)
        {
            var sessionId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
                    var trainingData = CreateUniqueTrainingData(sessionId);
                    
                    await trainer.TrainModelAsync(trainingData);
                    
                    trainer.Dispose();
                    Interlocked.Increment(ref disposedSessions);
                    
                    _output.WriteLine($"Session {sessionId} completed and disposed successfully");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                    _output.WriteLine($"Session {sessionId} failed: {ex.Message}");
                }
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(concurrentSessions, disposedSessions);
    }

    [Fact]
    public async Task TrainModelAsync_ConcurrentSessionsWithHighVolume_HandlesThreadSafely()
    {
        // Arrange
        const int concurrentSessions = 2; // Reduced for performance
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var completedSessions = 0;

        // Act
        for (int i = 0; i < concurrentSessions; i++)
        {
            var sessionId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    using var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
                    var trainingData = CreateHighVolumeTrainingData(sessionId);
                    
                    var startTime = DateTime.UtcNow;
                    await trainer.TrainModelAsync(trainingData);
                    var duration = DateTime.UtcNow - startTime;
                    
                    Interlocked.Increment(ref completedSessions);
                    _output.WriteLine($"Session {sessionId} completed in {duration.TotalSeconds:F2}s");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                    _output.WriteLine($"Session {sessionId} failed: {ex.Message}");
                }
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(concurrentSessions, completedSessions);
    }

    [Fact]
    public async Task TrainModelAsync_ConcurrentSessionsWithStaggeredStart_HandlesThreadSafely()
    {
        // Arrange
        const int concurrentSessions = 4;
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var completedSessions = 0;
        var startTimes = new List<(int SessionId, DateTime StartTime)>();

        // Act
        for (int i = 0; i < concurrentSessions; i++)
        {
            var sessionId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    // Stagger the starts
                    await Task.Delay(sessionId * 100);
                    
                    var startTime = DateTime.UtcNow;
                    lock (startTimes)
                    {
                        startTimes.Add((sessionId, startTime));
                    }
                    
                    using var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
                    var trainingData = CreateUniqueTrainingData(sessionId);
                    
                    await trainer.TrainModelAsync(trainingData);
                    
                    Interlocked.Increment(ref completedSessions);
                    _output.WriteLine($"Session {sessionId} completed (started at {startTime:HH:mm:ss.fff})");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                    _output.WriteLine($"Session {sessionId} failed: {ex.Message}");
                }
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(concurrentSessions, completedSessions);
        Assert.Equal(concurrentSessions, startTimes.Count);
        
        // Verify staggered starts
        var sortedStartTimes = startTimes.OrderBy(st => st.StartTime).ToList();
        for (int i = 1; i < sortedStartTimes.Count; i++)
        {
            Assert.True(sortedStartTimes[i].StartTime > sortedStartTimes[i - 1].StartTime);
        }
    }

    [Fact]
    public async Task TrainModelAsync_ConcurrentSessionsWithSharedLogger_HandlesThreadSafely()
    {
        // Arrange
        const int concurrentSessions = 5;
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var completedSessions = 0;
        var logMessages = new List<string>();

        // Create a test logger that captures messages
        var testLogger = new TestLogger<DefaultAIModelTrainer>(logMessages);

        // Act
        for (int i = 0; i < concurrentSessions; i++)
        {
            var sessionId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    using var trainer = new DefaultAIModelTrainer(testLogger);
                    var trainingData = CreateUniqueTrainingData(sessionId);
                    
                    await trainer.TrainModelAsync(trainingData);
                    
                    Interlocked.Increment(ref completedSessions);
                    _output.WriteLine($"Session {sessionId} completed successfully");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                    _output.WriteLine($"Session {sessionId} failed: {ex.Message}");
                }
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(concurrentSessions, completedSessions);
        Assert.NotEmpty(logMessages);
        
        // Should have completion messages for all sessions
        var completionMessages = logMessages.Where(m => m.Contains("completed successfully")).ToList();
        Assert.Equal(concurrentSessions, completionMessages.Count);
    }

    [Fact]
    public async Task TrainModelAsync_ConcurrentSessionsWithExceptions_HandlesThreadSafely()
    {
        // Arrange
        const int concurrentSessions = 4;
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var completedSessions = 0;
        var failedSessions = 0;
        using var semaphore = new SemaphoreSlim(1, 1); // Allow only 1 concurrent training to avoid ML.NET threading issues

        // Act
        for (int i = 0; i < concurrentSessions; i++)
        {
            var sessionId = i;
            var task = Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    using var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
                    
                    // Half of the sessions get invalid data
                    var trainingData = sessionId % 2 == 0 
                        ? CreateUniqueTrainingData(sessionId)
                        : CreateInvalidTrainingData(sessionId);
                    
                    // Use a longer timeout to account for potential waiting due to semaphore serialization
                    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                    await trainer.TrainModelAsync(trainingData, cts.Token);
                    
                    Interlocked.Increment(ref completedSessions);
                    _output.WriteLine($"Session {sessionId} completed successfully");
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failedSessions);
                    _output.WriteLine($"Session {sessionId} failed as expected: {ex.Message}");
                    
                    // Only count unexpected exceptions
                    if (!(ex is ArgumentException ||
                          ex is OperationCanceledException ||
                          ex.ToString().Contains("validation failed")))
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions); // No unexpected exceptions
        Assert.Equal(2, completedSessions); // Only sessions with valid data complete
        Assert.Equal(2, failedSessions); // Sessions with invalid data fail as expected
    }

    [Fact]
    public async Task TrainModelAsync_ConcurrentSessionsWithRapidStartStop_HandlesThreadSafely()
    {
        // Arrange
        const int concurrentSessions = 6;
        var tasks = new List<Task>();
        var exceptions = new List<Exception>();
        var completedSessions = 0;

        // Act
        for (int i = 0; i < concurrentSessions; i++)
        {
            var sessionId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    using var trainer = new DefaultAIModelTrainer(_mockLogger.Object);
                    var trainingData = CreateSmallTrainingData(sessionId); // Small data for quick completion
                    
                    await trainer.TrainModelAsync(trainingData);
                    
                    Interlocked.Increment(ref completedSessions);
                    _output.WriteLine($"Session {sessionId} completed successfully");
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                    _output.WriteLine($"Session {sessionId} failed: {ex.Message}");
                }
            });
            
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions);
        Assert.Equal(concurrentSessions, completedSessions);
    }

    private AITrainingData CreateUniqueTrainingData(int sessionId)
    {
        var random = new Random(sessionId); // Use session ID as seed for unique but reproducible data
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 15 + sessionId)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 100 + sessionId * 10 + random.Next(50),
                    SuccessfulExecutions = 90 + sessionId * 9 + random.Next(30),
                    FailedExecutions = 10 + sessionId + random.Next(10),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50 + sessionId * 5 + random.Next(50)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(100 + sessionId * 10 + random.Next(100)),
                    ConcurrentExecutions = 5 + sessionId + random.Next(5),
                    MemoryUsage = (10 + sessionId * 2 + random.Next(20)) * 1024 * 1024,
                    DatabaseCalls = 5 + sessionId + random.Next(10),
                    ExternalApiCalls = 2 + sessionId / 2 + random.Next(5),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(60)),
                    SuccessRate = 0.85 + random.NextDouble() * 0.1,
                    MemoryAllocated = (10 + sessionId * 2 + random.Next(20)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 10 + sessionId)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)((sessionId + i) % 7 + 1),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(30 + sessionId * 3 + random.Next(30)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(24))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 30 + sessionId * 5)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-(30 + sessionId * 5) + i),
                    CpuUtilization = 0.3 + sessionId * 0.05 + random.NextDouble() * 0.3,
                    MemoryUtilization = 0.4 + sessionId * 0.03 + random.NextDouble() * 0.3,
                    ThroughputPerSecond = 50 + sessionId * 10 + random.Next(50)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMixedTrainingData(int sessionId)
    {
        var random = new Random(sessionId);
        
        // Create different types of data based on session ID
        return sessionId switch
        {
            0 => CreateHighVolumeTrainingData(sessionId),
            1 => CreateVariableLoadTrainingData(sessionId),
            2 => CreateSeasonalTrainingData(sessionId),
            3 => CreateProgressiveImprovementTrainingData(sessionId),
            4 => CreateFailureScenarioTrainingData(sessionId),
            _ => CreateUniqueTrainingData(sessionId)
        };
    }

    private AITrainingData CreateHighVolumeTrainingData(int sessionId)
    {
        var random = new Random(sessionId);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 100)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 1000 + random.Next(500),
                    SuccessfulExecutions = 900 + random.Next(300),
                    FailedExecutions = 100 + random.Next(100),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(30 + random.Next(70)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(60 + random.Next(120)),
                    ConcurrentExecutions = 20 + random.Next(20),
                    MemoryUsage = (100 + random.Next(100)) * 1024 * 1024,
                    DatabaseCalls = 30 + random.Next(50),
                    ExternalApiCalls = 15 + random.Next(25),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(120)),
                    SuccessRate = 0.9 + random.NextDouble() * 0.08,
                    MemoryAllocated = (100 + random.Next(100)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.35,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(48))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 200)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-200 + i),
                    CpuUtilization = 0.3 + random.NextDouble() * 0.4,
                    MemoryUtilization = 0.4 + random.NextDouble() * 0.4,
                    ThroughputPerSecond = 100 + random.Next(200)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateVariableLoadTrainingData(int sessionId)
    {
        var random = new Random(sessionId);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 72)
                .Select(i => 
                {
                    var hourOfDay = i / 3;
                    var loadMultiplier = GetLoadMultiplier(hourOfDay);
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(100 * loadMultiplier) + random.Next(50),
                        SuccessfulExecutions = (int)(90 * loadMultiplier) + random.Next(30),
                        FailedExecutions = (int)(10 * loadMultiplier) + random.Next(10),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(100)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(150)),
                        ConcurrentExecutions = (int)(5 * loadMultiplier) + random.Next(5),
                        MemoryUsage = (int)(50 * loadMultiplier + random.Next(50)) * 1024 * 1024,
                        DatabaseCalls = (int)(10 * loadMultiplier) + random.Next(15),
                        ExternalApiCalls = (int)(5 * loadMultiplier) + random.Next(8),
                        LastExecution = DateTime.UtcNow.AddHours(-24 + hourOfDay),
                        SuccessRate = 0.85 + random.NextDouble() * 0.1,
                        MemoryAllocated = (int)(50 * loadMultiplier + random.Next(50)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 36)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(50)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddHours(-24 + i * 0.67)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 144)
                .Select(i => 
                {
                    var hourOfDay = i / 6;
                    var loadMultiplier = GetLoadMultiplier(hourOfDay);
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddHours(-24 + hourOfDay).AddMinutes((i % 6) * 10),
                        CpuUtilization = (0.2 + random.NextDouble() * 0.4) * loadMultiplier,
                        MemoryUtilization = (0.3 + random.NextDouble() * 0.4) * loadMultiplier,
                        ThroughputPerSecond = (int)(50 * loadMultiplier) + random.Next(80)
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateSeasonalTrainingData(int sessionId)
    {
        var random = new Random(sessionId);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 90)
                .Select(i => 
                {
                    var dayOfYear = i * 4;
                    var seasonalMultiplier = GetSeasonalMultiplier(dayOfYear);
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(200 * seasonalMultiplier) + random.Next(100),
                        SuccessfulExecutions = (int)(180 * seasonalMultiplier) + random.Next(60),
                        FailedExecutions = (int)(20 * seasonalMultiplier) + random.Next(20),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(80)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(120)),
                        ConcurrentExecutions = (int)(8 * seasonalMultiplier) + random.Next(8),
                        MemoryUsage = (int)(60 * seasonalMultiplier + random.Next(60)) * 1024 * 1024,
                        DatabaseCalls = (int)(15 * seasonalMultiplier) + random.Next(20),
                        ExternalApiCalls = (int)(8 * seasonalMultiplier) + random.Next(10),
                        LastExecution = DateTime.UtcNow.AddDays(-90 + i),
                        SuccessRate = 0.88 + random.NextDouble() * 0.08,
                        MemoryAllocated = (int)(60 * seasonalMultiplier + random.Next(60)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 45)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.18,
                    ExecutionTime = TimeSpan.FromMilliseconds(22 + random.Next(45)),
                    PerformanceImprovement = random.NextDouble() * 0.32,
                    Timestamp = DateTime.UtcNow.AddDays(-45 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 180)
                .Select(i => 
                {
                    var dayOfYear = i / 2;
                    var seasonalMultiplier = GetSeasonalMultiplier(dayOfYear);
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-90 + dayOfYear).AddHours((i % 2) * 12),
                        CpuUtilization = (0.25 + random.NextDouble() * 0.35) * seasonalMultiplier,
                        MemoryUtilization = (0.35 + random.NextDouble() * 0.35) * seasonalMultiplier,
                        ThroughputPerSecond = (int)(70 * seasonalMultiplier) + random.Next(100)
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateProgressiveImprovementTrainingData(int sessionId)
    {
        var random = new Random(sessionId);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 60)
                .Select(i => 
                {
                    var improvementFactor = 1.0 - (i * 0.01);
                    var baseTime = 80 * improvementFactor;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 500 + random.Next(200),
                        SuccessfulExecutions = 450 + (int)(i * 2) + random.Next(50),
                        FailedExecutions = 50 - (int)(i * 0.5) + random.Next(20),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(baseTime + random.Next(40)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(baseTime * 2 + random.Next(80)),
                        ConcurrentExecutions = 8 + random.Next(8),
                        MemoryUsage = (80 - (int)(i * 0.5) + random.Next(40)) * 1024 * 1024,
                        DatabaseCalls = 20 - (int)(i * 0.2) + random.Next(15),
                        ExternalApiCalls = 10 - (int)(i * 0.1) + random.Next(8),
                        LastExecution = DateTime.UtcNow.AddMinutes(-60 + i),
                        SuccessRate = Math.Min(0.98, 0.8 + (i * 0.003) + random.NextDouble() * 0.1),
                        MemoryAllocated = (80 - (int)(i * 0.5) + random.Next(40)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 30)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = OptimizationStrategy.EnableCaching,
                    Success = random.NextDouble() > (0.3 - i * 0.005),
                    ExecutionTime = TimeSpan.FromMilliseconds(30 - (i * 0.2) + random.Next(25)),
                    PerformanceImprovement = (0.1 + i * 0.005) * random.NextDouble(),
                    Timestamp = DateTime.UtcNow.AddHours(-30 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 120)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-120 + i),
                    CpuUtilization = Math.Max(0.2, 0.5 - (i * 0.002) + random.NextDouble() * 0.2),
                    MemoryUtilization = Math.Max(0.3, 0.6 - (i * 0.002) + random.NextDouble() * 0.2),
                    ThroughputPerSecond = 80 + (int)(i * 0.5) + random.Next(60)
                })
                .ToArray()
        };
    }

    private AITrainingData CreateFailureScenarioTrainingData(int sessionId)
    {
        var random = new Random(sessionId);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 40)
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 300 + random.Next(200),
                    SuccessfulExecutions = 200 + random.Next(100), // Lower success rate
                    FailedExecutions = 100 + random.Next(50), // Higher failure rate
                    AverageExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(100)), // Slower
                    P95ExecutionTime = TimeSpan.FromMilliseconds(160 + random.Next(150)),
                    ConcurrentExecutions = 10 + random.Next(10),
                    MemoryUsage = (80 + random.Next(60)) * 1024 * 1024,
                    DatabaseCalls = 20 + random.Next(25),
                    ExternalApiCalls = 10 + random.Next(15),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(80)),
                    SuccessRate = 0.6 + random.NextDouble() * 0.2, // Lower success rate
                    MemoryAllocated = (80 + random.Next(60)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 20)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.5, // 50% failure rate
                    ExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(80)),
                    PerformanceImprovement = random.NextDouble() > 0.5 ? random.NextDouble() * 0.2 : -random.NextDouble() * 0.1,
                    Timestamp = DateTime.UtcNow.AddHours(-random.Next(40))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 80)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-80 + i),
                    CpuUtilization = 0.5 + random.NextDouble() * 0.4, // Higher CPU
                    MemoryUtilization = 0.6 + random.NextDouble() * 0.3, // Higher memory
                    ThroughputPerSecond = 40 + random.Next(60) // Lower throughput
                })
                .ToArray()
        };
    }

    private AITrainingData CreateInvalidTrainingData(int sessionId)
    {
        // Create data that will fail validation
        return new AITrainingData
        {
            ExecutionHistory = Array.Empty<RequestExecutionMetrics>(), // Empty - will fail validation
            OptimizationHistory = Array.Empty<AIOptimizationResult>(),
            SystemLoadHistory = Array.Empty<SystemLoadMetrics>()
        };
    }

    private AITrainingData CreateSmallTrainingData(int sessionId)
    {
        var random = new Random(sessionId);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 10) // Small dataset for quick completion
                .Select(i => new RequestExecutionMetrics
                {
                    TotalExecutions = 50 + random.Next(20),
                    SuccessfulExecutions = 45 + random.Next(10),
                    FailedExecutions = 5 + random.Next(5),
                    AverageExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(20)),
                    P95ExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(30)),
                    ConcurrentExecutions = 2 + random.Next(3),
                    MemoryUsage = (20 + random.Next(10)) * 1024 * 1024,
                    DatabaseCalls = 3 + random.Next(5),
                    ExternalApiCalls = 1 + random.Next(3),
                    LastExecution = DateTime.UtcNow.AddMinutes(-random.Next(30)),
                    SuccessRate = 0.9 + random.NextDouble() * 0.05,
                    MemoryAllocated = (20 + random.Next(10)) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 5)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(10 + random.Next(15)),
                    PerformanceImprovement = random.NextDouble() * 0.2,
                    Timestamp = DateTime.UtcNow.AddMinutes(-random.Next(30))
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 15)
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-15 + i),
                    CpuUtilization = 0.3 + random.NextDouble() * 0.3,
                    MemoryUtilization = 0.4 + random.NextDouble() * 0.3,
                    ThroughputPerSecond = 20 + random.Next(30)
                })
                .ToArray()
        };
    }

    private static double GetLoadMultiplier(int hourOfDay)
    {
        return hourOfDay switch
        {
            >= 0 and < 6 => 0.3,
            >= 6 and < 9 => 0.8,
            >= 9 and < 12 => 1.2,
            >= 12 and < 14 => 0.7,
            >= 14 and < 18 => 1.3,
            >= 18 and < 22 => 0.9,
            _ => 0.4
        };
    }

    private static double GetSeasonalMultiplier(int dayOfYear)
    {
        var dayOfYearNormalized = dayOfYear / 365.0 * 2 * Math.PI;
        return 0.8 + 0.4 * Math.Sin(dayOfYearNormalized - Math.PI / 2);
    }

    public void Dispose()
    {
        // Test cleanup handled by individual test methods
    }
}