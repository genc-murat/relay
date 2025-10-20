using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Relay.Core;
using Relay.Core.Configuration.Options;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Core;
using Relay.Core.Tests.Testing;
using Relay.Core.Configuration.Options.Core;

namespace Relay.Core.Tests.Core;

/// <summary>
/// Tests for RelayImplementation PublishAsync method
/// </summary>
public class RelayImplementationPublishAsyncTests
{
    [Fact]
    public async Task PublishAsync_WithValidNotification_ShouldCallDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<INotificationDispatcher>();

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var notification = new TestNotification();

        // Act
        await relay.PublishAsync(notification);

        // Assert
        mockDispatcher.Verify(d => d.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithNullNotification_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            relay.PublishAsync<TestNotification>(null!).AsTask());
    }

    [Fact]
    public async Task PublishAsync_WithNoDispatcher_ShouldCompleteSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var notification = new TestNotification();

        // Act & Assert - Should not throw
        await relay.PublishAsync(notification);
    }

    [Fact]
    public async Task PublishAsync_WithCancellationToken_ShouldPassTokenToDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<INotificationDispatcher>();
        var cancellationToken = new CancellationToken(true);

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestNotification>(), cancellationToken))
            .ThrowsAsync(new OperationCanceledException());

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var notification = new TestNotification();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            relay.PublishAsync(notification, cancellationToken).AsTask());
    }

    [Fact]
    public async Task PublishAsync_WithNullNotification_UsesPreAllocatedExceptions_WhenEnabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.UsePreAllocatedExceptions = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            relay.PublishAsync<TestNotification>(null!).AsTask());
    }

    [Fact]
    public async Task PublishAsync_WithMemoryPrefetch_WhenEnabledAndSupported()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.EnableMemoryPrefetch = true;
        });

        var mockDispatcher = new Mock<INotificationDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        services.AddSingleton<INotificationDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var notification = new TestNotification();

        // Act
        await relay.PublishAsync(notification);

        // Assert
        mockDispatcher.Verify(x => x.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithDispatcherCachingDisabled_ResolvesDispatcherEachTime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.CacheDispatchers = false;
        });

        var mockDispatcher = new Mock<INotificationDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        services.AddSingleton<INotificationDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var notification = new TestNotification();

        // Act
        await relay.PublishAsync(notification);
        await relay.PublishAsync(notification);

        // Assert
        mockDispatcher.Verify(x => x.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}