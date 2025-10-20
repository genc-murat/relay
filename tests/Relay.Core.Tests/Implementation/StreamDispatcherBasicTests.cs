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
    public class StreamDispatcherBasicTests
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

        private IServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();
            var testHandler = new TestStreamHandler();

            services.AddSingleton<TestStreamHandler>(testHandler);
            services.AddSingleton<IStreamHandler<IStreamRequest<int>, int>>(sp => testHandler);

            return services.BuildServiceProvider();
        }

        [Fact]
        public void StreamDispatcher_Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => new StreamDispatcher(null!);

            // Assert
            var ex = Assert.Throws<ArgumentNullException>(act);
            Assert.Equal("serviceProvider", ex.ParamName);
        }

        [Fact]
        public void StreamDispatcher_Constructor_WithValidServiceProvider_ShouldSucceed()
        {
            // Arrange
            var serviceProvider = CreateServiceProvider();

            // Act
            var dispatcher = new StreamDispatcher(serviceProvider);

            // Assert
            Assert.NotNull(dispatcher);
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
            await Assert.ThrowsAsync<ArgumentNullException>(act);
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
            await Assert.ThrowsAsync<HandlerNotFoundException>(act);
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
            await Assert.ThrowsAsync<OperationCanceledException>(act);
        }
    }
}