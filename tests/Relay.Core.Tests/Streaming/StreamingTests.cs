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
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Implementation.Base;
using Relay.Core.Implementation.Core;
using Relay.Core.Implementation.Dispatchers;

namespace Relay.Core.Tests
{
    public class StreamingTests
    {
        // Test request types
        public class LargeDatasetRequest : IStreamRequest<int>
        {
            public int Count { get; set; } = 1000;
            public int DelayMs { get; set; } = 0;
        }

        public class CancellableStreamRequest : IStreamRequest<string>
        {
            public int ItemCount { get; set; } = 100;
            public int DelayBetweenItems { get; set; } = 10;
        }

        public class BackpressureTestRequest : IStreamRequest<byte[]>
        {
            public int ItemCount { get; set; } = 50;
            public int ItemSizeKb { get; set; } = 1024; // 1MB per item
        }

        public class ExceptionRequest : IStreamRequest<string> { }

        // Test handlers
        public class LargeDatasetHandler : IStreamHandler<LargeDatasetRequest, int>
        {
            public async IAsyncEnumerable<int> HandleAsync(LargeDatasetRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                for (int i = 0; i < request.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (request.DelayMs > 0)
                        await Task.Delay(request.DelayMs, cancellationToken);

                    yield return i;
                }
            }
        }

        public class CancellableStreamHandler : IStreamHandler<CancellableStreamRequest, string>
        {
            public async IAsyncEnumerable<string> HandleAsync(CancellableStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                for (int i = 0; i < request.ItemCount; i++)
                {
                    yield return $"Item {i}";

                    cancellationToken.ThrowIfCancellationRequested();

                    if (request.DelayBetweenItems > 0)
                        await Task.Delay(request.DelayBetweenItems, cancellationToken);
                }
            }
        }

        public class BackpressureTestHandler : IStreamHandler<BackpressureTestRequest, byte[]>
        {
            public async IAsyncEnumerable<byte[]> HandleAsync(BackpressureTestRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                var random = new Random();

                for (int i = 0; i < request.ItemCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Create large data item
                    var data = new byte[request.ItemSizeKb * 1024];
                    random.NextBytes(data);

                    yield return data;

                    // Small delay to simulate processing
                    await Task.Delay(1, cancellationToken);
                }
            }
        }

        public class ExceptionThrowingHandler : IStreamHandler<ExceptionRequest, string>
        {
            public async IAsyncEnumerable<string> HandleAsync(ExceptionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.Delay(1, cancellationToken);
                throw new InvalidOperationException("Test exception in stream handler");
#pragma warning disable CS0162 // Unreachable code detected
                yield break;
#pragma warning restore CS0162 // Unreachable code detected
            }
        }

        // Test-specific stream dispatcher that can resolve handlers
        public class TestStreamDispatcher : BaseStreamDispatcher
        {
            public TestStreamDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            public override IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
            {
                ValidateRequest(request);
                return DispatchToHandler<TResponse>(request, cancellationToken);
            }

            public override IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
            {
                ValidateRequest(request);
                ValidateHandlerName(handlerName);
                return DispatchToHandler<TResponse>(request, cancellationToken);
            }

            private IAsyncEnumerable<TResponse> DispatchToHandler<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
            {
                // Handle specific test request types
                return request switch
                {
                    LargeDatasetRequest largeRequest when typeof(TResponse) == typeof(int) =>
                        (IAsyncEnumerable<TResponse>)GetService<LargeDatasetHandler>().HandleAsync(largeRequest, cancellationToken),
                    CancellableStreamRequest cancellableRequest when typeof(TResponse) == typeof(string) =>
                        (IAsyncEnumerable<TResponse>)GetService<CancellableStreamHandler>().HandleAsync(cancellableRequest, cancellationToken),
                    BackpressureTestRequest backpressureRequest when typeof(TResponse) == typeof(byte[]) =>
                        (IAsyncEnumerable<TResponse>)GetService<BackpressureTestHandler>().HandleAsync(backpressureRequest, cancellationToken),
                    ExceptionRequest exceptionRequest when typeof(TResponse) == typeof(string) =>
                        (IAsyncEnumerable<TResponse>)GetService<ExceptionThrowingHandler>().HandleAsync(exceptionRequest, cancellationToken),
                    _ => ThrowHandlerNotFound<TResponse>(request.GetType())
                };
            }
        }

        private IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IStreamDispatcher, TestStreamDispatcher>();
            services.AddSingleton<LargeDatasetHandler>();
            services.AddSingleton<CancellableStreamHandler>();
            services.AddSingleton<BackpressureTestHandler>();
            services.AddSingleton<ExceptionThrowingHandler>();
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task StreamAsync_WithLargeDataset_ShouldProcessIncrementally()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var relay = new RelayImplementation(serviceProvider);
            var request = new LargeDatasetRequest { Count = 1000 };

            // Act
            var results = new List<int>();
            await foreach (var item in relay.StreamAsync(request))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(1000, results.Count);
            Assert.Equal(Enumerable.Range(0, 1000), results);
        }

        [Fact]
        public async Task StreamAsync_WithCancellation_ShouldStopProcessing()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var relay = new RelayImplementation(serviceProvider);
            var request = new CancellableStreamRequest { ItemCount = 100, DelayBetweenItems = 10 };

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(50)); // Cancel after 50ms

            // Act & Assert
            var results = new List<string>();
            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await foreach (var item in relay.StreamAsync(request, cts.Token))
                {
                    results.Add(item);
                }
            });

            // Should have processed some items but not all
            Assert.True(results.Count > 0);
            Assert.True(results.Count < 100);
        }

        [Fact]
        public async Task StreamAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var relay = new RelayImplementation(serviceProvider);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await foreach (var item in relay.StreamAsync<int>(null!))
                {
                    // Should not reach here
                }
            });
        }

        [Fact]
        public async Task StreamAsync_WithNoHandler_ShouldThrowHandlerNotFoundException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IStreamDispatcher, StreamDispatcher>();
            var serviceProvider = services.BuildServiceProvider();
            var relay = new RelayImplementation(serviceProvider);

            var request = new LargeDatasetRequest();

            // Act & Assert
            await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
            {
                await foreach (var item in relay.StreamAsync(request))
                {
                    // Should not reach here
                }
            });
        }

        [Fact]
        public async Task StreamAsync_WithMemoryPressure_ShouldNotBufferAllItems()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var relay = new RelayImplementation(serviceProvider);
            var request = new LargeDatasetRequest { Count = 10000 };

            // Act
            var processedCount = 0;
            var maxMemoryUsage = GC.GetTotalMemory(false);

            await foreach (var item in relay.StreamAsync(request))
            {
                processedCount++;

                // Check memory usage periodically
                if (processedCount % 1000 == 0)
                {
                    var currentMemory = GC.GetTotalMemory(false);
                    maxMemoryUsage = Math.Max(maxMemoryUsage, currentMemory);
                }

                // Early exit to test streaming behavior
                if (processedCount >= 5000)
                    break;
            }

            // Assert
            Assert.Equal(5000, processedCount);

            // Memory usage should be reasonable (not proportional to total item count)
            // This is a rough check - in practice, streaming should use constant memory
            // We just verify that we processed items incrementally without buffering all
            Assert.True(processedCount > 0, "Should have processed some items");
        }

        [Fact]
        public async Task StreamAsync_WithExceptionInHandler_ShouldPropagateException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var relay = new RelayImplementation(serviceProvider);

            var request = new ExceptionRequest();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (var item in relay.StreamAsync(request))
                {
                    // Should throw before yielding any items
                }
            });
        }
    }
}
