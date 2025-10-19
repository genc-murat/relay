using System;
using System.Collections.Generic;
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

namespace Relay.Core.Tests.Core;

/// <summary>
/// Tests for RelayImplementation StreamAsync method
/// </summary>
public class RelayImplementationStreamAsyncTests
{
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
    public async Task StreamAsync_WithMemoryPrefetch_WhenEnabledAndSupported()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.EnableMemoryPrefetch = true;
        });

        var mockDispatcher = new Mock<IStreamDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<TestStreamRequest>(), It.IsAny<CancellationToken>()))
            .Returns(CreateEmptyAsyncEnumerable<string>());

        services.AddSingleton<IStreamDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestStreamRequest();

        // Act
        await foreach (var item in relay.StreamAsync(request))
        {
            // Should not reach here as we return empty enumerable
            Assert.Fail("Should not have any items");
        }

        // Assert
        mockDispatcher.Verify(x => x.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
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

    private static async IAsyncEnumerable<T> CreateEmptyAsyncEnumerable<T>()
    {
        await Task.CompletedTask;
        yield break;
    }
}