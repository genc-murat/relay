using System;
using System.Diagnostics;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

/// <summary>
/// Tests for TelemetryContext static methods
/// </summary>
public class TelemetryContextTests
{
    [Fact]
    public void Create_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var requestType = typeof(string);
        var responseType = typeof(int);
        var handlerName = "TestHandler";
        var correlationId = "correlation-123";
        var activity = new Activity("TestActivity");

        // Act
        var context = TelemetryContext.Create(requestType, responseType, handlerName, correlationId, activity);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(requestType, context.RequestType);
        Assert.Equal(responseType, context.ResponseType);
        Assert.Equal(handlerName, context.HandlerName);
        Assert.Equal(correlationId, context.CorrelationId);
        Assert.Equal(activity, context.Activity);
        Assert.NotEqual(default, context.StartTime);
        Assert.NotNull(context.Properties);
        Assert.NotNull(context.RequestId);
    }

    [Fact]
    public void Create_WithMinimalParameters_ShouldSetRequiredProperties()
    {
        // Arrange
        var requestType = typeof(string);

        // Act
        var context = TelemetryContext.Create(requestType);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(requestType, context.RequestType);
        Assert.Null(context.ResponseType);
        Assert.Null(context.HandlerName);
        Assert.Null(context.CorrelationId);
        Assert.Null(context.Activity);
        Assert.NotEqual(default, context.StartTime);
        Assert.NotNull(context.Properties);
        Assert.NotNull(context.RequestId);
    }

    [Fact]
    public void Create_WithNullResponseType_ShouldSetResponseTypeToNull()
    {
        // Arrange
        var requestType = typeof(string);

        // Act
        var context = TelemetryContext.Create(requestType, null);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(requestType, context.RequestType);
        Assert.Null(context.ResponseType);
    }

    [Fact]
    public void Create_WithNullHandlerName_ShouldSetHandlerNameToNull()
    {
        // Arrange
        var requestType = typeof(string);

        // Act
        var context = TelemetryContext.Create(requestType, null, null);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(requestType, context.RequestType);
        Assert.Null(context.HandlerName);
    }

    [Fact]
    public void Create_WithNullCorrelationId_ShouldSetCorrelationIdToNull()
    {
        // Arrange
        var requestType = typeof(string);

        // Act
        var context = TelemetryContext.Create(requestType, null, null, null);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(requestType, context.RequestType);
        Assert.Null(context.CorrelationId);
    }

    [Fact]
    public void Create_WithNullActivity_ShouldSetActivityToNull()
    {
        // Arrange
        var requestType = typeof(string);

        // Act
        var context = TelemetryContext.Create(requestType, null, null, null, null);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(requestType, context.RequestType);
        Assert.Null(context.Activity);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueRequestId()
    {
        // Arrange
        var requestType = typeof(string);

        // Act
        var context1 = TelemetryContext.Create(requestType);
        var context2 = TelemetryContext.Create(requestType);

        // Assert
        Assert.NotNull(context1.RequestId);
        Assert.NotNull(context2.RequestId);
        Assert.NotEqual(context1.RequestId, context2.RequestId);
    }

    [Fact]
    public void Create_ShouldInitializePropertiesDictionary()
    {
        // Arrange
        var requestType = typeof(string);

        // Act
        var context = TelemetryContext.Create(requestType);

        // Assert
        Assert.NotNull(context.Properties);
        Assert.Empty(context.Properties);
    }

    [Fact]
    public void Create_ShouldSetStartTimeToCurrentTime()
    {
        // Arrange
        var requestType = typeof(string);
        var before = DateTimeOffset.UtcNow;

        // Act
        var context = TelemetryContext.Create(requestType);
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.InRange(context.StartTime, before, after);
    }

    [Fact]
    public void Create_WithActivity_ShouldSetActivityProperty()
    {
        // Arrange
        var requestType = typeof(string);
        var activity = new Activity("TestActivity").Start();

        // Act
        var context = TelemetryContext.Create(requestType, null, null, null, activity);

        // Assert
        Assert.Equal(activity, context.Activity);

        // Cleanup
        activity.Stop();
    }
}