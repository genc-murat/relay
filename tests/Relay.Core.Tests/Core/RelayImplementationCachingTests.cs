using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Relay.Core;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Core;
using Relay.Core.Testing;

namespace Relay.Core.Tests.Core;

/// <summary>
/// Tests for RelayImplementation caching and dispatcher resolution
/// </summary>
public class RelayImplementationCachingTests
{
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

    // Helper method
    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Delay(1);
            yield return item;
        }
    }
}
