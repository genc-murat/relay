using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Base;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class TelemetryIntegrationTests
{
    [Fact]
    public async Task TelemetryDecorators_EndToEnd_RequestProcessing_RecordsCompleteTelemetry()
    {
        // Arrange
        var services = new ServiceCollection();
        var testTelemetryProvider = new TestTelemetryProvider();

        // Register core services
        services.AddSingleton<ITelemetryProvider>(testTelemetryProvider);
        services.AddSingleton<IRequestDispatcher, TestRequestDispatcher>();
        services.AddSingleton<INotificationDispatcher, TestNotificationDispatcher>();
        services.AddSingleton<IStreamDispatcher, TestStreamDispatcher>();

        // Register telemetry decorators
        services.Decorate<IRequestDispatcher, TelemetryRequestDispatcher>();
        services.Decorate<INotificationDispatcher, TelemetryNotificationDispatcher>();
        services.Decorate<IStreamDispatcher, TelemetryStreamDispatcher>();

        var serviceProvider = services.BuildServiceProvider();

        var requestDispatcher = serviceProvider.GetRequiredService<IRequestDispatcher>();
        var notificationDispatcher = serviceProvider.GetRequiredService<INotificationDispatcher>();
        var streamDispatcher = serviceProvider.GetRequiredService<IStreamDispatcher>();

        // Act - Test request dispatching
        var request = new TestRequest();
        var response = await requestDispatcher.DispatchAsync<string>(request, CancellationToken.None);

        // Assert request telemetry
        Assert.Single(testTelemetryProvider.HandlerExecutions.Where(x => x.RequestType == typeof(TestRequest)));
        var execution = testTelemetryProvider.HandlerExecutions.First(x => x.RequestType == typeof(TestRequest));
        Assert.True(execution.Success);
        Assert.Equal("response", response);

        // Act - Test notification dispatching
        var notification = new TestNotification();
        await notificationDispatcher.DispatchAsync(notification, CancellationToken.None);

        // Assert notification telemetry
        Assert.Single(testTelemetryProvider.NotificationPublishes.Where(x => x.NotificationType == typeof(TestNotification)));
        var publish = testTelemetryProvider.NotificationPublishes.First(x => x.NotificationType == typeof(TestNotification));
        Assert.True(publish.Success);

        // Act - Test stream dispatching
        var streamRequest = new TestStreamRequest();
        var streamResults = new List<string>();
        await foreach (var item in streamDispatcher.DispatchAsync<string>(streamRequest, CancellationToken.None))
        {
            streamResults.Add(item);
        }

        // Assert stream telemetry
        Assert.Single(testTelemetryProvider.StreamingOperations.Where(x => x.RequestType == typeof(TestStreamRequest)));
        var operation = testTelemetryProvider.StreamingOperations.First(x => x.RequestType == typeof(TestStreamRequest));
        Assert.True(operation.Success);
        Assert.Equal(2, operation.ItemCount);
        Assert.Equal(new[] { "item1", "item2" }, streamResults);
    }

    [Fact]
    public async Task TelemetryDecorators_WithCorrelationId_PropagatesCorrelationId()
    {
        // Arrange
        var services = new ServiceCollection();
        var testTelemetryProvider = new TestTelemetryProvider();
        var correlationId = "integration-test-correlation";

        testTelemetryProvider.SetCorrelationId(correlationId);

        services.AddSingleton<ITelemetryProvider>(testTelemetryProvider);
        services.AddSingleton<IRequestDispatcher, TestRequestDispatcher>();
        services.Decorate<IRequestDispatcher, TelemetryRequestDispatcher>();

        var serviceProvider = services.BuildServiceProvider();
        var requestDispatcher = serviceProvider.GetRequiredService<IRequestDispatcher>();

        // Act
        var request = new TestRequest();
        await requestDispatcher.DispatchAsync<string>(request, CancellationToken.None);

        // Assert
        Assert.Single(testTelemetryProvider.Activities);
        var activity = testTelemetryProvider.Activities[0];
        Assert.Equal(correlationId, activity.Tags["relay.correlation_id"]);
    }

    // Test implementations
    private class TestRequestDispatcher : IRequestDispatcher
    {
        public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult((TResponse)(object)"response");
        }

        public ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult((TResponse)(object)"response");
        }

        public ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }

    private class TestNotificationDispatcher : INotificationDispatcher
    {
        public ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
            where TNotification : INotification
        {
            return ValueTask.CompletedTask;
        }
    }

    private class TestStreamDispatcher : IStreamDispatcher
    {
        public async IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
        {
            yield return (TResponse)(object)"item1";
            yield return (TResponse)(object)"item2";
        }

        public async IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
        {
            yield return (TResponse)(object)"item1";
            yield return (TResponse)(object)"item2";
        }
    }

    private class TestRequest : IRequest<string>
    {
    }

    private class TestNotification : INotification
    {
    }

    private class TestStreamRequest : IStreamRequest<string>
    {
    }
}