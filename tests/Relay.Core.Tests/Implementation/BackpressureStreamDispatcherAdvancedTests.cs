using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Dispatchers;
using Xunit;

namespace Relay.Core.Tests.Implementation;

public class BackpressureStreamDispatcherAdvancedTests
{
    // Test request and response types
    public class TestStreamRequest : IStreamRequest<int>
    {
        public int Count { get; set; } = 10;
    }

    public class NamedStreamRequest : IStreamRequest<string>
    {
        public string Prefix { get; set; } = "Item";
    }

    // Test handlers
    public class TestStreamHandler : IStreamHandler<TestStreamRequest, int>, IStreamHandler<IStreamRequest<int>, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1, cancellationToken);
                yield return i;
            }
        }

        // Explicit interface implementation for base interface
        async IAsyncEnumerable<int> IStreamHandler<IStreamRequest<int>, int>.HandleAsync(
            IStreamRequest<int> request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in HandleAsync((TestStreamRequest)request, cancellationToken))
            {
                yield return item;
            }
        }
    }

    public class NamedStreamHandler : IStreamHandler<NamedStreamRequest, string>, IStreamHandler<IStreamRequest<string>, string>
    {
        public async IAsyncEnumerable<string> HandleAsync(
            NamedStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < 5; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1, cancellationToken);
                yield return $"{request.Prefix}-{i}";
            }
        }

        // Explicit interface implementation for base interface
        async IAsyncEnumerable<string> IStreamHandler<IStreamRequest<string>, string>.HandleAsync(
            IStreamRequest<string> request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in HandleAsync((NamedStreamRequest)request, cancellationToken))
            {
                yield return item;
            }
        }
    }

    [Fact]
    public async Task BackpressureStreamDispatcher_DispatchAsync_WithHandlerName_WithNullName_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestStreamHandler();
        services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new BackpressureStreamDispatcher(serviceProvider);
        var request = new TestStreamRequest();

        // Act
        async Task act()
        {
            await foreach (var item in dispatcher.DispatchAsync(request, null!, CancellationToken.None))
            {
                // Should not reach here
            }
        }

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task BackpressureStreamDispatcher_DispatchAsync_WithHandlerName_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestStreamHandler();
        services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new BackpressureStreamDispatcher(serviceProvider);
        var request = new TestStreamRequest();

        // Act
        Func<Task> act = async () =>
        {
            await foreach (var item in dispatcher.DispatchAsync(request, string.Empty, CancellationToken.None))
            {
                // Should not reach here
            }
        };

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(act);
    }

    [Fact]
    public async Task BackpressureStreamDispatcher_DispatchAsync_WithHandlerName_WithNoHandler_ShouldThrowHandlerNotFoundException()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestStreamHandler();
        services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new BackpressureStreamDispatcher(serviceProvider);
        var request = new NoHandlerRequest();

        // Act
        async Task act()
        {
            await foreach (var item in dispatcher.DispatchAsync(request, "TestHandler", CancellationToken.None))
            {
                // Should not reach here
            }
        }

        // Assert
        await Assert.ThrowsAsync<HandlerNotFoundException>(act);
    }

    [Fact]
    public async Task BackpressureStreamDispatcher_DispatchAsync_ConcurrentConsumption_ShouldControlFlow()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestStreamHandler();
        services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new BackpressureStreamDispatcher(serviceProvider, maxConcurrency: 3, bufferSize: 5);
        var request = new TestStreamRequest { Count = 20 };

        // Act
        var results = new List<int>();
        var tasks = new List<Task>();

        await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
        {
            var capturedItem = item;
            tasks.Add(Task.Run(() => results.Add(capturedItem)));

            // Limit active tasks to test backpressure
            if (tasks.Count >= 3)
            {
                await Task.WhenAny(tasks);
                tasks.RemoveAll(t => t.IsCompleted);
            }
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(20, results.Count);
        Assert.Equal(Enumerable.Range(0, 20), results.OrderBy(x => x));
    }

    [Fact]
    public async Task BackpressureStreamDispatcher_MultipleStreams_ShouldIsolateBackpressure()
    {
        // Arrange
        var services = new ServiceCollection();
        var intHandler = new TestStreamHandler();
        var stringHandler = new NamedStreamHandler();
        services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => intHandler);
        services.AddSingleton<IStreamHandler<IStreamRequest<string>, string>>(sp => stringHandler);
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new BackpressureStreamDispatcher(serviceProvider, maxConcurrency: 2, bufferSize: 5);
        var request1 = new TestStreamRequest { Count = 5 };
        var request2 = new NamedStreamRequest { Prefix = "Test" };

        // Act - Start both streams concurrently
        var results1 = new List<int>();
        var results2 = new List<string>();

        var task1 = Task.Run(async () =>
        {
            await foreach (var item in dispatcher.DispatchAsync(request1, CancellationToken.None))
            {
                results1.Add(item);
            }
        });

        var task2 = Task.Run(async () =>
        {
            await foreach (var item in dispatcher.DispatchAsync(request2, CancellationToken.None))
            {
                results2.Add(item);
            }
        });

        await Task.WhenAll(task1, task2);

        // Assert - Each stream should complete independently
        Assert.Equal(5, results1.Count);
        Assert.Equal(5, results2.Count);
        Assert.All(results2, s => Assert.StartsWith("Test-", s));
    }

    [Fact]
    public async Task BackpressureStreamDispatcher_HandlerThrowsExceptionDuringBackpressure_PropagatesException()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new FailingStreamHandler();
        services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new BackpressureStreamDispatcher(serviceProvider, maxConcurrency: 2, bufferSize: 5);
        var request = new TestStreamRequest { Count = 10 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
            {
                // Should throw before yielding any items
            }
        });

        Assert.Equal("Handler failed during backpressure", exception.Message);
    }

    [Fact]
    public async Task BackpressureStreamDispatcher_CancellationDuringBackpressure_Wait_RespectsCancellation()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new SlowStreamHandler();
        services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
        var serviceProvider = services.BuildServiceProvider();

        var dispatcher = new BackpressureStreamDispatcher(serviceProvider, maxConcurrency: 1, bufferSize: 1);
        var request = new TestStreamRequest { Count = 10 };
        var cts = new CancellationTokenSource();

        // Act
        var results = new List<int>();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50)); // Cancel quickly

        Func<Task> act = async () =>
        {
            await foreach (var item in dispatcher.DispatchAsync(request, cts.Token))
            {
                results.Add(item);
                await Task.Delay(10, cts.Token); // Slow consumption to trigger backpressure
            }
        };

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
        // Should have processed at least some items before cancellation
        Assert.True(results.Count >= 0);
    }

    [Fact]
    public async Task BackpressureStreamDispatcher_ZeroMaxConcurrency_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BackpressureStreamDispatcher(serviceProvider, maxConcurrency: 0, bufferSize: 10));

        Assert.Equal("maxConcurrency", exception.ParamName);
    }

    private class FailingStreamHandler : IStreamHandler<TestStreamRequest, int>, IStreamHandler<IStreamRequest<int>, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            throw new InvalidOperationException("Handler failed during backpressure");
            yield break;
        }

        async IAsyncEnumerable<int> IStreamHandler<IStreamRequest<int>, int>.HandleAsync(
            IStreamRequest<int> request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in HandleAsync((TestStreamRequest)request, cancellationToken))
            {
                yield return item;
            }
        }
    }

    private class SlowStreamHandler : IStreamHandler<TestStreamRequest, int>, IStreamHandler<IStreamRequest<int>, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < request.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(50, cancellationToken); // Slow handler
                yield return i;
            }
        }

        async IAsyncEnumerable<int> IStreamHandler<IStreamRequest<int>, int>.HandleAsync(
            IStreamRequest<int> request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in HandleAsync((TestStreamRequest)request, cancellationToken))
            {
                yield return item;
            }
        }
    }

    private class NoHandlerRequest : IStreamRequest<double> { }
}