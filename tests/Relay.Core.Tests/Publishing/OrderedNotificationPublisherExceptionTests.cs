using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Strategies;
using Relay.Core.Publishing.Attributes;
using Relay.Core.Publishing.Interfaces;
using Relay.Core.Publishing.Strategies;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class OrderedNotificationPublisherExceptionTests
    {
        public record TestNotification(string Message) : INotification;

        // Static lock object for thread-safe logging
        private static readonly object _logLock = new();

        // Basic handler without attributes
        public class BasicHandler : INotificationHandler<TestNotification>
        {
            public static List<string> ExecutionLog { get; } = new();

            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                lock (_logLock)
                {
                    ExecutionLog.Add($"BasicHandler: {notification.Message}");
                }
                return ValueTask.CompletedTask;
            }
        }

        // Handler with order attribute
        [NotificationHandlerOrder(1)]
        public class OrderedHandler1 : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"OrderedHandler1: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        // Handler that throws exception
        public class ThrowingHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"ThrowingHandler: {notification.Message}");
                throw new InvalidOperationException("Handler failed");
            }
        }

        // Handler with suppress exceptions
        [NotificationExecutionMode(NotificationExecutionMode.Default, SuppressExceptions = true)]
        public class SuppressExceptionHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"SuppressExceptionHandler: {notification.Message}");
                throw new InvalidOperationException("This should be suppressed");
            }
        }

        // Handlers with group attributes
        [NotificationHandlerGroup("GroupA", 1)]
        public class GroupAHandler1 : INotificationHandler<TestNotification>
        {
            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                lock (_logLock) BasicHandler.ExecutionLog.Add($"GroupAHandler1-Start: {notification.Message}");
                await Task.Delay(10, cancellationToken);
                lock (_logLock) BasicHandler.ExecutionLog.Add($"GroupAHandler1-End: {notification.Message}");
            }
        }

        [NotificationHandlerGroup("GroupB", 2)]
        private class ThrowingGroupHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"ThrowingGroupHandler: {notification.Message}");
                throw new InvalidOperationException("Handler failed");
            }
        }

        [NotificationHandlerGroup("GroupC", 3)]
        private class GroupCHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"GroupCHandler: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        [Fact]
        public async Task PublishAsync_WithException_ContinuesWhenConfigured()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var mockLogger = new Mock<ILogger<OrderedNotificationPublisher>>();
            var publisher = new OrderedNotificationPublisher(mockLogger.Object, continueOnException: true);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new BasicHandler(),
                new ThrowingHandler(),
                new OrderedHandler1()
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(3, BasicHandler.ExecutionLog.Count);
            Assert.Equal("BasicHandler: test", BasicHandler.ExecutionLog[0]);
            Assert.Equal("ThrowingHandler: test", BasicHandler.ExecutionLog[1]);
            Assert.Equal("OrderedHandler1: test", BasicHandler.ExecutionLog[2]);

            // Verify error was logged
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task PublishAsync_WithException_StopsWhenConfigured()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var mockLogger = new Mock<ILogger<OrderedNotificationPublisher>>();
            var publisher = new OrderedNotificationPublisher(mockLogger.Object, continueOnException: false);

            // Use different groups to ensure sequential execution
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new GroupAHandler1(),     // Group A, order 1
                new ThrowingGroupHandler(), // Group B, order 2 - will throw
                new GroupCHandler()       // Group C, order 3 - should not execute
            };
            var notification = new TestNotification("test");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                publisher.PublishAsync(notification, handlers, CancellationToken.None).AsTask());

            Assert.Equal("Handler failed", exception.Message);

            // GroupA should have executed, ThrowingGroupHandler should have thrown,
            // GroupC should not have executed
            Assert.Contains(BasicHandler.ExecutionLog, x => x.Contains("GroupAHandler1"));
            Assert.Contains(BasicHandler.ExecutionLog, x => x.Contains("ThrowingGroupHandler"));
            Assert.DoesNotContain(BasicHandler.ExecutionLog, x => x.Contains("GroupCHandler"));
        }

        [Fact]
        public async Task PublishAsync_WithSuppressExceptionHandler_ContinuesAndDoesNotThrow()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var mockLogger = new Mock<ILogger<OrderedNotificationPublisher>>();
            var publisher = new OrderedNotificationPublisher(mockLogger.Object, continueOnException: false);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new BasicHandler(),
                new SuppressExceptionHandler(),  // Exception should be suppressed
                new OrderedHandler1()             // Should still execute
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(3, BasicHandler.ExecutionLog.Count);
            Assert.Equal("OrderedHandler1: test", BasicHandler.ExecutionLog[2]);
        }
    }
}
