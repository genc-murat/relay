using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

/// <summary>
using Relay.Core.Testing;
/// Tests for DefaultTelemetryProvider activity functionality
/// </summary>
public class DefaultTelemetryProviderActivityTests
{
    private readonly Mock<ILogger<DefaultTelemetryProvider>> _loggerMock;

    public DefaultTelemetryProviderActivityTests()
    {
        _loggerMock = new Mock<ILogger<DefaultTelemetryProvider>>();
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
}
