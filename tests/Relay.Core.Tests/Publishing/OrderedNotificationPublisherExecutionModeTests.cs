using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Strategies;
using Relay.Core.Publishing.Attributes;
using Relay.Core.Publishing.Interfaces;
using Relay.Core.Publishing.Strategies;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class OrderedNotificationPublisherExecutionModeTests
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

        // Handler with execution mode
        [NotificationExecutionMode(NotificationExecutionMode.Sequential)]
        public class SequentialHandler : INotificationHandler<TestNotification>
        {
            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"SequentialHandler-Start: {notification.Message}");
                await Task.Delay(20, cancellationToken);
                BasicHandler.ExecutionLog.Add($"SequentialHandler-End: {notification.Message}");
            }
        }

        [NotificationExecutionMode(NotificationExecutionMode.Parallel, AllowParallelExecution = true)]
        public class ParallelHandler1 : INotificationHandler<TestNotification>
        {
            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                lock (_logLock) BasicHandler.ExecutionLog.Add($"ParallelHandler1-Start: {notification.Message}");
                await Task.Delay(10, cancellationToken);
                lock (_logLock) BasicHandler.ExecutionLog.Add($"ParallelHandler1-End: {notification.Message}");
            }
        }

        [NotificationExecutionMode(NotificationExecutionMode.Parallel, AllowParallelExecution = true)]
        public class ParallelHandler2 : INotificationHandler<TestNotification>
        {
            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                lock (_logLock) BasicHandler.ExecutionLog.Add($"ParallelHandler2-Start: {notification.Message}");
                await Task.Delay(10, cancellationToken);
                lock (_logLock) BasicHandler.ExecutionLog.Add($"ParallelHandler2-End: {notification.Message}");
            }
        }

        [Fact]
        public async Task PublishAsync_WithSequentialHandler_ExecutesSequentially()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var publisher = new OrderedNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new SequentialHandler(),
                new BasicHandler()
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(3, BasicHandler.ExecutionLog.Count);
            // Sequential handler should fully complete before basic handler
            Assert.Equal("SequentialHandler-Start: test", BasicHandler.ExecutionLog[0]);
            Assert.Equal("SequentialHandler-End: test", BasicHandler.ExecutionLog[1]);
            Assert.Equal("BasicHandler: test", BasicHandler.ExecutionLog[2]);
        }

        [Fact]
        public async Task PublishAsync_WithParallelHandlers_ExecutesAllHandlers()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var publisher = new OrderedNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new ParallelHandler1(),
                new ParallelHandler2()
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            // PublishAsync should await all handlers, so all should be complete
            Assert.True(BasicHandler.ExecutionLog.Count >= 4);

            // Both handlers should execute fully
            Assert.Contains(BasicHandler.ExecutionLog, x => x.Contains("ParallelHandler1-Start"));
            Assert.Contains(BasicHandler.ExecutionLog, x => x.Contains("ParallelHandler1-End"));
            Assert.Contains(BasicHandler.ExecutionLog, x => x.Contains("ParallelHandler2-Start"));
            Assert.Contains(BasicHandler.ExecutionLog, x => x.Contains("ParallelHandler2-End"));
        }

        [Fact]
        public async Task PublishAsync_WithMixedExecutionModes_SequentialExecutesFirst()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var publisher = new OrderedNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new ParallelHandler1(),   // Should execute after sequential
                new SequentialHandler()   // Should execute first
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(4, BasicHandler.ExecutionLog.Count);

            // Sequential handler should complete before parallel handler starts
            var sequentialEndIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("SequentialHandler-End"));
            var parallelStartIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("ParallelHandler1-Start"));

            Assert.True(sequentialEndIndex < parallelStartIndex);
        }
    }
}