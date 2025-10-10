using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Core;
using Relay.Core.Tests.Testing;
using Xunit;

namespace Relay.Core.Tests.Core;

/// <summary>
/// Comprehensive unit tests for RelayImplementation
/// </summary>
public class RelayImplementationUnitTests
{
    [Fact]
    public void Constructor_WithValidServiceProvider_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // Assert
        Assert.NotNull(relay);
        Assert.IsAssignableFrom<IRelay>(relay);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RelayImplementation(null!));
    }

    [Fact]
    public async Task SendAsync_WithValidRequest_ShouldCallDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var expectedResponse = "test response";

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(expectedResponse));

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest<string>();

        // Act
        var result = await relay.SendAsync(request);

        // Assert
        Assert.Equal(expectedResponse, result);
        mockDispatcher.Verify(d => d.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithVoidRequest_ShouldCallDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var request = new TestVoidRequest();

        // Act
        await relay.SendAsync(request);

        // Assert
        mockDispatcher.Verify(d => d.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            relay.SendAsync<string>(null!).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithNullVoidRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            relay.SendAsync((IRequest)null!).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithNoDispatcher_ShouldThrowHandlerNotFoundException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest<string>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() =>
            relay.SendAsync(request).AsTask());

        Assert.Contains("TestRequest", exception.RequestType);
    }

    [Fact]
    public async Task StreamAsync_WithValidRequest_ShouldCallDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<IStreamDispatcher>();
        var expectedItems = new[] { 1, 2, 3 };

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IStreamRequest<int>>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(expectedItems));

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var request = new TestStreamRequest<int>();

        // Act
        var results = new List<int>();
        await foreach (var item in relay.StreamAsync(request))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(expectedItems, results);
        mockDispatcher.Verify(d => d.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void StreamAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            relay.StreamAsync<int>(null!));
    }

    [Fact]
    public async Task StreamAsync_WithNoDispatcher_ShouldThrowHandlerNotFoundException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestStreamRequest<int>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
        {
            await foreach (var item in relay.StreamAsync(request))
            {
                // Should not reach here
            }
        });

        Assert.Contains("TestStreamRequest", exception.RequestType);
    }

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
    public async Task SendAsync_WithCancellationToken_ShouldPassTokenToDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var cancellationToken = new CancellationToken(true);

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), cancellationToken))
            .ThrowsAsync(new OperationCanceledException());

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest<string>();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            relay.SendAsync(request, cancellationToken).AsTask());
    }

    [Fact]
    public async Task StreamAsync_WithCancellationToken_ShouldPassTokenToDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<IStreamDispatcher>();
        var cancellationToken = new CancellationToken(true);

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IStreamRequest<int>>(), cancellationToken))
            .Returns(CreateCancelledAsyncEnumerable<int>());

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var request = new TestStreamRequest<int>();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in relay.StreamAsync(request, cancellationToken))
            {
                // Should not reach here
            }
        });
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
    public async Task RelayImplementation_WithMultipleDispatchers_ShouldUseCorrectDispatcher()
    {
        // Arrange
        var mockRequestDispatcher = new Mock<IRequestDispatcher>();
        var mockStreamDispatcher = new Mock<IStreamDispatcher>();
        var mockNotificationDispatcher = new Mock<INotificationDispatcher>();

        mockRequestDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult("request result"));

        mockStreamDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IStreamRequest<int>>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(new[] { 1, 2, 3 }));

        mockNotificationDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(mockRequestDispatcher.Object);
        services.AddSingleton(mockStreamDispatcher.Object);
        services.AddSingleton(mockNotificationDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);

        // Act
        var requestResult = await relay.SendAsync(new TestRequest<string>());

        var streamResults = new List<int>();
        await foreach (var item in relay.StreamAsync(new TestStreamRequest<int>()))
        {
            streamResults.Add(item);
        }

        await relay.PublishAsync(new TestNotification());

        // Assert
        Assert.Equal("request result", requestResult);
        Assert.Equal(new[] { 1, 2, 3 }, streamResults);

        mockRequestDispatcher.Verify(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        mockStreamDispatcher.Verify(d => d.DispatchAsync(It.IsAny<IStreamRequest<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        mockNotificationDispatcher.Verify(d => d.DispatchAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Helper methods
    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Delay(1);
            yield return item;
        }
    }

    private static async IAsyncEnumerable<T> CreateCancelledAsyncEnumerable<T>()
    {
        await Task.Delay(1);
        throw new OperationCanceledException();
#pragma warning disable CS0162 // Unreachable code detected
        yield break;
#pragma warning restore CS0162 // Unreachable code detected
    }

    // Test classes
    private class TestVoidRequest : IRequest
    {
    }
}