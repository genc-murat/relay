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
using Relay.Core.Implementation.Fallback;

namespace Relay.Core.Tests
{
    public class FallbackDispatcherTests
    {
        [Fact]
        public async Task FallbackRequestDispatcher_DispatchAsync_WithRegisteredHandler_CallsHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new FallbackRequestDispatcher(serviceProvider);
            var request = new TestRequest { Message = "Test" };

            // Act
            var result = await dispatcher.DispatchAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal("Handled: Test", result);
        }

        [Fact]
        public async Task FallbackRequestDispatcher_DispatchAsync_WithoutHandler_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new FallbackRequestDispatcher(serviceProvider);
            var request = new TestRequest { Message = "Test" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() =>
                dispatcher.DispatchAsync(request, CancellationToken.None).AsTask());

            Assert.Contains("TestRequest", exception.RequestType);
        }

        [Fact]
        public async Task FallbackRequestDispatcher_DispatchAsync_VoidRequest_WithRegisteredHandler_CallsHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IRequestHandler<TestVoidRequest>, TestVoidRequestHandler>();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new FallbackRequestDispatcher(serviceProvider);
            var request = new TestVoidRequest { Message = "Test" };

            // Act & Assert - Should not throw
            await dispatcher.DispatchAsync(request, CancellationToken.None);
        }

        [Fact]
        public async Task FallbackRequestDispatcher_DispatchAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new FallbackRequestDispatcher(serviceProvider);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                dispatcher.DispatchAsync<string>(null!, CancellationToken.None).AsTask());
        }

        [Fact]
        public async Task FallbackRequestDispatcher_DispatchAsync_WithHandlerName_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new FallbackRequestDispatcher(serviceProvider);
            var request = new TestRequest { Message = "Test" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() =>
                dispatcher.DispatchAsync(request, "namedHandler", CancellationToken.None).AsTask());

            Assert.Contains("TestRequest", exception.RequestType);
            Assert.Equal("namedHandler", exception.HandlerName);
        }

        [Fact]
        public async Task FallbackStreamDispatcher_DispatchAsync_WithRegisteredHandler_CallsHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IStreamHandler<TestStreamRequest, string>, TestStreamHandler>();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new FallbackStreamDispatcher(serviceProvider);
            var request = new TestStreamRequest { Count = 3 };

            // Act
            var results = new List<string>();
            await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Equal("Item 0", results[0]);
            Assert.Equal("Item 1", results[1]);
            Assert.Equal("Item 2", results[2]);
        }

        [Fact]
        public async Task FallbackStreamDispatcher_DispatchAsync_WithoutHandler_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new FallbackStreamDispatcher(serviceProvider);
            var request = new TestStreamRequest { Count = 3 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
                {
                    // Should not reach here
                }
            });

            Assert.Contains("TestStreamRequest", exception.RequestType);
        }

        [Fact]
        public void FallbackStreamDispatcher_DispatchAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new FallbackStreamDispatcher(serviceProvider);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                dispatcher.DispatchAsync<string>(null!, CancellationToken.None));
        }

        [Fact]
        public async Task FallbackStreamDispatcher_DispatchAsync_WithHandlerName_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new FallbackStreamDispatcher(serviceProvider);
            var request = new TestStreamRequest { Count = 3 };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
            {
                await foreach (var item in dispatcher.DispatchAsync(request, "namedHandler", CancellationToken.None))
                {
                    // Should not reach here
                }
            });

            Assert.Contains("TestStreamRequest", exception.RequestType);
            Assert.Equal("namedHandler", exception.HandlerName);
        }

        [Fact]
        public async Task FallbackNotificationDispatcher_DispatchAsync_WithRegisteredHandlers_CallsAllHandlers()
        {
            // Arrange
            var handler1 = new TestNotificationHandler();
            var handler2 = new TestNotificationHandler();
            var handler3 = new TestNotificationHandler();

            var services = new ServiceCollection();
            services.AddSingleton<INotificationHandler<TestNotification>>(handler1);
            services.AddSingleton<INotificationHandler<TestNotification>>(handler2);
            services.AddSingleton<INotificationHandler<TestNotification>>(handler3);

            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new FallbackNotificationDispatcher(serviceProvider);
            var notification = new TestNotification { Message = "Test" };

            // Act
            await dispatcher.DispatchAsync(notification, CancellationToken.None);

            // Assert
            Assert.True(handler1.WasCalled);
            Assert.True(handler2.WasCalled);
            Assert.True(handler3.WasCalled);
        }

        [Fact]
        public async Task FallbackNotificationDispatcher_DispatchAsync_WithoutHandlers_CompletesSuccessfully()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new FallbackNotificationDispatcher(serviceProvider);
            var notification = new TestNotification { Message = "Test" };

            // Act & Assert - Should not throw
            await dispatcher.DispatchAsync(notification, CancellationToken.None);
        }

        [Fact]
        public async Task FallbackNotificationDispatcher_DispatchAsync_WithNullNotification_ThrowsArgumentNullException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new FallbackNotificationDispatcher(serviceProvider);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                dispatcher.DispatchAsync<TestNotification>(null!, CancellationToken.None).AsTask());
        }

        // Test classes
        private class TestRequest : IRequest<string>
        {
            public string Message { get; set; } = string.Empty;
        }

        private class TestVoidRequest : IRequest
        {
            public string Message { get; set; } = string.Empty;
        }

        private class TestStreamRequest : IStreamRequest<string>
        {
            public int Count { get; set; }
        }

        private class TestNotification : INotification
        {
            public string Message { get; set; } = string.Empty;
        }

        private class TestRequestHandler : IRequestHandler<TestRequest, string>
        {
            public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
            {
                return ValueTask.FromResult($"Handled: {request.Message}");
            }
        }

        private class TestVoidRequestHandler : IRequestHandler<TestVoidRequest>
        {
            public ValueTask HandleAsync(TestVoidRequest request, CancellationToken cancellationToken)
            {
                return ValueTask.CompletedTask;
            }
        }

        private class TestStreamHandler : IStreamHandler<TestStreamRequest, string>
        {
            public async IAsyncEnumerable<string> HandleAsync(TestStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                for (int i = 0; i < request.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return $"Item {i}";
                    await Task.Yield();
                }
            }
        }

        private class TestNotificationHandler : INotificationHandler<TestNotification>
        {
            public bool WasCalled { get; private set; }

            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                WasCalled = true;
                return ValueTask.CompletedTask;
            }
        }

        // Tests for FallbackDispatcherBase
        [Fact]
        public void ResponseInvokerCache_Create_CreatesValidEntry()
        {
            // Arrange
            var requestType = typeof(TestRequest);

            // Act
            var entry = FallbackDispatcherBase.ResponseInvokerCache<string>.Create(requestType);

            // Assert
            Assert.NotNull(entry);
            Assert.NotNull(entry.HandlerInterfaceType);
            Assert.NotNull(entry.Invoke);
            Assert.Equal(typeof(IRequestHandler<TestRequest, string>), entry.HandlerInterfaceType);
        }

        [Fact]
        public void ResponseInvokerCache_Cache_ReusesEntries()
        {
            // Arrange
            var requestType = typeof(TestRequest);

            // Act
            var entry1 = FallbackDispatcherBase.ResponseInvokerCache<string>.Cache.GetOrAdd(requestType, rt => FallbackDispatcherBase.ResponseInvokerCache<string>.Create(rt));
            var entry2 = FallbackDispatcherBase.ResponseInvokerCache<string>.Cache.GetOrAdd(requestType, rt => FallbackDispatcherBase.ResponseInvokerCache<string>.Create(rt));

            // Assert
            Assert.Same(entry1, entry2);
        }

        [Fact]
        public void VoidInvokerCache_Create_CreatesValidEntry()
        {
            // Arrange
            var requestType = typeof(TestVoidRequest);

            // Act
            var entry = FallbackDispatcherBase.VoidInvokerCache.Create(requestType);

            // Assert
            Assert.NotNull(entry);
            Assert.NotNull(entry.HandlerInterfaceType);
            Assert.NotNull(entry.Invoke);
            Assert.Equal(typeof(IRequestHandler<TestVoidRequest>), entry.HandlerInterfaceType);
        }

        [Fact]
        public void VoidInvokerCache_Cache_ReusesEntries()
        {
            // Arrange
            var requestType = typeof(TestVoidRequest);

            // Act
            var entry1 = FallbackDispatcherBase.VoidInvokerCache.Cache.GetOrAdd(requestType, rt => FallbackDispatcherBase.VoidInvokerCache.Create(rt));
            var entry2 = FallbackDispatcherBase.VoidInvokerCache.Cache.GetOrAdd(requestType, rt => FallbackDispatcherBase.VoidInvokerCache.Create(rt));

            // Assert
            Assert.Same(entry1, entry2);
        }

        [Fact]
        public void StreamInvokerCache_GetOrCreate_CreatesValidEntry()
        {
            // Arrange
            var requestType = typeof(TestStreamRequest);

            // Act
            var entry = FallbackDispatcherBase.StreamInvokerCache<string>.GetOrCreate(requestType);

            // Assert
            Assert.NotNull(entry);
            Assert.NotNull(entry.HandlerInterfaceType);
            Assert.NotNull(entry.Invoke);
            Assert.Equal(typeof(IStreamHandler<TestStreamRequest, string>), entry.HandlerInterfaceType);
        }

        [Fact]
        public void StreamInvokerCache_GetOrCreate_ReusesEntries()
        {
            // Arrange
            var requestType = typeof(TestStreamRequest);

            // Act
            var entry1 = FallbackDispatcherBase.StreamInvokerCache<string>.GetOrCreate(requestType);
            var entry2 = FallbackDispatcherBase.StreamInvokerCache<string>.GetOrCreate(requestType);

            // Assert
            Assert.Same(entry1, entry2);
        }

        [Fact]
        public async Task ExecuteWithCache_WithValidHandler_CallsHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IRequestHandler<TestRequest, string>, TestRequestHandler>();
            var serviceProvider = services.BuildServiceProvider();
            var request = new TestRequest { Message = "Test" };

            // Act
            var result = await FallbackDispatcherBase.ExecuteWithCache(
                request,
                serviceProvider,
                rt => FallbackDispatcherBase.ResponseInvokerCache<string>.Create(rt));

            // Assert
            Assert.Equal("Handled: Test", result);
        }

        [Fact]
        public async Task ExecuteWithCache_WithoutHandler_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var request = new TestRequest { Message = "Test" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() =>
                FallbackDispatcherBase.ExecuteWithCache(
                    request,
                    serviceProvider,
                    rt => FallbackDispatcherBase.ResponseInvokerCache<string>.Create(rt)).AsTask());

            Assert.Contains("TestRequest", exception.RequestType);
        }

        [Fact]
        public async Task ExecuteVoidWithCache_WithValidHandler_CallsHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<IRequestHandler<TestVoidRequest>, TestVoidRequestHandler>();
            var serviceProvider = services.BuildServiceProvider();
            var request = new TestVoidRequest { Message = "Test" };

            // Act & Assert - Should not throw
            await FallbackDispatcherBase.ExecuteVoidWithCache(
                request,
                serviceProvider,
                rt => FallbackDispatcherBase.VoidInvokerCache.Create(rt));
        }

        [Fact]
        public async Task ExecuteVoidWithCache_WithoutHandler_ThrowsHandlerNotFoundException()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var request = new TestVoidRequest { Message = "Test" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() =>
                FallbackDispatcherBase.ExecuteVoidWithCache(
                    request,
                    serviceProvider,
                    rt => FallbackDispatcherBase.VoidInvokerCache.Create(rt)).AsTask());

            Assert.Contains("TestVoidRequest", exception.RequestType);
        }

        [Fact]
        public void CreateHandlerNotFoundException_WithRequestType_CreatesException()
        {
            // Arrange
            var requestType = typeof(TestRequest);

            // Act
            var exception = FallbackDispatcherBase.CreateHandlerNotFoundException(requestType);

            // Assert
            Assert.NotNull(exception);
            Assert.Equal("TestRequest", exception.RequestType);
            Assert.Null(exception.HandlerName);
        }

        [Fact]
        public void CreateHandlerNotFoundException_WithRequestTypeAndHandlerName_CreatesException()
        {
            // Arrange
            var requestType = typeof(TestRequest);
            var handlerName = "TestHandler";

            // Act
            var exception = FallbackDispatcherBase.CreateHandlerNotFoundException(requestType, handlerName);

            // Assert
            Assert.NotNull(exception);
            Assert.Equal("TestRequest", exception.RequestType);
            Assert.Equal("TestHandler", exception.HandlerName);
        }

        [Fact]
        public void HandleException_WithRelayException_ReturnsSameException()
        {
            // Arrange
            var originalException = new HandlerNotFoundException("TestRequest");

            // Act
            var result = FallbackDispatcherBase.HandleException(originalException, "TestRequest");

            // Assert
            Assert.Same(originalException, result);
        }

        [Fact]
        public void HandleException_WithRegularException_WrapsInRelayException()
        {
            // Arrange
            var originalException = new InvalidOperationException("Test error");
            var requestType = "TestRequest";

            // Act
            var result = FallbackDispatcherBase.HandleException(originalException, requestType);

            // Assert
            Assert.IsType<RelayException>(result);
            var relayException = (RelayException)result;
            Assert.Equal(requestType, relayException.RequestType);
            Assert.Null(relayException.HandlerName);
            Assert.Contains("TestRequest", relayException.Message);
            Assert.Same(originalException, relayException.InnerException);
        }

        [Fact]
        public void HandleException_WithHandlerName_IncludesHandlerName()
        {
            // Arrange
            var originalException = new InvalidOperationException("Test error");
            var requestType = "TestRequest";
            var handlerName = "TestHandler";

            // Act
            var result = FallbackDispatcherBase.HandleException(originalException, requestType, handlerName);

            // Assert
            Assert.IsType<RelayException>(result);
            var relayException = (RelayException)result;
            Assert.Equal(requestType, relayException.RequestType);
            Assert.Equal(handlerName, relayException.HandlerName);
        }
    }
}