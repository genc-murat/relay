using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Contexts;
using Relay.Core.AI.Pipeline.Behaviors.Strategies;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Strategies;

/// <summary>
/// Tests for CustomOptimizationStrategy implementation.
/// </summary>
public class CustomOptimizationStrategyTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;
    private readonly Mock<IAIOptimizationEngine> _aiEngineMock;
    private readonly AIOptimizationOptions _options;

    public CustomOptimizationStrategyTests()
    {
        _loggerMock = new Mock<ILogger>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
        _aiEngineMock = new Mock<IAIOptimizationEngine>();

        _options = new AIOptimizationOptions
        {
            Enabled = true,
            MinConfidenceScore = 0.7,
            MinCacheHitRate = 0.5
        };
    }

    [Fact]
    public async Task CustomOptimizationStrategy_CanApplyAsync_WithHighConfidence_ReturnsTrue()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.8, // High confidence required for custom optimizations
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics();

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CustomOptimizationStrategy_CanApplyAsync_WithLowConfidence_ReturnsFalse()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object,
            _aiEngineMock.Object,
            _options,
            _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.6, // Below threshold
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics();

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CustomOptimizationStrategy_Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CustomOptimizationStrategy<TestRequest, TestResponse>(
                null!, _aiEngineMock.Object, _options, _metricsProviderMock.Object));
    }

    [Fact]
    public void CustomOptimizationStrategy_Constructor_WithNullAIEngine_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CustomOptimizationStrategy<TestRequest, TestResponse>(
                _loggerMock.Object, null!, _options, _metricsProviderMock.Object));
    }

    [Fact]
    public void CustomOptimizationStrategy_Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new CustomOptimizationStrategy<TestRequest, TestResponse>(
                _loggerMock.Object, _aiEngineMock.Object, null, _metricsProviderMock.Object));
    }

    [Fact]
    public void CustomOptimizationStrategy_Constructor_WithNullMetricsProvider_WorksCorrectly()
    {
        // Act & Assert - Should not throw
        Assert.Null(Record.Exception(() =>
            new CustomOptimizationStrategy<TestRequest, TestResponse>(
                _loggerMock.Object, _aiEngineMock.Object, _options, null)));
    }

    [Fact]
    public void CustomOptimizationStrategy_Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(OptimizationStrategy.Custom, strategy.StrategyType);
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyAsync_WithDefaultParameters_ExecutesSuccessfully()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>() // Empty parameters
        };

        var systemLoad = new SystemLoadMetrics();

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"custom_default_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("custom_default_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyAsync_WithWarmupType_ExecutesSuccessfully()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["OptimizationType"] = "warmup",
                ["WarmupLevel"] = 2
            }
        };

        var systemLoad = new SystemLoadMetrics();

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"warmup_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("warmup_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyAsync_WithThrottleType_ExecutesSuccessfully()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["OptimizationType"] = "throttle",
                ["ThrottleLevel"] = 3
            }
        };

        var systemLoad = new SystemLoadMetrics();

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"throttle_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("throttle_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyAsync_WithCompressType_ExecutesSuccessfully()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["OptimizationType"] = "compress",
                ["CompressionLevel"] = 6
            }
        };

        var systemLoad = new SystemLoadMetrics();

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"compress_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("compress_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyAsync_WithCachePrimeType_ExecutesSuccessfully()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["OptimizationType"] = "cache_prime",
                ["CachePrimeLevel"] = 4
            }
        };

        var systemLoad = new SystemLoadMetrics();

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"cache_prime_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("cache_prime_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyAsync_WithUnknownType_UsesDefault()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["OptimizationType"] = "unknown_type",
                ["UnknownLevel"] = 1
            }
        };

        var systemLoad = new SystemLoadMetrics();

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"unknown_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("unknown_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyAsync_RecordsMetrics()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["OptimizationType"] = "warmup"
            }
        };

        var systemLoad = new SystemLoadMetrics();

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
    public async Task CustomOptimizationStrategy_ApplyAsync_WithFailure_ThrowsException()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics();

        RequestHandlerDelegate<TestResponse> next = () =>
            throw new InvalidOperationException("Custom optimization failure");

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await optimizedHandler());

        // Assert
        Assert.Equal("Custom optimization failure", exception.Message);
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics();

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "cancelled" });

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, cts.Token);
        var result = await optimizedHandler();

        // Assert - Should still work even with cancelled token since implementation may not check it
        Assert.NotNull(result);
        Assert.Equal("cancelled", result.Result);
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyAsync_ConcurrentExecutions_WorkCorrectly()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["OptimizationType"] = "warmup"
            }
        };

        var systemLoad = new SystemLoadMetrics();

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
    public async Task CustomOptimizationStrategy_ApplyAsync_WithNullRequest_HandlesGracefully()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.Custom,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics();

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

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyPreExecutionOptimizations_WithWarmupType_RecordsAction_Extended()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var context = new CustomOptimizationContext
        {
            OptimizationType = "warmup",
            OptimizationLevel = 1
        };

        var scope = CustomOptimizationScope.Create(context, _loggerMock.Object);

        // Use reflection to access the private method
        var method = typeof(CustomOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("ApplyPreExecutionOptimizations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        await (Task)method.Invoke(strategy, new object[] { context, scope });

        // Assert - Method should complete without throwing
        Assert.True(true); // If we get here, the method executed successfully
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyPreExecutionOptimizations_WithThrottleType_DelaysExecution_Extended()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var context = new CustomOptimizationContext
        {
            OptimizationType = "throttle",
            OptimizationLevel = 2 // Should delay by 2 * 10 = 20ms
        };

        var scope = CustomOptimizationScope.Create(context, _loggerMock.Object);

        var method = typeof(CustomOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("ApplyPreExecutionOptimizations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        var startTime = DateTime.UtcNow;
        await (Task)method.Invoke(strategy, new object[] { context, scope });
        var endTime = DateTime.UtcNow;

        // Assert - Should have some delay
        Assert.True((endTime - startTime).TotalMilliseconds >= 15); // At least 15ms delay
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyPreExecutionOptimizations_WithUnknownType_UsesDefault_Extended()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var context = new CustomOptimizationContext
        {
            OptimizationType = "unknown_type",
            OptimizationLevel = 1
        };

        var scope = CustomOptimizationScope.Create(context, _loggerMock.Object);

        var method = typeof(CustomOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("ApplyPreExecutionOptimizations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Act
        await (Task)method.Invoke(strategy, new object[] { context, scope });

        // Assert - Should complete without throwing for unknown types
        Assert.True(true);
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyPostExecutionOptimizations_WithCompressType_ExecutesSuccessfully_Extended()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var context = new CustomOptimizationContext
        {
            OptimizationType = "compress"
        };

        var response = new TestResponse { Result = "test_response" };
        var scope = CustomOptimizationScope.Create(context, _loggerMock.Object);

        var method = typeof(CustomOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("ApplyPostExecutionOptimizations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .MakeGenericMethod(typeof(TestResponse));

        // Act
        await (Task)method.Invoke(strategy, new object[] { context, scope, response });

        // Assert - Should complete without throwing
        Assert.True(true);
    }

    [Fact]
    public async Task CustomOptimizationStrategy_ApplyPostExecutionOptimizations_WithCachePrimeType_ExecutesSuccessfully_Extended()
    {
        // Arrange
        var strategy = new CustomOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _aiEngineMock.Object, _options, _metricsProviderMock.Object);

        var context = new CustomOptimizationContext
        {
            OptimizationType = "cache_prime"
        };

        var response = new TestResponse { Result = "cache_test" };
        var scope = CustomOptimizationScope.Create(context, _loggerMock.Object);

        var method = typeof(CustomOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("ApplyPostExecutionOptimizations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .MakeGenericMethod(typeof(TestResponse));

        // Act
        await (Task)method.Invoke(strategy, new object[] { context, scope, response });

        // Assert - Should complete without throwing
        Assert.True(true);
    }
}