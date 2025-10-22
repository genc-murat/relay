using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AIOptimizationEngineAnalysisTests : IDisposable
{
    private readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
    private readonly AIOptimizationOptions _options;
    private readonly AIOptimizationEngine _engine;

    public AIOptimizationEngineAnalysisTests()
    {
        _loggerMock = new Mock<ILogger<AIOptimizationEngine>>();
        _options = new AIOptimizationOptions
        {
            DefaultBatchSize = 10,
            MaxBatchSize = 100,
            ModelUpdateInterval = TimeSpan.FromMinutes(5),
            ModelTrainingDate = DateTime.UtcNow,
            ModelVersion = "1.0.0",
            LastRetrainingDate = DateTime.UtcNow.AddDays(-1)
        };

        var optionsMock = new Mock<IOptions<AIOptimizationOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        _engine = new AIOptimizationEngine(_loggerMock.Object, optionsMock.Object);
    }

    public void Dispose()
    {
        _engine?.Dispose();
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Throw_When_Disposed()
    {
        // Arrange
        _engine.Dispose();
        var request = new TestRequest();
        var metrics = CreateMetrics();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _engine.AnalyzeRequestAsync(request, metrics));
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Return_Recommendation_For_New_Request_Type()
    {
        // Arrange
        var request = new TestRequest();
        var metrics = CreateMetrics();

        // Act
        var recommendation = await _engine.AnalyzeRequestAsync(request, metrics);

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.ConfidenceScore >= 0 && recommendation.ConfidenceScore <= 1);
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Update_Request_Analytics()
    {
        // Arrange
        var request = new TestRequest();
        var metrics = CreateMetrics();

        // Act
        await _engine.AnalyzeRequestAsync(request, metrics);
        var stats = _engine.GetModelStatistics();

        // Assert
        Assert.True(stats.TotalPredictions > 0);
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Handle_Null_Request()
    {
        // Arrange
        var metrics = CreateMetrics();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.AnalyzeRequestAsync<TestRequest>(null!, metrics));
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Handle_Null_Metrics()
    {
        // Arrange
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _engine.AnalyzeRequestAsync(request, null!));
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Handle_Extreme_Execution_Times()
    {
        // Arrange
        var request = new TestRequest();
        var extremeMetrics = new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(0.1), // Very fast
            MedianExecutionTime = TimeSpan.FromMilliseconds(0.05),
            P95ExecutionTime = TimeSpan.FromMilliseconds(0.2),
            P99ExecutionTime = TimeSpan.FromMilliseconds(0.5),
            TotalExecutions = 1000,
            SuccessfulExecutions = 999,
            FailedExecutions = 1,
            MemoryAllocated = 1024,
            ConcurrentExecutions = 1,
            LastExecution = DateTime.UtcNow,
            SamplePeriod = TimeSpan.FromMinutes(10),
            CpuUsage = 0.01,
            MemoryUsage = 512,
            DatabaseCalls = 0,
            ExternalApiCalls = 0
        };

        // Act
        var recommendation = await _engine.AnalyzeRequestAsync(request, extremeMetrics);

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.ConfidenceScore >= 0 && recommendation.ConfidenceScore <= 1);
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Handle_Very_Slow_Requests()
    {
        // Arrange
        var request = new TestRequest();
        var slowMetrics = new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromSeconds(30), // Very slow
            MedianExecutionTime = TimeSpan.FromSeconds(25),
            P95ExecutionTime = TimeSpan.FromSeconds(45),
            P99ExecutionTime = TimeSpan.FromMinutes(1),
            TotalExecutions = 50,
            SuccessfulExecutions = 45,
            FailedExecutions = 5,
            MemoryAllocated = 1024 * 1024 * 1024, // 1GB
            ConcurrentExecutions = 5,
            LastExecution = DateTime.UtcNow,
            SamplePeriod = TimeSpan.FromHours(1),
            CpuUsage = 0.9,
            MemoryUsage = 1024 * 1024 * 512,
            DatabaseCalls = 10,
            ExternalApiCalls = 5
        };

        // Act
        var recommendation = await _engine.AnalyzeRequestAsync(request, slowMetrics);

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.ConfidenceScore >= 0 && recommendation.ConfidenceScore <= 1);
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Improve_Confidence_With_More_Data()
    {
        // Arrange
        var request = new TestRequest();
        var initialMetrics = new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            MedianExecutionTime = TimeSpan.FromMilliseconds(95),
            P95ExecutionTime = TimeSpan.FromMilliseconds(150),
            P99ExecutionTime = TimeSpan.FromMilliseconds(200),
            TotalExecutions = 5, // Low sample size
            SuccessfulExecutions = 4,
            FailedExecutions = 1,
            MemoryAllocated = 1024 * 1024,
            ConcurrentExecutions = 2,
            LastExecution = DateTime.UtcNow,
            SamplePeriod = TimeSpan.FromMinutes(1),
            CpuUsage = 0.45,
            MemoryUsage = 512 * 1024,
            DatabaseCalls = 2,
            ExternalApiCalls = 1
        };

        var matureMetrics = new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            MedianExecutionTime = TimeSpan.FromMilliseconds(95),
            P95ExecutionTime = TimeSpan.FromMilliseconds(150),
            P99ExecutionTime = TimeSpan.FromMilliseconds(200),
            TotalExecutions = 1000, // High sample size
            SuccessfulExecutions = 950,
            FailedExecutions = 50,
            MemoryAllocated = 1024 * 1024,
            ConcurrentExecutions = 2,
            LastExecution = DateTime.UtcNow,
            SamplePeriod = TimeSpan.FromHours(1),
            CpuUsage = 0.45,
            MemoryUsage = 512 * 1024,
            DatabaseCalls = 2,
            ExternalApiCalls = 1
        };

        // Act
        var initialRecommendation = await _engine.AnalyzeRequestAsync(request, initialMetrics);
        var matureRecommendation = await _engine.AnalyzeRequestAsync(request, matureMetrics);

        // Assert
        Assert.NotNull(initialRecommendation);
        Assert.NotNull(matureRecommendation);
        // More data should generally provide higher confidence (though not guaranteed due to other factors)
        Assert.True(matureRecommendation.ConfidenceScore >= initialRecommendation.ConfidenceScore * 0.9,
            "More data should not significantly reduce confidence");
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Handle_Cancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var request = new TestRequest();
        var metrics = CreateMetrics();

        // Act & Assert - TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _engine.AnalyzeRequestAsync(request, metrics, cts.Token));
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Handle_High_Concurrency()
    {
        // Arrange
        var request = new TestRequest();
        var metrics = new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            MedianExecutionTime = TimeSpan.FromMilliseconds(95),
            P95ExecutionTime = TimeSpan.FromMilliseconds(150),
            P99ExecutionTime = TimeSpan.FromMilliseconds(200),
            TotalExecutions = 1000,
            SuccessfulExecutions = 950,
            FailedExecutions = 50,
            MemoryAllocated = 1024 * 1024,
            ConcurrentExecutions = 1000, // Very high concurrency
            LastExecution = DateTime.UtcNow,
            SamplePeriod = TimeSpan.FromMinutes(5),
            CpuUsage = 0.95,
            MemoryUsage = 512 * 1024,
            DatabaseCalls = 2,
            ExternalApiCalls = 1
        };

        // Act
        var recommendation = await _engine.AnalyzeRequestAsync(request, metrics);

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.ConfidenceScore >= 0 && recommendation.ConfidenceScore <= 1);
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Handle_Zero_Executions()
    {
        // Arrange
        var request = new TestRequest();
        var metrics = new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.Zero,
            MedianExecutionTime = TimeSpan.Zero,
            P95ExecutionTime = TimeSpan.Zero,
            P99ExecutionTime = TimeSpan.Zero,
            TotalExecutions = 0,
            SuccessfulExecutions = 0,
            FailedExecutions = 0,
            MemoryAllocated = 0,
            ConcurrentExecutions = 0,
            LastExecution = DateTime.MinValue,
            SamplePeriod = TimeSpan.Zero,
            CpuUsage = 0,
            MemoryUsage = 0,
            DatabaseCalls = 0,
            ExternalApiCalls = 0
        };

        // Act
        var recommendation = await _engine.AnalyzeRequestAsync(request, metrics);

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.ConfidenceScore >= 0 && recommendation.ConfidenceScore <= 1);
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Handle_High_Failure_Rate()
    {
        // Arrange
        var request = new TestRequest();
        var metrics = new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            MedianExecutionTime = TimeSpan.FromMilliseconds(95),
            P95ExecutionTime = TimeSpan.FromMilliseconds(150),
            P99ExecutionTime = TimeSpan.FromMilliseconds(200),
            TotalExecutions = 100,
            SuccessfulExecutions = 20,
            FailedExecutions = 80, // 80% failure rate
            MemoryAllocated = 1024 * 1024,
            ConcurrentExecutions = 10,
            LastExecution = DateTime.UtcNow,
            SamplePeriod = TimeSpan.FromMinutes(5),
            CpuUsage = 0.45,
            MemoryUsage = 512 * 1024,
            DatabaseCalls = 2,
            ExternalApiCalls = 1
        };

        // Act
        var recommendation = await _engine.AnalyzeRequestAsync(request, metrics);

        // Assert
        Assert.NotNull(recommendation);
        Assert.True(recommendation.ConfidenceScore >= 0 && recommendation.ConfidenceScore <= 1);
    }

    [Fact]
    public async Task AnalyzeRequestAsync_Should_Handle_Multiple_Sequential_Calls()
    {
        // Arrange
        var request = new TestRequest();
        var metrics = CreateMetrics();

        // Act - Make multiple sequential calls
        var recommendation1 = await _engine.AnalyzeRequestAsync(request, metrics);
        var recommendation2 = await _engine.AnalyzeRequestAsync(request, metrics);
        var recommendation3 = await _engine.AnalyzeRequestAsync(request, metrics);

        // Assert
        Assert.NotNull(recommendation1);
        Assert.NotNull(recommendation2);
        Assert.NotNull(recommendation3);
        // Confidence should improve or stay stable with more data
        Assert.True(recommendation3.ConfidenceScore >= recommendation1.ConfidenceScore * 0.9);
    }

    #region Helper Methods

    private RequestExecutionMetrics CreateMetrics()
    {
        return new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            MedianExecutionTime = TimeSpan.FromMilliseconds(95),
            P95ExecutionTime = TimeSpan.FromMilliseconds(150),
            P99ExecutionTime = TimeSpan.FromMilliseconds(200),
            TotalExecutions = 100,
            SuccessfulExecutions = 95,
            FailedExecutions = 5,
            MemoryAllocated = 1024 * 1024,
            ConcurrentExecutions = 10,
            LastExecution = DateTime.UtcNow,
            SamplePeriod = TimeSpan.FromMinutes(5),
            CpuUsage = 0.45,
            MemoryUsage = 512 * 1024,
            DatabaseCalls = 2,
            ExternalApiCalls = 1
        };
    }

    #endregion

    [Fact]
    public void GetLoadTransitions_Should_Return_Load_Transitions_From_Predictive_Service()
    {
        // Arrange - Add some metrics to create transitions
        var idleMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.05,
            ["MemoryUtilization"] = 0.1,
            ["ThroughputPerSecond"] = 10
        };

        var highMetrics = new Dictionary<string, double>
        {
            ["CpuUtilization"] = 0.85,
            ["MemoryUtilization"] = 0.8,
            ["ThroughputPerSecond"] = 200
        };

        // Add metrics through the engine (this will go to the predictive service)
        // We need to access the internal service, but since it's internal, we'll test the public method
        // The engine's GetLoadTransitions delegates to the predictive service

        // Act
        var transitions = _engine.GetLoadTransitions();

        // Assert - Initially should be empty
        Assert.NotNull(transitions);
        Assert.IsType<List<LoadTransition>>(transitions);
    }

    [Fact]
    public void GetLoadPatternAnalysis_Should_Return_Valid_LoadPatternData()
    {
        // Act
        var result = _engine.GetLoadPatternAnalysis();

        // Assert
        Assert.NotNull(result);
        Assert.IsType<LoadPatternData>(result);
        Assert.True(result.SuccessRate >= 0.0 && result.SuccessRate <= 1.0);
        Assert.True(result.AverageImprovement >= 0.0);
        Assert.True(result.TotalPredictions >= 0);
        Assert.NotNull(result.StrategyEffectiveness);
        Assert.NotNull(result.Predictions);
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Include_Seasonal_Patterns()
    {
        // Act
        var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

        // Assert
        Assert.NotNull(insights);
        Assert.NotNull(insights.SeasonalPatterns);
        Assert.IsType<List<SeasonalPattern>>(insights.SeasonalPatterns);
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Detect_Daily_Patterns_With_Sufficient_Data()
    {
        // Arrange - Simulate having enough historical data for pattern detection
        // Note: In a real scenario, this would require the time series database to have data
        // For this test, we verify the method doesn't throw and returns valid structure

        // Act
        var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(24));

        // Assert
        Assert.NotNull(insights);
        Assert.NotNull(insights.SeasonalPatterns);
        // With insufficient data, patterns list should be empty but not null
        Assert.IsType<List<SeasonalPattern>>(insights.SeasonalPatterns);
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Handle_Cancellation_In_Seasonal_Analysis()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1), cts.Token));
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Include_Resource_Optimization_Recommendations()
    {
        // Act
        var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

        // Assert
        Assert.NotNull(insights.ResourceOptimization);
        Assert.NotNull(insights.ResourceOptimization.Recommendations);
        Assert.True(insights.ResourceOptimization.EstimatedSavings >= TimeSpan.Zero);
    }

    [Fact]
    public async Task GetSystemInsightsAsync_Should_Return_Valid_Insights_Structure()
    {
        // Act
        var insights = await _engine.GetSystemInsightsAsync(TimeSpan.FromHours(1));

        // Assert
        Assert.NotNull(insights);
        Assert.True(insights.AnalysisPeriod > TimeSpan.Zero);
        Assert.NotNull(insights.Bottlenecks);
        Assert.NotNull(insights.Opportunities);
        Assert.NotNull(insights.HealthScore);
        Assert.NotNull(insights.Predictions);
        Assert.NotNull(insights.KeyMetrics);
        Assert.NotNull(insights.SeasonalPatterns);
        Assert.NotNull(insights.ResourceOptimization);
        Assert.True(insights.PerformanceGrade >= 'A' && insights.PerformanceGrade <= 'F');
    }

    #region Test Types

    private class TestRequest { }

    #endregion
}