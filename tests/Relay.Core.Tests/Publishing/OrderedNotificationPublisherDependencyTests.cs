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
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class OrderedNotificationPublisherDependencyTests
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

        // Handlers with ExecuteBefore attribute
        public class ExecuteLastHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"ExecuteLastHandler: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        [ExecuteBefore(typeof(ExecuteLastHandler))]
        public class ExecuteBeforeLastHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"ExecuteBeforeLastHandler: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        // Circular dependency handlers
        [ExecuteAfter(typeof(CircularHandler2))]
        public class CircularHandler1 : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"CircularHandler1: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        [ExecuteAfter(typeof(CircularHandler1))]
        public class CircularHandler2 : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"CircularHandler2: {notification.Message}");
                return ValueTask.CompletedTask;
            }
        }

        [Fact]
        public async Task PublishAsync_WithExecuteAfterDependency_ExecutesInCorrectOrder()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var publisher = new OrderedNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new DependentHandler(),      // Executes after DependencyBaseHandler
                new DependencyBaseHandler()  // Base handler
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(2, BasicHandler.ExecutionLog.Count);
            Assert.Equal("DependencyBaseHandler: test", BasicHandler.ExecutionLog[0]);
            Assert.Equal("DependentHandler: test", BasicHandler.ExecutionLog[1]);
        }

        [Fact]
        public async Task PublishAsync_WithExecuteBeforeDependency_ExecutesInCorrectOrder()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var publisher = new OrderedNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new ExecuteLastHandler(),       // Should execute last
                new ExecuteBeforeLastHandler()  // Should execute before last
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(2, BasicHandler.ExecutionLog.Count);
            Assert.Equal("ExecuteBeforeLastHandler: test", BasicHandler.ExecutionLog[0]);
            Assert.Equal("ExecuteLastHandler: test", BasicHandler.ExecutionLog[1]);
        }

        [Fact]
        public async Task PublishAsync_WithCircularDependency_LogsWarningAndExecutesAll()
        {
            // Arrange
            BasicHandler.ExecutionLog.Clear();
            var mockLogger = new Mock<ILogger<OrderedNotificationPublisher>>();
            var publisher = new OrderedNotificationPublisher(mockLogger.Object);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new CircularHandler1(),
                new CircularHandler2()
            };
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert
            Assert.Equal(2, BasicHandler.ExecutionLog.Count);

            // Verify warning was logged
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Circular dependency")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}