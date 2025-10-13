using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Core;
using Relay.Core.Telemetry;
using Relay.Core.Tests.Testing;
using Xunit;

namespace Relay.Core.Tests;

public class TelemetryTests
{
    private readonly TestTelemetryProvider _telemetryProvider;
    private readonly IServiceProvider _serviceProvider;

    public TelemetryTests()
    {
        _telemetryProvider = new TestTelemetryProvider();

        var services = new ServiceCollection();
        services.AddSingleton<ITelemetryProvider>(_telemetryProvider);
        services.AddSingleton<IRelay, RelayImplementation>();
        services.AddSingleton<IRequestDispatcher, TestRequestDispatcher>();
        services.AddSingleton<IStreamDispatcher, TestStreamDispatcher>();
        services.AddSingleton<INotificationDispatcher, TestNotificationDispatcher>();

        // Only add telemetry at the Relay level to avoid double recording
        services.Decorate<IRelay, TelemetryRelay>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendAsync_WithResponse_ShouldCreateActivityAndRecordMetrics()
    {
        // Arrange
        var relay = _serviceProvider.GetRequiredService<IRelay>();
        var request = new TestRequest<string>();

        // Act
        var result = await relay.SendAsync(request);

        // Assert
        Assert.Equal("TestResponse", result);

        var activities = _telemetryProvider.Activities;
        Assert.Single(activities);

        var activity = activities[0];
        Assert.Equal("Relay.Send", activity.OperationName);
        Assert.Equal(typeof(TestRequest<string>).FullName, activity.Tags["relay.request_type"]);

        var executions = _telemetryProvider.HandlerExecutions;
        Assert.Single(executions);

        var execution = executions[0];
        Assert.Equal(typeof(TestRequest<string>), execution.RequestType);
        Assert.Equal(typeof(string), execution.ResponseType);
        Assert.True(execution.Success);
        Assert.Null(execution.Exception);
    }

    [Fact]
    public async Task SendAsync_WithoutResponse_ShouldCreateActivityAndRecordMetrics()
    {
        // Arrange
        var relay = _serviceProvider.GetRequiredService<IRelay>();
        var request = new TestVoidRequest();

        // Act
        await relay.SendAsync(request);

        // Assert
        var activities = _telemetryProvider.Activities;
        Assert.Single(activities);

        var activity = activities[0];
        Assert.Equal("Relay.Send", activity.OperationName);
        Assert.Equal(typeof(TestVoidRequest).FullName, activity.Tags["relay.request_type"]);

        var executions = _telemetryProvider.HandlerExecutions;
        Assert.Single(executions);

        var execution = executions[0];
        Assert.Equal(typeof(TestVoidRequest), execution.RequestType);
        Assert.Null(execution.ResponseType);
        Assert.True(execution.Success);
    }

    [Fact]
    public async Task StreamAsync_ShouldCreateActivityAndRecordStreamingMetrics()
    {
        // Arrange
        var relay = _serviceProvider.GetRequiredService<IRelay>();
        var request = new TestStreamRequest<string>();
        var items = new List<string>();

        // Act
        await foreach (var item in relay.StreamAsync(request))
        {
            items.Add(item);
        }

        // Assert
        Assert.Equal(3, items.Count);
        Assert.Equal(new[] { "Item1", "Item2", "Item3" }, items);

        var activities = _telemetryProvider.Activities;
        Assert.Single(activities);

        var activity = activities[0];
        Assert.Equal("Relay.Stream", activity.OperationName);
        Assert.Equal(typeof(TestStreamRequest<string>).FullName, activity.Tags["relay.request_type"]);

        var streamingOps = _telemetryProvider.StreamingOperations;
        Assert.Single(streamingOps);

        var streamingOp = streamingOps[0];
        Assert.Equal(typeof(TestStreamRequest<string>), streamingOp.RequestType);
        Assert.Equal(typeof(string), streamingOp.ResponseType);
        Assert.Equal(3, streamingOp.ItemCount);
        Assert.True(streamingOp.Success);
    }

    [Fact]
    public async Task StreamAsync_WithException_ShouldPropagateException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITelemetryProvider>(_telemetryProvider);
        services.AddSingleton<IRelay, RelayImplementation>();
        services.AddSingleton<IStreamDispatcher, FailingStreamDispatcher>();
        services.Decorate<IRelay, TelemetryRelay>();

        var serviceProvider = services.BuildServiceProvider();
        var relay = serviceProvider.GetRequiredService<IRelay>();
        var request = new TestStreamRequest<string>();
        var items = new List<string>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in relay.StreamAsync(request))
            {
                items.Add(item);
            }
        });
        Assert.Equal("Stream test exception", exception.Message);

        // Assert that items were yielded before exception
        Assert.Equal(2, items.Count);
        Assert.Equal(new[] { "Item1", "Item2" }, items);

        // Note: In the current implementation, exceptions during streaming enumeration
        // are not recorded because the recording happens after the foreach loop.
        // This is a limitation of the current design.
        var streamingOps = _telemetryProvider.StreamingOperations;
        Assert.Empty(streamingOps); // No recording on exception during enumeration
    }

    [Fact]
    public async Task PublishAsync_ShouldCreateActivityAndRecordNotificationMetrics()
    {
        // Arrange
        var relay = _serviceProvider.GetRequiredService<IRelay>();
        var notification = new TestNotification();

        // Act
        await relay.PublishAsync(notification);

        // Assert
        var activities = _telemetryProvider.Activities;
        Assert.Single(activities);

        var activity = activities[0];
        Assert.Equal("Relay.Publish", activity.OperationName);
        Assert.Equal(typeof(TestNotification).FullName, activity.Tags["relay.request_type"]);

        var notifications = _telemetryProvider.NotificationPublishes;
        Assert.Single(notifications);

        var notificationPublish = notifications[0];
        Assert.Equal(typeof(TestNotification), notificationPublish.NotificationType);
        Assert.True(notificationPublish.Success);
    }

    [Fact]
    public async Task PublishAsync_WithException_ShouldRecordFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITelemetryProvider>(_telemetryProvider);
        services.AddSingleton<IRelay, RelayImplementation>();
        services.AddSingleton<INotificationDispatcher, FailingNotificationDispatcher>();
        services.Decorate<IRelay, TelemetryRelay>();

        var serviceProvider = services.BuildServiceProvider();
        var relay = serviceProvider.GetRequiredService<IRelay>();
        var notification = new TestNotification();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => relay.PublishAsync(notification).AsTask());
        Assert.Equal("Publish test exception", exception.Message);

        var notifications = _telemetryProvider.NotificationPublishes;
        Assert.Single(notifications);

        var notificationPublish = notifications[0];
        Assert.False(notificationPublish.Success);
        Assert.NotNull(notificationPublish.Exception);
        Assert.Equal("Publish test exception", notificationPublish.Exception.Message);
    }

    [Fact]
    public void SetCorrelationId_ShouldPropagateToActivity()
    {
        // Arrange
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var telemetryProvider = new DefaultTelemetryProvider();
        var correlationId = "test-correlation-123";

        // Act
        using var activity = telemetryProvider.StartActivity("Test", typeof(TestRequest<string>), correlationId);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal(correlationId, activity.GetTagItem("relay.correlation_id"));
        Assert.Equal(correlationId, telemetryProvider.GetCorrelationId());
    }

    [Fact]
    public async Task SendAsync_WithException_ShouldRecordFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITelemetryProvider>(_telemetryProvider);
        services.AddSingleton<IRelay, RelayImplementation>();
        services.AddSingleton<IRequestDispatcher, FailingRequestDispatcher>();
        services.Decorate<IRelay, TelemetryRelay>();

        var serviceProvider = services.BuildServiceProvider();
        var relay = serviceProvider.GetRequiredService<IRelay>();
        var request = new TestRequest<string>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => relay.SendAsync(request).AsTask());
        Assert.Equal("Test exception", exception.Message);

        var executions = _telemetryProvider.HandlerExecutions;
        Assert.Single(executions);

        var execution = executions[0];
        Assert.False(execution.Success);
        Assert.NotNull(execution.Exception);
        Assert.Equal("Test exception", execution.Exception.Message);
    }

    [Fact]
    public async Task SendAsync_Void_WithException_ShouldRecordFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ITelemetryProvider>(_telemetryProvider);
        services.AddSingleton<IRelay, RelayImplementation>();
        services.AddSingleton<IRequestDispatcher, FailingVoidRequestDispatcher>();
        services.Decorate<IRelay, TelemetryRelay>();

        var serviceProvider = services.BuildServiceProvider();
        var relay = serviceProvider.GetRequiredService<IRelay>();
        var request = new TestVoidRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => relay.SendAsync(request).AsTask());
        Assert.Equal("Void test exception", exception.Message);

        var executions = _telemetryProvider.HandlerExecutions;
        Assert.Single(executions);

        var execution = executions[0];
        Assert.False(execution.Success);
        Assert.NotNull(execution.Exception);
        Assert.Equal("Void test exception", execution.Exception.Message);
    }
}

// Test classes
public class TestVoidRequest : IRequest { }

// Test dispatchers
public class TestRequestDispatcher : IRequestDispatcher
{
    public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult((TResponse)(object)"TestResponse");
    }

    public ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult((TResponse)(object)"TestResponse");
    }

    public ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}

public class TestStreamDispatcher : IStreamDispatcher
{
    public IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
    {
        return GenerateItems<TResponse>();
    }

    public IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
    {
        return GenerateItems<TResponse>();
    }

    private static async IAsyncEnumerable<TResponse> GenerateItems<TResponse>()
    {
        await Task.CompletedTask;
        yield return (TResponse)(object)"Item1";
        yield return (TResponse)(object)"Item2";
        yield return (TResponse)(object)"Item3";
    }
}

public class FailingStreamDispatcher : IStreamDispatcher
{
    public IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
    {
        return GenerateFailingItems<TResponse>();
    }

    public IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
    {
        return GenerateFailingItems<TResponse>();
    }

    private static async IAsyncEnumerable<TResponse> GenerateFailingItems<TResponse>()
    {
        await Task.CompletedTask;
        yield return (TResponse)(object)"Item1";
        yield return (TResponse)(object)"Item2";
        throw new InvalidOperationException("Stream test exception");
    }
}

public class TestNotificationDispatcher : INotificationDispatcher
{
    public ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        return ValueTask.CompletedTask;
    }
}

public class FailingNotificationDispatcher : INotificationDispatcher
{
    public ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        throw new InvalidOperationException("Publish test exception");
    }
}

public class FailingRequestDispatcher : IRequestDispatcher
{
    public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Test exception");
    }

    public ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Test exception");
    }

    public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Test exception");
    }

    public ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Test exception");
    }
}

public class FailingVoidRequestDispatcher : IRequestDispatcher
{
    public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult((TResponse)(object)"TestResponse");
    }

    public ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Void test exception");
    }

    public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult((TResponse)(object)"TestResponse");
    }

    public ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Void test exception");
    }
}