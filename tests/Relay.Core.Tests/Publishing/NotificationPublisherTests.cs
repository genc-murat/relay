using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Extensions;
using Relay.Core.Publishing.Interfaces;
using Relay.Core.Publishing.Strategies;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Publishing;

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
    public async Task ParallelPublisher_Constructor_Should_Accept_Null_Logger()
    {
        // Act
        var publisher = new ParallelNotificationPublisher();

        // Assert - Should not throw
        Assert.NotNull(publisher);
    }

    [Fact]
    public async Task ParallelPublisher_Should_Throw_When_Notification_Is_Null()
    {
        // Arrange
        var publisher = new ParallelNotificationPublisher();
        var handlers = new INotificationHandler<TestNotification>[]
        {
            new TestHandler1()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await publisher.PublishAsync<TestNotification>(null!, handlers, default);
        });
    }

    [Fact]
    public async Task ParallelPublisher_Should_Throw_When_Handlers_Is_Null()
    {
        // Arrange
        var publisher = new ParallelNotificationPublisher();
        var notification = new TestNotification("test");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await publisher.PublishAsync(notification, null!, default);
        });
    }

    [Fact]
    public async Task ParallelPublisher_Should_Handle_Empty_Handlers_Collection()
    {
        // Arrange
        TestHandler1.ClearLog();
        
        var publisher = new ParallelNotificationPublisher();
        var handlers = Array.Empty<INotificationHandler<TestNotification>>();
        var notification = new TestNotification("test");

        // Act & Assert - Should not throw
        await publisher.PublishAsync(notification, handlers, default);

        // Assert - No handlers executed
        Assert.Empty(TestHandler1.ExecutionLog);
    }

    [Fact]
    public async Task ParallelPublisher_Should_Execute_Multiple_Handlers_In_Parallel()
    {
        // Arrange
        TestHandler1.ClearLog();
        
        // Set delays to make parallel execution more apparent
        TestHandler1.ExecutionDelay = 100;
        TestHandler2.ExecutionDelay = 100;
        TestHandler3.ExecutionDelay = 100;

        var publisher = new ParallelNotificationPublisher();
        var handlers = new INotificationHandler<TestNotification>[]
        {
            new TestHandler1(),
            new TestHandler2(),
            new TestHandler3()
        };

        var notification = new TestNotification("test");

        // Act - Measure the time it takes to execute all handlers
        var startTime = DateTime.UtcNow;
        await publisher.PublishAsync(notification, handlers, default);
        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;

        // All 3 handlers have 100ms delay, but if executed in parallel they should finish
        // in about 100ms rather than 300ms if executed sequentially
        Assert.True(duration.TotalMilliseconds < 250, 
            $"Parallel execution took {duration.TotalMilliseconds}ms, expected less than 250ms for parallel execution");

        // Assert - All handlers should have started and completed (3 handlers * 2 messages each = 6)
        var initialLogCount = TestHandler1.ExecutionLog.Count;
        Assert.Equal(6, initialLogCount);
        
        // Check that both start and end messages exist for all handlers
        Assert.Contains(TestHandler1.ExecutionLog, msg => msg.StartsWith("Handler1-Start"));
        Assert.Contains(TestHandler1.ExecutionLog, msg => msg.StartsWith("Handler2-Start"));
        Assert.Contains(TestHandler1.ExecutionLog, msg => msg.StartsWith("Handler3-Start"));
        Assert.Contains(TestHandler1.ExecutionLog, msg => msg.StartsWith("Handler1-End"));
        Assert.Contains(TestHandler1.ExecutionLog, msg => msg.StartsWith("Handler2-End"));
        Assert.Contains(TestHandler1.ExecutionLog, msg => msg.StartsWith("Handler3-End"));
    }

    [Fact]
    public async Task ParallelPublisher_With_Logger_Should_Log_Execution()
    {
        // Arrange
        TestHandler1.ClearLog();
        
        var testLogger = new TestLogger<ParallelNotificationPublisher>();
        var publisher = new ParallelNotificationPublisher(testLogger);
        var handlers = new INotificationHandler<TestNotification>[]
        {
            new TestHandler1(),
            new TestHandler2()
        };

        var notification = new TestNotification("test");

        // Act
        await publisher.PublishAsync(notification, handlers, default);

        // Assert - Check that appropriate log messages were generated
        Assert.Contains(testLogger.LoggedMessages, msg => 
            msg.LogLevel == LogLevel.Debug && 
            msg.Message.Contains("Publishing notification TestNotification to 2 handler(s) in parallel"));
        
        Assert.Contains(testLogger.LoggedMessages, msg => 
            msg.LogLevel == LogLevel.Debug && 
            msg.Message.Contains("All handlers completed for notification TestNotification"));
    }

    [Fact]
    public async Task ParallelPublisher_With_CancellationToken_Should_Respect_Cancellation()
    {
        // Arrange
        TestHandler1.ClearLog();
        
        var publisher = new ParallelNotificationPublisher();
        var handlers = new INotificationHandler<TestNotification>[]
        {
            new TestHandler1()
        };

        var notification = new TestNotification("test");
        using var cts = new CancellationTokenSource();
        
        // Cancel the token before execution
        cts.Cancel();

        // Act & Assert - Should throw TaskCanceledException (from Task.Delay)
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await publisher.PublishAsync(notification, handlers, cts.Token);
        });
    }

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
