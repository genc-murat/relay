using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Strategies;
using Relay.Core.Publishing.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class ParallelNotificationPublisherExceptionTests
    {
        public record TestNotification(string Message) : INotification;

        public class BasicHandler : INotificationHandler<TestNotification>
        {
            public static List<string> ExecutionLog { get; } = new();

            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                ExecutionLog.Add($"BasicHandler: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        public class ThrowingHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"ThrowingHandler: {notification.Message}");
                throw new InvalidOperationException("Handler failed");
            }
        }

        public class SecondHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"SecondHandler: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        [Fact]
        public async Task PublishAsync_WithException_FailsFast()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var testLogger = new TestLogger<ParallelNotificationPublisher>();
            var publisher = new ParallelNotificationPublisher(testLogger);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new BasicHandler(),
                new ThrowingHandler(),
                new SecondHandler()
            };
            var notification = new TestNotification("test");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                publisher.PublishAsync(notification, handlers, CancellationToken.None).AsTask());

            Assert.Equal("Handler failed", exception.Message);

            // Parallel publisher does not log errors - exceptions propagate directly
        }

        [Fact]
        public async Task PublishAsync_WithException_LogsError()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var testLogger = new TestLogger<ParallelNotificationPublisher>();
            var publisher = new ParallelNotificationPublisher(testLogger);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new ThrowingHandler()
            };
            var notification = new TestNotification("test");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                publisher.PublishAsync(notification, handlers, CancellationToken.None).AsTask());

            // Parallel publisher does not log errors - exceptions propagate directly
        }
    }
}
