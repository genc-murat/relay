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
    public class OrderedNotificationPublisherComplexTests
    {
        public record TestNotification(string Message) : INotification;

        // Static lock object for thread-safe logging
        private static readonly object _logLock = new();

        // Basic handler without attributes
        public class BasicHandler : INotificationHandler<TestNotification>
        {
            public static List<string> ExecutionLog { get; } = [];

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

        // Handlers with ExecuteAfter dependency
        public class DependencyBaseHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"DependencyBaseHandler: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        [ExecuteAfter(typeof(DependencyBaseHandler))]
        public class DependentHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"DependentHandler: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        // Handlers with group attributes
        [NotificationHandlerGroup("GroupB", 2)]
        public class GroupBHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"GroupBHandler: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        // Handler with multiple dependencies
        [ExecuteAfter(typeof(DependencyBaseHandler))]
        [ExecuteAfter(typeof(BasicHandler))]
        private class MultipleDependenciesHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"MultipleDependenciesHandler: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        [Fact]
        public async Task PublishAsync_WithComplexOrdering_ExecutesCorrectly()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var publisher = new OrderedNotificationPublisher();

            // Complex scenario:
            // - OrderedHandler1 (Order 1)
            // - DependentHandler (depends on DependencyBaseHandler)
            // - DependencyBaseHandler (no order)
            // - GroupBHandler (Group B, order 2)
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new OrderedHandler1(),       // Order 1
                new DependentHandler(),      // Depends on DependencyBaseHandler
                new DependencyBaseHandler(), // Order 0 (default)
                new GroupBHandler()          // Group B, order 2
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(4, BasicHandler.ExecutionLog.Count);

            // DependencyBaseHandler must execute before DependentHandler
            var baseIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("DependencyBaseHandler"));
            var dependentIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("DependentHandler"));
            Assert.True(baseIndex < dependentIndex);
        }

        [Fact]
        public async Task PublishAsync_WithSingleHandler_OptimizesExecution()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var publisher = new OrderedNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new BasicHandler()
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Single(BasicHandler.ExecutionLog);
            Assert.Equal("BasicHandler: test", BasicHandler.ExecutionLog[0]);
        }

        [Fact]
        public async Task PublishAsync_WithMultipleExecuteAfterAttributes_RespectsAllDependencies()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var publisher = new OrderedNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new MultipleDependenciesHandler(),
                new DependencyBaseHandler(),
                new BasicHandler()
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(3, BasicHandler.ExecutionLog.Count);

            // Both dependencies should execute before MultipleDependenciesHandler
            var multiDepIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("MultipleDependenciesHandler"));
            var baseIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("DependencyBaseHandler"));
            var basicIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("BasicHandler"));

            Assert.True(baseIndex < multiDepIndex);
            Assert.True(basicIndex < multiDepIndex);
        }
    }
}