using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class DefaultTelemetryProviderTests
{
    private readonly Mock<ILogger<DefaultTelemetryProvider>> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;

    public DefaultTelemetryProviderTests()
    {
        _loggerMock = new Mock<ILogger<DefaultTelemetryProvider>>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
    }

    [Fact]
    public void Constructor_WithLoggerAndMetricsProvider_ShouldInitializeCorrectly()
    {
        // Act
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal(_metricsProviderMock.Object, provider.MetricsProvider);
    }

    [Fact]
    public void Constructor_WithLoggerOnly_ShouldCreateDefaultMetricsProvider()
    {
        // Act
        var provider = new DefaultTelemetryProvider(_loggerMock.Object);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<DefaultMetricsProvider>(provider.MetricsProvider);
    }

    [Fact]
    public void Constructor_WithMetricsProviderOnly_ShouldInitializeCorrectly()
    {
        // Act
        var provider = new DefaultTelemetryProvider(null, _metricsProviderMock.Object);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal(_metricsProviderMock.Object, provider.MetricsProvider);
    }

    [Fact]
    public void Constructor_WithNoParameters_ShouldCreateDefaultMetricsProvider()
    {
        // Act
        var provider = new DefaultTelemetryProvider();

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<DefaultMetricsProvider>(provider.MetricsProvider);
    }

    [Fact]
    public void StartActivity_WithCorrelationId_ShouldCreateActivityWithTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new DefaultTelemetryProvider(_loggerMock.Object);
        var requestType = typeof(string);
        var operationName = "TestOperation";
        var correlationId = "test-correlation-123";

        // Act
        using var activity = provider.StartActivity(operationName, requestType, correlationId);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(operationName, activity.OperationName);
        Assert.Equal(requestType.FullName, activity.GetTagItem("relay.request_type"));
        Assert.Equal(operationName, activity.GetTagItem("relay.operation"));
        Assert.Equal(correlationId, activity.GetTagItem("relay.correlation_id"));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Started activity {activity.Id} for {requestType.Name}")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void StartActivity_WithoutCorrelationId_ShouldCreateActivityWithoutCorrelationTag()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new DefaultTelemetryProvider(_loggerMock.Object);
        var requestType = typeof(string);
        var operationName = "TestOperation";

        // Act
        using var activity = provider.StartActivity(operationName, requestType);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(operationName, activity.OperationName);
        Assert.Equal(requestType.FullName, activity.GetTagItem("relay.request_type"));
        Assert.Equal(operationName, activity.GetTagItem("relay.operation"));
        Assert.Null(activity.GetTagItem("relay.correlation_id"));
    }

    [Fact]
    public void StartActivity_WhenActivitySourceReturnsNull_ShouldReturnNull()
    {
        // Arrange - ActivitySource will return null if no listener is configured
        var provider = new DefaultTelemetryProvider(_loggerMock.Object);
        var requestType = typeof(string);
        var operationName = "TestOperation";

        // Act
        var activity = provider.StartActivity(operationName, requestType);

        // Assert
        Assert.Null(activity);
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void RecordHandlerExecution_WithSuccessfulExecution_ShouldRecordMetricsAndSetTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        var requestType = typeof(string);
        var responseType = typeof(int);
        var handlerName = "TestHandler";
        var duration = TimeSpan.FromMilliseconds(100);

        using var activity = provider.StartActivity("Test", requestType);

        // Act
        provider.RecordHandlerExecution(requestType, responseType, handlerName, duration, true);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(handlerName, activity.GetTagItem("relay.handler_name"));
        Assert.Equal(responseType.FullName, activity.GetTagItem("relay.response_type"));
        Assert.Equal(duration.TotalMilliseconds, activity.GetTagItem("relay.duration_ms"));
        Assert.Equal(true, activity.GetTagItem("relay.success"));
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);

        _metricsProviderMock.Verify(
            x => x.RecordHandlerExecution(It.Is<HandlerExecutionMetrics>(m =>
                m.RequestType == requestType &&
                m.ResponseType == responseType &&
                m.HandlerName == handlerName &&
                m.Duration == duration &&
                m.Success == true &&
                m.Exception == null)),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Handler execution completed: {requestType.Name} -> {responseType.Name} in {duration.TotalMilliseconds}ms (Success: True)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordHandlerExecution_WithException_ShouldRecordFailureAndSetErrorStatus()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        var requestType = typeof(string);
        var responseType = typeof(int);
        var handlerName = "TestHandler";
        var duration = TimeSpan.FromMilliseconds(100);
        var exception = new InvalidOperationException("Test error");

        using var activity = provider.StartActivity("Test", requestType);

        // Act
        provider.RecordHandlerExecution(requestType, responseType, handlerName, duration, false, exception);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(handlerName, activity.GetTagItem("relay.handler_name"));
        Assert.Equal(responseType.FullName, activity.GetTagItem("relay.response_type"));
        Assert.Equal(duration.TotalMilliseconds, activity.GetTagItem("relay.duration_ms"));
        Assert.Equal(false, activity.GetTagItem("relay.success"));
        Assert.Equal(typeof(InvalidOperationException).FullName, activity.GetTagItem("relay.exception_type"));
        Assert.Equal("Test error", activity.GetTagItem("relay.exception_message"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("Test error", activity.StatusDescription);

        _metricsProviderMock.Verify(
            x => x.RecordHandlerExecution(It.Is<HandlerExecutionMetrics>(m =>
                m.RequestType == requestType &&
                m.ResponseType == responseType &&
                m.HandlerName == handlerName &&
                m.Duration == duration &&
                m.Success == false &&
                m.Exception == exception)),
            Times.Once);
    }

    [Fact]
    public void RecordHandlerExecution_WithNullActivity_ShouldStillRecordMetrics()
    {
        // Arrange - No activity listener, so Activity.Current will be null
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        var requestType = typeof(string);
        var responseType = typeof(int);
        var handlerName = "TestHandler";
        var duration = TimeSpan.FromMilliseconds(100);

        // Act
        provider.RecordHandlerExecution(requestType, responseType, handlerName, duration, true);

        // Assert
        _metricsProviderMock.Verify(
            x => x.RecordHandlerExecution(It.Is<HandlerExecutionMetrics>(m =>
                m.RequestType == requestType &&
                m.ResponseType == responseType &&
                m.HandlerName == handlerName &&
                m.Duration == duration &&
                m.Success == true &&
                m.Exception == null &&
                m.OperationId != null)),
            Times.Once);
    }

    [Fact]
    public void RecordNotificationPublish_WithSuccessfulPublish_ShouldRecordMetricsAndSetTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        var notificationType = typeof(string);
        var handlerCount = 3;
        var duration = TimeSpan.FromMilliseconds(50);

        using var activity = provider.StartActivity("Test", notificationType);

        // Act
        provider.RecordNotificationPublish(notificationType, handlerCount, duration, true);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(notificationType.FullName, activity.GetTagItem("relay.notification_type"));
        Assert.Equal(handlerCount, activity.GetTagItem("relay.handler_count"));
        Assert.Equal(duration.TotalMilliseconds, activity.GetTagItem("relay.duration_ms"));
        Assert.Equal(true, activity.GetTagItem("relay.success"));
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);

        _metricsProviderMock.Verify(
            x => x.RecordNotificationPublish(It.Is<NotificationPublishMetrics>(m =>
                m.NotificationType == notificationType &&
                m.HandlerCount == handlerCount &&
                m.Duration == duration &&
                m.Success == true &&
                m.Exception == null)),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Notification published: {notificationType.Name} to {handlerCount} handlers in {duration.TotalMilliseconds}ms (Success: True)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordNotificationPublish_WithException_ShouldRecordFailure()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        var notificationType = typeof(string);
        var handlerCount = 2;
        var duration = TimeSpan.FromMilliseconds(75);
        var exception = new Exception("Publish failed");

        using var activity = provider.StartActivity("Test", notificationType);

        // Act
        provider.RecordNotificationPublish(notificationType, handlerCount, duration, false, exception);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(notificationType.FullName, activity.GetTagItem("relay.notification_type"));
        Assert.Equal(handlerCount, activity.GetTagItem("relay.handler_count"));
        Assert.Equal(duration.TotalMilliseconds, activity.GetTagItem("relay.duration_ms"));
        Assert.Equal(false, activity.GetTagItem("relay.success"));
        Assert.Equal(typeof(Exception).FullName, activity.GetTagItem("relay.exception_type"));
        Assert.Equal("Publish failed", activity.GetTagItem("relay.exception_message"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);

        _metricsProviderMock.Verify(
            x => x.RecordNotificationPublish(It.Is<NotificationPublishMetrics>(m =>
                m.NotificationType == notificationType &&
                m.HandlerCount == handlerCount &&
                m.Duration == duration &&
                m.Success == false &&
                m.Exception == exception)),
            Times.Once);
    }

    [Fact]
    public void RecordNotificationPublish_WithNullActivity_ShouldStillRecordMetrics()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        var notificationType = typeof(string);
        var handlerCount = 1;
        var duration = TimeSpan.FromMilliseconds(25);

        // Act
        provider.RecordNotificationPublish(notificationType, handlerCount, duration, true);

        // Assert
        _metricsProviderMock.Verify(
            x => x.RecordNotificationPublish(It.Is<NotificationPublishMetrics>(m =>
                m.NotificationType == notificationType &&
                m.HandlerCount == handlerCount &&
                m.Duration == duration &&
                m.Success == true &&
                m.Exception == null)),
            Times.Once);
    }

    [Fact]
    public void RecordStreamingOperation_WithSuccessfulOperation_ShouldRecordMetricsAndSetTags()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        var requestType = typeof(string);
        var responseType = typeof(int);
        var handlerName = "StreamHandler";
        var duration = TimeSpan.FromMilliseconds(200);
        var itemCount = 10L;

        using var activity = provider.StartActivity("Test", requestType);

        // Act
        provider.RecordStreamingOperation(requestType, responseType, handlerName, duration, itemCount, true);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(requestType.FullName, activity.GetTagItem("relay.request_type"));
        Assert.Equal(responseType.FullName, activity.GetTagItem("relay.response_type"));
        Assert.Equal(handlerName, activity.GetTagItem("relay.handler_name"));
        Assert.Equal(itemCount, activity.GetTagItem("relay.item_count"));
        Assert.Equal(duration.TotalMilliseconds, activity.GetTagItem("relay.duration_ms"));
        Assert.Equal(true, activity.GetTagItem("relay.success"));
        Assert.Equal(ActivityStatusCode.Ok, activity.Status);

        _metricsProviderMock.Verify(
            x => x.RecordStreamingOperation(It.Is<StreamingOperationMetrics>(m =>
                m.RequestType == requestType &&
                m.ResponseType == responseType &&
                m.HandlerName == handlerName &&
                m.Duration == duration &&
                m.ItemCount == itemCount &&
                m.Success == true &&
                m.Exception == null)),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Streaming operation completed: {requestType.Name} -> {responseType.Name} ({itemCount} items) in {duration.TotalMilliseconds}ms (Success: True)")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordStreamingOperation_WithException_ShouldRecordFailure()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        var requestType = typeof(string);
        var responseType = typeof(int);
        var handlerName = "StreamHandler";
        var duration = TimeSpan.FromMilliseconds(150);
        var itemCount = 5L;
        var exception = new TimeoutException("Stream timeout");

        using var activity = provider.StartActivity("Test", requestType);

        // Act
        provider.RecordStreamingOperation(requestType, responseType, handlerName, duration, itemCount, false, exception);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(requestType.FullName, activity.GetTagItem("relay.request_type"));
        Assert.Equal(responseType.FullName, activity.GetTagItem("relay.response_type"));
        Assert.Equal(handlerName, activity.GetTagItem("relay.handler_name"));
        Assert.Equal(itemCount, activity.GetTagItem("relay.item_count"));
        Assert.Equal(duration.TotalMilliseconds, activity.GetTagItem("relay.duration_ms"));
        Assert.Equal(false, activity.GetTagItem("relay.success"));
        Assert.Equal(typeof(TimeoutException).FullName, activity.GetTagItem("relay.exception_type"));
        Assert.Equal("Stream timeout", activity.GetTagItem("relay.exception_message"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);

        _metricsProviderMock.Verify(
            x => x.RecordStreamingOperation(It.Is<StreamingOperationMetrics>(m =>
                m.RequestType == requestType &&
                m.ResponseType == responseType &&
                m.HandlerName == handlerName &&
                m.Duration == duration &&
                m.ItemCount == itemCount &&
                m.Success == false &&
                m.Exception == exception)),
            Times.Once);
    }

    [Fact]
    public void RecordStreamingOperation_WithNullActivity_ShouldStillRecordMetrics()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);
        var requestType = typeof(string);
        var responseType = typeof(int);
        var handlerName = "StreamHandler";
        var duration = TimeSpan.FromMilliseconds(100);
        var itemCount = 3L;

        // Act
        provider.RecordStreamingOperation(requestType, responseType, handlerName, duration, itemCount, true);

        // Assert
        _metricsProviderMock.Verify(
            x => x.RecordStreamingOperation(It.Is<StreamingOperationMetrics>(m =>
                m.RequestType == requestType &&
                m.ResponseType == responseType &&
                m.HandlerName == handlerName &&
                m.Duration == duration &&
                m.ItemCount == itemCount &&
                m.Success == true &&
                m.Exception == null)),
            Times.Once);
    }

    [Fact]
    public void SetCorrelationId_WithCurrentActivity_ShouldSetAsyncLocalAndActivityTag()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new DefaultTelemetryProvider();
        var correlationId = "test-correlation-456";

        using var activity = provider.StartActivity("Test", typeof(string));

        // Act
        provider.SetCorrelationId(correlationId);

        // Assert
        Assert.Equal(correlationId, activity.GetTagItem("relay.correlation_id"));
        Assert.Equal(correlationId, provider.GetCorrelationId());
    }

    [Fact]
    public void SetCorrelationId_WithNoCurrentActivity_ShouldOnlySetAsyncLocal()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider();
        var correlationId = "test-correlation-789";

        // Act
        provider.SetCorrelationId(correlationId);

        // Assert
        Assert.Equal(correlationId, provider.GetCorrelationId());
    }

    [Fact]
    public void GetCorrelationId_WithAsyncLocalValue_ShouldReturnAsyncLocalValue()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider();
        var correlationId = "async-local-correlation";

        provider.SetCorrelationId(correlationId);

        // Act
        var result = provider.GetCorrelationId();

        // Assert
        Assert.Equal(correlationId, result);
    }

    [Fact]
    public void GetCorrelationId_WithActivityTagAndNoAsyncLocal_ShouldReturnActivityTag()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new DefaultTelemetryProvider();
        var correlationId = "activity-tag-correlation";

        using var activity = provider.StartActivity("Test", typeof(string));
        activity.SetTag("relay.correlation_id", correlationId);

        // Clear AsyncLocal by creating new provider
        var newProvider = new DefaultTelemetryProvider();

        // Act
        var result = newProvider.GetCorrelationId();

        // Assert
        Assert.Equal(correlationId, result);
    }

    [Fact]
    public void GetCorrelationId_WithNoAsyncLocalAndNoActivity_ShouldReturnNull()
    {
        // Arrange
        var provider = new DefaultTelemetryProvider();

        // Act
        var result = provider.GetCorrelationId();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetCorrelationId_AsyncLocalTakesPrecedenceOverActivityTag()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var provider = new DefaultTelemetryProvider();
        var asyncLocalCorrelationId = "async-local-priority";
        var activityCorrelationId = "activity-tag-secondary";

        using var activity = provider.StartActivity("Test", typeof(string));
        activity.SetTag("relay.correlation_id", activityCorrelationId);

        provider.SetCorrelationId(asyncLocalCorrelationId);

        // Act
        var result = provider.GetCorrelationId();

        // Assert
        Assert.Equal(asyncLocalCorrelationId, result);
    }
}