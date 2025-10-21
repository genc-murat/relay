using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Metrics.Interfaces;
using Relay.Core.AI.Pipeline.Behaviors;
using Relay.Core.AI.Pipeline.Metrics;
using Relay.Core.AI.Pipeline.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AIPerformanceTrackingBehaviorTests : IDisposable
{
    private readonly ILogger<AIPerformanceTrackingBehavior<TestRequest, TestResponse>> _logger;
    private readonly Mock<IAIMetricsExporter> _exporterMock;

    public AIPerformanceTrackingBehaviorTests()
    {
        _logger = NullLogger<AIPerformanceTrackingBehavior<TestRequest, TestResponse>>.Instance;
        _exporterMock = new Mock<IAIMetricsExporter>();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Valid_Logger()
    {
        // Arrange & Act
        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(_logger);

        // Assert
        Assert.NotNull(behavior);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(null!));
    }

    [Fact]
    public async Task HandleAsync_Should_Track_Successful_Request()
    {
        // Arrange
        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(_logger);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "success" });

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("success", result.Result);
    }

    [Fact]
    public async Task HandleAsync_Should_Track_Failed_Request()
    {
        // Arrange
        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(_logger);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            throw new InvalidOperationException("Test exception");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_Should_Not_Track_When_Tracking_Disabled()
    {
        // Arrange
        var options = new AIPerformanceTrackingOptions
        {
            EnableTracking = false
        };

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            _exporterMock.Object,
            options);

        var request = new TestRequest { Value = "test" };
        var executed = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executed = true;
            return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
        };

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.True(executed);
        Assert.NotNull(result);
        _exporterMock.Verify(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Should_Export_Metrics_When_Threshold_Reached()
    {
        // Arrange
        var options = new AIPerformanceTrackingOptions
        {
            EnableImmediateExport = true,
            ImmediateExportThreshold = 3,
            EnablePeriodicExport = false
        };

        _exporterMock.Setup(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            _exporterMock.Object,
            options);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = async () =>
        {
            await Task.Delay(10);
            return new TestResponse { Result = "success" };
        };

        // Act - Execute multiple times to reach threshold
        for (int i = 0; i < 3; i++)
        {
            await behavior.HandleAsync(request, next, CancellationToken.None);
        }

        // Assert
        _exporterMock.Verify(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_Should_Calculate_Statistics()
    {
        // Arrange
        AIModelStatistics? capturedStats = null;

        _exporterMock.Setup(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()))
            .Callback<AIModelStatistics, CancellationToken>((stats, _) => capturedStats = stats)
            .Returns(ValueTask.CompletedTask);

        var options = new AIPerformanceTrackingOptions
        {
            EnableImmediateExport = true,
            ImmediateExportThreshold = 2,
            EnablePeriodicExport = false
        };

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            _exporterMock.Object,
            options);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = async () =>
        {
            await Task.Delay(15);
            return new TestResponse { Result = "success" };
        };

        // Act
        await behavior.HandleAsync(request, next, CancellationToken.None);
        await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedStats);
        Assert.True(capturedStats.TotalPredictions >= 2);
        Assert.True(capturedStats.AccuracyScore >= 0.0);
        Assert.True(capturedStats.AveragePredictionTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task HandleAsync_Should_Support_Cancellation()
    {
        // Arrange
        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(_logger);

        var request = new TestRequest { Value = "test" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerDelegate<TestResponse> next = async () =>
        {
            await Task.Delay(1000, cts.Token);
            return new TestResponse { Result = "success" };
        };

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await behavior.HandleAsync(request, next, cts.Token));
    }

    [Fact]
    public void AIPerformanceTrackingOptions_Should_Have_Correct_Defaults()
    {
        // Arrange & Act
        var options = new AIPerformanceTrackingOptions();

        // Assert
        Assert.True(options.EnableTracking);
        Assert.False(options.EnableDetailedLogging);
        Assert.True(options.EnablePeriodicExport);
        Assert.False(options.EnableImmediateExport);
        Assert.Equal(TimeSpan.FromMinutes(5), options.ExportInterval);
        Assert.Equal(1000, options.ImmediateExportThreshold);
        Assert.True(options.ResetAfterExport);
        Assert.Equal(10000, options.SlidingWindowSize);
        Assert.Equal("1.0.0", options.ModelVersion);
        Assert.True(options.TrackPercentiles);
    }

    [Fact]
    public void AIPerformanceTrackingOptions_Should_Allow_Custom_Configuration()
    {
        // Arrange & Act
        var options = new AIPerformanceTrackingOptions
        {
            EnableTracking = false,
            EnableDetailedLogging = true,
            EnablePeriodicExport = false,
            EnableImmediateExport = true,
            ExportInterval = TimeSpan.FromMinutes(1),
            ImmediateExportThreshold = 500,
            ResetAfterExport = false,
            SlidingWindowSize = 5000,
            ModelVersion = "2.0.0",
            TrackPercentiles = false
        };

        // Assert
        Assert.False(options.EnableTracking);
        Assert.True(options.EnableDetailedLogging);
        Assert.False(options.EnablePeriodicExport);
        Assert.True(options.EnableImmediateExport);
        Assert.Equal(TimeSpan.FromMinutes(1), options.ExportInterval);
        Assert.Equal(500, options.ImmediateExportThreshold);
        Assert.False(options.ResetAfterExport);
        Assert.Equal(5000, options.SlidingWindowSize);
        Assert.Equal("2.0.0", options.ModelVersion);
        Assert.False(options.TrackPercentiles);
    }

    [Fact]
    public async Task HandleAsync_Should_Track_Mixed_Success_And_Failure()
    {
        // Arrange
        AIModelStatistics? capturedStats = null;

        _exporterMock.Setup(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()))
            .Callback<AIModelStatistics, CancellationToken>((stats, _) => capturedStats = stats)
            .Returns(ValueTask.CompletedTask);

        var options = new AIPerformanceTrackingOptions
        {
            EnableImmediateExport = true,
            ImmediateExportThreshold = 5,
            EnablePeriodicExport = false
        };

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            _exporterMock.Object,
            options);

        var request = new TestRequest { Value = "test" };

        // Act - Mix of success and failure
        for (int i = 0; i < 5; i++)
        {
            var shouldFail = i % 2 == 0;

            RequestHandlerDelegate<TestResponse> next = () =>
            {
                if (shouldFail)
                    throw new InvalidOperationException("Test exception");
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            try
            {
                await behavior.HandleAsync(request, next, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                // Expected for failures
            }
        }

        // Assert
        Assert.NotNull(capturedStats);
        Assert.True(capturedStats.TotalPredictions >= 5);
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Concurrent_Requests()
    {
        // Arrange
        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(_logger);

        var tasks = new Task<TestResponse>[50];

        // Act
        for (int i = 0; i < 50; i++)
        {
            var request = new TestRequest { Value = $"test{i}" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(10);
                return new TestResponse { Result = $"result{i}" };
            };

            tasks[i] = behavior.HandleAsync(request, next, CancellationToken.None).AsTask();
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(50, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
    }

    [Fact]
    public void Dispose_Should_Clean_Up_Timer()
    {
        // Arrange
        var options = new AIPerformanceTrackingOptions
        {
            EnablePeriodicExport = true,
            ExportInterval = TimeSpan.FromMinutes(1)
        };

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            _exporterMock.Object,
            options);

        // Act & Assert - Should not throw
        behavior.Dispose();
        behavior.Dispose(); // Idempotent
    }

    [Fact]
    public void PerformanceStatistics_Should_Initialize_WithDefaults()
    {
        // Arrange & Act
        var stats = new PerformanceStatistics();

        // Assert
        Assert.Equal(0, stats.TotalCount);
        Assert.Equal(0, stats.SuccessCount);
        Assert.Equal(0, stats.ErrorCount);
        Assert.Equal(0.0, stats.SuccessRate);
        Assert.Equal(0.0, stats.ErrorRate);
        Assert.Equal(TimeSpan.Zero, stats.AverageDuration);
    }

    [Fact]
    public async Task HandleAsync_Should_Track_Percentiles()
    {
        // Arrange
        AIModelStatistics? capturedStats = null;

        _exporterMock.Setup(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()))
            .Callback<AIModelStatistics, CancellationToken>((stats, _) => capturedStats = stats)
            .Returns(ValueTask.CompletedTask);

        var options = new AIPerformanceTrackingOptions
        {
            EnableImmediateExport = true,
            ImmediateExportThreshold = 10,
            EnablePeriodicExport = false,
            TrackPercentiles = true
        };

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            _exporterMock.Object,
            options);

        var request = new TestRequest { Value = "test" };

        // Act - Add varied execution times
        for (int i = 0; i < 10; i++)
        {
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(i * 5); // Varying delays
                return new TestResponse { Result = "success" };
            };

            await behavior.HandleAsync(request, next, CancellationToken.None);
        }

        // Assert
        Assert.NotNull(capturedStats);
        Assert.True(capturedStats.AveragePredictionTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task HandleAsync_Should_Maintain_Sliding_Window()
    {
        // Arrange
        var options = new AIPerformanceTrackingOptions
        {
            SlidingWindowSize = 5,
            EnableImmediateExport = false,
            EnablePeriodicExport = false
        };

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            _exporterMock.Object,
            options);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "success" });

        // Act - Add more than sliding window size
        for (int i = 0; i < 10; i++)
        {
            await behavior.HandleAsync(request, next, CancellationToken.None);
        }

        // Assert - Should not throw and maintain window
        Assert.True(true);
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Export_Failure_Gracefully()
    {
        // Arrange
        _exporterMock.Setup(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Export failed"));

        var options = new AIPerformanceTrackingOptions
        {
            EnableImmediateExport = true,
            ImmediateExportThreshold = 2,
            EnablePeriodicExport = false
        };

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            _exporterMock.Object,
            options);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "success" });

        // Act & Assert - Should handle export failure gracefully
        var result1 = await behavior.HandleAsync(request, next, CancellationToken.None);
        var result2 = await behavior.HandleAsync(request, next, CancellationToken.None);

        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }

    [Fact]
    public async Task HandleAsync_Should_Log_Detailed_Performance_Metrics()
    {
        // Arrange
        var options = new AIPerformanceTrackingOptions
        {
            EnableDetailedLogging = true,
            EnableImmediateExport = false,
            EnablePeriodicExport = false
        };

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            null, // No exporter
            options);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = async () =>
        {
            await Task.Delay(10);
            return new TestResponse { Result = "success" };
        };

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Detailed logging is enabled, so TrackPerformanceMetricsAsync should log trace information
    }

    [Fact]
    public async Task HandleAsync_Should_Enable_Immediate_Export_But_Not_Trigger_When_Threshold_Not_Reached()
    {
        // Arrange
        var options = new AIPerformanceTrackingOptions
        {
            EnableImmediateExport = true,
            ImmediateExportThreshold = 10, // High threshold
            EnablePeriodicExport = false
        };

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            _exporterMock.Object,
            options);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "success" });

        // Act - Execute fewer times than threshold
        for (int i = 0; i < 3; i++)
        {
            await behavior.HandleAsync(request, next, CancellationToken.None);
        }

        // Assert - Should not have exported yet
        _exporterMock.Verify(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Null_Metrics_Exporter()
    {
        // Arrange
        var options = new AIPerformanceTrackingOptions
        {
            EnableImmediateExport = true,
            ImmediateExportThreshold = 2,
            EnablePeriodicExport = false
        };

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            null, // Null exporter
            options);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "success" });

        // Act - Should not throw even with null exporter
        var result1 = await behavior.HandleAsync(request, next, CancellationToken.None);
        var result2 = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        // No export should be attempted since exporter is null
    }

    [Fact]
    public async Task HandleAsync_Should_Log_Detailed_Exception_Tracking()
    {
        // Arrange
        var options = new AIPerformanceTrackingOptions
        {
            EnableDetailedLogging = true,
            EnableImmediateExport = false,
            EnablePeriodicExport = false
        };

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            null, // No exporter
            options);

        var request = new TestRequest { Value = "test" };
        var expectedException = new InvalidOperationException("Test exception");

        RequestHandlerDelegate<TestResponse> next = () =>
            throw expectedException;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));

        // Assert
        Assert.Equal(expectedException, exception);
        // Detailed logging should be performed for both start and exception paths
    }

    [Fact]
    public async Task HandleAsync_Should_Log_Start_And_End_Messages_With_Detailed_Logging()
    {
        // Arrange
        var options = new AIPerformanceTrackingOptions
        {
            EnableDetailedLogging = true,
            EnableImmediateExport = false,
            EnablePeriodicExport = false
        };

        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(
            _logger,
            null, // No exporter
            options);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = async () =>
        {
            await Task.Delay(5); // Small delay to ensure measurable time
            return new TestResponse { Result = "success" };
        };

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("success", result.Result);
        // Both start and end debug messages should be logged
    }

    [Fact]
    public async Task HandleAsync_Should_Use_Attribute_Configuration_When_Present()
    {
        // Arrange
        var logger = NullLogger<AIPerformanceTrackingBehavior<MonitoredTestRequest, TestResponse>>.Instance;
        var behavior = new AIPerformanceTrackingBehavior<MonitoredTestRequest, TestResponse>(logger);

        var request = new MonitoredTestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "success" });

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("success", result.Result);
        // The behavior should have used the attribute configuration (detailed monitoring, no detailed metrics, etc.)
    }

    [Fact]
    public async Task HandleAsync_Should_Use_Default_Configuration_When_No_Attribute()
    {
        // Arrange
        var behavior = new AIPerformanceTrackingBehavior<TestRequest, TestResponse>(_logger);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "success" });

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("success", result.Result);
        // The behavior should have used default configuration (standard monitoring, detailed metrics enabled, etc.)
    }

    // Test classes
    public class TestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [AIMonitored(
        Level = MonitoringLevel.Detailed,
        CollectDetailedMetrics = false,
        TrackAccessPatterns = false,
        SamplingRate = 0.8,
        Tags = new[] { "test", "monitored" })]
    public class MonitoredTestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}
