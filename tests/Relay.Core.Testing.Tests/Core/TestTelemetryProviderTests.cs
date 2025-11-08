using System;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class TestTelemetryProviderTests
{
    [Fact]
    public void TestTelemetryProvider_StartActivity_RecordsActivity()
    {
        // Arrange
        var provider = new TestTelemetryProvider();

        // Act
        var activity = provider.StartActivity("TestOperation", typeof(string));

        // Assert
        Assert.Single(provider.Activities);
        var recordedActivity = provider.Activities[0];
        Assert.Equal("TestOperation", recordedActivity.OperationName);
        Assert.Contains("relay.request_type", recordedActivity.Tags);
        Assert.Contains("relay.operation", recordedActivity.Tags);
        Assert.Equal("System.String", recordedActivity.Tags["relay.request_type"]);
        Assert.Equal("TestOperation", recordedActivity.Tags["relay.operation"]);
        // Activity may be null if no listeners are configured
    }

    [Fact]
    public void TestTelemetryProvider_StartActivity_WithCorrelationId_SetsCorrelationId()
    {
        // Arrange
        var provider = new TestTelemetryProvider();
        var correlationId = "test-correlation-id";

        // Act
        provider.StartActivity("TestOperation", typeof(string), correlationId);

        // Assert
        Assert.Single(provider.Activities);
        var recordedActivity = provider.Activities[0];
        Assert.Contains("relay.correlation_id", recordedActivity.Tags);
        Assert.Equal(correlationId, recordedActivity.Tags["relay.correlation_id"]);
        Assert.Equal(correlationId, provider.GetCorrelationId());
    }

    [Fact]
    public void TestTelemetryProvider_StartActivity_SetsTagsOnActivity_WithoutCorrelationId()
    {
        // Arrange
        var provider = new TestTelemetryProvider();

        // Act
        var activity = provider.StartActivity("TestOperation", typeof(string));

        // Assert
        Assert.Single(provider.Activities);
        var recordedActivity = provider.Activities[0];

        // Verify tags are recorded in TestActivity
        Assert.Equal(2, recordedActivity.Tags.Count);
        Assert.Contains("relay.request_type", recordedActivity.Tags);
        Assert.Contains("relay.operation", recordedActivity.Tags);

        // Verify tags are set on the actual Activity (if it exists)
        if (activity != null)
        {
            var tags = activity.Tags.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value);
            Assert.Equal(2, tags.Count);
            Assert.Contains("relay.request_type", tags);
            Assert.Contains("relay.operation", tags);
            Assert.Equal("System.String", tags["relay.request_type"]);
            Assert.Equal("TestOperation", tags["relay.operation"]);
        }
    }

    [Fact]
    public void TestTelemetryProvider_StartActivity_SetsTagsOnActivity_WithCorrelationId()
    {
        // Arrange
        var provider = new TestTelemetryProvider();
        var correlationId = "test-correlation-id";

        // Act
        var activity = provider.StartActivity("TestOperation", typeof(string), correlationId);

        // Assert
        Assert.Single(provider.Activities);
        var recordedActivity = provider.Activities[0];

        // Verify tags are recorded in TestActivity
        Assert.Equal(3, recordedActivity.Tags.Count);
        Assert.Contains("relay.request_type", recordedActivity.Tags);
        Assert.Contains("relay.operation", recordedActivity.Tags);
        Assert.Contains("relay.correlation_id", recordedActivity.Tags);

        // Verify tags are set on the actual Activity (if it exists)
        if (activity != null)
        {
            var tags = activity.Tags.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value);
            Assert.Equal(3, tags.Count);
            Assert.Contains("relay.request_type", tags);
            Assert.Contains("relay.operation", tags);
            Assert.Contains("relay.correlation_id", tags);
            Assert.Equal("System.String", tags["relay.request_type"]);
            Assert.Equal("TestOperation", tags["relay.operation"]);
            Assert.Equal(correlationId, tags["relay.correlation_id"]);
        }
    }

    [Fact]
    public void TestTelemetryProvider_RecordHandlerExecution_RecordsExecution()
    {
        // Arrange
        var provider = new TestTelemetryProvider();
        var requestType = typeof(string);
        var responseType = typeof(int);
        var handlerName = "TestHandler";
        var duration = TimeSpan.FromMilliseconds(100);
        var success = true;
        var exception = new Exception("Test exception");

        // Act
        provider.RecordHandlerExecution(requestType, responseType, handlerName, duration, success, exception);

        // Assert
        Assert.Single(provider.HandlerExecutions);
        var execution = provider.HandlerExecutions[0];
        Assert.Equal(requestType, execution.RequestType);
        Assert.Equal(responseType, execution.ResponseType);
        Assert.Equal(handlerName, execution.HandlerName);
        Assert.Equal(duration, execution.Duration);
        Assert.Equal(success, execution.Success);
        Assert.Equal(exception, execution.Exception);
    }

    [Fact]
    public void TestTelemetryProvider_RecordNotificationPublish_RecordsPublish()
    {
        // Arrange
        var provider = new TestTelemetryProvider();
        var notificationType = typeof(string);
        var handlerCount = 5;
        var duration = TimeSpan.FromMilliseconds(200);
        var success = true;
        var exception = new Exception("Publish failed");

        // Act
        provider.RecordNotificationPublish(notificationType, handlerCount, duration, success, exception);

        // Assert
        Assert.Single(provider.NotificationPublishes);
        var publish = provider.NotificationPublishes[0];
        Assert.Equal(notificationType, publish.NotificationType);
        Assert.Equal(handlerCount, publish.HandlerCount);
        Assert.Equal(duration, publish.Duration);
        Assert.Equal(success, publish.Success);
        Assert.Equal(exception, publish.Exception);
    }

    [Fact]
    public void TestTelemetryProvider_RecordStreamingOperation_RecordsOperation()
    {
        // Arrange
        var provider = new TestTelemetryProvider();
        var requestType = typeof(string);
        var responseType = typeof(int);
        var handlerName = "StreamingHandler";
        var duration = TimeSpan.FromSeconds(1);
        var itemCount = 100L;
        var success = true;
        var exception = new Exception("Streaming failed");

        // Act
        provider.RecordStreamingOperation(requestType, responseType, handlerName, duration, itemCount, success, exception);

        // Assert
        Assert.Single(provider.StreamingOperations);
        var operation = provider.StreamingOperations[0];
        Assert.Equal(requestType, operation.RequestType);
        Assert.Equal(responseType, operation.ResponseType);
        Assert.Equal(handlerName, operation.HandlerName);
        Assert.Equal(duration, operation.Duration);
        Assert.Equal(itemCount, operation.ItemCount);
        Assert.Equal(success, operation.Success);
        Assert.Equal(exception, operation.Exception);
    }

    [Fact]
    public void TestTelemetryProvider_RecordCircuitBreakerStateChange_RecordsChange()
    {
        // Arrange
        var provider = new TestTelemetryProvider();
        var circuitBreakerName = "TestCircuitBreaker";
        var oldState = "Closed";
        var newState = "Open";

        // Act
        provider.RecordCircuitBreakerStateChange(circuitBreakerName, oldState, newState);

        // Assert
        Assert.Single(provider.CircuitBreakerStateChanges);
        var change = provider.CircuitBreakerStateChanges[0];
        Assert.Equal(circuitBreakerName, change.CircuitBreakerName);
        Assert.Equal(oldState, change.OldState);
        Assert.Equal(newState, change.NewState);
    }

    [Fact]
    public void TestTelemetryProvider_RecordCircuitBreakerOperation_RecordsOperation()
    {
        // Arrange
        var provider = new TestTelemetryProvider();
        var circuitBreakerName = "TestCircuitBreaker";
        var operation = "Execute";
        var success = false;
        var exception = new Exception("Circuit breaker operation failed");

        // Act
        provider.RecordCircuitBreakerOperation(circuitBreakerName, operation, success, exception);

        // Assert
        Assert.Single(provider.CircuitBreakerOperations);
        var recordedOperation = provider.CircuitBreakerOperations[0];
        Assert.Equal(circuitBreakerName, recordedOperation.CircuitBreakerName);
        Assert.Equal(operation, recordedOperation.Operation);
        Assert.Equal(success, recordedOperation.Success);
        Assert.Equal(exception, recordedOperation.Exception);
    }

    [Fact]
    public void TestTelemetryProvider_SetCorrelationId_SetsId()
    {
        // Arrange
        var provider = new TestTelemetryProvider();
        var correlationId = "test-id-123";

        // Act
        provider.SetCorrelationId(correlationId);

        // Assert
        Assert.Equal(correlationId, provider.GetCorrelationId());
    }

    [Fact]
    public void TestTelemetryProvider_GetCorrelationId_ReturnsNullWhenNotSet()
    {
        // Arrange
        var provider = new TestTelemetryProvider();

        // Act & Assert
        Assert.Null(provider.GetCorrelationId());
    }

    [Fact]
    public void TestTelemetryProvider_HasMetricsProvider()
    {
        // Arrange
        var provider = new TestTelemetryProvider();

        // Act & Assert
        Assert.NotNull(provider.MetricsProvider);
        Assert.IsType<TestMetricsProvider>(provider.MetricsProvider);
    }

    [Fact]
    public void TestActivity_InitializesWithDefaults()
    {
        // Act
        var activity = new TestActivity();

        // Assert
        Assert.NotNull(activity.OperationName);
        Assert.NotNull(activity.Tags);
        Assert.Empty(activity.OperationName);
        Assert.Empty(activity.Tags);
    }

    [Fact]
    public void HandlerExecution_InitializesWithDefaults()
    {
        // Act
        var execution = new HandlerExecution();

        // Assert
        Assert.Null(execution.RequestType); // Initialized with null!
        Assert.Equal(default, execution.Duration);
        Assert.False(execution.Success);
        Assert.Null(execution.ResponseType);
        Assert.Null(execution.HandlerName);
        Assert.Null(execution.Exception);
    }

    [Fact]
    public void NotificationPublish_InitializesWithDefaults()
    {
        // Act
        var publish = new NotificationPublish();

        // Assert
        Assert.Null(publish.NotificationType); // Initialized with null!
        Assert.Equal(default, publish.Duration);
        Assert.Equal(0, publish.HandlerCount);
        Assert.False(publish.Success);
        Assert.Null(publish.Exception);
    }

    [Fact]
    public void StreamingOperation_InitializesWithDefaults()
    {
        // Act
        var operation = new StreamingOperation();

        // Assert
        Assert.Null(operation.RequestType); // Initialized with null!
        Assert.Null(operation.ResponseType); // Initialized with null!
        Assert.Equal(default, operation.Duration);
        Assert.Equal(0L, operation.ItemCount);
        Assert.False(operation.Success);
        Assert.Null(operation.HandlerName);
        Assert.Null(operation.Exception);
    }

    [Fact]
    public void CircuitBreakerStateChange_InitializesWithDefaults()
    {
        // Act
        var change = new CircuitBreakerStateChange();

        // Assert
        Assert.NotNull(change.CircuitBreakerName);
        Assert.NotNull(change.OldState);
        Assert.NotNull(change.NewState);
        Assert.Empty(change.CircuitBreakerName);
        Assert.Empty(change.OldState);
        Assert.Empty(change.NewState);
    }

    [Fact]
    public void CircuitBreakerOperation_InitializesWithDefaults()
    {
        // Act
        var operation = new CircuitBreakerOperation();

        // Assert
        Assert.NotNull(operation.CircuitBreakerName);
        Assert.NotNull(operation.Operation);
        Assert.Empty(operation.CircuitBreakerName);
        Assert.Empty(operation.Operation);
        Assert.False(operation.Success);
        Assert.Null(operation.Exception);
    }
}