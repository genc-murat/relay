using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Strategies;
using Relay.Core.Publishing.Attributes;
using Relay.Core.Publishing.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class OrderedNotificationPublisherOrderTests
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

        [NotificationHandlerOrder(2)]
        public class OrderedHandler2 : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"OrderedHandler2: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        [NotificationHandlerOrder(3)]
        public class OrderedHandler3 : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"OrderedHandler3: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        [Fact]
        public async Task PublishAsync_WithOrderedHandlers_ExecutesInCorrectOrder()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var publisher = new OrderedNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new OrderedHandler3(), // Order 3
                new OrderedHandler1(), // Order 1
                new OrderedHandler2()  // Order 2
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(3, BasicHandler.ExecutionLog.Count);
            Assert.Equal("OrderedHandler1: test", BasicHandler.ExecutionLog[0]);
            Assert.Equal("OrderedHandler2: test", BasicHandler.ExecutionLog[1]);
            Assert.Equal("OrderedHandler3: test", BasicHandler.ExecutionLog[2]);
        }

        [Fact]
        public async Task PublishAsync_WithMixedOrderAndNoOrder_OrderedHandlersExecuteFirst()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var publisher = new OrderedNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new BasicHandler(),      // No order (default 0)
                new OrderedHandler1(),   // Order 1
                new OrderedHandler2()    // Order 2
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(3, BasicHandler.ExecutionLog.Count);
            // Handler with no order (0) should execute first, then ordered handlers
            Assert.Equal("BasicHandler: test", BasicHandler.ExecutionLog[0]);
            Assert.Equal("OrderedHandler1: test", BasicHandler.ExecutionLog[1]);
            Assert.Equal("OrderedHandler2: test", BasicHandler.ExecutionLog[2]);
        }
    }
}