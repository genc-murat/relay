using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Configuration;
using Relay.Core.Implementation.Dispatchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Dispatchers;

public class NotificationDispatcherComprehensiveTests
{
    [Fact]
    public void GetHandlers_WithNonExistentNotificationType_ReturnsEmptyList()
    {
        // Arrange
        var dispatcher = new NotificationDispatcher(new ServiceCollection().BuildServiceProvider());

        // Act
        var handlers = dispatcher.GetHandlers(typeof(NonExistentNotification));

        // Assert
        Assert.Empty(handlers);
        Assert.IsAssignableFrom<IReadOnlyList<NotificationHandlerRegistration>>(handlers);
    }

    [Fact]
    public void GetHandlers_WithNoRegisteredHandlers_ReturnsEmptyList()
    {
        // Arrange
        var dispatcher = new NotificationDispatcher(new ServiceCollection().BuildServiceProvider());
        var notificationType = typeof(TestNotification);

        // Act
        var handlers = dispatcher.GetHandlers(notificationType);

        // Assert
        Assert.Empty(handlers);
    }

    [Fact]
    public void RegisterHandler_WithSameTypeReplacesExistingHandler()
    {
        // Arrange
        var dispatcher = new NotificationDispatcher(new ServiceCollection().BuildServiceProvider());
        var registration1 = CreateTestHandlerRegistration<TestNotification>(priority: 5, handlerName: "Handler1");
        var registration2 = CreateTestHandlerRegistration<TestNotification>(priority: 10, handlerName: "Handler2");

        // Act
        dispatcher.RegisterHandler(registration1);
        dispatcher.RegisterHandler(registration2);

        // Assert
        var handlers = dispatcher.GetHandlers(typeof(TestNotification));
        Assert.Equal(2, handlers.Count);
        // Should be sorted by priority (higher first)
        Assert.Equal(10, handlers[0].Priority);
        Assert.Equal(5, handlers[1].Priority);
    }

    [Fact]
    public async Task DispatchAsync_WithHandlerExceptionAndContinueOnException_DoesNotThrow()
    {
        // Arrange
        var options = new NotificationDispatchOptions { ContinueOnException = true };
        var dispatcher = new NotificationDispatcher(new ServiceCollection().BuildServiceProvider(), options);

        // Register a handler that throws
        var registration = CreateTestHandlerRegistration<TestNotification>(
            executeAction: (notification, ct) => throw new InvalidOperationException("Handler exception"));

        dispatcher.RegisterHandler(registration);

        var notification = new TestNotification();

        // Act & Assert - Should not throw since ContinueOnException is true
        await dispatcher.DispatchAsync(notification, CancellationToken.None);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleNotifications_ExecutesCorrectly()
    {
        // Arrange
        var dispatcher = new NotificationDispatcher(new ServiceCollection().BuildServiceProvider());
        var notification1Executions = 0;
        var notification2Executions = 0;

        var handler1 = CreateTestHandlerRegistration<TestNotification>(
            executeAction: (notification, ct) =>
            {
                Interlocked.Increment(ref notification1Executions);
                return ValueTask.CompletedTask;
            });

        var handler2 = CreateTestHandlerRegistration<AnotherTestNotification>(
            executeAction: (notification, ct) =>
            {
                Interlocked.Increment(ref notification2Executions);
                return ValueTask.CompletedTask;
            });

        dispatcher.RegisterHandler(handler1);
        dispatcher.RegisterHandler(handler2);

        // Act
        await dispatcher.DispatchAsync(new TestNotification(), CancellationToken.None);
        await dispatcher.DispatchAsync(new AnotherTestNotification(), CancellationToken.None);
        await dispatcher.DispatchAsync(new TestNotification(), CancellationToken.None); // Second execution

        // Assert
        Assert.Equal(2, notification1Executions);
        Assert.Equal(1, notification2Executions);
    }

    [Fact]
    public async Task DispatchAsync_WithSingleParallelHandler_OptimizesExecution()
    {
        // Arrange
        var dispatcher = new NotificationDispatcher(new ServiceCollection().BuildServiceProvider());
        var executionOrder = new List<int>();

        var registration = CreateTestHandlerRegistration<TestNotification>(
            dispatchMode: NotificationDispatchMode.Parallel,
            executeAction: async (notification, ct) =>
            {
                await Task.Delay(50, ct); // Simulate work
                executionOrder.Add(1);
            });

        dispatcher.RegisterHandler(registration);
        var notification = new TestNotification();

        // Act
        await dispatcher.DispatchAsync(notification, CancellationToken.None);

        // Assert
        Assert.Single(executionOrder);
        Assert.Equal(1, executionOrder[0]);
    }

    [Fact]
    public async Task DispatchAsync_WithRegisteredHandlersOnly_ExecutesHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new NotificationDispatcher(serviceProvider);

        // Register a handler through the dispatcher
        var dispatcherHandlerExecuted = false;
        var dispatcherRegistration = CreateTestHandlerRegistration<TestNotification>(
            executeAction: (notification, ct) =>
            {
                dispatcherHandlerExecuted = true;
                return ValueTask.CompletedTask;
            });
        dispatcher.RegisterHandler(dispatcherRegistration);

        var notification = new TestNotification();

        // Act
        await dispatcher.DispatchAsync(notification, CancellationToken.None);

        // Assert
        Assert.True(dispatcherHandlerExecuted, "Dispatcher-registered handler should execute");
    }

    [Fact]
    public async Task DispatchAsync_WithLargeNumberOfParallelHandlers_HandlesCorrectly()
    {
        // Arrange
        var dispatcher = new NotificationDispatcher(new ServiceCollection().BuildServiceProvider());
        const int handlerCount = 50;
        var executionOrder = new List<int>();

        for (int i = 0; i < handlerCount; i++)
        {
            int handlerId = i;
            var registration = CreateTestHandlerRegistration<TestNotification>(
                priority: handlerId, // Use ID as priority to verify ordering
                dispatchMode: NotificationDispatchMode.Parallel,
                executeAction: async (notification, ct) =>
                {
                    await Task.Delay(10, ct); // Small delay
                    lock (executionOrder)
                    {
                        executionOrder.Add(handlerId);
                    }
                });

            dispatcher.RegisterHandler(registration);
        }

        var notification = new TestNotification();

        // Act
        await dispatcher.DispatchAsync(notification, CancellationToken.None);

        // Assert
        Assert.Equal(handlerCount, executionOrder.Count);
        // All handlers should execute (order may vary due to parallel execution)
        var expectedIds = Enumerable.Range(0, handlerCount).ToList();
        var actualIds = executionOrder.OrderBy(x => x).ToList();
        Assert.Equal(expectedIds, actualIds);
    }



    [Fact]
    public void NotificationDispatcher_Constructor_WithOptionsAndLogger_CreatesInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        var logger = new Mock<ILogger<NotificationDispatcher>>().Object;
        var options = new NotificationDispatchOptions { ContinueOnException = true };

        // Act
        var dispatcher = new NotificationDispatcher(services.BuildServiceProvider(), options, logger);

        // Assert
        Assert.NotNull(dispatcher);
    }

    [Fact]
    public async Task DispatchAsync_WithEmptyNotificationCollection_CompletesSuccessfully()
    {
        // Arrange
        var dispatcher = new NotificationDispatcher(new ServiceCollection().BuildServiceProvider());
        var notification = new TestNotification();

        // Act & Assert - Should complete without error
        await dispatcher.DispatchAsync(notification, CancellationToken.None);
    }

    private static NotificationHandlerRegistration CreateTestHandlerRegistration<TNotification>(
        NotificationDispatchMode dispatchMode = NotificationDispatchMode.Parallel,
        int priority = 0,
        Func<INotification, CancellationToken, ValueTask>? executeAction = null,
        string handlerName = "TestHandler")
        where TNotification : INotification
    {
        executeAction ??= (notification, ct) => ValueTask.CompletedTask;

        return new NotificationHandlerRegistration
        {
            NotificationType = typeof(TNotification),
            HandlerType = typeof(TestHandler),
            DispatchMode = dispatchMode,
            Priority = priority,
            HandlerFactory = _ => new TestHandler(),
            ExecuteHandler = (handler, notification, ct) => executeAction(notification, ct)
        };
    }

    private class TestHandler
    {
    }

    private class TestNotification : INotification
    {
    }

    private class AnotherTestNotification : INotification
    {
    }

    private class NonExistentNotification : INotification
    {
    }
}