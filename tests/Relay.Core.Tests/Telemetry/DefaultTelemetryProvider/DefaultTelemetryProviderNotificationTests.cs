using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

/// <summary>
using Relay.Core.Testing;
/// Tests for DefaultTelemetryProvider notification publishing functionality
/// </summary>
public class DefaultTelemetryProviderNotificationTests
{
    private readonly Mock<ILogger<DefaultTelemetryProvider>> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;

    public DefaultTelemetryProviderNotificationTests()
    {
        _loggerMock = new Mock<ILogger<DefaultTelemetryProvider>>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
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
}
