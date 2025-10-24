using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using HandlerExecutionMetrics = Relay.Core.Telemetry.HandlerExecutionMetrics;
using IMetricsProvider = Relay.Core.Telemetry.IMetricsProvider;
using NotificationPublishMetrics = Relay.Core.Telemetry.NotificationPublishMetrics;
using RelayTelemetryOptions = Relay.Core.Telemetry.RelayTelemetryOptions;
using RelayTelemetryProvider = Relay.Core.Telemetry.RelayTelemetryProvider;
using StreamingOperationMetrics = Relay.Core.Telemetry.StreamingOperationMetrics;

namespace Relay.Core.Tests.Telemetry;

public class RelayTelemetryProviderEdgeCaseTests
{
    private readonly IOptions<RelayTelemetryOptions> _options;
    private readonly Mock<ILogger<RelayTelemetryProvider>> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;

    public RelayTelemetryProviderEdgeCaseTests()
    {
        _options = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = true
        });
        _loggerMock = new Mock<ILogger<RelayTelemetryProvider>>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
    }

    [Fact]
    public void Constructor_With_Null_Options_Should_Throw_ArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RelayTelemetryProvider(null!, _loggerMock.Object, _metricsProviderMock.Object));
    }

    [Fact]
    public void Constructor_With_Null_Component_In_Options_Should_Throw_ArgumentNullException()
    {
        // Arrange
        var badOptions = Options.Create(new RelayTelemetryOptions
        {
            Component = null,
            EnableTracing = true
        });

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RelayTelemetryProvider(badOptions, _loggerMock.Object, _metricsProviderMock.Object));
    }

    [Fact]
    public void StartActivity_With_Valid_Parameters_Should_Not_Throw()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object);

        // Act
        var exception = Record.Exception(() => provider.StartActivity("TestOperation", typeof(string), "test-correlation"));

        // Assert
        Assert.Null(exception);
        // Note: Activity creation depends on ActivitySource being listened to by ActivityListeners
        // In test environments without listeners, this may return null, which is acceptable
    }

    [Fact]
    public void RecordHandlerExecution_With_Zero_Duration_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, _metricsProviderMock.Object);

        // Act
        var exception = Record.Exception(() => provider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.Zero, true));

        // Assert
        Assert.Null(exception);
        _metricsProviderMock.Verify(m => m.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()), Times.Once);
    }

    [Fact]
    public void RecordHandlerExecution_With_Negative_Duration_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, _metricsProviderMock.Object);

        // Act
        var exception = Record.Exception(() => provider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromTicks(-1), true));

        // Assert
        Assert.Null(exception);
        _metricsProviderMock.Verify(m => m.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()), Times.Once);
    }

    [Fact]
    public void RecordNotificationPublish_With_Zero_Handler_Count_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, _metricsProviderMock.Object);

        // Act
        var exception = Record.Exception(() => provider.RecordNotificationPublish(typeof(string), 0, TimeSpan.Zero, true));

        // Assert
        Assert.Null(exception);
        _metricsProviderMock.Verify(m => m.RecordNotificationPublish(It.IsAny<NotificationPublishMetrics>()), Times.Once);
    }

    [Fact]
    public void RecordNotificationPublish_With_Negative_Handler_Count_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, _metricsProviderMock.Object);

        // Act
        var exception = Record.Exception(() => provider.RecordNotificationPublish(typeof(string), -1, TimeSpan.Zero, true));

        // Assert
        Assert.Null(exception);
        _metricsProviderMock.Verify(m => m.RecordNotificationPublish(It.IsAny<NotificationPublishMetrics>()), Times.Once);
    }

    [Fact]
    public void RecordStreamingOperation_With_Zero_Item_Count_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, _metricsProviderMock.Object);

        // Act
        var exception = Record.Exception(() => provider.RecordStreamingOperation(typeof(string), typeof(int), "TestHandler", TimeSpan.Zero, 0, true));

        // Assert
        Assert.Null(exception);
        _metricsProviderMock.Verify(m => m.RecordStreamingOperation(It.IsAny<StreamingOperationMetrics>()), Times.Once);
    }

    [Fact]
    public void RecordStreamingOperation_With_Negative_Item_Count_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, _metricsProviderMock.Object);

        // Act
        var exception = Record.Exception(() => provider.RecordStreamingOperation(typeof(string), typeof(int), "TestHandler", TimeSpan.Zero, -10, true));

        // Assert
        Assert.Null(exception);
        _metricsProviderMock.Verify(m => m.RecordStreamingOperation(It.IsAny<StreamingOperationMetrics>()), Times.Once);
    }

    [Fact]
    public void RecordMessagePublished_With_Zero_Payload_Size_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object);

        // Act
        var exception = Record.Exception(() => provider.RecordMessagePublished(typeof(string), 0, TimeSpan.Zero, true));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void RecordMessagePublished_With_Negative_Payload_Size_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object);

        // Act
        var exception = Record.Exception(() => provider.RecordMessagePublished(typeof(string), -100, TimeSpan.Zero, true));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void RecordMessageReceived_With_Valid_Type_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object);

        // Act
        var exception = Record.Exception(() => provider.RecordMessageReceived(typeof(string)));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void RecordMessageProcessed_With_Zero_Duration_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object);

        // Act
        var exception = Record.Exception(() => provider.RecordMessageProcessed(typeof(string), TimeSpan.Zero, true));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void RecordMessageProcessed_With_Negative_Duration_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object);

        // Act
        var exception = Record.Exception(() => provider.RecordMessageProcessed(typeof(string), TimeSpan.FromTicks(-1), true));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void SetCorrelationId_With_Special_Characters_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object);
        var specialCorrelationId = "test-correlation-id-with-special-chars-@#$%^&*()";

        // Act
        var exception = Record.Exception(() => provider.SetCorrelationId(specialCorrelationId));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void SetCorrelationId_With_Very_Long_String_Should_Work()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object);
        var longCorrelationId = new string('A', 10000); // Very long string

        // Act
        var exception = Record.Exception(() => provider.SetCorrelationId(longCorrelationId));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_Multiple_Times_Should_Not_Throw()
    {
        // Arrange
        var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, _metricsProviderMock.Object);

        // Act
        provider.Dispose();

        // Assert - Second dispose should not throw
        var exception = Record.Exception(() => provider.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public async Task Concurrent_Access_To_Provider_Methods_Should_Be_Thread_Safe()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, _metricsProviderMock.Object);
        var tasks = new List<Task>();

        // Act - Run multiple operations concurrently
        for (int i = 0; i < 50; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                switch (taskId % 8)
                {
                    case 0:
                        provider.RecordHandlerExecution(typeof(string), typeof(int), $"Handler{taskId}", 
                            TimeSpan.FromMilliseconds(taskId), taskId % 2 == 0);
                        break;
                    case 1:
                        provider.RecordNotificationPublish(typeof(string), taskId, 
                            TimeSpan.FromMilliseconds(taskId), taskId % 2 == 0);
                        break;
                    case 2:
                        provider.RecordStreamingOperation(typeof(string), typeof(int), $"StreamHandler{taskId}",
                            TimeSpan.FromMilliseconds(taskId), taskId, taskId % 2 == 0);
                        break;
                    case 3:
                        provider.SetCorrelationId($"corr-{taskId}");
                        break;
                    case 4:
                        provider.RecordMessagePublished(typeof(string), taskId * 100, 
                            TimeSpan.FromMilliseconds(taskId), taskId % 2 == 0);
                        break;
                    case 5:
                        provider.RecordMessageReceived(typeof(string));
                        break;
                    case 6:
                        provider.RecordMessageProcessed(typeof(string), TimeSpan.FromMilliseconds(taskId), taskId % 2 == 0);
                        break;
                    case 7:
                        var correlationId = provider.GetCorrelationId();
                        break;
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All operations should complete without exception
        _metricsProviderMock.Verify(m => m.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()), Times.AtLeast(1));
        _metricsProviderMock.Verify(m => m.RecordNotificationPublish(It.IsAny<NotificationPublishMetrics>()), Times.AtLeast(1));
        _metricsProviderMock.Verify(m => m.RecordStreamingOperation(It.IsAny<StreamingOperationMetrics>()), Times.AtLeast(1));
    }

    [Fact]
    public void MetricsProvider_Property_With_Null_MetricsProvider_Should_Return_Null()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, null);

        // Act
        var metricsProvider = provider.MetricsProvider;

        // Assert
        Assert.Null(metricsProvider);
    }

    [Fact]
    public void MetricsProvider_Property_With_Valid_MetricsProvider_Should_Return_Instance()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, _metricsProviderMock.Object);

        // Act
        var metricsProvider = provider.MetricsProvider;

        // Assert
        Assert.Same(_metricsProviderMock.Object, metricsProvider);
    }

    [Fact]
    public void StartActivity_With_Tracing_Disabled_Should_Return_Null()
    {
        // Arrange
        var disabledOptions = Options.Create(new RelayTelemetryOptions
        {
            Component = "TestComponent",
            EnableTracing = false
        });
        using var provider = new RelayTelemetryProvider(disabledOptions, _loggerMock.Object);

        // Act
        var activity = provider.StartActivity("TestOperation", typeof(string));

        // Assert
        Assert.Null(activity);
    }

    [Fact]
    public void RecordHandlerExecution_With_Exception_Should_Record_Error_Info()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, _metricsProviderMock.Object);
        var exception = new InvalidOperationException("Test exception");

        // Act
        var recordedException = Record.Exception(() => provider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), false, exception));

        // Assert
        Assert.Null(recordedException);
        _metricsProviderMock.Verify(m => m.RecordHandlerExecution(It.IsAny<HandlerExecutionMetrics>()), Times.Once);
    }

    [Fact]
    public void RecordNotificationPublish_With_Exception_Should_Record_Error_Info()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, _metricsProviderMock.Object);
        var exception = new InvalidOperationException("Test exception");

        // Act
        var recordedException = Record.Exception(() => provider.RecordNotificationPublish(typeof(string), 1, TimeSpan.FromMilliseconds(50), false, exception));

        // Assert
        Assert.Null(recordedException);
        _metricsProviderMock.Verify(m => m.RecordNotificationPublish(It.IsAny<NotificationPublishMetrics>()), Times.Once);
    }

    [Fact]
    public void RecordStreamingOperation_With_Exception_Should_Record_Error_Info()
    {
        // Arrange
        using var provider = new RelayTelemetryProvider(_options, _loggerMock.Object, _metricsProviderMock.Object);
        var exception = new InvalidOperationException("Test exception");

        // Act
        var recordedException = Record.Exception(() => provider.RecordStreamingOperation(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(75), 5, false, exception));

        // Assert
        Assert.Null(recordedException);
        _metricsProviderMock.Verify(m => m.RecordStreamingOperation(It.IsAny<StreamingOperationMetrics>()), Times.Once);
    }
}