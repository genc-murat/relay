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
    public class StreamDispatcherEdgeCasesTests
    {
        // Test request and response types
        public class TestStreamRequest : IStreamRequest<int>
        {
            public int Count { get; set; } = 10;
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

        [Fact]
        public async Task StreamDispatcher_DispatchAsync_EmptyStream_ShouldThrowHandlerNotFoundException()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler = new TestStreamHandler();
            services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
            var serviceProvider = services.BuildServiceProvider();

            var dispatcher = new StreamDispatcher(serviceProvider);
            var request = new TestStreamRequest { Count = 0 };

            // Act
            Func<Task> act = async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
                {
                    // Should not reach here
                }
            };

            // Assert - StreamDispatcher uses PipelineExecutor which cannot resolve the handler
            await Assert.ThrowsAsync<HandlerNotFoundException>(act);
        }

        [Fact]
        public async Task BackpressureStreamDispatcher_DispatchAsync_EmptyStream_ShouldReturnEmpty()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler = new TestStreamHandler();
            services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
            var serviceProvider = services.BuildServiceProvider();

            var dispatcher = new BackpressureStreamDispatcher(serviceProvider);
            var request = new TestStreamRequest { Count = 0 };

            // Act
            var results = new List<int>();
            await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task BackpressureStreamDispatcher_HighConcurrency_ShouldNotDeadlock()
        {
            // Arrange
            var services = new ServiceCollection();
            var handler = new TestStreamHandler();
            services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => handler);
            var serviceProvider = services.BuildServiceProvider();

            var dispatcher = new BackpressureStreamDispatcher(serviceProvider, maxConcurrency: 100, bufferSize: 1000);
            var request = new TestStreamRequest { Count = 50 };

            // Act
            var results = new List<int>();
            var timeout = Task.Delay(TimeSpan.FromSeconds(5));
            var streamTask = Task.Run(async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
                {
                    results.Add(item);
                }
            });

            var completed = await Task.WhenAny(streamTask, timeout);

            // Assert
            Assert.Equal(streamTask, completed);
            Assert.Equal(50, results.Count);
        }
    }
}
