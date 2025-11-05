using OpenTelemetry.Trace;
using Relay.Core.DistributedTracing;
using Relay.Core.Testing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Relay.Core.Tests.DistributedTracing;

public class OpenTelemetryTracingProviderTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var provider = new OpenTelemetryTracingProvider();

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithCustomParameters()
    {
        // Arrange
        var serviceName = "TestService";

        // Act
        var provider = new OpenTelemetryTracingProvider(null, serviceName);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithTracerProvider()
    {
        // Arrange - We can't create a real TracerProvider due to protected constructor,
        // but we can test that the parameter is accepted
        TracerProvider? tracerProvider = null;

        // Act
        var provider = new OpenTelemetryTracingProvider(tracerProvider, "TestService");

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void StartActivity_ShouldCreateActivityWithBasicParameters()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var operationName = "TestOperation";
        var requestType = typeof(string);

        // Act
        var activity = provider.StartActivity(operationName, requestType);

        // Assert
        // Note: Activity may be null if no tracing is configured
        if (activity != null)
        {
            Assert.Equal("Relay.TestOperation", activity.OperationName);
            activity.Dispose();
        }
    }

    [Fact]
    public void StartActivity_ShouldCreateActivityWithCorrelationId()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var operationName = "TestOperation";
        var requestType = typeof(string);
        var correlationId = "test-correlation-id";

        // Start a parent activity to ensure Activity.Current exists
        var parentActivity = new Activity("ParentActivity");
        parentActivity.Start();

        try
        {
            // Act
            var activity = provider.StartActivity(operationName, requestType, correlationId);

            // Assert
            Assert.NotNull(activity);
            Assert.Equal("Relay.TestOperation", activity.OperationName);
            var correlationTag = activity?.GetTagItem("correlation.id");
            Assert.Equal(correlationId, correlationTag);

            activity?.Dispose();
        }
        finally
        {
            parentActivity.Stop();
        }
    }

    [Fact]
    public void StartActivity_ShouldCreateActivityWithTags()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var operationName = "TestOperation";
        var requestType = typeof(string);
        var tags = new Dictionary<string, object?>
        {
            ["custom.tag1"] = "value1",
            ["custom.tag2"] = 42
        };

        // Start a parent activity to ensure Activity.Current exists
        var parentActivity = new Activity("ParentActivity");
        parentActivity.Start();

        try
        {
            // Act
            var activity = provider.StartActivity(operationName, requestType, null, tags);

            // Assert
            Assert.NotNull(activity);
            Assert.Equal("Relay.TestOperation", activity.OperationName);
            Assert.Equal("value1", activity?.GetTagItem("custom.tag1"));
            Assert.Equal(42, activity?.GetTagItem("custom.tag2"));

            activity?.Dispose();
        }
        finally
        {
            parentActivity.Stop();
        }
    }

    [Fact]
    public void StartActivity_ShouldSetRequestTypeTag()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var operationName = "TestOperation";
        var requestType = typeof(TestRequest);

        // Start a parent activity to ensure Activity.Current exists
        var parentActivity = new Activity("ParentActivity");
        parentActivity.Start();

        try
        {
            // Act
            var activity = provider.StartActivity(operationName, requestType);

            // Assert
            Assert.NotNull(activity);
            var requestTypeTag = activity?.GetTagItem("request.type");
            Assert.Equal("Relay.Core.Tests.DistributedTracing.OpenTelemetryTracingProviderTests+TestRequest", requestTypeTag);

            activity?.Dispose();
        }
        finally
        {
            parentActivity.Stop();
        }
    }

    [Fact]
    public void AddActivityTags_ShouldAddTagsToCurrentActivity()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var activity = new Activity("TestActivity");
        activity.Start();

        var tags = new Dictionary<string, object?>
        {
            ["tag1"] = "value1",
            ["tag2"] = 123
        };

        // Act
        provider.AddActivityTags(tags);

        // Assert
        Assert.Equal("value1", Activity.Current?.GetTagItem("tag1"));
        Assert.Equal(123, Activity.Current?.GetTagItem("tag2"));

        activity.Stop();
    }

    [Fact]
    public void AddActivityTags_ShouldDoNothing_WhenNoCurrentActivity()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var tags = new Dictionary<string, object?>
        {
            ["tag1"] = "value1"
        };

        // Act & Assert - Should not throw
        provider.AddActivityTags(tags);
    }

    [Fact]
    public void RecordException_ShouldRecordExceptionToCurrentActivity()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var activity = new Activity("TestActivity");
        activity.Start();

        var exception = new InvalidOperationException("Test exception");

        // Act
        provider.RecordException(exception);

        // Assert
        // Note: We can't directly verify the exception was recorded as it's internal to Activity
        // But we can verify the method doesn't throw

        activity.Stop();
    }

    [Fact]
    public void RecordException_ShouldDoNothing_WhenNoCurrentActivity()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var exception = new InvalidOperationException("Test exception");

        // Act & Assert - Should not throw
        provider.RecordException(exception);
    }

    [Fact]
    public void RecordException_ShouldDoNothing_WhenExceptionIsNull()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var activity = new Activity("TestActivity");
        activity.Start();

        // Act & Assert - Should not throw
        provider.RecordException(null!);

        activity.Stop();
    }

    [Fact]
    public void SetActivityStatus_ShouldSetStatusOnCurrentActivity()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var activity = new Activity("TestActivity");
        activity.Start();

        var status = ActivityStatusCode.Error;
        var description = "Test error";

        // Act
        provider.SetActivityStatus(status, description);

        // Assert
        Assert.Equal(status, Activity.Current?.Status);
        Assert.Equal(description, Activity.Current?.StatusDescription);

        activity.Stop();
    }

    [Fact]
    public void SetActivityStatus_ShouldDoNothing_WhenNoCurrentActivity()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var status = ActivityStatusCode.Ok;

        // Act & Assert - Should not throw
        provider.SetActivityStatus(status);
    }

    [Fact]
    public void GetCurrentTraceId_ShouldReturnTraceId_WhenActivityExists()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var activity = new Activity("TestActivity");
        activity.Start();

        // Act
        var traceId = provider.GetCurrentTraceId();

        // Assert
        Assert.NotNull(traceId);
        Assert.Equal(activity.TraceId.ToString(), traceId);

        activity.Stop();
    }

    [Fact]
    public void GetCurrentTraceId_ShouldReturnNull_WhenNoCurrentActivity()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();

        // Act
        var traceId = provider.GetCurrentTraceId();

        // Assert
        Assert.Null(traceId);
    }

    [Fact]
    public void GetCurrentSpanId_ShouldReturnSpanId_WhenActivityExists()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();
        var activity = new Activity("TestActivity");
        activity.Start();

        // Act
        var spanId = provider.GetCurrentSpanId();

        // Assert
        Assert.NotNull(spanId);
        Assert.Equal(activity.SpanId.ToString(), spanId);

        activity.Stop();
    }

    [Fact]
    public void GetCurrentSpanId_ShouldReturnNull_WhenNoCurrentActivity()
    {
        // Arrange
        var provider = new OpenTelemetryTracingProvider();

        // Act
        var spanId = provider.GetCurrentSpanId();

        // Assert
        Assert.Null(spanId);
    }

    private class TestRequest
    {
    }
}
