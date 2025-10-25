using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Extensions;
using Relay.Core.Publishing.Interfaces;
using Relay.Core.Publishing.Strategies;
using System;
using System.Collections.Generic;
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

    [Fact]
    public async Task SequentialPublisher_Constructor_Should_Accept_Null_Logger()
    {
        // Act
        var publisher = new SequentialNotificationPublisher();

        // Assert - Should not throw
        Assert.NotNull(publisher);
    }

    [Fact]
    public async Task SequentialPublisher_Should_Throw_When_Notification_Is_Null()
    {
        // Arrange
        var publisher = new SequentialNotificationPublisher();
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
    public async Task SequentialPublisher_Should_Throw_When_Handlers_Is_Null()
    {
        // Arrange
        var publisher = new SequentialNotificationPublisher();
        var notification = new TestNotification("test");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await publisher.PublishAsync(notification, null!, default);
        });
    }

    [Fact]
    public async Task SequentialPublisher_Should_Handle_Empty_Handlers_Collection()
    {
        // Arrange
        TestHandler1.ClearLog();
        
        var publisher = new SequentialNotificationPublisher();
        var handlers = Array.Empty<INotificationHandler<TestNotification>>();
        var notification = new TestNotification("test");

        // Act & Assert - Should not throw
        await publisher.PublishAsync(notification, handlers, default);

        // Assert - No handlers executed (log should remain empty)
        var finalLog = TestHandler1.ExecutionLog.ToList();
        Assert.Empty(finalLog);
    }

    [Fact]
    public async Task SequentialPublisher_With_Logger_Should_Log_Execution()
    {
        // Arrange
        TestHandler1.ClearLog();
        
        var testLogger = new TestLogger<SequentialNotificationPublisher>();
        var publisher = new SequentialNotificationPublisher(testLogger);
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
            msg.Message.Contains("Publishing notification TestNotification to 2 handler(s) sequentially"));
        
        Assert.Contains(testLogger.LoggedMessages, msg => 
            msg.LogLevel == LogLevel.Debug && 
            msg.Message.Contains("All handlers completed for notification TestNotification"));
    }

    [Fact]
    public async Task SequentialPublisher_With_CancellationToken_Should_Respect_Cancellation()
    {
        // Arrange
        TestHandler1.ExecutionDelay = 100; // Ensure delay is used
        var publisher = new SequentialNotificationPublisher();
        var handlers = new INotificationHandler<TestNotification>[]
        {
            new TestHandler1()
        };

        var notification = new TestNotification("test");
        using var cts = new CancellationTokenSource();
        
        // Cancel the token before execution
        cts.Cancel();

        // Act & Assert - Should throw TaskCanceledException
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await publisher.PublishAsync(notification, handlers, cts.Token);
        });
        
        // Reset delay for other tests
        TestHandler1.ExecutionDelay = 0;
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
        TestHandler1.ExecutionDelay = 10; // Ensure Task.Delay is called

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

        // Reset ExecutionDelay for other tests
        TestHandler1.ExecutionDelay = 0;
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

        // Assert - Store the log in a local variable to avoid multiple enumerations
        var logEntries = TestHandler1.ExecutionLog.ToList();
        Assert.Equal(2, logEntries.Count);
        Assert.Equal("Handler1-Start: test", logEntries[0]);
        Assert.Equal("Handler1-End: test", logEntries[1]);
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

    [Fact]
    public async Task ParallelWhenAllPublisher_Constructor_Should_Accept_ContinueOnException_Parameter()
    {
        // Act
        var publisher1 = new ParallelWhenAllNotificationPublisher(continueOnException: true);
        var publisher2 = new ParallelWhenAllNotificationPublisher(continueOnException: false);
        var publisher3 = new ParallelWhenAllNotificationPublisher(); // default is true

        // Assert - Should not throw
        Assert.NotNull(publisher1);
        Assert.NotNull(publisher2);
        Assert.NotNull(publisher3);
    }

    [Fact]
    public async Task ParallelWhenAllPublisher_Should_Throw_When_Notification_Is_Null()
    {
        // Arrange
        var publisher = new ParallelWhenAllNotificationPublisher();
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
    public async Task ParallelWhenAllPublisher_Should_Throw_When_Handlers_Is_Null()
    {
        // Arrange
        var publisher = new ParallelWhenAllNotificationPublisher();
        var notification = new TestNotification("test");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await publisher.PublishAsync(notification, null!, default);
        });
    }

    [Fact]
    public async Task ParallelWhenAllPublisher_Should_Handle_Empty_Handlers_Collection()
    {
        // Arrange
        TestHandler1.ClearLog();
        
        var publisher = new ParallelWhenAllNotificationPublisher();
        var handlers = Array.Empty<INotificationHandler<TestNotification>>();
        var notification = new TestNotification("test");

        // Act & Assert - Should not throw
        await publisher.PublishAsync(notification, handlers, default);

        // Assert - No handlers executed
        Assert.Empty(TestHandler1.ExecutionLog);
    }

    [Fact]
    public async Task ParallelWhenAllPublisher_With_ContinueOnException_False_Should_Fail_Fast()
    {
        // Arrange
        TestHandler1.ClearLog();

        var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: false);
        var handlers = new INotificationHandler<TestNotification>[]
        {
            new TestHandler1(),
            new ThrowingHandler(),  // This should cause immediate failure
            new TestHandler2()      // This should not execute due to fail-fast
        };

        var notification = new TestNotification("test");

        // Act & Assert - Should throw the first exception immediately
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await publisher.PublishAsync(notification, handlers, default);
        });

        // With Task.WhenAll (fail-fast mode), all tasks start executing but we expect
        // that when one throws, the operation stops. Note that in practice with parallel
        // execution, other handlers might already have started before the exception is detected.
        var executedHandlers = TestHandler1.ExecutionLog.ToList();
        
        // At a minimum, we should see that the first handler executed
        Assert.Contains(executedHandlers, msg => msg.Contains("Handler1"));
        
        // This demonstrates the fail-fast nature (vs continue-on-exception behavior)
    }

    [Fact]
    public async Task ParallelWhenAllPublisher_With_Logger_Should_Log_Execution()
    {
        // Arrange
        TestHandler1.ClearLog();
        
        var testLogger = new TestLogger<ParallelWhenAllNotificationPublisher>();
        var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true, testLogger);
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
    public async Task ParallelWhenAllPublisher_With_CancellationToken_Should_Respect_Cancellation()
    {
        // Arrange
        var publisher = new ParallelWhenAllNotificationPublisher();
        var handlers = new INotificationHandler<TestNotification>[]
        {
            new TestHandler1()
        };

        var notification = new TestNotification("test");
        using var cts = new CancellationTokenSource();
        
        // Cancel the token before execution
        cts.Cancel();

        // Act & Assert - Should throw TaskCanceledException
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await publisher.PublishAsync(notification, handlers, cts.Token);
        });
    }

    [Fact]
    public async Task ParallelWhenAllPublisher_With_ContinueOnException_And_Logger_Should_Log_Exceptions()
    {
        // Arrange
        TestHandler1.ClearLog();
        
        var testLogger = new TestLogger<ParallelWhenAllNotificationPublisher>();
        var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true, testLogger);
        var handlers = new INotificationHandler<TestNotification>[]
        {
            new TestHandler1(),
            new ThrowingHandler(),  // This will throw
            new TestHandler2()
        };

        var notification = new TestNotification("test");

        // Act & Assert - Should collect exceptions as AggregateException
        var exception = await Assert.ThrowsAsync<AggregateException>(async () =>
        {
            await publisher.PublishAsync(notification, handlers, default);
        });

        // Verify exceptions were collected
        Assert.Single(exception.InnerExceptions);
        Assert.IsType<InvalidOperationException>(exception.InnerExceptions[0]);
        
        // Verify logging occurred for the exception
        Assert.Contains(testLogger.LoggedMessages, msg => 
            msg.LogLevel == LogLevel.Error && 
            msg.Message.Contains("Handler") && 
            msg.Message.Contains("failed while processing notification"));
        
        Assert.Contains(testLogger.LoggedMessages, msg => 
            msg.LogLevel == LogLevel.Warning && 
            msg.Message.Contains("1 handler(s) failed while processing notification"));
    }

    [Fact]
    public async Task ParallelWhenAllPublisher_With_ContinueOnException_Should_Aggregate_Multiple_Exceptions_Correctly()
    {
        // Arrange
        TestHandler1.ClearLog();
        
        var testLogger = new TestLogger<ParallelWhenAllNotificationPublisher>();
        var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true, testLogger);
        var handlers = new INotificationHandler<TestNotification>[]
        {
            new ThrowingHandler(),  // Exception 1
            new TestHandler1(),    // Should succeed
            new ThrowingHandler(), // Exception 2
            new TestHandler2()     // Should succeed
        };

        var notification = new TestNotification("test");

        // Act & Assert - Should collect multiple exceptions as AggregateException
        var exception = await Assert.ThrowsAsync<AggregateException>(async () =>
        {
            await publisher.PublishAsync(notification, handlers, default);
        });

        // Verify multiple exceptions were collected
        Assert.Equal(2, exception.InnerExceptions.Count);
        foreach (var innerException in exception.InnerExceptions)
        {
            Assert.IsType<InvalidOperationException>(innerException);
        }
        
        // Verify all handlers ran (success and failure)
        Assert.Contains("Handler1-Start: test", TestHandler1.ExecutionLog);
        Assert.Contains("ThrowingHandler: test", TestHandler1.ExecutionLog);
        Assert.Contains("Handler2-Start: test", TestHandler1.ExecutionLog);
        
        // Verify warning was logged about multiple failures
        Assert.Contains(testLogger.LoggedMessages, msg => 
            msg.LogLevel == LogLevel.Warning && 
            msg.Message.Contains("2 handler(s) failed while processing notification"));
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

    [Fact]
    public void UseCustomNotificationPublisher_Generic_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.UseCustomNotificationPublisher<TestCustomNotificationPublisher>();
        var provider = services.BuildServiceProvider();
        var publisher = provider.GetService<INotificationPublisher>();

        // Assert
        Assert.NotNull(publisher);
        Assert.IsType<TestCustomNotificationPublisher>(publisher);
    }

    [Fact]
    public void UseCustomNotificationPublisher_WithFactory_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.UseCustomNotificationPublisher(sp => new TestCustomNotificationPublisher());
        var provider = services.BuildServiceProvider();
        var publisher = provider.GetService<INotificationPublisher>();

        // Assert
        Assert.NotNull(publisher);
        Assert.IsType<TestCustomNotificationPublisher>(publisher);
    }

    [Fact]
    public void UseOrderedNotificationPublisher_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.UseOrderedNotificationPublisher();
        var provider = services.BuildServiceProvider();
        var publisher = provider.GetService<INotificationPublisher>();

        // Assert
        Assert.NotNull(publisher);
        Assert.IsType<OrderedNotificationPublisher>(publisher);
    }

    [Fact]
    public void UseOrderedNotificationPublisher_WithOptions_Should_Register_Correctly()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.UseOrderedNotificationPublisher(continueOnException: false, maxDegreeOfParallelism: 4);
        var provider = services.BuildServiceProvider();
        var publisher = provider.GetService<INotificationPublisher>();

        // Assert
        Assert.NotNull(publisher);
        Assert.IsType<OrderedNotificationPublisher>(publisher);
    }

    [Fact]
    public void UseSequentialNotificationPublisher_With_Custom_Lifetime_Should_Register_With_Correct_Lifetime()
    {
        // Test Singleton lifetime (default)
        var services1 = new ServiceCollection();
        services1.UseSequentialNotificationPublisher(ServiceLifetime.Singleton);
        var provider1 = services1.BuildServiceProvider();
        var publisher1a = provider1.GetService<INotificationPublisher>();
        var publisher1b = provider1.GetService<INotificationPublisher>();
        Assert.Same(publisher1a, publisher1b); // Should be same instance for singleton

        // Test Transient lifetime
        var services2 = new ServiceCollection();
        services2.UseSequentialNotificationPublisher(ServiceLifetime.Transient);
        var provider2 = services2.BuildServiceProvider();
        var publisher2a = provider2.GetService<INotificationPublisher>();
        var publisher2b = provider2.GetService<INotificationPublisher>();
        Assert.NotSame(publisher2a, publisher2b); // Should be different instances for transient

        // Test Scoped lifetime
        var services3 = new ServiceCollection();
        services3.UseSequentialNotificationPublisher(ServiceLifetime.Scoped);
        var provider3 = services3.BuildServiceProvider();
        
        using (var scope = provider3.CreateScope())
        {
            var publisher3a = scope.ServiceProvider.GetService<INotificationPublisher>();
            var publisher3b = scope.ServiceProvider.GetService<INotificationPublisher>();
            Assert.Same(publisher3a, publisher3b); // Should be same instance within scope
        }
    }

    [Fact]
    public void UseParallelNotificationPublisher_With_Custom_Lifetime_Should_Register_With_Correct_Lifetime()
    {
        // Test Singleton lifetime (default)
        var services1 = new ServiceCollection();
        services1.UseParallelNotificationPublisher(ServiceLifetime.Singleton);
        var provider1 = services1.BuildServiceProvider();
        var publisher1a = provider1.GetService<INotificationPublisher>();
        var publisher1b = provider1.GetService<INotificationPublisher>();
        Assert.Same(publisher1a, publisher1b); // Should be same instance for singleton
    }

    [Fact]
    public void UseParallelWhenAllNotificationPublisher_With_Custom_Lifetime_Should_Register_With_Correct_Lifetime()
    {
        // Test Singleton lifetime (default)
        var services1 = new ServiceCollection();
        services1.UseParallelWhenAllNotificationPublisher(continueOnException: true, ServiceLifetime.Singleton);
        var provider1 = services1.BuildServiceProvider();
        var publisher1a = provider1.GetService<INotificationPublisher>();
        var publisher1b = provider1.GetService<INotificationPublisher>();
        Assert.Same(publisher1a, publisher1b); // Should be same instance for singleton
    }

    #endregion
}

// Custom publisher for testing
internal class TestCustomNotificationPublisher : INotificationPublisher
{
    public ValueTask PublishAsync<TNotification>(
        TNotification notification,
        IEnumerable<INotificationHandler<TNotification>> handlers,
        CancellationToken cancellationToken) where TNotification : INotification
    {
        // Implementation not needed for registration test
        return new ValueTask();
    }
}
