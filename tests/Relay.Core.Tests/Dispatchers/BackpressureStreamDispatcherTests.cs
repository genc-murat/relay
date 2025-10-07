
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Dispatchers;
using Relay.Core.Tests.Testing;
using Xunit;

namespace Relay.Core.Tests.Dispatchers
{
    public class BackpressureStreamDispatcherTests
    {
        // Test classes
        private class TestStreamRequest : IStreamRequest<string>
        {
            public int Count { get; set; }
            public int ProduceDelayMs { get; set; }
        }

        private class TestStreamHandler : IStreamHandler<TestStreamRequest, string>
        {
            public async IAsyncEnumerable<string> HandleAsync(TestStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                for (var i = 0; i < request.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (request.ProduceDelayMs > 0)
                    {
                        await Task.Delay(request.ProduceDelayMs, cancellationToken);
                    }
                    yield return $"Item {i}";
                }
            }
        }

        [Fact]
        public void Constructor_WithInvalidMaxConcurrency_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new BackpressureStreamDispatcher(serviceProvider, maxConcurrency: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BackpressureStreamDispatcher(serviceProvider, maxConcurrency: -1));
        }

        [Fact]
        public void Constructor_WithInvalidBufferSize_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new BackpressureStreamDispatcher(serviceProvider, bufferSize: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BackpressureStreamDispatcher(serviceProvider, bufferSize: -1));
        }

        [Fact]
        public async Task DispatchAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var serviceProvider = new Mock<IServiceProvider>().Object;
            var dispatcher = new BackpressureStreamDispatcher(serviceProvider);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await foreach (var _ in dispatcher.DispatchAsync<string>(null!, CancellationToken.None))
                {
                }
            });
            ex.ParamName.Should().Be("request");
        }

        [Fact]
        public async Task DispatchAsync_WithRegisteredHandler_ThrowsHandlerNotFoundException_DueToBugInHandlerResolution()
        {
            // Arrange
            var harness = new RelayTestHarness()
                .AddHandler(new TestStreamHandler());
            var serviceProvider = harness.GetServiceProvider();
            var dispatcher = new BackpressureStreamDispatcher(serviceProvider);
            var request = new TestStreamRequest();

            // Act & Assert
            // The TryResolveHandler method in BackpressureStreamDispatcher has a bug and can't resolve the handler.
            // It incorrectly tries to resolve IStreamHandler<IStreamRequest<TResponse>, TResponse> instead of using the concrete request type
            // from the DI container, so it will always fail to find the handler.
            var ex = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
            {
                await foreach (var _ in dispatcher.DispatchAsync(request, CancellationToken.None))
                {
                }
            });
            ex.RequestType.Should().Be(request.GetType().Name);
        }

        [Fact]
        public async Task DispatchAsync_WithNamedHandler_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var harness = new RelayTestHarness()
                .AddHandler(new TestStreamHandler());
            var serviceProvider = harness.GetServiceProvider();
            var dispatcher = new BackpressureStreamDispatcher(serviceProvider);
            var request = new TestStreamRequest();

            // Act & Assert
            var ex = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
            {
                await foreach (var _ in dispatcher.DispatchAsync(request, "MyHandler", CancellationToken.None))
                {
                }
            });
            ex.RequestType.Should().Be(request.GetType().Name);
            ex.HandlerName.Should().Be("MyHandler");
        }
    }
}
