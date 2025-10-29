using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors;
using Relay.Core.AI.Pipeline.Interfaces;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

/// <summary>
/// Integration tests that exercise helper methods through public API
/// </summary>
public class AIOptimizationPipelineBehaviorIntegrationTests
{
    private readonly Mock<IAIOptimizationEngine> _aiEngineMock;
    private readonly Mock<ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<ISystemLoadMetricsProvider> _systemMetricsMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;
    private readonly AIOptimizationOptions _options;

    public AIOptimizationPipelineBehaviorIntegrationTests()
    {
        _aiEngineMock = new Mock<IAIOptimizationEngine>();
        _loggerMock = new Mock<ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>>>();
        _systemMetricsMock = new Mock<ISystemLoadMetricsProvider>();
        _metricsProviderMock = new Mock<IMetricsProvider>();

        _options = new AIOptimizationOptions
        {
            Enabled = true,
            LearningEnabled = true,
            MinConfidenceScore = 0.7,
            MinCacheHitRate = 0.5
        };
    }

    [Fact]
    public async Task HandleAsync_WithRequestTypeAttribute_UsesGetAIOptimizationAttributes()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            RequestType = typeof(OptimizedTestRequest),
            TotalExecutions = 100,
            SuccessfulExecutions = 95,
            FailedExecutions = 5,
            AverageExecutionTime = TimeSpan.FromMilliseconds(150),
            P50ExecutionTime = TimeSpan.FromMilliseconds(120),
            P95ExecutionTime = TimeSpan.FromMilliseconds(250),
            P99ExecutionTime = TimeSpan.FromMilliseconds(400),
            LastExecution = DateTimeOffset.UtcNow,
            Properties = new Dictionary<string, object>
            {
                ["DatabaseCalls"] = 2,
                ["ExternalApiCalls"] = 1
            }
        };

        _metricsProviderMock.Setup(m => m.GetHandlerExecutionStats(typeof(OptimizedTestRequest)))
            .Returns(stats);

        _systemMetricsMock.Setup(m => m.GetCurrentLoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemLoadMetrics
            {
                CpuUtilization = 0.5,
                MemoryUtilization = 0.5
            });

        _aiEngineMock.Setup(m => m.AnalyzeRequestAsync(
                It.IsAny<OptimizedTestRequest>(),
                It.IsAny<RequestExecutionMetrics>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.None,
                ConfidenceScore = 0.5
            });

        var optimizedLoggerMock = new Mock<ILogger<AIOptimizationPipelineBehavior<OptimizedTestRequest, TestResponse>>>();
        var behavior = new AIOptimizationPipelineBehavior<OptimizedTestRequest, TestResponse>(
            _aiEngineMock.Object,
            optimizedLoggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        var executionCount = 0;
        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executionCount++;
            return new ValueTask<TestResponse>(new TestResponse { Result = "test" });
        };

        // Act
        var result = await behavior.HandleAsync(new OptimizedTestRequest(), next, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, executionCount);
        _metricsProviderMock.Verify(m => m.GetHandlerExecutionStats(typeof(OptimizedTestRequest)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_WithHistoricalMetrics_UsesGetHistoricalMetrics()
    {
        // Arrange - Setup stats with database and API calls
        var stats = new HandlerExecutionStats
        {
            RequestType = typeof(TestRequest),
            TotalExecutions = 100,
            SuccessfulExecutions = 95,
            FailedExecutions = 5,
            AverageExecutionTime = TimeSpan.FromMilliseconds(250),
            P50ExecutionTime = TimeSpan.FromMilliseconds(200),
            P95ExecutionTime = TimeSpan.FromMilliseconds(400),
            P99ExecutionTime = TimeSpan.FromMilliseconds(800),
            LastExecution = DateTimeOffset.UtcNow.AddSeconds(-10),
            Properties = new Dictionary<string, object>
            {
                ["DatabaseCalls"] = 3,
                ["ExternalApiCalls"] = 2,
                ["HttpCalls"] = 2L
            }
        };

        _metricsProviderMock.Setup(m => m.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns(stats);

        _systemMetricsMock.Setup(m => m.GetCurrentLoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemLoadMetrics
            {
                CpuUtilization = 0.5,
                MemoryUtilization = 0.5
            });

        RequestExecutionMetrics? capturedMetrics = null;
        _aiEngineMock.Setup(m => m.AnalyzeRequestAsync(
                It.IsAny<TestRequest>(),
                It.IsAny<RequestExecutionMetrics>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestRequest, RequestExecutionMetrics, CancellationToken>((req, metrics, ct) =>
            {
                capturedMetrics = metrics;
            })
            .ReturnsAsync(new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.None,
                ConfidenceScore = 0.5
            });

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "test" });

        // Act
        await behavior.HandleAsync(new TestRequest(), next, CancellationToken.None);

        // Assert - Verify that historical metrics were used
        Assert.NotNull(capturedMetrics);
        Assert.Equal(100, capturedMetrics.TotalExecutions);
        Assert.Equal(3, capturedMetrics.DatabaseCalls);
        Assert.Equal(2, capturedMetrics.ExternalApiCalls);
    }

    [Fact]
    public async Task HandleAsync_WithoutHistoricalMetrics_UsesDefaults()
    {
        // Arrange - No historical metrics available
        _metricsProviderMock.Setup(m => m.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns((HandlerExecutionStats?)null);

        _systemMetricsMock.Setup(m => m.GetCurrentLoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemLoadMetrics
            {
                CpuUtilization = 0.5,
                MemoryUtilization = 0.5
            });

        RequestExecutionMetrics? capturedMetrics = null;
        _aiEngineMock.Setup(m => m.AnalyzeRequestAsync(
                It.IsAny<TestRequest>(),
                It.IsAny<RequestExecutionMetrics>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestRequest, RequestExecutionMetrics, CancellationToken>((req, metrics, ct) =>
            {
                capturedMetrics = metrics;
            })
            .ReturnsAsync(new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.None,
                ConfidenceScore = 0.5
            });

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "test" });

        // Act
        await behavior.HandleAsync(new TestRequest(), next, CancellationToken.None);

        // Assert - Verify defaults were used
        Assert.NotNull(capturedMetrics);
        Assert.Equal(0, capturedMetrics.TotalExecutions);
        Assert.Equal(TimeSpan.FromMilliseconds(100), capturedMetrics.AverageExecutionTime);
    }

    [Fact]
    public async Task HandleAsync_WithHighCpuUsagePattern_EstimatesCpuCorrectly()
    {
        // Arrange - High P99/P50 ratio indicates high CPU usage
        var stats = new HandlerExecutionStats
        {
            RequestType = typeof(TestRequest),
            TotalExecutions = 100,
            SuccessfulExecutions = 95,
            FailedExecutions = 5,
            AverageExecutionTime = TimeSpan.FromMilliseconds(200),
            P50ExecutionTime = TimeSpan.FromMilliseconds(100),
            P95ExecutionTime = TimeSpan.FromMilliseconds(400),
            P99ExecutionTime = TimeSpan.FromMilliseconds(600), // 6x ratio = high CPU
            LastExecution = DateTimeOffset.UtcNow,
            Properties = new Dictionary<string, object>()
        };

        _metricsProviderMock.Setup(m => m.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns(stats);

        _systemMetricsMock.Setup(m => m.GetCurrentLoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemLoadMetrics
            {
                CpuUtilization = 0.5,
                MemoryUtilization = 0.5
            });

        RequestExecutionMetrics? capturedMetrics = null;
        _aiEngineMock.Setup(m => m.AnalyzeRequestAsync(
                It.IsAny<TestRequest>(),
                It.IsAny<RequestExecutionMetrics>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestRequest, RequestExecutionMetrics, CancellationToken>((req, metrics, ct) =>
            {
                capturedMetrics = metrics;
            })
            .ReturnsAsync(new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.None,
                ConfidenceScore = 0.5
            });

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "test" });

        // Act
        await behavior.HandleAsync(new TestRequest(), next, CancellationToken.None);

        // Assert - CPU usage should be high (0.8)
        Assert.NotNull(capturedMetrics);
        Assert.Equal(0.8, capturedMetrics.CpuUsage);
    }

    [Fact]
    public async Task HandleAsync_WithDatabaseCallsInProperties_ExtractsThem()
    {
        // Arrange - Different property formats for database calls
        var stats = new HandlerExecutionStats
        {
            RequestType = typeof(TestRequest),
            TotalExecutions = 50,
            SuccessfulExecutions = 50,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(150),
            P50ExecutionTime = TimeSpan.FromMilliseconds(120),
            P95ExecutionTime = TimeSpan.FromMilliseconds(200),
            P99ExecutionTime = TimeSpan.FromMilliseconds(300),
            LastExecution = DateTimeOffset.UtcNow,
            Properties = new Dictionary<string, object>
            {
                ["DatabaseCalls"] = 5
            }
        };

        _metricsProviderMock.Setup(m => m.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns(stats);

        _systemMetricsMock.Setup(m => m.GetCurrentLoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemLoadMetrics());

        RequestExecutionMetrics? capturedMetrics = null;
        _aiEngineMock.Setup(m => m.AnalyzeRequestAsync(
                It.IsAny<TestRequest>(),
                It.IsAny<RequestExecutionMetrics>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestRequest, RequestExecutionMetrics, CancellationToken>((req, metrics, ct) =>
            {
                capturedMetrics = metrics;
            })
            .ReturnsAsync(new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.None,
                ConfidenceScore = 0.5
            });

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse());

        // Act
        await behavior.HandleAsync(new TestRequest(), next, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedMetrics);
        Assert.Equal(5, capturedMetrics.DatabaseCalls);
    }

    [Fact]
    public async Task HandleAsync_WithAvgDatabaseCallsInProperties_RoundsThem()
    {
        // Arrange
        var stats = new HandlerExecutionStats
        {
            RequestType = typeof(TestRequest),
            TotalExecutions = 50,
            SuccessfulExecutions = 50,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(150),
            P50ExecutionTime = TimeSpan.FromMilliseconds(120),
            P95ExecutionTime = TimeSpan.FromMilliseconds(200),
            P99ExecutionTime = TimeSpan.FromMilliseconds(300),
            LastExecution = DateTimeOffset.UtcNow,
            Properties = new Dictionary<string, object>
            {
                ["AvgDatabaseCalls"] = 3.7
            }
        };

        _metricsProviderMock.Setup(m => m.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns(stats);

        _systemMetricsMock.Setup(m => m.GetCurrentLoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemLoadMetrics());

        RequestExecutionMetrics? capturedMetrics = null;
        _aiEngineMock.Setup(m => m.AnalyzeRequestAsync(
                It.IsAny<TestRequest>(),
                It.IsAny<RequestExecutionMetrics>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestRequest, RequestExecutionMetrics, CancellationToken>((req, metrics, ct) =>
            {
                capturedMetrics = metrics;
            })
            .ReturnsAsync(new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.None,
                ConfidenceScore = 0.5
            });

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse());

        // Act
        await behavior.HandleAsync(new TestRequest(), next, CancellationToken.None);

        // Assert - Should round 3.7 to 4
        Assert.NotNull(capturedMetrics);
        Assert.Equal(4, capturedMetrics.DatabaseCalls);
    }

    [Fact]
    public async Task HandleAsync_WithLongExecutionTime_EstimatesDatabaseCalls()
    {
        // Arrange - No explicit DB call data, estimate from execution time
        var stats = new HandlerExecutionStats
        {
            RequestType = typeof(TestRequest),
            TotalExecutions = 50,
            SuccessfulExecutions = 50,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(250), // Long enough to estimate
            P50ExecutionTime = TimeSpan.FromMilliseconds(200),
            P95ExecutionTime = TimeSpan.FromMilliseconds(300),
            P99ExecutionTime = TimeSpan.FromMilliseconds(400),
            LastExecution = DateTimeOffset.UtcNow,
            Properties = new Dictionary<string, object>() // No explicit DB calls
        };

        _metricsProviderMock.Setup(m => m.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns(stats);

        _systemMetricsMock.Setup(m => m.GetCurrentLoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemLoadMetrics());

        RequestExecutionMetrics? capturedMetrics = null;
        _aiEngineMock.Setup(m => m.AnalyzeRequestAsync(
                It.IsAny<TestRequest>(),
                It.IsAny<RequestExecutionMetrics>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestRequest, RequestExecutionMetrics, CancellationToken>((req, metrics, ct) =>
            {
                capturedMetrics = metrics;
            })
            .ReturnsAsync(new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.None,
                ConfidenceScore = 0.5
            });

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse());

        // Act
        await behavior.HandleAsync(new TestRequest(), next, CancellationToken.None);

        // Assert - Should estimate DB calls from time (250ms / 50ms = 5)
        Assert.NotNull(capturedMetrics);
        Assert.True(capturedMetrics.DatabaseCalls > 0);
    }

    [Fact]
    public async Task HandleAsync_WithHighVariabilityExternalApi_EstimatesApiCalls()
    {
        // Arrange - High P99/P50 ratio with long execution suggests external API
        var stats = new HandlerExecutionStats
        {
            RequestType = typeof(TestRequest),
            TotalExecutions = 50,
            SuccessfulExecutions = 50,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(300),
            P50ExecutionTime = TimeSpan.FromMilliseconds(200),
            P95ExecutionTime = TimeSpan.FromMilliseconds(500),
            P99ExecutionTime = TimeSpan.FromMilliseconds(1000), // High variability (5x ratio)
            LastExecution = DateTimeOffset.UtcNow,
            Properties = new Dictionary<string, object>() // No explicit API calls
        };

        _metricsProviderMock.Setup(m => m.GetHandlerExecutionStats(typeof(TestRequest)))
            .Returns(stats);

        _systemMetricsMock.Setup(m => m.GetCurrentLoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemLoadMetrics());

        RequestExecutionMetrics? capturedMetrics = null;
        _aiEngineMock.Setup(m => m.AnalyzeRequestAsync(
                It.IsAny<TestRequest>(),
                It.IsAny<RequestExecutionMetrics>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestRequest, RequestExecutionMetrics, CancellationToken>((req, metrics, ct) =>
            {
                capturedMetrics = metrics;
            })
            .ReturnsAsync(new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.None,
                ConfidenceScore = 0.5
            });

        var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
            _aiEngineMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            _systemMetricsMock.Object,
            _metricsProviderMock.Object);

        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse());

        // Act
        await behavior.HandleAsync(new TestRequest(), next, CancellationToken.None);

        // Assert - Should estimate 1 external API call
        Assert.NotNull(capturedMetrics);
        Assert.Equal(1, capturedMetrics.ExternalApiCalls);
    }

    // Helper classes
    public class TestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    [AIOptimized(EnableMetricsTracking = true, AutoApplyOptimizations = true)]
    public class OptimizedTestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }
}
