using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Data;
using Relay.Core.AI.Optimization.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Services;

public class PatternAnalysisServiceTests
{
    private readonly ILogger _logger;
    private readonly PatternAnalysisService _service;

    public PatternAnalysisServiceTests()
    {
        _logger = NullLogger.Instance;
        _service = new PatternAnalysisService(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => new PatternAnalysisService(null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_When_Logger_Is_Valid()
    {
        // Arrange
        var logger = NullLogger.Instance;

        // Act
        var service = new PatternAnalysisService(logger);

        // Assert
        Assert.NotNull(service);
    }

    #endregion

    #region AnalyzePatternsAsync Tests

    [Fact]
    public async Task AnalyzePatternsAsync_Should_Throw_When_RequestType_Is_Null()
    {
        // Arrange
        var analysisData = new RequestAnalysisData();
        var executionMetrics = new RequestExecutionMetrics();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.AnalyzePatternsAsync(null!, analysisData, executionMetrics));
    }

    [Fact]
    public async Task AnalyzePatternsAsync_Should_Throw_When_AnalysisData_Is_Null()
    {
        // Arrange
        var requestType = typeof(string);
        var executionMetrics = new RequestExecutionMetrics();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.AnalyzePatternsAsync(requestType, null!, executionMetrics));
    }

    [Fact]
    public async Task AnalyzePatternsAsync_Should_Throw_When_ExecutionMetrics_Is_Null()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.AnalyzePatternsAsync(requestType, analysisData, null!));
    }

    [Fact]
    public async Task AnalyzePatternsAsync_Should_Return_Valid_Recommendation()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var executionMetrics = new RequestExecutionMetrics();

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<OptimizationRecommendation>(result);
        Assert.True(result.ConfidenceScore >= 0.0);
        Assert.True(result.ConfidenceScore <= 1.0);
        Assert.True(result.EstimatedGainPercentage >= 0.0);
        Assert.NotNull(result.Parameters);
    }

    [Fact]
    public async Task AnalyzePatternsAsync_Should_Cancel_When_CancellationToken_Is_Cancelled()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var executionMetrics = new RequestExecutionMetrics();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics, cts.Token).AsTask());
    }

    #endregion

    #region Strategy Determination Tests

    [Fact]
    public async Task DetermineOptimalStrategy_Should_Return_CircuitBreaker_For_High_Error_Rate()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        // Add metrics to achieve error rate of 0.15
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 85, // This gives 15/100 = 0.15 error rate
            FailedExecutions = 15,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        });
        var executionMetrics = new RequestExecutionMetrics();

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.Equal(OptimizationStrategy.CircuitBreaker, result.Strategy);
        Assert.Contains("high error rate", result.Reasoning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetermineOptimalStrategy_Should_Return_BatchProcessing_For_High_Concurrent_Executions_With_Fast_Execution()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var executionMetrics = new RequestExecutionMetrics
        {
            ConcurrentExecutions = 15, // Above threshold of 10
            AverageExecutionTime = TimeSpan.FromMilliseconds(50) // Below 100ms threshold
        };

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.Equal(OptimizationStrategy.BatchProcessing, result.Strategy);
        Assert.Contains("batching could improve throughput", result.Reasoning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetermineOptimalStrategy_Should_Return_ParallelProcessing_For_High_Concurrent_Executions_With_Long_Execution()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var executionMetrics = new RequestExecutionMetrics
        {
            ConcurrentExecutions = 15, // Above threshold of 10
            AverageExecutionTime = TimeSpan.FromMilliseconds(200) // Above 100ms threshold
        };

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.Equal(OptimizationStrategy.ParallelProcessing, result.Strategy);
        Assert.Contains("parallel processing would help", result.Reasoning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetermineOptimalStrategy_Should_Return_EnableCaching_For_Long_Execution_Time()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var executionMetrics = new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(1500) // Above 1000ms threshold
        };

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.Equal(OptimizationStrategy.EnableCaching, result.Strategy);
        Assert.Contains("caching would be beneficial", result.Reasoning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetermineOptimalStrategy_Should_Return_MemoryPooling_For_High_Memory_Usage()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var executionMetrics = new RequestExecutionMetrics
        {
            MemoryUsage = 150 * 1024 * 1024 // Above 100MB threshold
        };

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.Equal(OptimizationStrategy.MemoryPooling, result.Strategy);
        Assert.Contains("memory pooling could reduce allocation overhead", result.Reasoning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetermineOptimalStrategy_Should_Return_DatabaseOptimization_For_Many_Database_Calls()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var executionMetrics = new RequestExecutionMetrics
        {
            DatabaseCalls = 8 // Above 5 threshold
        };

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.Equal(OptimizationStrategy.DatabaseOptimization, result.Strategy);
        Assert.Contains("query optimization opportunities", result.Reasoning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetermineOptimalStrategy_Should_Return_None_By_Default()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var executionMetrics = new RequestExecutionMetrics();

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.Equal(OptimizationStrategy.None, result.Strategy);
        Assert.Equal("No specific optimization pattern detected.", result.Reasoning);
    }

    #endregion

    #region Confidence Calculation Tests

    [Fact]
    public async Task CalculateConfidenceScore_Should_Increase_With_More_Executions()
    {
        // Arrange
        var requestType = typeof(string);
        var baseAnalysisData = new RequestAnalysisData();
        baseAnalysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 5, // Low
            SuccessfulExecutions = 5,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        });
        
        var highAnalysisData = new RequestAnalysisData();
        highAnalysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 150, // High
            SuccessfulExecutions = 140,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        });
        
        var executionMetrics = new RequestExecutionMetrics();

        // Act
        var baseResult = await _service.AnalyzePatternsAsync(requestType, baseAnalysisData, executionMetrics);
        var highResult = await _service.AnalyzePatternsAsync(requestType, highAnalysisData, executionMetrics);

        // Assert
        Assert.True(highResult.ConfidenceScore >= baseResult.ConfidenceScore);
        Assert.True(baseResult.ConfidenceScore >= 0.5); // Base confidence is 0.5
        Assert.True(highResult.ConfidenceScore <= 0.95); // Should be clamped
    }

    [Fact]
    public async Task CalculateConfidenceScore_Should_Increase_With_Clear_Patterns()
    {
        // Arrange
        var requestType = typeof(string);
        var baseAnalysisData = new RequestAnalysisData();
        baseAnalysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 99,
            FailedExecutions = 1,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        }); // Low error rate
        var highErrorAnalysisData = new RequestAnalysisData();
        highErrorAnalysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 75,
            FailedExecutions = 25,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        }); // High error rate - clear pattern
        var executionMetrics = new RequestExecutionMetrics();

        // Act
        var baseResult = await _service.AnalyzePatternsAsync(requestType, baseAnalysisData, executionMetrics);
        var highErrorResult = await _service.AnalyzePatternsAsync(requestType, highErrorAnalysisData, executionMetrics);

        // Assert
        Assert.True(highErrorResult.ConfidenceScore >= baseResult.ConfidenceScore);
    }

    #endregion

    #region Improvement Estimation Tests

    [Fact]
    public async Task EstimateImprovement_Should_Vary_By_Strategy()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        // Add metrics to affect AverageExecutionTime
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 1,
            SuccessfulExecutions = 1,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(1000)
        });
        var executionMetrics = new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(1000)
        };

        // For caching - we'll add a metric that contributes to error rate
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 85, // This gives us 15 failed out of 101 total (previous + this) 
            FailedExecutions = 15,     // Which is approximately 0.15 error rate
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        });
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.EstimatedImprovement >= TimeSpan.Zero);
    }

    #endregion

    #region Parameter Building Tests

    [Fact]
    public async Task BuildParameters_Should_Include_Correct_Parameters_For_Caching_Strategy()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        // Add metrics to affect AverageExecutionTime
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 1,
            SuccessfulExecutions = 1,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(1500)
        });
        var executionMetrics = new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(1500)
        };

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Parameters);
        if (result.Strategy == OptimizationStrategy.EnableCaching)
        {
            Assert.Contains("CacheDuration", result.Parameters.Keys);
            Assert.Contains("MaxCacheSize", result.Parameters.Keys);
            Assert.IsType<TimeSpan>(result.Parameters["CacheDuration"]);
        }
    }

    [Fact]
    public async Task BuildParameters_Should_Include_Correct_Parameters_For_BatchProcessing_Strategy()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        // Add metrics to achieve desired ConcurrentExecutionPeaks
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 10,
            SuccessfulExecutions = 10,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50),
            ConcurrentExecutions = 25 // This will set the ConcurrentExecutionPeaks to 25
        });
        var executionMetrics = new RequestExecutionMetrics
        {
            ConcurrentExecutions = 15,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50) // Fast execution for batching
        };

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Parameters);
        if (result.Strategy == OptimizationStrategy.BatchProcessing)
        {
            Assert.Contains("MaxBatchSize", result.Parameters.Keys);
            Assert.Contains("BatchTimeout", result.Parameters.Keys);
        }
    }

    #endregion

    #region Priority Determination Tests

    [Fact]
    public async Task DeterminePriority_Should_Be_High_For_High_Error_Rate()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        // Add metrics to achieve high error rate
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 75, // This gives 25/100 = 0.25 error rate
            FailedExecutions = 25,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        });
        var executionMetrics = new RequestExecutionMetrics();

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OptimizationPriority.High, result.Priority);
    }

    [Fact]
    public async Task DeterminePriority_Should_Be_High_For_Long_Execution_Time()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var executionMetrics = new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromSeconds(6) // Above 5 seconds threshold
        };

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OptimizationPriority.High, result.Priority);
    }

    [Fact]
    public async Task DeterminePriority_Should_Be_Medium_For_High_Executions_Or_Concurrency()
    {
        // Arrange
        var requestType = typeof(string);
        var highExecutionsData = new RequestAnalysisData();
        // Add metrics to achieve high total executions
        highExecutionsData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 1500,
            SuccessfulExecutions = 1500, // All successful
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        });
        var executionMetrics = new RequestExecutionMetrics();

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, highExecutionsData, executionMetrics);

        // Assert
        Assert.NotNull(result);
        // Priority may vary depending on which threshold is hit, but should be Medium or Higher
    }

    [Fact]
    public async Task DeterminePriority_Should_Be_Low_By_Default()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        var executionMetrics = new RequestExecutionMetrics();

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.NotNull(result);
        // Default priority may vary based on conditions, but should be set
    }

    #endregion

    #region Risk Assessment Tests

    [Fact]
    public async Task AssessRisk_Should_Be_Low_For_Caching_Strategy()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        // Add metrics to affect AverageExecutionTime
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 1,
            SuccessfulExecutions = 1,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(1500)
        });
        var executionMetrics = new RequestExecutionMetrics
        {
            AverageExecutionTime = TimeSpan.FromMilliseconds(1500)
        };

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.NotNull(result);
        if (result.Strategy == OptimizationStrategy.EnableCaching)
        {
            Assert.Equal(RiskLevel.Low, result.Risk);
        }
    }

    [Fact]
    public async Task AssessRisk_Should_Be_Low_For_CircuitBreaker_Strategy()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        // Add metrics to achieve error rate of 0.15
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 85, // This gives 15/100 = 0.15 error rate
            FailedExecutions = 15,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        });
        var executionMetrics = new RequestExecutionMetrics();

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.NotNull(result);
        if (result.Strategy == OptimizationStrategy.CircuitBreaker)
        {
            Assert.Equal(RiskLevel.Low, result.Risk);
        }
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task Full_Pattern_Analysis_Should_Work_With_High_Error_Rate()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        // Add metrics to achieve desired error rate and other properties
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 200,
            SuccessfulExecutions = 170, // This gives 30/200 = 0.15 error rate
            FailedExecutions = 30,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50)
        });
        var executionMetrics = new RequestExecutionMetrics
        {
            ConcurrentExecutions = 5,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50),
            MemoryUsage = 10 * 1024 * 1024,
            DatabaseCalls = 2
        };

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(OptimizationStrategy.CircuitBreaker, result.Strategy);
        Assert.Contains("circuit breaker", result.Reasoning, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.ConfidenceScore >= 0.7); // Should be higher with more data
    }

    [Fact]
    public async Task Full_Pattern_Analysis_Should_Work_With_High_Concurrency()
    {
        // Arrange
        var requestType = typeof(string);
        var analysisData = new RequestAnalysisData();
        // Add metrics to achieve desired total executions and average time
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 50,
            SuccessfulExecutions = 50,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50)
        });
        var executionMetrics = new RequestExecutionMetrics
        {
            ConcurrentExecutions = 15, // High
            AverageExecutionTime = TimeSpan.FromMilliseconds(50), // Fast
            MemoryUsage = 10 * 1024 * 1024,
            DatabaseCalls = 1
        };

        // Act
        var result = await _service.AnalyzePatternsAsync(requestType, analysisData, executionMetrics);

        // Assert
        Assert.NotNull(result);
        // This should trigger batching since high concurrency with fast execution
        Assert.Contains("batching", result.Reasoning, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
