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
    public class NamedRelayTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RelayImplementation _relay;
        private readonly NamedRelay _namedRelay;

        public NamedRelayTests()
        {
            var services = new ServiceCollection();
            _serviceProvider = services.BuildServiceProvider();
            _relay = new RelayImplementation(_serviceProvider);
            _namedRelay = new NamedRelay(_relay, _serviceProvider);
        }

        [Fact]
        public void Constructor_WithNullRelay_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new NamedRelay(null!, _serviceProvider));
        }

        [Fact]
        public async Task SendAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _namedRelay.SendAsync<string>(null!, "handler").AsTask());
        }

        [Fact]
        public async Task SendAsync_WithNullHandlerName_ThrowsArgumentException()
        {
            // Arrange
            var request = new TestRequest();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _namedRelay.SendAsync(request, null!).AsTask());
        }

        [Fact]
        public async Task SendAsync_WithEmptyHandlerName_ThrowsArgumentException()
        {
            // Arrange
            var request = new TestRequest();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _namedRelay.SendAsync(request, "").AsTask());
        }

        [Fact]
        public async Task SendAsync_WithoutResponse_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => 
                _namedRelay.SendAsync((IRequest)null!, "handler").AsTask());
        }

        [Fact]
        public async Task SendAsync_WithoutResponse_WithNullHandlerName_ThrowsArgumentException()
        {
            // Arrange
            var request = new TestVoidRequest();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _namedRelay.SendAsync(request, null!).AsTask());
        }

        [Fact]
        public async Task SendAsync_WithNoDispatcher_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var request = new TestRequest();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() => 
                _namedRelay.SendAsync(request, "testHandler").AsTask());
            
            Assert.Contains("IRequest`1", exception.RequestType);
            Assert.Equal("testHandler", exception.HandlerName);
        }

        [Fact]
        public async Task SendAsync_WithoutResponse_WithNoDispatcher_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var request = new TestVoidRequest();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() => 
                _namedRelay.SendAsync(request, "testHandler").AsTask());
            
            Assert.Contains("IRequest", exception.RequestType);
            Assert.Equal("testHandler", exception.HandlerName);
        }

        [Fact]
        public void StreamAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _namedRelay.StreamAsync<string>(null!, "handler"));
        }

        [Fact]
        public void StreamAsync_WithNullHandlerName_ThrowsArgumentException()
        {
            // Arrange
            var request = new TestStreamRequest();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _namedRelay.StreamAsync(request, null!));
        }

        [Fact]
        public void StreamAsync_WithEmptyHandlerName_ThrowsArgumentException()
        {
            // Arrange
            var request = new TestStreamRequest();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _namedRelay.StreamAsync(request, ""));
        }

        [Fact]
        public async Task StreamAsync_WithNoDispatcher_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var request = new TestStreamRequest();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
            {
                await foreach (var item in _namedRelay.StreamAsync(request, "testHandler"))
                {
                    // Should not reach here
                }
            });
            
            Assert.Contains("IStreamRequest`1", exception.RequestType);
            Assert.Equal("testHandler", exception.HandlerName);
        }

        [Fact]
        public async Task SendAsync_WithMockDispatcher_CallsDispatcherWithHandlerName()
        {
            // Arrange
            var mockDispatcher = new MockRequestDispatcher();
            var services = new ServiceCollection();
            services.AddSingleton<IRequestDispatcher>(mockDispatcher);
            
            var serviceProvider = services.BuildServiceProvider();
            var relay = new RelayImplementation(serviceProvider);
            var namedRelay = new NamedRelay(relay, serviceProvider);
            var request = new TestRequest();

            // Act
            await namedRelay.SendAsync(request, "testHandler");

            // Assert
            Assert.True(mockDispatcher.NamedDispatchAsyncCalled);
            Assert.Equal("testHandler", mockDispatcher.LastHandlerName);
        }

        [Fact]
        public async Task StreamAsync_WithMockDispatcher_CallsDispatcherWithHandlerName()
        {
            // Arrange
            var mockDispatcher = new MockStreamDispatcher();
            var services = new ServiceCollection();
            services.AddSingleton<IStreamDispatcher>(mockDispatcher);
            
            var serviceProvider = services.BuildServiceProvider();
            var relay = new RelayImplementation(serviceProvider);
            var namedRelay = new NamedRelay(relay, serviceProvider);
            var request = new TestStreamRequest();

            // Act
            await foreach (var item in namedRelay.StreamAsync(request, "testHandler"))
            {
                // Enumerate the stream
            }

            // Assert
            Assert.True(mockDispatcher.NamedDispatchAsyncCalled);
            Assert.Equal("testHandler", mockDispatcher.LastHandlerName);
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

        // Mock dispatchers
        private class MockRequestDispatcher : IRequestDispatcher
        {
            public bool NamedDispatchAsyncCalled { get; private set; }
            public string? LastHandlerName { get; private set; }

            public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
            {
                return ValueTask.FromResult(default(TResponse)!);
            }

            public ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
            {
                return ValueTask.CompletedTask;
            }

            public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
            {
                NamedDispatchAsyncCalled = true;
                LastHandlerName = handlerName;
                return ValueTask.FromResult(default(TResponse)!);
            }

            public ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
            {
                NamedDispatchAsyncCalled = true;
                LastHandlerName = handlerName;
                return ValueTask.CompletedTask;
            }
        }

        private class MockStreamDispatcher : IStreamDispatcher
        {
            public bool NamedDispatchAsyncCalled { get; private set; }
            public string? LastHandlerName { get; private set; }

            public async IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                await Task.CompletedTask;
                yield break;
            }

            public async IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                NamedDispatchAsyncCalled = true;
                LastHandlerName = handlerName;
                await Task.CompletedTask;
                yield break;
            }
        }
    }
}