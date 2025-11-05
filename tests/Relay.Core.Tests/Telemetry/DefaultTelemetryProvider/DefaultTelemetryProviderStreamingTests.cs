using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Relay.Core.Testing;
using Xunit;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Relay.Core.Tests.Telemetry;

/// <summary>
/// Tests for DefaultTelemetryProvider streaming operation functionality
/// </summary>
public class DefaultTelemetryProviderStreamingTests
{
    private readonly Mock<ILogger<DefaultTelemetryProvider>> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;

    public DefaultTelemetryProviderStreamingTests()
    {
        _loggerMock = new Mock<ILogger<DefaultTelemetryProvider>>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
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
                MsLogLevel.Debug,
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
}
