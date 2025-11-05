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
/// Tests for DefaultTelemetryProvider correlation ID functionality
/// </summary>
public class DefaultTelemetryProviderCorrelationTests
{
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
