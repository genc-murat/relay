using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Publishing;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class NotificationPublisherTests
    {
        #region Test Models

        public record TestNotification(string Message) : INotification;

        public class TestHandler1 : INotificationHandler<TestNotification>
        {
            public static List<string> ExecutionLog { get; } = new();
            public static int ExecutionDelay { get; set; } = 0;

            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                ExecutionLog.Add($"Handler1-Start: {notification.Message}");
                if (ExecutionDelay > 0)
                    await Task.Delay(ExecutionDelay, cancellationToken);
                ExecutionLog.Add($"Handler1-End: {notification.Message}");
            }
        }

        public class TestHandler2 : INotificationHandler<TestNotification>
        {
            public static int ExecutionDelay { get; set; } = 0;

            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                TestHandler1.ExecutionLog.Add($"Handler2-Start: {notification.Message}");
                if (ExecutionDelay > 0)
                    await Task.Delay(ExecutionDelay, cancellationToken);
                TestHandler1.ExecutionLog.Add($"Handler2-End: {notification.Message}");
            }
        }

        public class TestHandler3 : INotificationHandler<TestNotification>
        {
            public static int ExecutionDelay { get; set; } = 0;

            public async ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                TestHandler1.ExecutionLog.Add($"Handler3-Start: {notification.Message}");
                if (ExecutionDelay > 0)
                    await Task.Delay(ExecutionDelay, cancellationToken);
                TestHandler1.ExecutionLog.Add($"Handler3-End: {notification.Message}");
            }
        }

        public class ThrowingHandler : INotificationHandler<TestNotification>
        {
            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                TestHandler1.ExecutionLog.Add($"ThrowingHandler: {notification.Message}");
                throw new InvalidOperationException("Handler failed");
            }
        }

        #endregion

        #region Sequential Publisher Tests

        [Fact]
        public async Task SequentialPublisher_Should_Execute_Handlers_In_Order()
        {
            // Arrange
            TestHandler1.ExecutionLog.Clear();
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
            TestHandler1.ExecutionLog.Should().HaveCount(6);
            // Sequential execution means handlers complete one at a time
            TestHandler1.ExecutionLog[0].Should().Be("Handler1-Start: test");
            TestHandler1.ExecutionLog[1].Should().Be("Handler1-End: test");
            TestHandler1.ExecutionLog[2].Should().Be("Handler2-Start: test");
            TestHandler1.ExecutionLog[3].Should().Be("Handler2-End: test");
            TestHandler1.ExecutionLog[4].Should().Be("Handler3-Start: test");
            TestHandler1.ExecutionLog[5].Should().Be("Handler3-End: test");
        }

        [Fact]
        public async Task SequentialPublisher_Should_Stop_On_Exception()
        {
            // Arrange
            TestHandler1.ExecutionLog.Clear();

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
            TestHandler1.ExecutionLog.Should().Contain("Handler1-Start: test");
            TestHandler1.ExecutionLog.Should().Contain("ThrowingHandler: test");
            TestHandler1.ExecutionLog.Should().NotContain(x => x.Contains("Handler2"));
        }

        #endregion

        #region Parallel Publisher Tests

        [Fact(Skip = "Flaky Test")]
        public async Task ParallelPublisher_Should_Execute_Handlers_Concurrently()
        {
            // Arrange
            TestHandler1.ExecutionLog.Clear();
            TestHandler1.ExecutionDelay = 50;
            TestHandler2.ExecutionDelay = 50;
            TestHandler3.ExecutionDelay = 50;

            var publisher = new ParallelNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new TestHandler1(),
                new TestHandler2(),
                new TestHandler3()
            };

            var notification = new TestNotification("test");

            // Act
            var startTime = DateTime.UtcNow;
            await publisher.PublishAsync(notification, handlers, default);
            var elapsed = DateTime.UtcNow - startTime;

            // Assert
            TestHandler1.ExecutionLog.Should().HaveCount(6);
            // All handlers should have started before any completed
            var startCount = TestHandler1.ExecutionLog.Count(x => x.Contains("-Start:"));
            var endCount = TestHandler1.ExecutionLog.Count(x => x.Contains("-End:"));
            startCount.Should().Be(3);
            endCount.Should().Be(3);

            // Parallel execution should be faster than sequential (3 * 50ms = 150ms)
            elapsed.TotalMilliseconds.Should().BeLessThan(120);
        }

        [Fact]
        public async Task ParallelPublisher_Should_Fail_Fast_On_Exception()
        {
            // Arrange
            TestHandler1.ExecutionLog.Clear();

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
            TestHandler1.ExecutionLog.Clear();

            var publisher = new ParallelNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new TestHandler1()
            };

            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, default);

            // Assert
            TestHandler1.ExecutionLog.Should().HaveCount(2);
            TestHandler1.ExecutionLog[0].Should().Be("Handler1-Start: test");
            TestHandler1.ExecutionLog[1].Should().Be("Handler1-End: test");
        }

        #endregion

        #region ParallelWhenAll Publisher Tests

        [Fact]
        public async Task ParallelWhenAllPublisher_Should_Continue_On_Exception()
        {
            // Arrange
            TestHandler1.ExecutionLog.Clear();

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
            TestHandler1.ExecutionLog.Should().Contain("Handler1-Start: test");
            TestHandler1.ExecutionLog.Should().Contain("ThrowingHandler: test");
            TestHandler1.ExecutionLog.Should().Contain("Handler2-Start: test");

            exception.InnerExceptions.Should().ContainSingle();
            exception.InnerExceptions[0].Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public async Task ParallelWhenAllPublisher_Should_Collect_Multiple_Exceptions()
        {
            // Arrange
            TestHandler1.ExecutionLog.Clear();

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

            exception.InnerExceptions.Should().HaveCount(2);
            TestHandler1.ExecutionLog.Should().Contain("Handler1-Start: test");
        }

        [Fact]
        public async Task ParallelWhenAllPublisher_Should_Not_Throw_If_No_Exceptions()
        {
            // Arrange
            TestHandler1.ExecutionLog.Clear();

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
            TestHandler1.ExecutionLog.Should().HaveCount(6);
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
            publisher.Should().NotBeNull();
            publisher.Should().BeOfType<SequentialNotificationPublisher>();
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
            publisher.Should().NotBeNull();
            publisher.Should().BeOfType<ParallelNotificationPublisher>();
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
            publisher.Should().NotBeNull();
            publisher.Should().BeOfType<ParallelWhenAllNotificationPublisher>();
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
            publisher.Should().NotBeNull();
            publisher.Should().BeOfType<SequentialNotificationPublisher>();
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
            publisher.Should().BeSameAs(customPublisher);
        }

        #endregion
    }
}
