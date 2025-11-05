using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Strategies;
using Relay.Core.Publishing.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class ParallelWhenAllNotificationPublisherExecutionModeTests
    {
        public record TestNotification(string Message) : INotification;

        // Use a lock for thread-safe logging
        private static readonly object _logLock = new();

        public class Handler1 : INotificationHandler<TestNotification>
        {
            public static List<string> ExecutionLog { get; } = new();

            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                lock (_logLock) ExecutionLog.Add($"Handler1-Start: {notification.Message}");
                await Task.Delay(20, cancellationToken);
                lock (_logLock) ExecutionLog.Add($"Handler1-End: {notification.Message}");
            }
        }

        public class Handler2 : INotificationHandler<TestNotification>
        {
            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                lock (_logLock) Handler1.ExecutionLog.Add($"Handler2-Start: {notification.Message}");
                await Task.Delay(10, cancellationToken);
                lock (_logLock) Handler1.ExecutionLog.Add($"Handler2-End: {notification.Message}");
            }
        }

        [Fact]
        public async Task PublishAsync_ExecutesHandlersInParallel()
        {
            // Arrange
            Handler1.ExecutionLog.Clear();
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new Handler1(),
                new Handler2()
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            // All handlers should complete
            Assert.Equal(4, Handler1.ExecutionLog.Count);

            // Both handlers should have started before either finished (parallel execution)
            Assert.Contains(Handler1.ExecutionLog, x => x.Contains("Handler1-Start"));
            Assert.Contains(Handler1.ExecutionLog, x => x.Contains("Handler2-Start"));

            // Verify parallel execution by checking that both handlers started before the first one finished
            var handler1StartIndex = Handler1.ExecutionLog.FindIndex(x => x.Contains("Handler1-Start"));
            var handler2StartIndex = Handler1.ExecutionLog.FindIndex(x => x.Contains("Handler2-Start"));
            var firstEndIndex = Math.Min(
                Handler1.ExecutionLog.FindIndex(x => x.Contains("Handler1-End")),
                Handler1.ExecutionLog.FindIndex(x => x.Contains("Handler2-End"))
            );

            // Both handlers should have started before the first handler finished
            Assert.True(handler1StartIndex < firstEndIndex, "Handler1 should start before any handler finishes");
            Assert.True(handler2StartIndex < firstEndIndex, "Handler2 should start before any handler finishes");

            // Verify both handlers completed
            Assert.Contains(Handler1.ExecutionLog, x => x.Contains("Handler1-End"));
            Assert.Contains(Handler1.ExecutionLog, x => x.Contains("Handler2-End"));
        }

        [Fact]
        public async Task PublishAsync_WithSingleHandler_ExecutesCorrectly()
        {
            // Arrange
            Handler1.ExecutionLog.Clear();
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new Handler1()
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(2, Handler1.ExecutionLog.Count);
            Assert.Equal("Handler1-Start: test", Handler1.ExecutionLog[0]);
            Assert.Equal("Handler1-End: test", Handler1.ExecutionLog[1]);
        }

        [Fact]
        public async Task PublishAsync_WithContinueOnExceptionFalse_ExecutesInParallel()
        {
            // Arrange
            Handler1.ExecutionLog.Clear();
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: false);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new Handler1(),
                new Handler2()
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            // All handlers should complete
            Assert.Equal(4, Handler1.ExecutionLog.Count);

            // Both handlers should have started
            Assert.Contains(Handler1.ExecutionLog, x => x.Contains("Handler1-Start"));
            Assert.Contains(Handler1.ExecutionLog, x => x.Contains("Handler2-Start"));
        }
    }
}
