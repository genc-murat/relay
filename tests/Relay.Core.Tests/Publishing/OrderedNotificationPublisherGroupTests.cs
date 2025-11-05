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
    public class OrderedNotificationPublisherGroupTests
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

        [NotificationHandlerGroup("GroupA", 1)]
        public class GroupAHandler2 : INotificationHandler<TestNotification>
        {
            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                lock (_logLock) BasicHandler.ExecutionLog.Add($"GroupAHandler2-Start: {notification.Message}");
                await Task.Delay(10, cancellationToken);
                lock (_logLock) BasicHandler.ExecutionLog.Add($"GroupAHandler2-End: {notification.Message}");
            }
        }

        [NotificationHandlerGroup("GroupB", 2)]
        public class GroupBHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"GroupBHandler: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        [Fact]
        public async Task PublishAsync_WithGroupedHandlers_ExecutesGroupsSequentially()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var publisher = new OrderedNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new GroupBHandler(),    // Group B, order 2
                new GroupAHandler1(),   // Group A, order 1
                new GroupAHandler2()    // Group A, order 1
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(5, BasicHandler.ExecutionLog.Count);

            // Group A handlers should both complete before Group B starts
            var groupAEndIndex = BasicHandler.ExecutionLog.FindLastIndex(x => x.Contains("GroupAHandler"));
            var groupBStartIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("GroupBHandler"));

            Assert.True(groupAEndIndex < groupBStartIndex);
        }

        [Fact]
        public async Task PublishAsync_WithHandlersInSameGroup_ExecutesAllHandlers()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var publisher = new OrderedNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new GroupAHandler1(),   // Group A
                new GroupAHandler2()    // Group A
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            // PublishAsync should await all handlers, so all should be complete
            Assert.True(BasicHandler.ExecutionLog.Count >= 4);
            Assert.Contains(BasicHandler.ExecutionLog, x => x.Contains("GroupAHandler1-Start"));
            Assert.Contains(BasicHandler.ExecutionLog, x => x.Contains("GroupAHandler1-End"));
            Assert.Contains(BasicHandler.ExecutionLog, x => x.Contains("GroupAHandler2-Start"));
            Assert.Contains(BasicHandler.ExecutionLog, x => x.Contains("GroupAHandler2-End"));
        }
    }
}
