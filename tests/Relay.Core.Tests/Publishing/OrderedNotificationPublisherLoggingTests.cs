using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Strategies;
using Relay.Core.Publishing.Attributes;
using Relay.Core.Publishing.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class OrderedNotificationPublisherLoggingTests
    {
        #region Test Models

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

        [NotificationHandlerOrder(2)]
        public class OrderedHandler2 : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"OrderedHandler2: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        #endregion

        #region Logging Tests

        [Fact]
        public async Task PublishAsync_WithLogger_LogsExecutionDetails()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<OrderedNotificationPublisher>>();
            var publisher = new OrderedNotificationPublisher(mockLogger.Object);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new BasicHandler()
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert - verify debug logs were called
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task PublishAsync_WithLogger_LogsHandlerExecutionOrder()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<OrderedNotificationPublisher>>();
            var publisher = new OrderedNotificationPublisher(mockLogger.Object);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new OrderedHandler1(),
                new OrderedHandler2()
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert - verify trace logs for handler execution
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Trace,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion
    }
}