using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Fallback;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Dispatchers;

/// <summary>
/// Tests for FallbackNotificationDispatcher functionality
/// </summary>
public class FallbackNotificationDispatcherTests
{
    [Fact]
    public async Task FallbackNotificationDispatcher_DispatchAsync_WithRegisteredHandlers_CallsAllHandlers()
    {
        // Arrange
        var handler1 = new TestNotificationHandler();
        var handler2 = new TestNotificationHandler();
        var handler3 = new TestNotificationHandler();

        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<TestNotification>>(handler1);
        services.AddSingleton<INotificationHandler<TestNotification>>(handler2);
        services.AddSingleton<INotificationHandler<TestNotification>>(handler3);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackNotificationDispatcher(serviceProvider);
        var notification = new TestNotification { Message = "Test" };

        // Act
        await dispatcher.DispatchAsync(notification, CancellationToken.None);

        // Assert
        Assert.True(handler1.WasCalled);
        Assert.True(handler2.WasCalled);
        Assert.True(handler3.WasCalled);
    }

    [Fact]
    public async Task FallbackNotificationDispatcher_DispatchAsync_WithoutHandlers_CompletesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackNotificationDispatcher(serviceProvider);
        var notification = new TestNotification { Message = "Test" };

        // Act & Assert - Should not throw
        await dispatcher.DispatchAsync(notification, CancellationToken.None);
    }

    [Fact]
    public async Task FallbackNotificationDispatcher_DispatchAsync_WithSingleHandler_CallsHandler()
    {
        // Arrange
        var handler = new TestNotificationHandler();

        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<TestNotification>>(handler);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackNotificationDispatcher(serviceProvider);
        var notification = new TestNotification { Message = "Test" };

        // Act
        await dispatcher.DispatchAsync(notification, CancellationToken.None);

        // Assert
        Assert.True(handler.WasCalled);
    }

    [Fact]
    public async Task FallbackNotificationDispatcher_DispatchAsync_WithHandlerException_WrapsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<TestNotification>, ThrowingTestNotificationHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackNotificationDispatcher(serviceProvider);
        var notification = new TestNotification { Message = "Test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RelayException>(() =>
            dispatcher.DispatchAsync(notification, CancellationToken.None).AsTask());

        Assert.Equal("TestNotification", exception.RequestType);
        Assert.Contains("Test notification handler exception", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task FallbackNotificationDispatcher_DispatchAsync_WithNullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackNotificationDispatcher(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.DispatchAsync<TestNotification>(null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public void FallbackNotificationDispatcher_Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FallbackNotificationDispatcher(null!));
    }

    [Fact]
    public async Task FallbackNotificationDispatcher_DispatchAsync_WithMultipleHandlers_OneThrowsException_WrapsException()
    {
        // Arrange
        var handler1 = new TestNotificationHandler();
        var handler2 = new ThrowingTestNotificationHandler();
        var handler3 = new TestNotificationHandler();

        var services = new ServiceCollection();
        services.AddSingleton<INotificationHandler<TestNotification>>(handler1);
        services.AddSingleton<INotificationHandler<TestNotification>>(handler2);
        services.AddSingleton<INotificationHandler<TestNotification>>(handler3);

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackNotificationDispatcher(serviceProvider);
        var notification = new TestNotification { Message = "Test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RelayException>(() =>
            dispatcher.DispatchAsync(notification, CancellationToken.None).AsTask());

        Assert.Equal("TestNotification", exception.RequestType);
        Assert.Contains("Test notification handler exception", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    // Test classes
    private class TestNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }

    private class TestNotificationHandler : INotificationHandler<TestNotification>
    {
        public bool WasCalled { get; private set; }

        public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return ValueTask.CompletedTask;
        }
    }

    private class ThrowingTestNotificationHandler : INotificationHandler<TestNotification>
    {
        public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test notification handler exception");
        }
    }
}
