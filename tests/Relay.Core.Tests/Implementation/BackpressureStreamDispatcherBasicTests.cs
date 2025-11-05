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

namespace Relay.Core.Tests.Implementation
{
    public class BackpressureStreamDispatcherBasicTests
    {
        // Test request and response types
        public class TestStreamRequest : IStreamRequest<int>
        {
            public int Count { get; set; } = 10;
        }

        public class NoHandlerRequest : IStreamRequest<double> { }

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

        [Fact]
        public async Task BackpressureStreamDispatcher_DispatchAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler = new TestStreamHandler();
            services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new BackpressureStreamDispatcher(serviceProvider);

            // Act
            Func<Task> act = async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync<int>(null!, CancellationToken.None))
                {
                    // Should not reach here
                }
            };

            // Assert
            await Assert.ThrowsAsync<ArgumentNullException>(act);
        }

        [Fact]
        public async Task BackpressureStreamDispatcher_DispatchAsync_WithNoHandler_ShouldThrowHandlerNotFoundException()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler = new TestStreamHandler();
            services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new BackpressureStreamDispatcher(serviceProvider);
            var request = new NoHandlerRequest();

            // Act
            Func<Task> act = async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
                {
                    // Should not reach here
                }
            };

            // Assert
            await Assert.ThrowsAsync<HandlerNotFoundException>(act);
        }

        [Fact]
        public async Task BackpressureStreamDispatcher_DispatchAsync_WithValidRequest_ShouldApplyBackpressure()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler = new TestStreamHandler();
            // Register as the base interface type that TryResolveHandler expects
            services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
            var serviceProvider = services.BuildServiceProvider();

            // Use low concurrency to test backpressure
            var dispatcher = new BackpressureStreamDispatcher(serviceProvider, maxConcurrency: 2, bufferSize: 10);
            var request = new TestStreamRequest { Count = 10 };

            // Act
            var results = new List<int>();
            await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(10, results.Count);
            Assert.True(results.SequenceEqual(results.OrderBy(x => x)));
        }

        [Fact]
        public async Task BackpressureStreamDispatcher_DispatchAsync_WithCancellation_ShouldRespectCancellation()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler = new TestStreamHandler();
            services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
            var serviceProvider = services.BuildServiceProvider();

            var dispatcher = new BackpressureStreamDispatcher(serviceProvider);
            // Use a request with longer count and shorter delay per item to ensure we get some results before cancellation
            var request = new TestStreamRequest { Count = 1000 }; // More items
            var cts = new CancellationTokenSource();

            // Act
            var results = new List<int>();
            cts.CancelAfter(TimeSpan.FromMilliseconds(150)); // Give time for items to start processing

            Func<Task> act = async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync(request, cts.Token))
                {
                    results.Add(item);
                }
            };

            // Assert
            await Assert.ThrowsAnyAsync<OperationCanceledException>(act);
            Assert.True(results.Count < 1000); // Should have stopped before processing all items
        }
    }
}
