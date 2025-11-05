using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

/// <summary>
using Relay.Core.Testing;
/// Tests for DefaultTelemetryProvider handler execution recording functionality
/// </summary>
public class DefaultTelemetryProviderHandlerExecutionTests
{
    private readonly Mock<ILogger<DefaultTelemetryProvider>> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;

    public DefaultTelemetryProviderHandlerExecutionTests()
    {
        _loggerMock = new Mock<ILogger<DefaultTelemetryProvider>>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
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
}
