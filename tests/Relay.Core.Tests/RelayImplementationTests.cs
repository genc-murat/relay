using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Relay.Core;

namespace Relay.Core.Tests
{
    public class RelayImplementationTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RelayImplementation _relay;

        public RelayImplementationTests()
        {
            var services = new ServiceCollection();
            _serviceProvider = services.BuildServiceProvider();
            _relay = new RelayImplementation(_serviceProvider);
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RelayImplementation(null!));
        }

        [Fact]
        public async Task SendAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _relay.SendAsync<string>(null!).AsTask());
        }

        [Fact]
        public async Task SendAsync_WithoutResponse_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _relay.SendAsync((IRequest)null!).AsTask());
        }

        [Fact]
        public async Task SendAsync_WithNoDispatcher_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var request = new TestRequest();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() => 
                _relay.SendAsync(request).AsTask());
            
            Assert.Contains("IRequest`1", exception.RequestType);
        }

        [Fact]
        public async Task SendAsync_WithoutResponse_WithNoDispatcher_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var request = new TestVoidRequest();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() => 
                _relay.SendAsync(request).AsTask());
            
            Assert.Contains("IRequest", exception.RequestType);
        }

        [Fact]
        public void StreamAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _relay.StreamAsync<string>(null!));
        }

        [Fact]
        public async Task StreamAsync_WithNoDispatcher_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var request = new TestStreamRequest();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
            {
                await foreach (var item in _relay.StreamAsync(request))
                {
                    // Should not reach here
                }
            });
            
            Assert.Contains("IStreamRequest`1", exception.RequestType);
        }

        [Fact]
        public async Task PublishAsync_WithNullNotification_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _relay.PublishAsync<TestNotification>(null!).AsTask());
        }

        [Fact]
        public async Task PublishAsync_WithNoDispatcher_CompletesSuccessfully()
        {
            // Arrange
            var notification = new TestNotification();

            // Act & Assert - Should not throw
            await _relay.PublishAsync(notification);
        }

        [Fact]
        public void RelayImplementation_WithDispatcher_ResolvesCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IRequestDispatcher, MockRequestDispatcher>();
            services.AddSingleton<IStreamDispatcher, MockStreamDispatcher>();
            services.AddSingleton<INotificationDispatcher, MockNotificationDispatcher>();
            
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var relay = new RelayImplementation(serviceProvider);

            // Assert
            Assert.NotNull(relay);
        }

        [Fact]
        public async Task SendAsync_WithMockDispatcher_CallsDispatcher()
        {
            // Arrange
            var mockDispatcher = new MockRequestDispatcher();
            var services = new ServiceCollection();
            services.AddSingleton<IRequestDispatcher>(mockDispatcher);
            
            var serviceProvider = services.BuildServiceProvider();
            var relay = new RelayImplementation(serviceProvider);
            var request = new TestRequest();

            // Act
            await relay.SendAsync(request);

            // Assert
            Assert.True(mockDispatcher.DispatchAsyncCalled);
        }

        [Fact]
        public async Task PublishAsync_WithMockDispatcher_CallsDispatcher()
        {
            // Arrange
            var mockDispatcher = new MockNotificationDispatcher();
            var services = new ServiceCollection();
            services.AddSingleton<INotificationDispatcher>(mockDispatcher);
            
            var serviceProvider = services.BuildServiceProvider();
            var relay = new RelayImplementation(serviceProvider);
            var notification = new TestNotification();

            // Act
            await relay.PublishAsync(notification);

            // Assert
            Assert.True(mockDispatcher.DispatchAsyncCalled);
        }

        // Test classes
        private class TestRequest : IRequest<string>
        {
        }

        private class TestVoidRequest : IRequest
        {
        }

        private class TestStreamRequest : IStreamRequest<string>
        {
        }

        private class TestNotification : INotification
        {
        }

        // Mock dispatchers
        private class MockRequestDispatcher : IRequestDispatcher
        {
            public bool DispatchAsyncCalled { get; private set; }

            public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
            {
                DispatchAsyncCalled = true;
                return ValueTask.FromResult(default(TResponse)!);
            }

            public ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
            {
                DispatchAsyncCalled = true;
                return ValueTask.CompletedTask;
            }

            public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
            {
                DispatchAsyncCalled = true;
                return ValueTask.FromResult(default(TResponse)!);
            }

            public ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
            {
                DispatchAsyncCalled = true;
                return ValueTask.CompletedTask;
            }
        }

        private class MockStreamDispatcher : IStreamDispatcher
        {
            public async IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                yield break;
            }

            public async IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                yield break;
            }
        }

        private class MockNotificationDispatcher : INotificationDispatcher
        {
            public bool DispatchAsyncCalled { get; private set; }

            public ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken) where TNotification : INotification
            {
                DispatchAsyncCalled = true;
                return ValueTask.CompletedTask;
            }
        }
    }
}