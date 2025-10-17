 using System;
 using System.Collections.Concurrent;
 using System.Collections.Generic;
 using System.Linq;
 using System.Threading;
 using System.Threading.Tasks;
 using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Interfaces;
using Relay.Core.Publishing.Extensions;
using Relay.Core.Publishing.Options;
using Relay.Core.Publishing.Strategies;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class NotificationPublisherTests
    {
        #region Test Models

        public record TestNotification(string Message) : INotification;

        public class TestHandler1 : INotificationHandler<TestNotification>
        {
            public static ConcurrentQueue<string> ExecutionLog { get; private set; } = new();
            public static int ExecutionDelay { get; set; } = 0;

            public static void ClearLog() => ExecutionLog = new ConcurrentQueue<string>();

            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                ExecutionLog.Enqueue($"Handler1-Start: {notification.Message}");
                if (ExecutionDelay > 0)
                    await Task.Delay(ExecutionDelay, cancellationToken);
                ExecutionLog.Enqueue($"Handler1-End: {notification.Message}");
            }
        }

        public class TestHandler2 : INotificationHandler<TestNotification>
        {
            public static int ExecutionDelay { get; set; } = 0;

            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                TestHandler1.ExecutionLog.Enqueue($"Handler2-Start: {notification.Message}");
                if (ExecutionDelay > 0)
                    await Task.Delay(ExecutionDelay, cancellationToken);
                TestHandler1.ExecutionLog.Enqueue($"Handler2-End: {notification.Message}");
            }
        }

        public class TestHandler3 : INotificationHandler<TestNotification>
        {
            public static int ExecutionDelay { get; set; } = 0;

            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                TestHandler1.ExecutionLog.Enqueue($"Handler3-Start: {notification.Message}");
                if (ExecutionDelay > 0)
                    await Task.Delay(ExecutionDelay, cancellationToken);
                TestHandler1.ExecutionLog.Enqueue($"Handler3-End: {notification.Message}");
            }
        }

        public class ThrowingHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                TestHandler1.ExecutionLog.Enqueue($"ThrowingHandler: {notification.Message}");
                throw new InvalidOperationException("Handler failed");
            }
        }

        #endregion

        #region Sequential Publisher Tests

        [Fact]
        public async Task SequentialPublisher_Should_Execute_Handlers_In_Order()
        {
            // Arrange
            TestHandler1.ClearLog();
            TestHandler1.ExecutionDelay = 10;
            TestHandler2.ExecutionDelay = 10;
            TestHandler3.ExecutionDelay = 10;

            var publisher = new SequentialNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new TestHandler1(),
                new TestHandler2(),
                new TestHandler3()
            };

            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, default);

            // Assert
            Assert.Equal(6, TestHandler1.ExecutionLog.Count);
            // Sequential execution means handlers complete one at a time
            Assert.Equal("Handler1-Start: test", TestHandler1.ExecutionLog.ElementAt(0));
            Assert.Equal("Handler1-End: test", TestHandler1.ExecutionLog.ElementAt(1));
            Assert.Equal("Handler2-Start: test", TestHandler1.ExecutionLog.ElementAt(2));
            Assert.Equal("Handler2-End: test", TestHandler1.ExecutionLog.ElementAt(3));
            Assert.Equal("Handler3-Start: test", TestHandler1.ExecutionLog.ElementAt(4));
            Assert.Equal("Handler3-End: test", TestHandler1.ExecutionLog.ElementAt(5));
        }

        [Fact]
        public async Task SequentialPublisher_Should_Stop_On_Exception()
        {
            // Arrange
            TestHandler1.ClearLog();

            var publisher = new SequentialNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new TestHandler1(),
                new ThrowingHandler(),
                new TestHandler2() // Should not execute
            };

            var notification = new TestNotification("test");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await publisher.PublishAsync(notification, handlers, default);
            });

            // Handler2 should not have executed
            Assert.Contains("Handler1-Start: test", TestHandler1.ExecutionLog);
            Assert.Contains("ThrowingHandler: test", TestHandler1.ExecutionLog);
            Assert.DoesNotContain(TestHandler1.ExecutionLog, x => x.Contains("Handler2"));
        }

        #endregion

        #region Parallel Publisher Tests
        [Fact]
        public async Task ParallelPublisher_Should_Fail_Fast_On_Exception()
        {
            // Arrange
            TestHandler1.ClearLog();

            var publisher = new ParallelNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new TestHandler1(),
                new ThrowingHandler(),
                new TestHandler2()
            };

            var notification = new TestNotification("test");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await publisher.PublishAsync(notification, handlers, default);
            });
        }

        [Fact]
        public async Task ParallelPublisher_Should_Optimize_Single_Handler()
        {
            // Arrange
            TestHandler1.ClearLog();

            var publisher = new ParallelNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new TestHandler1()
            };

            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, default);

            // Assert
            Assert.Equal(2, TestHandler1.ExecutionLog.Count);
            Assert.Equal("Handler1-Start: test", TestHandler1.ExecutionLog.ElementAt(0));
            Assert.Equal("Handler1-End: test", TestHandler1.ExecutionLog.ElementAt(1));
        }

        #endregion

        #region ParallelWhenAll Publisher Tests

        [Fact]
        public async Task ParallelWhenAllPublisher_Should_Continue_On_Exception()
        {
            // Arrange
            TestHandler1.ClearLog();

            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new TestHandler1(),
                new ThrowingHandler(),
                new TestHandler2() // Should still execute
            };

            var notification = new TestNotification("test");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await publisher.PublishAsync(notification, handlers, default);
            });

            // All handlers should have executed
            Assert.Contains("Handler1-Start: test", TestHandler1.ExecutionLog);
            Assert.Contains("ThrowingHandler: test", TestHandler1.ExecutionLog);
            Assert.Contains("Handler2-Start: test", TestHandler1.ExecutionLog);

            Assert.Single(exception.InnerExceptions);
            Assert.IsType<InvalidOperationException>(exception.InnerExceptions[0]);
        }

        [Fact]
        public async Task ParallelWhenAllPublisher_Should_Collect_Multiple_Exceptions()
        {
            // Arrange
            TestHandler1.ClearLog();

            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new ThrowingHandler(),
                new ThrowingHandler(),
                new TestHandler1()
            };

            var notification = new TestNotification("test");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<AggregateException>(async () =>
            {
                await publisher.PublishAsync(notification, handlers, default);
            });

            Assert.Equal(2, exception.InnerExceptions.Count);
            Assert.Contains("Handler1-Start: test", TestHandler1.ExecutionLog);
        }

        [Fact]
        public async Task ParallelWhenAllPublisher_Should_Not_Throw_If_No_Exceptions()
        {
            // Arrange
            TestHandler1.ClearLog();

            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true);
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new TestHandler1(),
                new TestHandler2(),
                new TestHandler3()
            };

            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, default);

            // Assert
            Assert.Equal(6, TestHandler1.ExecutionLog.Count);
        }

        #endregion

        #region DI Configuration Tests

        [Fact]
        public void UseSequentialNotificationPublisher_Should_Register_Correctly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.UseSequentialNotificationPublisher();
            var provider = services.BuildServiceProvider();
            var publisher = provider.GetService<INotificationPublisher>();

            // Assert
            Assert.NotNull(publisher);
            Assert.IsType<SequentialNotificationPublisher>(publisher);
        }

        [Fact]
        public void UseParallelNotificationPublisher_Should_Register_Correctly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.UseParallelNotificationPublisher();
            var provider = services.BuildServiceProvider();
            var publisher = provider.GetService<INotificationPublisher>();

            // Assert
            Assert.NotNull(publisher);
            Assert.IsType<ParallelNotificationPublisher>(publisher);
        }

        [Fact]
        public void UseParallelWhenAllNotificationPublisher_Should_Register_Correctly()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.UseParallelWhenAllNotificationPublisher();
            var provider = services.BuildServiceProvider();
            var publisher = provider.GetService<INotificationPublisher>();

            // Assert
            Assert.NotNull(publisher);
            Assert.IsType<ParallelWhenAllNotificationPublisher>(publisher);
        }

        [Fact]
        public void ConfigureNotificationPublisher_Should_Use_Custom_Type()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ConfigureNotificationPublisher(options =>
            {
                options.PublisherType = typeof(SequentialNotificationPublisher);
            });
            var provider = services.BuildServiceProvider();
            var publisher = provider.GetService<INotificationPublisher>();

            // Assert
            Assert.NotNull(publisher);
            Assert.IsType<SequentialNotificationPublisher>(publisher);
        }

        [Fact]
        public void ConfigureNotificationPublisher_Should_Use_Custom_Instance()
        {
            // Arrange
            var services = new ServiceCollection();
            var customPublisher = new SequentialNotificationPublisher();

            // Act
            services.ConfigureNotificationPublisher(options =>
            {
                options.Publisher = customPublisher;
            });
            var provider = services.BuildServiceProvider();
            var publisher = provider.GetService<INotificationPublisher>();

            // Assert
            Assert.Same(customPublisher, publisher);
        }

        #endregion
    }
}
