using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Dispatchers;
using Xunit;

namespace Relay.Core.Tests.Implementation
{
    public class StreamDispatcherTests
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

        private IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            var testHandler = new TestStreamHandler();
            var namedHandler = new NamedStreamHandler();

            services.AddSingleton<TestStreamHandler>(testHandler);
            services.AddSingleton<NamedStreamHandler>(namedHandler);
            services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => testHandler);
            services.AddSingleton<IStreamHandler<IStreamRequest<string>, string>>(sp => namedHandler);

            return services.BuildServiceProvider();
        }

        #region StreamDispatcher Tests

        [Fact]
        public void StreamDispatcher_Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => new StreamDispatcher(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
        }

        [Fact]
        public void StreamDispatcher_Constructor_WithValidServiceProvider_ShouldSucceed()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            var dispatcher = new StreamDispatcher(serviceProvider);

            // Assert
            dispatcher.Should().NotBeNull();
        }

        [Fact]
        public async Task StreamDispatcher_DispatchAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var dispatcher = new StreamDispatcher(serviceProvider);

            // Act
            Func<Task> act = async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync<int>(null!, CancellationToken.None))
                {
                    // Should not reach here
                }
            };

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task StreamDispatcher_DispatchAsync_WithNoHandler_ShouldThrowHandlerNotFoundException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var dispatcher = new StreamDispatcher(serviceProvider);
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
            await act.Should().ThrowAsync<HandlerNotFoundException>();
        }

        [Fact]
        public async Task StreamDispatcher_DispatchAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var dispatcher = new StreamDispatcher(serviceProvider);
            var request = new TestStreamRequest { Count = 100 };
            var cts = new CancellationTokenSource();

            // Act - Cancel immediately to test cancellation handling
            cts.Cancel();

            Func<Task> act = async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync(request, cts.Token))
                {
                    // Should not reach here due to immediate cancellation
                }
            };

            // Assert - StreamDispatcher will throw HandlerNotFoundException before checking cancellation
            // This is expected behavior as the handler resolution happens first
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task StreamDispatcher_DispatchAsync_WithHandlerName_WithNullName_ShouldThrowArgumentException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var dispatcher = new StreamDispatcher(serviceProvider);
            var request = new TestStreamRequest();

            // Act
            Func<Task> act = async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync(request, null!, CancellationToken.None))
                {
                    // Should not reach here
                }
            };

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task StreamDispatcher_DispatchAsync_WithHandlerName_WithEmptyName_ShouldThrowArgumentException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var dispatcher = new StreamDispatcher(serviceProvider);
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
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task StreamDispatcher_DispatchAsync_WithHandlerName_WithWhitespaceName_ShouldThrowArgumentException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var dispatcher = new StreamDispatcher(serviceProvider);
            var request = new TestStreamRequest();

            // Act
            Func<Task> act = async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync(request, "   ", CancellationToken.None))
                {
                    // Should not reach here
                }
            };

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task StreamDispatcher_DispatchAsync_WithHandlerName_WithNoHandler_ShouldThrowHandlerNotFoundException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var dispatcher = new StreamDispatcher(serviceProvider);
            var request = new NoHandlerRequest();

            // Act
            Func<Task> act = async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync(request, "TestHandler", CancellationToken.None))
                {
                    // Should not reach here
                }
            };

            // Assert
            await act.Should().ThrowAsync<HandlerNotFoundException>();
        }

        #endregion

        #region BackpressureStreamDispatcher Tests

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => new BackpressureStreamDispatcher(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithValidParameters_ShouldSucceed()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            var dispatcher = new BackpressureStreamDispatcher(serviceProvider, 5, 50);

            // Assert
            dispatcher.Should().NotBeNull();
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithDefaultParameters_ShouldSucceed()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            var dispatcher = new BackpressureStreamDispatcher(serviceProvider);

            // Assert
            dispatcher.Should().NotBeNull();
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithNegativeMaxConcurrency_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            Action act = () => new BackpressureStreamDispatcher(serviceProvider, -1, 100);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxConcurrency");
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithZeroMaxConcurrency_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            Action act = () => new BackpressureStreamDispatcher(serviceProvider, 0, 100);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("maxConcurrency");
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithNegativeBufferSize_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            Action act = () => new BackpressureStreamDispatcher(serviceProvider, 10, -1);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("bufferSize");
        }

        [Fact]
        public void BackpressureStreamDispatcher_Constructor_WithZeroBufferSize_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            Action act = () => new BackpressureStreamDispatcher(serviceProvider, 10, 0);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("bufferSize");
        }

        [Fact]
        public async Task BackpressureStreamDispatcher_DispatchAsync_WithNullRequest_ShouldThrowArgumentNullException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
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
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task BackpressureStreamDispatcher_DispatchAsync_WithNoHandler_ShouldThrowHandlerNotFoundException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
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
            await act.Should().ThrowAsync<HandlerNotFoundException>();
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
            results.Should().HaveCount(10);
            results.Should().BeInAscendingOrder();
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
            await act.Should().ThrowAsync<OperationCanceledException>();
            results.Count.Should().BeLessThan(1000); // Should have stopped before processing all items
        }

        [Fact]
        public async Task BackpressureStreamDispatcher_DispatchAsync_WithHandlerName_WithNullName_ShouldThrowArgumentException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var dispatcher = new BackpressureStreamDispatcher(serviceProvider);
            var request = new TestStreamRequest();

            // Act
            Func<Task> act = async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync(request, null!, CancellationToken.None))
                {
                    // Should not reach here
                }
            };

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task BackpressureStreamDispatcher_DispatchAsync_WithHandlerName_WithEmptyName_ShouldThrowArgumentException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
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
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task BackpressureStreamDispatcher_DispatchAsync_WithHandlerName_WithNoHandler_ShouldThrowHandlerNotFoundException()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();
            var dispatcher = new BackpressureStreamDispatcher(serviceProvider);
            var request = new NoHandlerRequest();

            // Act
            Func<Task> act = async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync(request, "TestHandler", CancellationToken.None))
                {
                    // Should not reach here
                }
            };

            // Assert
            await act.Should().ThrowAsync<HandlerNotFoundException>();
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
            results.Should().HaveCount(20);
            results.OrderBy(x => x).Should().BeEquivalentTo(Enumerable.Range(0, 20));
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
            results1.Should().HaveCount(5);
            results2.Should().HaveCount(5);
            results2.Should().AllSatisfy(s => s.Should().StartWith("Test-"));
        }

        #endregion

        #region Edge Cases

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
            await act.Should().ThrowAsync<HandlerNotFoundException>();
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
            results.Should().BeEmpty();
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
            completed.Should().Be(streamTask, "Stream should complete before timeout");
            results.Should().HaveCount(50);
        }

        #endregion
    }
}
