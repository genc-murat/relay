using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests
{
    public class NotificationDispatcherTests
    {
        private readonly ServiceCollection _services;
        private readonly IServiceProvider _serviceProvider;
        private readonly Mock<ILogger<NotificationDispatcher>> _mockLogger;

        public NotificationDispatcherTests()
        {
            _services = new ServiceCollection();
            _serviceProvider = _services.BuildServiceProvider();
            _mockLogger = new Mock<ILogger<NotificationDispatcher>>();
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new NotificationDispatcher(null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var dispatcher = new NotificationDispatcher(_serviceProvider);

            // Assert
            Assert.NotNull(dispatcher);
        }

        [Fact]
        public void RegisterHandler_WithNullRegistration_ThrowsArgumentNullException()
        {
            // Arrange
            var dispatcher = new NotificationDispatcher(_serviceProvider);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => dispatcher.RegisterHandler(null!));
        }

        [Fact]
        public void RegisterHandler_WithValidRegistration_AddsHandler()
        {
            // Arrange
            var dispatcher = new NotificationDispatcher(_serviceProvider);
            var registration = CreateTestHandlerRegistration<TestNotification>();

            // Act
            dispatcher.RegisterHandler(registration);

            // Assert
            var handlers = dispatcher.GetHandlers(typeof(TestNotification));
            Assert.Single(handlers);
            Assert.Equal(registration.HandlerType, handlers[0].HandlerType);
        }

        [Fact]
        public void RegisterHandler_WithMultipleHandlers_SortsByPriority()
        {
            // Arrange
            var dispatcher = new NotificationDispatcher(_serviceProvider);
            var lowPriorityRegistration = CreateTestHandlerRegistration<TestNotification>(priority: 1);
            var highPriorityRegistration = CreateTestHandlerRegistration<TestNotification>(priority: 10);
            var mediumPriorityRegistration = CreateTestHandlerRegistration<TestNotification>(priority: 5);

            // Act
            dispatcher.RegisterHandler(lowPriorityRegistration);
            dispatcher.RegisterHandler(highPriorityRegistration);
            dispatcher.RegisterHandler(mediumPriorityRegistration);

            // Assert
            var handlers = dispatcher.GetHandlers(typeof(TestNotification));
            Assert.Equal(3, handlers.Count);
            Assert.Equal(10, handlers[0].Priority); // Highest priority first
            Assert.Equal(5, handlers[1].Priority);
            Assert.Equal(1, handlers[2].Priority);
        }

        [Fact]
        public async Task DispatchAsync_WithNoHandlers_CompletesSuccessfully()
        {
            // Arrange
            var dispatcher = new NotificationDispatcher(_serviceProvider, logger: _mockLogger.Object);
            var notification = new TestNotification();

            // Act & Assert
            await dispatcher.DispatchAsync(notification, CancellationToken.None);

            // Verify debug log was called
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No handlers registered")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_WithNullNotification_ThrowsArgumentNullException()
        {
            // Arrange
            var dispatcher = new NotificationDispatcher(_serviceProvider);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                dispatcher.DispatchAsync<TestNotification>(null!, CancellationToken.None).AsTask());
        }

        [Fact]
        public async Task DispatchAsync_WithParallelHandlers_ExecutesInParallel()
        {
            // Arrange
            var dispatcher = new NotificationDispatcher(_serviceProvider);
            var executionOrder = new List<int>();
            var executionLock = new object();

            var registration1 = CreateTestHandlerRegistration<TestNotification>(
                dispatchMode: NotificationDispatchMode.Parallel,
                executeAction: async (notification, ct) =>
                {
                    await Task.Delay(50, ct); // Simulate work
                    lock (executionLock)
                    {
                        executionOrder.Add(1);
                    }
                });

            var registration2 = CreateTestHandlerRegistration<TestNotification>(
                dispatchMode: NotificationDispatchMode.Parallel,
                executeAction: async (notification, ct) =>
                {
                    await Task.Delay(25, ct); // Simulate less work
                    lock (executionLock)
                    {
                        executionOrder.Add(2);
                    }
                });

            dispatcher.RegisterHandler(registration1);
            dispatcher.RegisterHandler(registration2);

            var notification = new TestNotification();

            // Act
            await dispatcher.DispatchAsync(notification, CancellationToken.None);

            // Assert
            Assert.Equal(2, executionOrder.Count);
            // In parallel execution, both handlers should complete, but order may vary
            // Just verify both handlers executed
            Assert.Contains(1, executionOrder);
            Assert.Contains(2, executionOrder);
        }

        [Fact]
        public async Task DispatchAsync_WithSequentialHandlers_ExecutesInOrder()
        {
            // Arrange
            var dispatcher = new NotificationDispatcher(_serviceProvider);
            var executionOrder = new List<int>();

            var registration1 = CreateTestHandlerRegistration<TestNotification>(
                dispatchMode: NotificationDispatchMode.Sequential,
                priority: 10,
                executeAction: async (notification, ct) =>
                {
                    await Task.Delay(10, ct);
                    executionOrder.Add(1);
                });

            var registration2 = CreateTestHandlerRegistration<TestNotification>(
                dispatchMode: NotificationDispatchMode.Sequential,
                priority: 5,
                executeAction: async (notification, ct) =>
                {
                    await Task.Delay(10, ct);
                    executionOrder.Add(2);
                });

            dispatcher.RegisterHandler(registration1);
            dispatcher.RegisterHandler(registration2);

            var notification = new TestNotification();

            // Act
            await dispatcher.DispatchAsync(notification, CancellationToken.None);

            // Assert
            Assert.Equal(2, executionOrder.Count);
            // Should execute in priority order (higher priority first)
            Assert.Equal(1, executionOrder[0]);
            Assert.Equal(2, executionOrder[1]);
        }

        [Fact]
        public async Task DispatchAsync_WithMixedDispatchModes_ExecutesSequentialFirst()
        {
            // Arrange
            var dispatcher = new NotificationDispatcher(_serviceProvider);
            var executionOrder = new List<string>();
            var executionLock = new object();

            var sequentialRegistration = CreateTestHandlerRegistration<TestNotification>(
                dispatchMode: NotificationDispatchMode.Sequential,
                executeAction: async (notification, ct) =>
                {
                    await Task.Delay(50, ct);
                    lock (executionLock)
                    {
                        executionOrder.Add("sequential");
                    }
                });

            var parallelRegistration = CreateTestHandlerRegistration<TestNotification>(
                dispatchMode: NotificationDispatchMode.Parallel,
                executeAction: async (notification, ct) =>
                {
                    await Task.Delay(10, ct);
                    lock (executionLock)
                    {
                        executionOrder.Add("parallel");
                    }
                });

            dispatcher.RegisterHandler(sequentialRegistration);
            dispatcher.RegisterHandler(parallelRegistration);

            var notification = new TestNotification();

            // Act
            await dispatcher.DispatchAsync(notification, CancellationToken.None);

            // Assert
            Assert.Equal(2, executionOrder.Count);
            // Sequential should complete first even though it takes longer
            Assert.Equal("sequential", executionOrder[0]);
            Assert.Equal("parallel", executionOrder[1]);
        }

        [Fact]
        public async Task DispatchAsync_WithHandlerException_ContinuesWithOtherHandlers()
        {
            // Arrange
            var options = new NotificationDispatchOptions { ContinueOnException = true };
            var dispatcher = new NotificationDispatcher(_serviceProvider, options, _mockLogger.Object);
            var successfulHandlerExecuted = false;

            var failingRegistration = CreateTestHandlerRegistration<TestNotification>(
                executeAction: (notification, ct) => throw new InvalidOperationException("Test exception"));

            var successfulRegistration = CreateTestHandlerRegistration<TestNotification>(
                executeAction: (notification, ct) =>
                {
                    successfulHandlerExecuted = true;
                    return ValueTask.CompletedTask;
                });

            dispatcher.RegisterHandler(failingRegistration);
            dispatcher.RegisterHandler(successfulRegistration);

            var notification = new TestNotification();

            // Act
            await dispatcher.DispatchAsync(notification, CancellationToken.None);

            // Assert
            Assert.True(successfulHandlerExecuted);

            // Verify error was logged
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed while processing")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_WithHandlerExceptionAndStopOnException_ThrowsException()
        {
            // Arrange
            var options = new NotificationDispatchOptions { ContinueOnException = false };
            var dispatcher = new NotificationDispatcher(_serviceProvider, options, _mockLogger.Object);

            var failingRegistration = CreateTestHandlerRegistration<TestNotification>(
                executeAction: (notification, ct) => throw new InvalidOperationException("Test exception"));

            dispatcher.RegisterHandler(failingRegistration);

            var notification = new TestNotification();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                dispatcher.DispatchAsync(notification, CancellationToken.None).AsTask());

            Assert.Equal("Test exception", exception.Message);
        }

        [Fact]
        public async Task DispatchAsync_WithCancellation_PropagatesCancellationToken()
        {
            // Arrange
            var dispatcher = new NotificationDispatcher(_serviceProvider);
            var cancellationTokenReceived = false;

            var registration = CreateTestHandlerRegistration<TestNotification>(
                executeAction: (notification, ct) =>
                {
                    cancellationTokenReceived = ct.IsCancellationRequested;
                    return ValueTask.CompletedTask;
                });

            dispatcher.RegisterHandler(registration);

            var notification = new TestNotification();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await dispatcher.DispatchAsync(notification, cts.Token);

            // Assert
            Assert.True(cancellationTokenReceived);
        }

        [Fact]
        public void GetRegisteredNotificationTypes_WithNoRegistrations_ReturnsEmptyCollection()
        {
            // Arrange
            var dispatcher = new NotificationDispatcher(_serviceProvider);

            // Act
            var types = dispatcher.GetRegisteredNotificationTypes();

            // Assert
            Assert.Empty(types);
        }

        [Fact]
        public void GetRegisteredNotificationTypes_WithRegistrations_ReturnsCorrectTypes()
        {
            // Arrange
            var dispatcher = new NotificationDispatcher(_serviceProvider);
            var registration1 = CreateTestHandlerRegistration<TestNotification>();
            var registration2 = CreateTestHandlerRegistration<AnotherTestNotification>();

            dispatcher.RegisterHandler(registration1);
            dispatcher.RegisterHandler(registration2);

            // Act
            var types = dispatcher.GetRegisteredNotificationTypes();

            // Assert
            Assert.Equal(2, types.Count);
            Assert.Contains(typeof(TestNotification), types);
            Assert.Contains(typeof(AnotherTestNotification), types);
        }

        private static NotificationHandlerRegistration CreateTestHandlerRegistration<TNotification>(
            NotificationDispatchMode dispatchMode = NotificationDispatchMode.Parallel,
            int priority = 0,
            Func<INotification, CancellationToken, ValueTask>? executeAction = null)
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
    }
}