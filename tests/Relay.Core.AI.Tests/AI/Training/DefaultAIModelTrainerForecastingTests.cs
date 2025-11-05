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

public class DefaultAIModelTrainerForecastingTests : IDisposable
{
    private readonly Mock<ILogger<DefaultAIModelTrainer>> _mockLogger;
    private readonly DefaultAIModelTrainer _trainer;

    public DefaultAIModelTrainerForecastingTests()
    {
        _mockLogger = new Mock<ILogger<DefaultAIModelTrainer>>();
        _trainer = new DefaultAIModelTrainer(_mockLogger.Object);
    }

    #region Short-term Forecasting Tests

    [Fact]
    public async Task TrainModelAsync_WithShortTermForecastingData_HandlesCorrectly()
    {
        // Arrange
        var trainingData = CreateShortTermForecastingData();

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
    public async Task TrainModelAsync_WithRealTimeForecastingData_ProcessesEfficiently()
    {
        // Arrange
        var trainingData = CreateRealTimeForecastingData();

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
    public async Task TrainModelAsync_WithMinuteLevelForecasting_HandlesHighFrequency()
    {
        // Arrange
        var trainingData = CreateMinuteLevelForecastingData();

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

    #region Medium-term Forecasting Tests

    [Fact]
    public async Task TrainModelAsync_WithHourlyForecastingData_HandlesTrends()
    {
        // Arrange
        var trainingData = CreateHourlyForecastingData();

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
    public async Task TrainModelAsync_WithDailyForecastingData_HandlesPatterns()
    {
        // Arrange
        var trainingData = CreateDailyForecastingData();

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
    public async Task TrainModelAsync_WithWeeklyForecastingData_HandlesSeasonality()
    {
        // Arrange
        var trainingData = CreateWeeklyForecastingData();

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

    #region Long-term Forecasting Tests

    [Fact]
    public async Task TrainModelAsync_WithMonthlyForecastingData_HandlesLongTrends()
    {
        // Arrange
        var trainingData = CreateMonthlyForecastingData();

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
    public async Task TrainModelAsync_WithQuarterlyForecastingData_HandlesBusinessCycles()
    {
        // Arrange
        var trainingData = CreateQuarterlyForecastingData();

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
    public async Task TrainModelAsync_WithYearlyForecastingData_HandlesAnnualPatterns()
    {
        // Arrange
        var trainingData = CreateYearlyForecastingData();

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

    #region Pattern-based Forecasting Tests

    [Fact]
    public async Task TrainModelAsync_WithSeasonalForecastingData_HandlesSeasonality()
    {
        // Arrange
        var trainingData = CreateSeasonalForecastingData();

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
    public async Task TrainModelAsync_WithTrendingForecastingData_HandlesTrends()
    {
        // Arrange
        var trainingData = CreateTrendingForecastingData();

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
    public async Task TrainModelAsync_WithCyclicalForecastingData_HandlesCycles()
    {
        // Arrange
        var trainingData = CreateCyclicalForecastingData();

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
    public async Task TrainModelAsync_WithIrregularForecastingData_HandlesIrregularity()
    {
        // Arrange
        var trainingData = CreateIrregularForecastingData();

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

    #region Multi-horizon Forecasting Tests

    [Fact]
    public async Task TrainModelAsync_WithMultiHorizonForecastingData_HandlesMultipleScales()
    {
        // Arrange
        var trainingData = CreateMultiHorizonForecastingData();

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
    public async Task TrainModelAsync_WithVariableHorizonForecastingData_AdaptsToDifferentRanges()
    {
        // Arrange
        var trainingData = CreateVariableHorizonForecastingData();

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

    #region Volatility and Uncertainty Tests

    [Fact]
    public async Task TrainModelAsync_WithHighVolatilityData_HandlesUncertainty()
    {
        // Arrange
        var trainingData = CreateHighVolatilityForecastingData();

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
    public async Task TrainModelAsync_WithLowVolatilityData_HandlesStability()
    {
        // Arrange
        var trainingData = CreateLowVolatilityForecastingData();

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
    public async Task TrainModelAsync_WithBurstyData_HandlesSpikes()
    {
        // Arrange
        var trainingData = CreateBurstyForecastingData();

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

    #region Edge Case Forecasting Tests

    [Fact]
    public async Task TrainModelAsync_WithSparseForecastingData_HandlesMissingData()
    {
        // Arrange
        var trainingData = CreateSparseForecastingData();

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
    public async Task TrainModelAsync_WithOutlierForecastingData_HandlesAnomalies()
    {
        // Arrange
        var trainingData = CreateOutlierForecastingData();

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
    public async Task TrainModelAsync_WithMinimalForecastingData_HandlesEdgeCase()
    {
        // Arrange
        var trainingData = CreateMinimalForecastingData();

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

    private AITrainingData CreateShortTermForecastingData()
    {
        var random = new Random(42);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 144) // 24 hours of minute data
                .Select(i => 
                {
                    var minuteOfDay = i % 1440;
                    var trend = minuteOfDay * 0.001; // Small upward trend
                    var noise = random.NextDouble() * 0.1 - 0.05;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 100 + (int)(trend * 100) + random.Next(50),
                        SuccessfulExecutions = 90 + (int)(trend * 90) + random.Next(40),
                        FailedExecutions = 10 + (int)(trend * 10) + random.Next(10),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(50 + trend * 20 + random.Next(30)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(100 + trend * 40 + random.Next(60)),
                        ConcurrentExecutions = 5 + (int)(trend * 5) + random.Next(10),
                        MemoryUsage = (50 + (int)(trend * 50) + random.Next(30)) * 1024 * 1024,
                        DatabaseCalls = 10 + (int)(trend * 10) + random.Next(20),
                        ExternalApiCalls = 5 + (int)(trend * 5) + random.Next(10),
                        LastExecution = DateTime.UtcNow.AddMinutes(-144 + i),
                        SuccessRate = 0.85 + noise,
                        MemoryAllocated = (50 + (int)(trend * 50) + random.Next(30)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 72)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(40)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddMinutes(-72 * 20 + i * 20)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 288) // 5-minute intervals for 24 hours
                .Select(i => 
                {
                    var timeSlot = i % 288;
                    var hourlyPattern = Math.Sin((timeSlot / 288.0) * 2 * Math.PI) * 0.2;
                    var trend = timeSlot * 0.0005;
                    var noise = random.NextDouble() * 0.1 - 0.05;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddMinutes(-288 * 5 + i * 5),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 + hourlyPattern + trend + noise)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.5 + hourlyPattern * 0.8 + trend + noise)),
                        ThroughputPerSecond = (int)(50 + Math.Sin(timeSlot * 0.1) * 20 + trend * 100 + random.Next(30))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateRealTimeForecastingData()
    {
        var random = new Random(123);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 300) // 5 minutes of second-level data
                .Select(i => 
                {
                    var secondOfDay = i % 60;
                    var microTrend = i * 0.01;
                    var microNoise = random.NextDouble() * 0.05;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = 20 + (int)(microTrend) + random.Next(10),
                        SuccessfulExecutions = 18 + (int)(microTrend * 0.9) + random.Next(8),
                        FailedExecutions = 2 + (int)(microTrend * 0.1) + random.Next(2),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(30 + microTrend * 2 + random.Next(20)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(60 + microTrend * 4 + random.Next(40)),
                        ConcurrentExecutions = 2 + (int)(microTrend * 0.2) + random.Next(3),
                        MemoryUsage = (20 + (int)(microTrend * 2) + random.Next(15)) * 1024 * 1024,
                        DatabaseCalls = 3 + (int)(microTrend * 0.3) + random.Next(5),
                        ExternalApiCalls = 1 + (int)(microTrend * 0.1) + random.Next(2),
                        LastExecution = DateTime.UtcNow.AddSeconds(-300 + i),
                        SuccessRate = 0.9 + microNoise,
                        MemoryAllocated = (20 + (int)(microTrend * 2) + random.Next(15)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 150)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(10 + random.Next(20)),
                    PerformanceImprovement = random.NextDouble() * 0.2,
                    Timestamp = DateTime.UtcNow.AddSeconds(-150 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 600) // 10 minutes of 1-second intervals
                .Select(i => 
                {
                    var rapidOscillation = Math.Sin(i * 0.5) * 0.1;
                    var microTrend = i * 0.0001;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddSeconds(-600 + i),
                        CpuUtilization = Math.Max(0.05, Math.Min(0.95, 0.3 + rapidOscillation + microTrend)),
                        MemoryUtilization = Math.Max(0.05, Math.Min(0.95, 0.4 + rapidOscillation * 0.7 + microTrend)),
                        ThroughputPerSecond = (int)(30 + Math.Sin(i * 0.3) * 10 + microTrend * 50 + random.Next(20))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMinuteLevelForecastingData()
    {
        var random = new Random(456);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 1440) // 24 hours of minute data
                .Select(i => 
                {
                    var minuteOfDay = i % 1440;
                    var dailyPattern = Math.Sin((minuteOfDay / 1440.0) * 2 * Math.PI) * 0.3;
                    var hourlyPattern = Math.Sin((minuteOfDay / 60.0) * 2 * Math.PI) * 0.1;
                    var trend = i * 0.0002;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(50 + dailyPattern * 30 + hourlyPattern * 10 + trend * 100 + random.Next(20)),
                        SuccessfulExecutions = (int)(45 + dailyPattern * 27 + hourlyPattern * 9 + trend * 90 + random.Next(18)),
                        FailedExecutions = (int)(5 + dailyPattern * 3 + hourlyPattern * 1 + trend * 10 + random.Next(2)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(40 + dailyPattern * 20 + hourlyPattern * 5 + trend * 10 + random.Next(15)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(80 + dailyPattern * 40 + hourlyPattern * 10 + trend * 20 + random.Next(30)),
                        ConcurrentExecutions = (int)(3 + dailyPattern * 2 + hourlyPattern * 1 + trend * 5 + random.Next(5)),
                        MemoryUsage = (int)(30 + dailyPattern * 20 + hourlyPattern * 5 + trend * 20 + random.Next(20)) * 1024 * 1024,
                        DatabaseCalls = (int)(5 + dailyPattern * 3 + hourlyPattern * 1 + trend * 5 + random.Next(8)),
                        ExternalApiCalls = (int)(2 + dailyPattern * 1 + hourlyPattern * 0.5 + trend * 2 + random.Next(3)),
                        LastExecution = DateTime.UtcNow.AddMinutes(-1440 + i),
                        SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.85 + dailyPattern * 0.05 + random.NextDouble() * 0.05)),
                        MemoryAllocated = (int)(30 + dailyPattern * 20 + hourlyPattern * 5 + trend * 20 + random.Next(20)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 720)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.18,
                    ExecutionTime = TimeSpan.FromMilliseconds(15 + random.Next(25)),
                    PerformanceImprovement = random.NextDouble() * 0.25,
                    Timestamp = DateTime.UtcNow.AddMinutes(-720 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 2880) // 48 hours of minute data
                .Select(i => 
                {
                    var minuteOfDay = i % 1440;
                    var dailyPattern = Math.Sin((minuteOfDay / 1440.0) * 2 * Math.PI) * 0.25;
                    var trend = i * 0.0001;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddMinutes(-2880 + i),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 + dailyPattern + trend)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.5 + dailyPattern * 0.8 + trend)),
                        ThroughputPerSecond = (int)(40 + dailyPattern * 25 + trend * 50 + random.Next(25))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateHourlyForecastingData()
    {
        var random = new Random(789);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 168) // 7 days of hourly data
                .Select(i => 
                {
                    var hourOfDay = i % 24;
                    var dayOfWeek = i / 24;
                    var hourlyPattern = GetHourlyPattern(hourOfDay);
                    var weeklyPattern = GetWeeklyPattern(dayOfWeek);
                    var trend = i * 0.01;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(100 * hourlyPattern * weeklyPattern + trend + random.Next(50)),
                        SuccessfulExecutions = (int)(90 * hourlyPattern * weeklyPattern + trend * 0.9 + random.Next(40)),
                        FailedExecutions = (int)(10 * hourlyPattern * weeklyPattern + trend * 0.1 + random.Next(10)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(50 / (hourlyPattern * weeklyPattern) + trend * 2 + random.Next(30)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(100 / (hourlyPattern * weeklyPattern) + trend * 4 + random.Next(60)),
                        ConcurrentExecutions = (int)(5 * hourlyPattern * weeklyPattern + trend * 0.5 + random.Next(10)),
                        MemoryUsage = (int)(50 * hourlyPattern * weeklyPattern + trend * 10 + random.Next(50)) * 1024 * 1024,
                        DatabaseCalls = (int)(10 * hourlyPattern * weeklyPattern + trend * 2 + random.Next(20)),
                        ExternalApiCalls = (int)(5 * hourlyPattern * weeklyPattern + trend + random.Next(10)),
                        LastExecution = DateTime.UtcNow.AddHours(-168 + i),
                        SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.85 + random.NextDouble() * 0.1)),
                        MemoryAllocated = (int)(50 * hourlyPattern * weeklyPattern + trend * 10 + random.Next(50)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 84)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(50)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddHours(-84 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 336) // 14 days of hourly data
                .Select(i => 
                {
                    var hourOfDay = i % 24;
                    var dayOfWeek = i / 24;
                    var hourlyPattern = GetHourlyPattern(hourOfDay);
                    var weeklyPattern = GetWeeklyPattern(dayOfWeek);
                    var trend = i * 0.005;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddHours(-336 + i),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 * hourlyPattern * weeklyPattern + trend)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.5 * hourlyPattern * weeklyPattern + trend)),
                        ThroughputPerSecond = (int)(50 * hourlyPattern * weeklyPattern + trend * 20 + random.Next(30))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateDailyForecastingData()
    {
        var random = new Random(321);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 90) // 3 months of daily data
                .Select(i => 
                {
                    var dayOfWeek = i % 7;
                    var dayOfMonth = i % 30;
                    var weeklyPattern = GetWeeklyPattern(dayOfWeek);
                    var monthlyPattern = GetMonthlyPattern(dayOfMonth);
                    var trend = i * 0.1;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(200 * weeklyPattern * monthlyPattern + trend + random.Next(100)),
                        SuccessfulExecutions = (int)(180 * weeklyPattern * monthlyPattern + trend * 0.9 + random.Next(80)),
                        FailedExecutions = (int)(20 * weeklyPattern * monthlyPattern + trend * 0.1 + random.Next(20)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(60 / (weeklyPattern * monthlyPattern) + trend * 3 + random.Next(40)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(120 / (weeklyPattern * monthlyPattern) + trend * 6 + random.Next(80)),
                        ConcurrentExecutions = (int)(10 * weeklyPattern * monthlyPattern + trend + random.Next(15)),
                        MemoryUsage = (int)(100 * weeklyPattern * monthlyPattern + trend * 20 + random.Next(100)) * 1024 * 1024,
                        DatabaseCalls = (int)(20 * weeklyPattern * monthlyPattern + trend * 4 + random.Next(40)),
                        ExternalApiCalls = (int)(10 * weeklyPattern * monthlyPattern + trend * 2 + random.Next(20)),
                        LastExecution = DateTime.UtcNow.AddDays(-90 + i),
                        SuccessRate = Math.Max(0.75, Math.Min(0.95, 0.85 + random.NextDouble() * 0.1)),
                        MemoryAllocated = (int)(100 * weeklyPattern * monthlyPattern + trend * 20 + random.Next(100)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 45)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(30 + random.Next(60)),
                    PerformanceImprovement = random.NextDouble() * 0.35,
                    Timestamp = DateTime.UtcNow.AddDays(-45 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 180) // 6 months of daily data
                .Select(i => 
                {
                    var dayOfWeek = i % 7;
                    var dayOfMonth = i % 30;
                    var weeklyPattern = GetWeeklyPattern(dayOfWeek);
                    var monthlyPattern = GetMonthlyPattern(dayOfMonth);
                    var trend = i * 0.05;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-180 + i),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 * weeklyPattern * monthlyPattern + trend)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.5 * weeklyPattern * monthlyPattern + trend)),
                        ThroughputPerSecond = (int)(80 * weeklyPattern * monthlyPattern + trend * 30 + random.Next(40))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateWeeklyForecastingData()
    {
        var random = new Random(654);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 104) // 2 years of weekly data
                .Select(i => 
                {
                    var weekOfYear = i % 52;
                    var seasonalPattern = GetSeasonalPattern(weekOfYear);
                    var trend = i * 0.5;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(500 * seasonalPattern + trend + random.Next(200)),
                        SuccessfulExecutions = (int)(450 * seasonalPattern + trend * 0.9 + random.Next(150)),
                        FailedExecutions = (int)(50 * seasonalPattern + trend * 0.1 + random.Next(50)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(80 / seasonalPattern + trend * 5 + random.Next(50)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(160 / seasonalPattern + trend * 10 + random.Next(100)),
                        ConcurrentExecutions = (int)(20 * seasonalPattern + trend * 2 + random.Next(20)),
                        MemoryUsage = (int)(200 * seasonalPattern + trend * 50 + random.Next(200)) * 1024 * 1024,
                        DatabaseCalls = (int)(40 * seasonalPattern + trend * 8 + random.Next(80)),
                        ExternalApiCalls = (int)(20 * seasonalPattern + trend * 4 + random.Next(40)),
                        LastExecution = DateTime.UtcNow.AddDays(-104 * 7 + i * 7),
                        SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.85 + random.NextDouble() * 0.1)),
                        MemoryAllocated = (int)(200 * seasonalPattern + trend * 50 + random.Next(200)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 52)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.18,
                    ExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(80)),
                    PerformanceImprovement = random.NextDouble() * 0.4,
                    Timestamp = DateTime.UtcNow.AddDays(-52 * 7 + i * 7)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 208) // 4 years of weekly data
                .Select(i => 
                {
                    var weekOfYear = i % 52;
                    var seasonalPattern = GetSeasonalPattern(weekOfYear);
                    var trend = i * 0.25;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-208 * 7 + i * 7),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 * seasonalPattern + trend)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.5 * seasonalPattern + trend)),
                        ThroughputPerSecond = (int)(150 * seasonalPattern + trend * 50 + random.Next(60))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMonthlyForecastingData()
    {
        var random = new Random(987);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 60) // 5 years of monthly data
                .Select(i => 
                {
                    var monthOfYear = i % 12;
                    var seasonalPattern = GetMonthlySeasonalPattern(monthOfYear);
                    var trend = i * 2;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(1000 * seasonalPattern + trend + random.Next(400)),
                        SuccessfulExecutions = (int)(900 * seasonalPattern + trend * 0.9 + random.Next(300)),
                        FailedExecutions = (int)(100 * seasonalPattern + trend * 0.1 + random.Next(100)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(100 / seasonalPattern + trend * 8 + random.Next(60)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(200 / seasonalPattern + trend * 16 + random.Next(120)),
                        ConcurrentExecutions = (int)(30 * seasonalPattern + trend * 3 + random.Next(30)),
                        MemoryUsage = (int)(300 * seasonalPattern + trend * 80 + random.Next(300)) * 1024 * 1024,
                        DatabaseCalls = (int)(60 * seasonalPattern + trend * 12 + random.Next(120)),
                        ExternalApiCalls = (int)(30 * seasonalPattern + trend * 6 + random.Next(60)),
                        LastExecution = DateTime.UtcNow.AddMonths(-60 + i),
                        SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.85 + random.NextDouble() * 0.1)),
                        MemoryAllocated = (int)(300 * seasonalPattern + trend * 80 + random.Next(300)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 30)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(100)),
                    PerformanceImprovement = random.NextDouble() * 0.45,
                    Timestamp = DateTime.UtcNow.AddMonths(-30 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 120) // 10 years of monthly data
                .Select(i => 
                {
                    var monthOfYear = i % 12;
                    var seasonalPattern = GetMonthlySeasonalPattern(monthOfYear);
                    var trend = i * 1;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddMonths(-120 + i),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 * seasonalPattern + trend)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.5 * seasonalPattern + trend)),
                        ThroughputPerSecond = (int)(200 * seasonalPattern + trend * 80 + random.Next(80))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateQuarterlyForecastingData()
    {
        var random = new Random(111);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 40) // 10 years of quarterly data
                .Select(i => 
                {
                    var quarterOfYear = i % 4;
                    var businessCyclePattern = GetBusinessCyclePattern(quarterOfYear);
                    var trend = i * 5;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(2000 * businessCyclePattern + trend + random.Next(800)),
                        SuccessfulExecutions = (int)(1800 * businessCyclePattern + trend * 0.9 + random.Next(600)),
                        FailedExecutions = (int)(200 * businessCyclePattern + trend * 0.1 + random.Next(200)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(120 / businessCyclePattern + trend * 10 + random.Next(80)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(240 / businessCyclePattern + trend * 20 + random.Next(160)),
                        ConcurrentExecutions = (int)(40 * businessCyclePattern + trend * 4 + random.Next(40)),
                        MemoryUsage = (int)(400 * businessCyclePattern + trend * 100 + random.Next(400)) * 1024 * 1024,
                        DatabaseCalls = (int)(80 * businessCyclePattern + trend * 16 + random.Next(160)),
                        ExternalApiCalls = (int)(40 * businessCyclePattern + trend * 8 + random.Next(80)),
                        LastExecution = DateTime.UtcNow.AddMonths(-40 * 3 + i * 3),
                        SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.85 + random.NextDouble() * 0.1)),
                        MemoryAllocated = (int)(400 * businessCyclePattern + trend * 100 + random.Next(400)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 20)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(60 + random.Next(120)),
                    PerformanceImprovement = random.NextDouble() * 0.5,
                    Timestamp = DateTime.UtcNow.AddMonths(-20 * 6 + i * 6)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 80) // 20 years of quarterly data
                .Select(i => 
                {
                    var quarterOfYear = i % 4;
                    var businessCyclePattern = GetBusinessCyclePattern(quarterOfYear);
                    var trend = i * 2.5;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddMonths(-80 * 3 + i * 3),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 * businessCyclePattern + trend)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.5 * businessCyclePattern + trend)),
                        ThroughputPerSecond = (int)(300 * businessCyclePattern + trend * 120 + random.Next(100))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateYearlyForecastingData()
    {
        var random = new Random(222);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 20) // 20 years of yearly data
                .Select(i => 
                {
                    var yearTrend = i * 0.1;
                    var economicCycle = GetEconomicCyclePattern(i);
                    var trend = i * 10;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(5000 * economicCycle + trend + random.Next(2000)),
                        SuccessfulExecutions = (int)(4500 * economicCycle + trend * 0.9 + random.Next(1500)),
                        FailedExecutions = (int)(500 * economicCycle + trend * 0.1 + random.Next(500)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(150 / economicCycle + trend * 12 + random.Next(100)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(300 / economicCycle + trend * 24 + random.Next(200)),
                        ConcurrentExecutions = (int)(50 * economicCycle + trend * 5 + random.Next(50)),
                        MemoryUsage = (int)(500 * economicCycle + trend * 120 + random.Next(500)) * 1024 * 1024,
                        DatabaseCalls = (int)(100 * economicCycle + trend * 20 + random.Next(200)),
                        ExternalApiCalls = (int)(50 * economicCycle + trend * 10 + random.Next(100)),
                        LastExecution = DateTime.UtcNow.AddYears(-20 + i),
                        SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.85 + random.NextDouble() * 0.1)),
                        MemoryAllocated = (int)(500 * economicCycle + trend * 120 + random.Next(500)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 10)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(160)),
                    PerformanceImprovement = random.NextDouble() * 0.6,
                    Timestamp = DateTime.UtcNow.AddYears(-10 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 40) // 40 years of yearly data
                .Select(i => 
                {
                    var economicCycle = GetEconomicCyclePattern(i);
                    var trend = i * 5;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddYears(-40 + i),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 * economicCycle + trend)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.5 * economicCycle + trend)),
                        ThroughputPerSecond = (int)(400 * economicCycle + trend * 150 + random.Next(120))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateSeasonalForecastingData()
    {
        var random = new Random(333);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 365) // 1 year of daily data with strong seasonality
                .Select(i => 
                {
                    var dayOfYear = i;
                    var seasonalPattern = 0.8 + 0.6 * Math.Sin((dayOfYear / 365.0) * 2 * Math.PI - Math.PI / 2);
                    var weeklyPattern = 0.9 + 0.2 * Math.Sin((dayOfYear / 7.0) * 2 * Math.PI);
                    var combinedPattern = seasonalPattern * weeklyPattern;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(300 * combinedPattern + random.Next(150)),
                        SuccessfulExecutions = (int)(270 * combinedPattern + random.Next(120)),
                        FailedExecutions = (int)(30 * combinedPattern + random.Next(30)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(70 / combinedPattern + random.Next(50)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(140 / combinedPattern + random.Next(100)),
                        ConcurrentExecutions = (int)(15 * combinedPattern + random.Next(15)),
                        MemoryUsage = (int)(150 * combinedPattern + random.Next(150)) * 1024 * 1024,
                        DatabaseCalls = (int)(30 * combinedPattern + random.Next(30)),
                        ExternalApiCalls = (int)(15 * combinedPattern + random.Next(15)),
                        LastExecution = DateTime.UtcNow.AddDays(-365 + i),
                        SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.85 + random.NextDouble() * 0.1)),
                        MemoryAllocated = (int)(150 * combinedPattern + random.Next(150)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 180)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(35 + random.Next(70)),
                    PerformanceImprovement = random.NextDouble() * 0.4,
                    Timestamp = DateTime.UtcNow.AddDays(-180 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 730) // 2 years of daily data
                .Select(i => 
                {
                    var dayOfYear = i % 365;
                    var seasonalPattern = 0.8 + 0.5 * Math.Sin((dayOfYear / 365.0) * 2 * Math.PI - Math.PI / 2);
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-730 + i),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 * seasonalPattern + random.NextDouble() * 0.1)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.5 * seasonalPattern + random.NextDouble() * 0.1)),
                        ThroughputPerSecond = (int)(120 * seasonalPattern + random.Next(60))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateTrendingForecastingData()
    {
        var random = new Random(444);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 200) // Data with clear trend
                .Select(i => 
                {
                    var linearTrend = i * 2.5; // Linear growth
                    var exponentialTrend = Math.Pow(1.01, i); // Slight exponential growth
                    var combinedTrend = linearTrend * exponentialTrend;
                    var noise = random.NextDouble() * 0.2 - 0.1;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(100 + combinedTrend + random.Next(50)),
                        SuccessfulExecutions = (int)(90 + combinedTrend * 0.9 + random.Next(40)),
                        FailedExecutions = (int)(10 + combinedTrend * 0.1 + random.Next(10)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 50 - combinedTrend * 0.05 + random.Next(30))),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(1, 100 - combinedTrend * 0.1 + random.Next(60))),
                        ConcurrentExecutions = (int)(5 + combinedTrend * 0.02 + random.Next(10)),
                        MemoryUsage = (int)(50 + combinedTrend * 0.5 + random.Next(50)) * 1024 * 1024,
                        DatabaseCalls = (int)(10 + combinedTrend * 0.1 + random.Next(20)),
                        ExternalApiCalls = (int)(5 + combinedTrend * 0.05 + random.Next(10)),
                        LastExecution = DateTime.UtcNow.AddDays(-200 + i),
                        SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.8 + combinedTrend * 0.001 + noise)),
                        MemoryAllocated = (int)(50 + combinedTrend * 0.5 + random.Next(50)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 100)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.15,
                    ExecutionTime = TimeSpan.FromMilliseconds(30 + random.Next(60)),
                    PerformanceImprovement = random.NextDouble() * 0.35,
                    Timestamp = DateTime.UtcNow.AddDays(-100 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 400)
                .Select(i => 
                {
                    var trend = i * 0.5;
                    var noise = random.NextDouble() * 20 - 10;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-400 + i),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.3 + trend * 0.01 + noise * 0.01)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 + trend * 0.01 + noise * 0.01)),
                        ThroughputPerSecond = (int)(50 + trend + noise)
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateCyclicalForecastingData()
    {
        var random = new Random(555);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 240) // Data with cycles
                .Select(i => 
                {
                    var primaryCycle = Math.Sin(i * 0.1) * 0.3; // Long cycle
                    var secondaryCycle = Math.Sin(i * 0.5) * 0.2; // Medium cycle
                    var tertiaryCycle = Math.Sin(i * 2.0) * 0.1; // Short cycle
                    var combinedCycles = 1.0 + primaryCycle + secondaryCycle + tertiaryCycle;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(200 * combinedCycles + random.Next(100)),
                        SuccessfulExecutions = (int)(180 * combinedCycles + random.Next(80)),
                        FailedExecutions = (int)(20 * combinedCycles + random.Next(20)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(60 / combinedCycles + random.Next(40)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(120 / combinedCycles + random.Next(80)),
                        ConcurrentExecutions = (int)(10 * combinedCycles + random.Next(12)),
                        MemoryUsage = (int)(100 * combinedCycles + random.Next(100)) * 1024 * 1024,
                        DatabaseCalls = (int)(20 * combinedCycles + random.Next(25)),
                        ExternalApiCalls = (int)(10 * combinedCycles + random.Next(12)),
                        LastExecution = DateTime.UtcNow.AddDays(-240 + i),
                        SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.85 + random.NextDouble() * 0.1)),
                        MemoryAllocated = (int)(100 * combinedCycles + random.Next(100)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 120)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.18,
                    ExecutionTime = TimeSpan.FromMilliseconds(32 + random.Next(64)),
                    PerformanceImprovement = random.NextDouble() * 0.38,
                    Timestamp = DateTime.UtcNow.AddDays(-120 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 480)
                .Select(i => 
                {
                    var primaryCycle = Math.Sin(i * 0.05) * 0.25;
                    var secondaryCycle = Math.Sin(i * 0.2) * 0.15;
                    var combinedCycles = 1.0 + primaryCycle + secondaryCycle;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-480 + i),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 * combinedCycles + random.NextDouble() * 0.1)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.5 * combinedCycles + random.NextDouble() * 0.1)),
                        ThroughputPerSecond = (int)(80 * combinedCycles + random.Next(40))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateIrregularForecastingData()
    {
        var random = new Random(666);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 180)
                .Select(i => 
                {
                    // Create irregular patterns with random jumps
                    var baseValue = 100;
                    var irregularJump = random.NextDouble() > 0.9 ? random.Next(50, 200) : 0;
                    var randomWalk = random.NextDouble() * 20 - 10;
                    var value = baseValue + irregularJump + randomWalk;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(value + random.Next(50)),
                        SuccessfulExecutions = (int)(value * 0.9 + random.Next(40)),
                        FailedExecutions = (int)(value * 0.1 + random.Next(10)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(1000 / value + random.Next(50)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(2000 / value + random.Next(100)),
                        ConcurrentExecutions = (int)(value / 20 + random.Next(8)),
                        MemoryUsage = (int)(value * 2 + random.Next(100)) * 1024 * 1024,
                        DatabaseCalls = (int)(value / 10 + random.Next(15)),
                        ExternalApiCalls = (int)(value / 20 + random.Next(8)),
                        LastExecution = DateTime.UtcNow.AddDays(-180 + i),
                        SuccessRate = Math.Max(0.6, Math.Min(0.95, 0.85 + random.NextDouble() * 0.15)),
                        MemoryAllocated = (int)(value * 2 + random.Next(100)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 90)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.25,
                    ExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(80)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddDays(-90 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 360)
                .Select(i => 
                {
                    var baseLoad = 50;
                    var irregularSpike = random.NextDouble() > 0.85 ? random.Next(100, 300) : 0;
                    var randomFluctuation = random.NextDouble() * 30 - 15;
                    var load = baseLoad + irregularSpike + randomFluctuation;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-360 + i),
                        CpuUtilization = Math.Max(0.05, Math.Min(0.95, load / 500.0 + random.NextDouble() * 0.1)),
                        MemoryUtilization = Math.Max(0.05, Math.Min(0.95, load / 400.0 + random.NextDouble() * 0.1)),
                        ThroughputPerSecond = (int)load
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMultiHorizonForecastingData()
    {
        var random = new Random(777);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 300)
                .Select(i => 
                {
                    // Combine multiple time scales
                    var dailyPattern = Math.Sin((i % 24) / 24.0 * 2 * Math.PI) * 0.2;
                    var weeklyPattern = Math.Sin((i % 168) / 168.0 * 2 * Math.PI) * 0.3;
                    var monthlyTrend = (i % 720) / 720.0 * 0.5;
                    var combinedPattern = 1.0 + dailyPattern + weeklyPattern + monthlyTrend;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(150 * combinedPattern + random.Next(75)),
                        SuccessfulExecutions = (int)(135 * combinedPattern + random.Next(60)),
                        FailedExecutions = (int)(15 * combinedPattern + random.Next(15)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(55 / combinedPattern + random.Next(35)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(110 / combinedPattern + random.Next(70)),
                        ConcurrentExecutions = (int)(8 * combinedPattern + random.Next(10)),
                        MemoryUsage = (int)(80 * combinedPattern + random.Next(80)) * 1024 * 1024,
                        DatabaseCalls = (int)(16 * combinedPattern + random.Next(20)),
                        ExternalApiCalls = (int)(8 * combinedPattern + random.Next(10)),
                        LastExecution = DateTime.UtcNow.AddHours(-300 + i),
                        SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.85 + random.NextDouble() * 0.1)),
                        MemoryAllocated = (int)(80 * combinedPattern + random.Next(80)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 150)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.2,
                    ExecutionTime = TimeSpan.FromMilliseconds(28 + random.Next(56)),
                    PerformanceImprovement = random.NextDouble() * 0.32,
                    Timestamp = DateTime.UtcNow.AddHours(-150 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 600)
                .Select(i => 
                {
                    var hourlyPattern = Math.Sin((i % 24) / 24.0 * 2 * Math.PI) * 0.15;
                    var dailyPattern = Math.Sin((i % 168) / 168.0 * 2 * Math.PI) * 0.25;
                    var trend = i * 0.001;
                    var combinedPattern = 1.0 + hourlyPattern + dailyPattern + trend;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddHours(-600 + i),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 * combinedPattern + random.NextDouble() * 0.05)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.5 * combinedPattern + random.NextDouble() * 0.05)),
                        ThroughputPerSecond = (int)(70 * combinedPattern + random.Next(35))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateVariableHorizonForecastingData()
    {
        var random = new Random(888);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 250)
                .Select(i => 
                {
                    // Variable patterns that change over time
                    var phase = i / 50; // Change pattern every 50 data points
                    var pattern = phase switch
                    {
                        0 => Math.Sin(i * 0.1) * 0.3, // Slow sine wave
                        1 => Math.Cos(i * 0.2) * 0.4, // Medium cosine wave
                        2 => Math.Sin(i * 0.5) * 0.2, // Fast sine wave
                        3 => i * 0.01, // Linear trend
                        4 => 0.5 + Math.Sin(i * 0.3) * 0.3, // Offset sine wave
                        _ => 0.0
                    };
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(120 * (1.0 + pattern) + random.Next(60)),
                        SuccessfulExecutions = (int)(108 * (1.0 + pattern) + random.Next(48)),
                        FailedExecutions = (int)(12 * (1.0 + pattern) + random.Next(12)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(50 / (1.0 + pattern) + random.Next(30)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(100 / (1.0 + pattern) + random.Next(60)),
                        ConcurrentExecutions = (int)(6 * (1.0 + pattern) + random.Next(8)),
                        MemoryUsage = (int)(60 * (1.0 + pattern) + random.Next(60)) * 1024 * 1024,
                        DatabaseCalls = (int)(12 * (1.0 + pattern) + random.Next(15)),
                        ExternalApiCalls = (int)(6 * (1.0 + pattern) + random.Next(8)),
                        LastExecution = DateTime.UtcNow.AddHours(-250 + i),
                        SuccessRate = Math.Max(0.7, Math.Min(0.95, 0.85 + random.NextDouble() * 0.1)),
                        MemoryAllocated = (int)(60 * (1.0 + pattern) + random.Next(60)) * 1024 * 1024
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 125)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.22,
                    ExecutionTime = TimeSpan.FromMilliseconds(25 + random.Next(50)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddHours(-125 * 2 + i * 2)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 500)
                .Select(i => 
                {
                    var phase = i / 100;
                    var pattern = phase switch
                    {
                        0 => Math.Sin(i * 0.05) * 0.2,
                        1 => Math.Cos(i * 0.1) * 0.3,
                        2 => i * 0.002,
                        3 => 0.5 + Math.Sin(i * 0.15) * 0.2,
                        4 => Math.Sin(i * 0.08) * 0.25,
                        _ => 0.0
                    };
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddHours(-500 + i),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.9, 0.4 * (1.0 + pattern) + random.NextDouble() * 0.05)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.9, 0.5 * (1.0 + pattern) + random.NextDouble() * 0.05)),
                        ThroughputPerSecond = (int)(60 * (1.0 + pattern) + random.Next(30))
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateHighVolatilityForecastingData()
    {
        var random = new Random(999);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 200)
                .Select(i => 
                {
                    var baseValue = 100;
                    var highVolatility = random.NextDouble() * 80 - 40; // ±40% variation
                    var extremeSpikes = random.NextDouble() > 0.95 ? random.Next(100, 200) : 0;
                    var value = baseValue + highVolatility + extremeSpikes;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(Math.Max(10, value) + random.Next(50)),
                        SuccessfulExecutions = (int)(Math.Max(9, value * 0.9) + random.Next(40)),
                        FailedExecutions = (int)(Math.Max(1, value * 0.1) + random.Next(10)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(10, 1000 / value) + random.Next(100)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(20, 2000 / value) + random.Next(200)),
                        ConcurrentExecutions = (int)(Math.Max(1, value / 20) + random.Next(15)),
                        MemoryUsage = (int)(Math.Max(10 * 1024 * 1024, value * 2 * 1024 * 1024) + random.Next(100) * 1024 * 1024),
                        DatabaseCalls = (int)(Math.Max(1, value / 10) + random.Next(20)),
                        ExternalApiCalls = (int)(Math.Max(1, value / 20) + random.Next(10)),
                        LastExecution = DateTime.UtcNow.AddDays(-200 + i),
                        SuccessRate = Math.Max(0.5, Math.Min(0.98, 0.8 + random.NextDouble() * 0.3)),
                        MemoryAllocated = (int)(Math.Max(10 * 1024 * 1024, value * 2 * 1024 * 1024) + random.Next(100) * 1024 * 1024)
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 100)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.3, // Lower success rate due to volatility
                    ExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(100)),
                    PerformanceImprovement = random.NextDouble() * 0.4 - 0.1, // Can be negative
                    Timestamp = DateTime.UtcNow.AddDays(-100 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 400)
                .Select(i => 
                {
                    var baseLoad = 50;
                    var volatility = random.NextDouble() * 60 - 30; // ±30% variation
                    var spikes = random.NextDouble() > 0.9 ? random.Next(100, 200) : 0;
                    var load = Math.Max(5, baseLoad + volatility + spikes);
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-400 + i),
                        CpuUtilization = Math.Max(0.05, Math.Min(0.98, load / 300.0)),
                        MemoryUtilization = Math.Max(0.05, Math.Min(0.98, load / 250.0)),
                        ThroughputPerSecond = (int)load
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateLowVolatilityForecastingData()
    {
        var random = new Random(101);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 200)
                .Select(i => 
                {
                    var baseValue = 100;
                    var lowVolatility = random.NextDouble() * 10 - 5; // ±5% variation
                    var gentleTrend = i * 0.05;
                    var value = baseValue + lowVolatility + gentleTrend;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(value + random.Next(20)),
                        SuccessfulExecutions = (int)(value * 0.95 + random.Next(18)),
                        FailedExecutions = (int)(value * 0.05 + random.Next(2)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(80 + random.Next(20)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(160 + random.Next(40)),
                        ConcurrentExecutions = (int)(value / 10 + random.Next(3)),
                        MemoryUsage = (int)(value * 1024 * 1024 + random.Next(20) * 1024 * 1024),
                        DatabaseCalls = (int)(value / 5 + random.Next(5)),
                        ExternalApiCalls = (int)(value / 10 + random.Next(3)),
                        LastExecution = DateTime.UtcNow.AddDays(-200 + i),
                        SuccessRate = Math.Max(0.9, Math.Min(0.98, 0.94 + random.NextDouble() * 0.04)),
                        MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(20) * 1024 * 1024)
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 100)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.1, // High success rate due to stability
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + random.Next(30)),
                    PerformanceImprovement = random.NextDouble() * 0.2,
                    Timestamp = DateTime.UtcNow.AddDays(-100 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 400)
                .Select(i => 
                {
                    var baseLoad = 80;
                    var lowVolatility = random.NextDouble() * 8 - 4; // ±4% variation
                    var gentleTrend = i * 0.02;
                    var load = baseLoad + lowVolatility + gentleTrend;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-400 + i),
                        CpuUtilization = Math.Max(0.3, Math.Min(0.7, load / 200.0)),
                        MemoryUtilization = Math.Max(0.4, Math.Min(0.8, load / 150.0)),
                        ThroughputPerSecond = (int)load
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateBurstyForecastingData()
    {
        var random = new Random(202);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 180)
                .Select(i => 
                {
                    var baseValue = 50;
                    var burst = random.NextDouble() > 0.8 ? random.Next(100, 500) : 0; // 20% chance of burst
                    var value = baseValue + burst;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(value + random.Next(30)),
                        SuccessfulExecutions = (int)(value * 0.92 + random.Next(25)),
                        FailedExecutions = (int)(value * 0.08 + random.Next(5)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(burst > 0 ? 200 : 60 + random.Next(40)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(burst > 0 ? 400 : 120 + random.Next(80)),
                        ConcurrentExecutions = (int)(value / 5 + random.Next(10)),
                        MemoryUsage = (int)(value * 1024 * 1024 + random.Next(50) * 1024 * 1024),
                        DatabaseCalls = (int)(value / 3 + random.Next(15)),
                        ExternalApiCalls = (int)(value / 6 + random.Next(8)),
                        LastExecution = DateTime.UtcNow.AddDays(-180 + i),
                        SuccessRate = Math.Max(0.6, Math.Min(0.95, burst > 0 ? 0.75 : 0.88 + random.NextDouble() * 0.07)),
                        MemoryAllocated = (int)(value * 1024 * 1024 + random.Next(50) * 1024 * 1024)
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 90)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.25,
                    ExecutionTime = TimeSpan.FromMilliseconds(random.NextDouble() > 0.8 ? 100 : 30 + random.Next(50)),
                    PerformanceImprovement = random.NextDouble() * 0.35,
                    Timestamp = DateTime.UtcNow.AddDays(-90 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 360)
                .Select(i => 
                {
                    var baseLoad = 40;
                    var burst = random.NextDouble() > 0.85 ? random.Next(150, 400) : 0; // 15% chance of burst
                    var load = baseLoad + burst;
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-360 + i),
                        CpuUtilization = Math.Max(0.1, Math.Min(0.95, load / 500.0)),
                        MemoryUtilization = Math.Max(0.1, Math.Min(0.95, load / 400.0)),
                        ThroughputPerSecond = (int)load
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateSparseForecastingData()
    {
        var random = new Random(303);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 100)
                .Select(i => 
                {
                    // Create sparse data with many missing values simulated as minimal valid values
                    var isMissing = random.NextDouble() > 0.7; // 30% missing data
                    var value = isMissing ? 1 : (50 + random.Next(100)); // Use 1 instead of 0 for missing data
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)value,
                        SuccessfulExecutions = isMissing ? 1 : (int)(value * 0.9), // Minimum 1 for missing data
                        FailedExecutions = isMissing ? 0 : (int)(value * 0.1),
                        AverageExecutionTime = isMissing ? TimeSpan.FromMilliseconds(1) : TimeSpan.FromMilliseconds(60 + random.Next(40)), // Minimal positive time
                        P95ExecutionTime = isMissing ? TimeSpan.FromMilliseconds(1) : TimeSpan.FromMilliseconds(120 + random.Next(80)), // Minimal positive time
                        ConcurrentExecutions = isMissing ? 1 : (int)(value / 10 + random.Next(5)), // Minimum 1 for missing data
                        MemoryUsage = isMissing ? 1024 * 1024 : (int)(value * 1024 * 1024 + random.Next(50) * 1024 * 1024), // Minimal memory
                        DatabaseCalls = isMissing ? 1 : (int)(value / 5 + random.Next(10)), // Minimum 1 for missing data
                        ExternalApiCalls = isMissing ? 0 : (int)(value / 10 + random.Next(5)),
                        LastExecution = isMissing ? DateTime.UtcNow.AddDays(-100 + i) : DateTime.UtcNow.AddDays(-100 + i), // Valid timestamp
                        SuccessRate = isMissing ? 0.5 : Math.Max(0.7, Math.Min(0.95, 0.85 + random.NextDouble() * 0.1)), // Lower success rate for missing
                        MemoryAllocated = isMissing ? 1024 * 1024 : (int)(value * 1024 * 1024 + random.Next(50) * 1024 * 1024) // Minimal memory
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 50)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.3,
                    ExecutionTime = TimeSpan.FromMilliseconds(40 + random.Next(80)),
                    PerformanceImprovement = random.NextDouble() * 0.3,
                    Timestamp = DateTime.UtcNow.AddDays(-50 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 200)
                .Select(i => 
                {
                    var isMissing = random.NextDouble() > 0.75; // 25% missing data
                    var load = isMissing ? 1 : (30 + random.Next(70)); // Use 1 instead of 0 for missing data
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-200 + i),
                        CpuUtilization = isMissing ? 0.01 : Math.Max(0.05, Math.Min(0.9, load / 200.0)), // Minimal positive utilization
                        MemoryUtilization = isMissing ? 0.01 : Math.Max(0.05, Math.Min(0.9, load / 150.0)), // Minimal positive utilization
                        ThroughputPerSecond = (int)load
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateOutlierForecastingData()
    {
        var random = new Random(404);
        
        return new AITrainingData
        {
            ExecutionHistory = Enumerable.Range(0, 120)
                .Select(i => 
                {
                    var baseValue = 80;
                    var outlier = random.NextDouble() > 0.9 ? random.Next(-50, 300) : 0; // 10% outliers
                    var value = baseValue + outlier;
                    
                    return new RequestExecutionMetrics
                    {
                        TotalExecutions = (int)(Math.Max(10, value) + random.Next(40)),
                        SuccessfulExecutions = (int)(Math.Max(9, value * 0.9) + random.Next(32)),
                        FailedExecutions = (int)(Math.Max(1, value * 0.1) + random.Next(8)),
                        AverageExecutionTime = TimeSpan.FromMilliseconds(Math.Max(10, 1000 / Math.Max(10, value)) + random.Next(50)),
                        P95ExecutionTime = TimeSpan.FromMilliseconds(Math.Max(20, 2000 / Math.Max(10, value)) + random.Next(100)),
                        ConcurrentExecutions = (int)(Math.Max(1, value / 8) + random.Next(8)),
                        MemoryUsage = (int)(Math.Max(10 * 1024 * 1024, value * 1024 * 1024) + random.Next(80) * 1024 * 1024),
                        DatabaseCalls = (int)(Math.Max(1, value / 4) + random.Next(12)),
                        ExternalApiCalls = (int)(Math.Max(1, value / 8) + random.Next(6)),
                        LastExecution = DateTime.UtcNow.AddDays(-120 + i),
                        SuccessRate = Math.Max(0.5, Math.Min(0.98, 0.85 + random.NextDouble() * 0.2)),
                        MemoryAllocated = (int)(Math.Max(10 * 1024 * 1024, value * 1024 * 1024) + random.Next(80) * 1024 * 1024)
                    };
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 60)
                .Select(i => new AIOptimizationResult
                {
                    Strategy = (OptimizationStrategy)random.Next(1, 8),
                    Success = random.NextDouble() > 0.25,
                    ExecutionTime = TimeSpan.FromMilliseconds(35 + random.Next(70)),
                    PerformanceImprovement = random.NextDouble() * 0.35,
                    Timestamp = DateTime.UtcNow.AddDays(-60 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 240)
                .Select(i => 
                {
                    var baseLoad = 60;
                    var outlier = random.NextDouble() > 0.92 ? random.Next(-100, 400) : 0; // 8% outliers
                    var load = Math.Max(0, baseLoad + outlier);
                    
                    return new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddDays(-240 + i),
                        CpuUtilization = Math.Max(0.0, Math.Min(0.98, load / 500.0)),
                        MemoryUtilization = Math.Max(0.0, Math.Min(0.98, load / 400.0)),
                        ThroughputPerSecond = (int)load
                    };
                })
                .ToArray()
        };
    }

    private AITrainingData CreateMinimalForecastingData()
    {
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
                    MemoryUsage = (20 + i) * 1024 * 1024,
                    DatabaseCalls = 3 + i,
                    ExternalApiCalls = 1 + i,
                    LastExecution = DateTime.UtcNow.AddMinutes(-10 + i),
                    SuccessRate = 0.9,
                    MemoryAllocated = (20 + i) * 1024 * 1024
                })
                .ToArray(),

            OptimizationHistory = Enumerable.Range(0, 5) // Minimum required: 5
                .Select(i => new AIOptimizationResult
                {
                    Strategy = OptimizationStrategy.EnableCaching,
                    Success = true,
                    ExecutionTime = TimeSpan.FromMilliseconds(20 + i),
                    PerformanceImprovement = 0.1 + i * 0.01,
                    Timestamp = DateTime.UtcNow.AddMinutes(-5 + i)
                })
                .ToArray(),

            SystemLoadHistory = Enumerable.Range(0, 10) // Minimum required: 10
                .Select(i => new SystemLoadMetrics
                {
                    Timestamp = DateTime.UtcNow.AddMinutes(-10 + i),
                    CpuUtilization = 0.3 + i * 0.01,
                    MemoryUtilization = 0.4 + i * 0.01,
                    ThroughputPerSecond = 10 + i
                })
                .ToArray()
        };
    }

    #endregion

    #region Helper Methods

    private static double GetHourlyPattern(int hourOfDay)
    {
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

    private static double GetWeeklyPattern(int dayOfWeek)
    {
        return dayOfWeek switch
        {
            0 => 0.6, // Sunday
            1 => 1.0, // Monday
            2 => 1.1, // Tuesday
            3 => 1.2, // Wednesday
            4 => 1.1, // Thursday
            5 => 0.9, // Friday
            6 => 0.5, // Saturday
            _ => 1.0
        };
    }

    private static double GetMonthlyPattern(int dayOfMonth)
    {
        // Beginning and end of month are typically busier
        if (dayOfMonth <= 5 || dayOfMonth >= 25) return 1.2;
        if (dayOfMonth >= 10 && dayOfMonth <= 20) return 0.8;
        return 1.0;
    }

    private static double GetSeasonalPattern(int weekOfYear)
    {
        // Northern hemisphere pattern: summer peak, winter low
        var weekNormalized = weekOfYear / 52.0;
        return 0.8 + 0.4 * Math.Sin(weekNormalized * 2 * Math.PI - Math.PI / 2);
    }

    private static double GetMonthlySeasonalPattern(int monthOfYear)
    {
        return monthOfYear switch
        {
            12 or 1 or 2 => 0.7, // Winter
            3 or 4 or 5 => 1.1, // Spring
            6 or 7 or 8 => 1.3, // Summer
            9 or 10 or 11 => 0.9, // Fall
            _ => 1.0
        };
    }

    private static double GetBusinessCyclePattern(int quarter)
    {
        return quarter switch
        {
            0 => 1.2, // Q1 - New year planning
            1 => 1.0, // Q2 - Steady
            2 => 0.8, // Q3 - Summer slowdown
            3 => 1.1, // Q4 - Year-end rush
            _ => 1.0
        };
    }

    private static double GetEconomicCyclePattern(int year)
    {
        // Simulate ~7-year economic cycles
        var cyclePosition = year % 7;
        return 0.8 + 0.4 * Math.Sin((cyclePosition / 7.0) * 2 * Math.PI);
    }

    #endregion

    public void Dispose()
    {
        _trainer?.Dispose();
    }
}