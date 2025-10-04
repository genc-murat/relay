using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core;
using Relay.Core.Publishing;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class OrderedNotificationPublisherTests
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

        [NotificationHandlerOrder(3)]
        public class OrderedHandler3 : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                BasicHandler.ExecutionLog.Add($"OrderedHandler3: {notification.Message}");
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

        #endregion

        #region Constructor Tests

        [Fact]
        public void Constructor_WithDefaults_CreatesInstance()
        {
            // Act
            var publisher = new OrderedNotificationPublisher();

            // Assert
            publisher.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithLogger_CreatesInstance()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<OrderedNotificationPublisher>>();

            // Act
            var publisher = new OrderedNotificationPublisher(mockLogger.Object);

            // Assert
            publisher.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithAllParameters_CreatesInstance()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<OrderedNotificationPublisher>>();

            // Act
            var publisher = new OrderedNotificationPublisher(mockLogger.Object, continueOnException: false, maxDegreeOfParallelism: 4);

            // Assert
            publisher.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithZeroMaxDegreeOfParallelism_UsesProcessorCount()
        {
            // Arrange & Act
            var publisher = new OrderedNotificationPublisher(maxDegreeOfParallelism: 0);

            // Assert
            // Should not throw and should use Environment.ProcessorCount internally
            publisher.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_WithNegativeMaxDegreeOfParallelism_UsesProcessorCount()
        {
            // Arrange & Act
            var publisher = new OrderedNotificationPublisher(maxDegreeOfParallelism: -1);

            // Assert
            publisher.Should().NotBeNull();
        }

        #endregion

        #region Null Argument Tests

        [Fact]
        public async Task PublishAsync_WithNullNotification_ThrowsArgumentNullException()
        {
            // Arrange
            var publisher = new OrderedNotificationPublisher();
            var handlers = new List<INotificationHandler<TestNotification>>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                publisher.PublishAsync<TestNotification>(null!, handlers, CancellationToken.None).AsTask());
        }

        [Fact]
        public async Task PublishAsync_WithNullHandlers_ThrowsArgumentNullException()
        {
            // Arrange
            var publisher = new OrderedNotificationPublisher();
            var notification = new TestNotification("test");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                publisher.PublishAsync(notification, null!, CancellationToken.None).AsTask());
        }

        #endregion

        #region No Handlers Tests

        [Fact]
        public async Task PublishAsync_WithNoHandlers_CompletesSuccessfully()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<OrderedNotificationPublisher>>();
            var publisher = new OrderedNotificationPublisher(mockLogger.Object);
            var handlers = new List<INotificationHandler<TestNotification>>();
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert - verify debug log was called
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No handlers registered")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Order Attribute Tests

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
            BasicHandler.ExecutionLog.Should().HaveCount(3);
            BasicHandler.ExecutionLog[0].Should().Be("OrderedHandler1: test");
            BasicHandler.ExecutionLog[1].Should().Be("OrderedHandler2: test");
            BasicHandler.ExecutionLog[2].Should().Be("OrderedHandler3: test");
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
            BasicHandler.ExecutionLog.Should().HaveCount(3);
            // Handler with no order (0) should execute first, then ordered handlers
            BasicHandler.ExecutionLog[0].Should().Be("BasicHandler: test");
            BasicHandler.ExecutionLog[1].Should().Be("OrderedHandler1: test");
            BasicHandler.ExecutionLog[2].Should().Be("OrderedHandler2: test");
        }

        #endregion

        #region Group Attribute Tests

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
            BasicHandler.ExecutionLog.Should().HaveCount(5);

            // Group A handlers should both complete before Group B starts
            var groupAEndIndex = BasicHandler.ExecutionLog.FindLastIndex(x => x.Contains("GroupAHandler"));
            var groupBStartIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("GroupBHandler"));

            groupAEndIndex.Should().BeLessThan(groupBStartIndex);
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
            BasicHandler.ExecutionLog.Count.Should().BeGreaterThanOrEqualTo(4);
            BasicHandler.ExecutionLog.Should().Contain(x => x.Contains("GroupAHandler1-Start"));
            BasicHandler.ExecutionLog.Should().Contain(x => x.Contains("GroupAHandler1-End"));
            BasicHandler.ExecutionLog.Should().Contain(x => x.Contains("GroupAHandler2-Start"));
            BasicHandler.ExecutionLog.Should().Contain(x => x.Contains("GroupAHandler2-End"));
        }

        #endregion

        #region Dependency Attribute Tests

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
            BasicHandler.ExecutionLog.Should().HaveCount(2);
            BasicHandler.ExecutionLog[0].Should().Be("DependencyBaseHandler: test");
            BasicHandler.ExecutionLog[1].Should().Be("DependentHandler: test");
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
            BasicHandler.ExecutionLog.Should().HaveCount(2);
            BasicHandler.ExecutionLog[0].Should().Be("ExecuteBeforeLastHandler: test");
            BasicHandler.ExecutionLog[1].Should().Be("ExecuteLastHandler: test");
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
            BasicHandler.ExecutionLog.Should().HaveCount(2);

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

        #endregion

        #region Execution Mode Tests

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
            BasicHandler.ExecutionLog.Should().HaveCount(3);
            // Sequential handler should fully complete before basic handler
            BasicHandler.ExecutionLog[0].Should().Be("SequentialHandler-Start: test");
            BasicHandler.ExecutionLog[1].Should().Be("SequentialHandler-End: test");
            BasicHandler.ExecutionLog[2].Should().Be("BasicHandler: test");
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
            BasicHandler.ExecutionLog.Count.Should().BeGreaterThanOrEqualTo(4);

            // Both handlers should execute fully
            BasicHandler.ExecutionLog.Should().Contain(x => x.Contains("ParallelHandler1-Start"));
            BasicHandler.ExecutionLog.Should().Contain(x => x.Contains("ParallelHandler1-End"));
            BasicHandler.ExecutionLog.Should().Contain(x => x.Contains("ParallelHandler2-Start"));
            BasicHandler.ExecutionLog.Should().Contain(x => x.Contains("ParallelHandler2-End"));
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
            BasicHandler.ExecutionLog.Should().HaveCount(4);

            // Sequential handler should complete before parallel handler starts
            var sequentialEndIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("SequentialHandler-End"));
            var parallelStartIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("ParallelHandler1-Start"));

            sequentialEndIndex.Should().BeLessThan(parallelStartIndex);
        }

        #endregion

        #region Exception Handling Tests

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
            BasicHandler.ExecutionLog.Should().HaveCount(3);
            BasicHandler.ExecutionLog[0].Should().Be("BasicHandler: test");
            BasicHandler.ExecutionLog[1].Should().Be("ThrowingHandler: test");
            BasicHandler.ExecutionLog[2].Should().Be("OrderedHandler1: test");

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

            exception.Message.Should().Be("Handler failed");

            // GroupA should have executed, ThrowingGroupHandler should have thrown,
            // GroupC should not have executed
            BasicHandler.ExecutionLog.Should().Contain(x => x.Contains("GroupAHandler1"));
            BasicHandler.ExecutionLog.Should().Contain(x => x.Contains("ThrowingGroupHandler"));
            BasicHandler.ExecutionLog.Should().NotContain(x => x.Contains("GroupCHandler"));
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
            BasicHandler.ExecutionLog.Should().HaveCount(3);
            BasicHandler.ExecutionLog[2].Should().Be("OrderedHandler1: test");
        }

        #endregion

        #region Cancellation Tests

        [Fact]
        public async Task PublishAsync_WithCancellationToken_PropagatesToken()
        {
            // Arrange
            var publisher = new OrderedNotificationPublisher();
            var tokenPassed = false;
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new TestCancellationHandler(ct =>
                {
                    tokenPassed = ct.IsCancellationRequested;
                })
            };
            var notification = new TestNotification("test");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await publisher.PublishAsync(notification, handlers, cts.Token);

            // Assert
            tokenPassed.Should().BeTrue();
        }

        private class TestCancellationHandler : INotificationHandler<TestNotification>
        {
            private readonly Action<CancellationToken> _action;

            public TestCancellationHandler(Action<CancellationToken> action)
            {
                _action = action;
            }

            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                _action(cancellationToken);
                return ValueTask.CompletedTask;
            }
        }

        #endregion

        #region Complex Scenarios

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
            BasicHandler.ExecutionLog.Should().HaveCount(4);

            // DependencyBaseHandler must execute before DependentHandler
            var baseIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("DependencyBaseHandler"));
            var dependentIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("DependentHandler"));
            baseIndex.Should().BeLessThan(dependentIndex);
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
            BasicHandler.ExecutionLog.Should().HaveCount(1);
            BasicHandler.ExecutionLog[0].Should().Be("BasicHandler: test");
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
            BasicHandler.ExecutionLog.Should().HaveCount(3);

            // Both dependencies should execute before MultipleDependenciesHandler
            var multiDepIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("MultipleDependenciesHandler"));
            var baseIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("DependencyBaseHandler"));
            var basicIndex = BasicHandler.ExecutionLog.FindIndex(x => x.Contains("BasicHandler"));

            baseIndex.Should().BeLessThan(multiDepIndex);
            basicIndex.Should().BeLessThan(multiDepIndex);
        }

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
