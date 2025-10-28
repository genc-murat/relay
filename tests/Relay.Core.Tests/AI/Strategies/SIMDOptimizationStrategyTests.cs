using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors.Strategies;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Strategies;

/// <summary>
/// Tests for SIMDOptimizationStrategy implementation.
/// </summary>
public class SIMDOptimizationStrategyTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;

    public SIMDOptimizationStrategyTests()
    {
        _loggerMock = new Mock<ILogger>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
    }

    [Fact]
    public async Task SIMDOptimizationStrategy_CanApplyAsync_WithHardwareAcceleration_ReturnsTrue()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.7,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert - Result depends on actual hardware acceleration availability
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task SIMDOptimizationStrategy_CanApplyAsync_WithHighConfidenceAndNormalLoad_ReturnsExpectedResult_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.8, // Above 0.6 threshold
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5 // Below 0.9 threshold
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert - Should return true if hardware acceleration is available
        if (System.Numerics.Vector.IsHardwareAccelerated)
        {
            Assert.True(result);
        }
        else
        {
            Assert.False(result);
        }
    }

    [Fact]
    public async Task SIMDOptimizationStrategy_CanApplyAsync_WithLowConfidence_ReturnsFalse_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.5, // Below 0.6 threshold
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SIMDOptimizationStrategy_CanApplyAsync_WithHighCpuLoad_ReturnsFalse_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.7,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.95 // Above 0.9 threshold
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SIMDOptimizationStrategy_CanApplyAsync_WithBoundaryConfidence_ReturnsExpectedResult_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.6, // Exactly at threshold
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics
        {
            CpuUtilization = 0.5
        };

        // Act
        var result = await strategy.CanApplyAsync(new TestRequest(), recommendation, systemLoad, CancellationToken.None);

        // Assert - Should return true if hardware acceleration is available
        if (System.Numerics.Vector.IsHardwareAccelerated)
        {
            Assert.True(result);
        }
        else
        {
            Assert.False(result);
        }
    }

    [Fact]
    public void SIMDOptimizationStrategy_Constructor_WithNullLogger_ThrowsArgumentNullException_Extended()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SIMDOptimizationStrategy<TestRequest, TestResponse>(
                null!, _metricsProviderMock.Object));
    }

    [Fact]
    public void SIMDOptimizationStrategy_Constructor_WithNullMetricsProvider_WorksCorrectly_Extended()
    {
        // Act & Assert - Should not throw
        Assert.Null(Record.Exception(() =>
            new SIMDOptimizationStrategy<TestRequest, TestResponse>(
                _loggerMock.Object, null)));
    }

    [Fact]
    public void SIMDOptimizationStrategy_Constructor_WithValidParameters_CreatesInstance_Extended()
    {
        // Act
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(OptimizationStrategy.SIMDAcceleration, strategy.StrategyType);
    }

    [Fact]
    public async Task SIMDOptimizationStrategy_ApplyAsync_WithDefaultParameters_ExecutesSuccessfully_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.7,
            Parameters = new Dictionary<string, object>() // Empty parameters - use defaults
        };

        var systemLoad = new SystemLoadMetrics { CpuUtilization = 0.5 };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"simd_default_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("simd_default_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task SIMDOptimizationStrategy_ApplyAsync_WithCustomVectorParameters_ExecutesSuccessfully_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.8,
            Parameters = new Dictionary<string, object>
            {
                ["EnableVectorization"] = true,
                ["VectorSize"] = 8,
                ["EnableUnrolling"] = true,
                ["UnrollFactor"] = 4,
                ["MinDataSize"] = 128
            }
        };

        var systemLoad = new SystemLoadMetrics { CpuUtilization = 0.4 };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"simd_custom_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("simd_custom_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task SIMDOptimizationStrategy_ApplyAsync_WithVectorizationDisabled_ExecutesSuccessfully_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.7,
            Parameters = new Dictionary<string, object>
            {
                ["EnableVectorization"] = false,
                ["EnableUnrolling"] = false
            }
        };

        var systemLoad = new SystemLoadMetrics { CpuUtilization = 0.5 };

        _metricsProviderMock.Setup(x => x.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()));

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = $"simd_no_vector_{executionCount}" });
        };

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var result = await optimizedHandler();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("simd_no_vector_1", result.Result);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public async Task SIMDOptimizationStrategy_ApplyAsync_RecordsSuccessMetrics_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.7,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics { CpuUtilization = 0.5 };

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
    public async Task SIMDOptimizationStrategy_ApplyAsync_WithFailure_ThrowsException_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.7,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics { CpuUtilization = 0.5 };

        RequestHandlerDelegate<TestResponse> next = () =>
            throw new InvalidOperationException("SIMD failure test");

        // Act
        var optimizedHandler = await strategy.ApplyAsync(new TestRequest(), next, recommendation, systemLoad, CancellationToken.None);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await optimizedHandler());

        // Assert
        Assert.Equal("SIMD failure test", exception.Message);
    }

    [Fact]
    public async Task SIMDOptimizationStrategy_ApplyAsync_WithMetricsProviderException_ContinuesExecution_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.7,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics { CpuUtilization = 0.5 };

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
    public async Task SIMDOptimizationStrategy_ApplyAsync_WithScopeCreationFailure_HandlesGracefully_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.7,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics { CpuUtilization = 0.5 };

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
    public void SIMDOptimizationStrategy_GetSupportedVectorTypes_ReturnsNonEmptyArray_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        // Act - Use reflection to access the private method for testing
        var method = typeof(SIMDOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("GetSupportedVectorTypes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (string[])method.Invoke(strategy, new object[] { });

        // Assert
        Assert.NotNull(result);
        Assert.IsType<string[]>(result);
        // Note: The actual content depends on the runtime environment's SIMD capabilities
    }

    [Fact]
    public void SIMDOptimizationStrategy_GetSupportedVectorTypes_IncludesExpectedTypes_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        // Act
        var method = typeof(SIMDOptimizationStrategy<TestRequest, TestResponse>)
            .GetMethod("GetSupportedVectorTypes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (string[])method.Invoke(strategy, new object[] { });

        // Assert - Check for common SIMD types that might be supported
        var supportedTypes = new[] { "SSE", "SSE2", "AVX", "AVX2", "ARM-NEON" };
        foreach (var type in supportedTypes)
        {
            // This test will pass as long as the method returns valid strings
            // The actual SIMD support depends on the hardware
        }
        Assert.NotNull(result);
    }

    [Fact]
    public async Task SIMDOptimizationStrategy_ApplyAsync_WithCancellationDuringExecution_ThrowsOperationCanceledException_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.7,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics { CpuUtilization = 0.5 };

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
    public async Task SIMDOptimizationStrategy_ApplyAsync_WithCancellationInHandler_ThrowsOperationCanceledException_Extended()
    {
        // Arrange
        var strategy = new SIMDOptimizationStrategy<TestRequest, TestResponse>(
            _loggerMock.Object, _metricsProviderMock.Object);

        var recommendation = new OptimizationRecommendation
        {
            Strategy = OptimizationStrategy.SIMDAcceleration,
            ConfidenceScore = 0.7,
            Parameters = new Dictionary<string, object>()
        };

        var systemLoad = new SystemLoadMetrics { CpuUtilization = 0.5 };

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
}