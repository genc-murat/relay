using Relay.Core.Implementation.Configuration;
using Relay.Core.Contracts.Requests;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Implementation;

public class ConfigurationTests
{
    // Test notification type
    public class TestNotification : INotification { }

    // Test handler
    public class TestNotificationHandler
    {
        public bool Handled { get; private set; }

        public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            Handled = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void NotificationDispatchOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var options = new NotificationDispatchOptions();

        // Assert
        Assert.Equal(NotificationDispatchMode.Parallel, options.DefaultDispatchMode);
        Assert.True(options.ContinueOnException);
        Assert.Equal(Environment.ProcessorCount, options.MaxDegreeOfParallelism);
    }

    [Fact]
    public void NotificationDispatchOptions_CanSetProperties()
    {
        // Arrange
        var options = new NotificationDispatchOptions();

        // Act
        options.DefaultDispatchMode = NotificationDispatchMode.Sequential;
        options.ContinueOnException = false;
        options.MaxDegreeOfParallelism = 5;

        // Assert
        Assert.Equal(NotificationDispatchMode.Sequential, options.DefaultDispatchMode);
        Assert.False(options.ContinueOnException);
        Assert.Equal(5, options.MaxDegreeOfParallelism);
    }

    [Fact]
    public void NotificationHandlerRegistration_CanSetProperties()
    {
        // Arrange
        var registration = new NotificationHandlerRegistration();
        var handler = new TestNotificationHandler();

        // Act
        registration.NotificationType = typeof(TestNotification);
        registration.HandlerType = typeof(TestNotificationHandler);
        registration.DispatchMode = NotificationDispatchMode.Sequential;
        registration.Priority = 10;
        registration.HandlerFactory = sp => handler;
        registration.ExecuteHandler = (h, n, ct) => ((TestNotificationHandler)h).HandleAsync((TestNotification)n, ct);

        // Assert
        Assert.Equal(typeof(TestNotification), registration.NotificationType);
        Assert.Equal(typeof(TestNotificationHandler), registration.HandlerType);
        Assert.Equal(NotificationDispatchMode.Sequential, registration.DispatchMode);
        Assert.Equal(10, registration.Priority);
        Assert.NotNull(registration.HandlerFactory);
        Assert.NotNull(registration.ExecuteHandler);
    }

    [Fact]
    public void NotificationHandlerRegistration_HandlerFactory_CanCreateHandler()
    {
        // Arrange
        var registration = new NotificationHandlerRegistration();
        var handler = new TestNotificationHandler();
        registration.HandlerFactory = sp => handler;

        // Act
        var createdHandler = registration.HandlerFactory(null!);

        // Assert
        Assert.Same(handler, createdHandler);
    }

    [Fact]
    public async Task NotificationHandlerRegistration_ExecuteHandler_CanExecuteHandler()
    {
        // Arrange
        var registration = new NotificationHandlerRegistration();
        var handler = new TestNotificationHandler();
        var notification = new TestNotification();

        registration.ExecuteHandler = (h, n, ct) => ((TestNotificationHandler)h).HandleAsync((TestNotification)n, ct);

        // Act
        await registration.ExecuteHandler(handler, notification, CancellationToken.None);

        // Assert
        Assert.True(handler.Handled);
    }

    [Fact]
    public void NotificationHandlerRegistration_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var registration = new NotificationHandlerRegistration();

        // Assert
        Assert.Null(registration.NotificationType);
        Assert.Null(registration.HandlerType);
        Assert.Equal(NotificationDispatchMode.Parallel, registration.DispatchMode); // Default enum value
        Assert.Equal(0, registration.Priority);
        Assert.Null(registration.HandlerFactory);
        Assert.Null(registration.ExecuteHandler);
    }
}