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
    public class ParallelWhenAllNotificationPublisherExceptionTests
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
        public async Task PublishAsync_WithContinueOnException_ContinuesAndAggregatesExceptions()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var testLogger = new TestLogger<ParallelWhenAllNotificationPublisher>();
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true, testLogger);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new BasicHandler(),
                new ThrowingHandler(),
                new SecondHandler()
            };
            var notification = new TestNotification("test");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AggregateException>(() =>
                publisher.PublishAsync(notification, handlers, CancellationToken.None).AsTask());

            // All handlers should have executed
            Assert.Contains("BasicHandler: test", BasicHandler.ExecutionLog);
            Assert.Contains("ThrowingHandler: test", BasicHandler.ExecutionLog);
            Assert.Contains("SecondHandler: test", BasicHandler.ExecutionLog);

            Assert.Single(exception.InnerExceptions);
            Assert.IsType<InvalidOperationException>(exception.InnerExceptions[0]);

            // Verify error was logged
            Assert.Contains(testLogger.LoggedMessages, msg =>
                msg.LogLevel == LogLevel.Error &&
                msg.Message.Contains("failed"));
        }

        [Fact]
        public async Task PublishAsync_WithContinueOnExceptionFalse_FailsFast()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var testLogger = new TestLogger<ParallelWhenAllNotificationPublisher>();
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: false, testLogger);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new BasicHandler(),
                new ThrowingHandler(),
                new SecondHandler()
            };
            var notification = new TestNotification("test");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                publisher.PublishAsync(notification, handlers, CancellationToken.None).AsTask());

            // When continueOnException is false, exceptions propagate directly without error logging
        }

        [Fact]
        public async Task PublishAsync_WithMultipleExceptions_AggregatesAll()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var testLogger = new TestLogger<ParallelWhenAllNotificationPublisher>();
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true, testLogger);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new ThrowingHandler(),
                new ThrowingHandler(),
                new BasicHandler()
            };
            var notification = new TestNotification("test");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AggregateException>(() =>
                publisher.PublishAsync(notification, handlers, CancellationToken.None).AsTask());

            Assert.Equal(2, exception.InnerExceptions.Count);
            foreach (var innerException in exception.InnerExceptions)
            {
                Assert.IsType<InvalidOperationException>(innerException);
            }

            // Verify all handlers ran
            Assert.Contains("BasicHandler: test", BasicHandler.ExecutionLog);
        }
    }
}