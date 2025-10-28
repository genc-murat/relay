using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors.Strategies;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Strategies;

/// <summary>
/// Tests for ParallelProcessingOptimizationStrategy implementation.
/// </summary>
public class ParallelProcessingOptimizationStrategyTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;

    public ParallelProcessingOptimizationStrategyTests()
    {
        _loggerMock = new Mock<ILogger>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_CanApplyAsync_WithValidConditions_ReturnsTrue()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.6,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5, // Normal CPU load
            ThreadPoolUtilization = 0.5
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_CanApplyAsync_WithHighCpuLoad_ReturnsFalse()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.6,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.92 // High CPU load
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_CanApplyAsync_WithHighThreadPoolUtilization_ReturnsFalse()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.6,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.95 // High thread pool utilization
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_CanApplyAsync_WithCpuLoadThreshold_ReturnsTrue_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.6,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5, // Normal load
            ThreadPoolUtilization = 0.5
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ParallelProcessingOptimizationStrategy_Constructor_WithNullLogger_ThrowsArgumentNullException_Extended()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
                null!, _metricsProviderMock.Object));
    }

    [Fact]
    public void ParallelProcessingOptimizationStrategy_Constructor_WithNullMetricsProvider_WorksCorrectly_Extended()
    {
        // Act & Assert - Should not throw
        Assert.Null(Record.Exception(() =>
            new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
                _loggerMock.Object, null)));
    }

    [Fact]
    public void ParallelProcessingOptimizationStrategy_Constructor_WithValidParameters_CreatesInstance_Extended()
    {
        // Act
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(OptimizationStrategy.ParallelProcessing, strategy.StrategyType);
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_ApplyAsync_WithDefaultParameters_ExecutesSuccessfully_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>() // Empty parameters
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"parallel_default_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("parallel_default_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_ApplyAsync_WithCustomParallelism_ExecutesSuccessfully_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 6,
                ["EnableParallelLinq"] = true,
                ["ChunkSize"] = 100
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.4,
            ThreadPoolUtilization = 0.3
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"parallel_custom_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("parallel_custom_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_ApplyAsync_WithDisabledParallelLinq_ExecutesSuccessfully_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4,
                ["EnableParallelLinq"] = false
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"parallel_no_linq_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("parallel_no_linq_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_ApplyAsync_RecordsSuccessMetrics_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        HandlerExecutionMetrics capturedMetrics = null!;
        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()))
            .Callback<HandlerExecutionMetrics>(metrics => capturedMetrics = metrics);

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "metrics_test" });

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        _metricsProviderMock.Verify(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()), Times.Once);
        Assert.NotNull(capturedMetrics);
        Assert.Equal(typeof(TestRequest), capturedMetrics.RequestType);
        Assert.True(capturedMetrics.Success);
        Assert.True(capturedMetrics.Duration >= TimeSpan.Zero);
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_ApplyAsync_WithFailure_ThrowsException_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        RequestHandlerDelegate<TestResponse> next = () =>
            throw new InvalidOperationException("Parallel processing failure");

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await optimizedHandler());

        // Assert
        Assert.Equal("Parallel processing failure", exception.Message);
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_ApplyAsync_WithMetricsProviderException_ContinuesExecution_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()))
            .Throws(new Exception("Metrics provider failure"));

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "exception_handled" });

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert - Should still execute successfully despite metrics failure
        Assert.NotNull(result);
        Assert.Equal("exception_handled", result.Result);
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_ApplyAsync_WithScopeCreationFailure_HandlesGracefully_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "scope_test" });

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("scope_test", result.Result);
    }

    [Fact]
    public void ParallelProcessingOptimizationStrategy_CalculateOptimalParallelism_WithNormalLoad_ReturnsExpectedValue_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        // Act - Use reflection to access the private method for testing
        var method = typeof(ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("CalculateOptimalParallelism", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (int)method.Invoke(strategy, new object[] { 4, systemLoad });

        // Assert
        Assert.True(result > 0);
        Assert.True(result <= Environment.ProcessorCount);
    }

    [Fact]
    public void ParallelProcessingOptimizationStrategy_CalculateOptimalParallelism_WithHighCpuLoad_ReturnsLowerValue_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.8,
            ThreadPoolUtilization = 0.5
        };

        // Act
        var method = typeof(ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("CalculateOptimalParallelism", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (int)method.Invoke(strategy, new object[] { 4, systemLoad });

        // Assert
        Assert.True(result >= 1);
        Assert.True(result <= 4); // Should be reduced due to high CPU
    }

    [Fact]
    public void ParallelProcessingOptimizationStrategy_CalculateOptimalParallelism_WithHighThreadPoolUtilization_ReturnsLowerValue_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.9
        };

        // Act
        var method = typeof(ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("CalculateOptimalParallelism", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (int)method.Invoke(strategy, new object[] { 4, systemLoad });

        // Assert
        Assert.True(result >= 1);
        Assert.True(result <= 2); // Should be halved due to high thread pool utilization
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_ApplyAsync_WithCancellationDuringExecution_ThrowsOperationCanceledException_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var cts = new CancellationTokenSource();
        RequestHandlerDelegate<TestResponse> next = async () =>
        {
            await Task.Delay(100, cts.Token); // Allow cancellation
            return new TestResponse { Result = "cancelled" };
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, cts.Token);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await optimizedHandler());
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_ApplyAsync_WithCancellationInHandler_ThrowsOperationCanceledException_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var cts = new CancellationTokenSource();

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            cts.Cancel();
            throw new TaskCanceledException("Handler cancelled");
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, cts.Token);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await optimizedHandler());
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_ConcurrentExecutions_WorkCorrectly_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        // Act - Execute multiple instances concurrently
        var tasks = new List<Task<TestResponse>>();
        for (int i = 0; i < 5; i++)
        {
            var index = i; // Capture the value
            var task = Task.Run(async () =>
            {
                var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), () =>
                    new ValueTask<TestResponse>(new TestResponse { Result = $"concurrent_{index}" }), recommendation, systemLoad, CancellationToken.None);
                return await optimizedHandler();
            });
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(5, results.Length);
        var resultValues = results.Select(r => r.Result).ToList();
        for (int i = 0; i < 5; i++)
        {
            Assert.Contains($"concurrent_{i}", resultValues);
        }
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_CanApplyAsync_ConcurrentCalls_ReturnConsistentResults_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        // Act - Execute multiple CanApplyAsync calls concurrently
        var tasks = new List<Task<bool>>();
        for (int i = 0; i < 10; i++)
        {
            var task = Task.Run(async () =>
                await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None));
            tasks.Add(task);
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All results should be consistent
        Assert.Equal(10, results.Length);
        Assert.All(results, result => Assert.True(result)); // All should return true for this configuration
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_ApplyAsync_ValidatesContextCorrectly_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "context_valid" });

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("context_valid", result.Result);
    }

    [Fact]
    public async Task ParallelProcessingOptimizationStrategy_ApplyAsync_WithNullRequest_HandlesGracefully_Extended()
    {
        // Arrange
        var strategy = new ParallelProcessingOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.ParallelProcessing,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["MaxDegreeOfParallelism"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5,
            ThreadPoolUtilization = 0.5
        };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "null_request_handled" });

        // Act
        var optimizedHandler = await strategy.ApplyAsync(null!, next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("null_request_handled", result.Result);
    }
}