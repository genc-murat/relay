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
    public class StreamDispatcherHandlerNameTests
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
            await Assert.ThrowsAsync<ArgumentException>(act);
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
            await Assert.ThrowsAsync<ArgumentException>(act);
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
            await Assert.ThrowsAsync<ArgumentException>(act);
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
            await Assert.ThrowsAsync<HandlerNotFoundException>(act);
        }
    }
}