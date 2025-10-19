using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Implementation.Fallback;

namespace Relay.Core.Tests.Dispatchers;

/// <summary>
/// Tests for FallbackStreamDispatcher functionality
/// </summary>
public class FallbackStreamDispatcherTests
{
    [Fact]
    public async Task FallbackStreamDispatcher_DispatchAsync_WithRegisteredHandler_CallsHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IStreamHandler<TestStreamRequest, string>, TestStreamHandler>();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackStreamDispatcher(serviceProvider);
        var request = new TestStreamRequest { Count = 3 };

        // Act
        var results = new List<string>();
        await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("Item 0", results[0]);
        Assert.Equal("Item 1", results[1]);
        Assert.Equal("Item 2", results[2]);
    }

    [Fact]
    public async Task FallbackStreamDispatcher_DispatchAsync_WithoutHandler_ThrowsHandlerNotFoundException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackStreamDispatcher(serviceProvider);
        var request = new TestStreamRequest { Count = 3 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
        {
            await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
            {
                // Should not reach here
            }
        });

        Assert.Contains("TestStreamRequest", exception.RequestType);
    }

    [Fact]
    public void FallbackStreamDispatcher_DispatchAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackStreamDispatcher(serviceProvider);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            dispatcher.DispatchAsync<string>(null!, CancellationToken.None));
    }

    [Fact]
    public async Task FallbackStreamDispatcher_DispatchAsync_WithHandlerName_ThrowsHandlerNotFoundException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackStreamDispatcher(serviceProvider);
        var request = new TestStreamRequest { Count = 3 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
        {
            await foreach (var item in dispatcher.DispatchAsync(request, "namedHandler", CancellationToken.None))
            {
                // Should not reach here
            }
        });

        Assert.Contains("TestStreamRequest", exception.RequestType);
        Assert.Equal("namedHandler", exception.HandlerName);
    }

    // Test classes
    private class TestStreamRequest : IStreamRequest<string>
    {
        public int Count { get; set; }
    }

    private class TestStreamHandler : IStreamHandler<TestStreamRequest, string>
    {
        public async IAsyncEnumerable<string> HandleAsync(TestStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return $"Item {i}";
                await Task.Yield();
            }
        }
    }
}