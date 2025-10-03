using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Relay.Core;

namespace Relay.Core.Tests
{
    public class BaseDispatcherTests
    {
        [Fact]
        public void BaseDispatcher_Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestBaseDispatcher(null!));
        }

        [Fact]
        public void BaseDispatcher_GetService_ResolvesService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<TestService>();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new TestBaseDispatcher(serviceProvider);

            // Act
            var service = dispatcher.GetServicePublic<TestService>();

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void BaseDispatcher_GetServiceOrNull_WithRegisteredService_ReturnsService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<TestService>();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new TestBaseDispatcher(serviceProvider);

            // Act
            var service = dispatcher.GetServiceOrNullPublic<TestService>();

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void BaseDispatcher_GetServiceOrNull_WithUnregisteredService_ReturnsNull()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new TestBaseDispatcher(serviceProvider);

            // Act
            var service = dispatcher.GetServiceOrNullPublic<TestService>();

            // Assert
            Assert.Null(service);
        }

        [Fact]
        public void BaseDispatcher_CreateScope_ReturnsScope()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var dispatcher = new TestBaseDispatcher(serviceProvider);

            // Act
            using var scope = dispatcher.CreateScopePublic();

            // Assert
            Assert.NotNull(scope);
        }

        [Fact]
        public void BaseDispatcher_HandleException_WithRelayException_ReturnsOriginal()
        {
            // Arrange
            var originalException = new HandlerNotFoundException("TestRequest");

            // Act
            var result = TestBaseDispatcher.HandleExceptionPublic(originalException, "TestRequest");

            // Assert
            Assert.Same(originalException, result);
        }

        [Fact]
        public void BaseDispatcher_HandleException_WithOtherException_WrapsInRelayException()
        {
            // Arrange
            var originalException = new InvalidOperationException("Test error");

            // Act
            var result = TestBaseDispatcher.HandleExceptionPublic(originalException, "TestRequest");

            // Assert
            Assert.IsType<RelayException>(result);
            Assert.Equal("TestRequest", result.RequestType);
            Assert.Same(originalException, result.InnerException);
        }

        [Fact]
        public void BaseDispatcher_ValidateRequest_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => TestBaseDispatcher.ValidateRequestPublic(null));
        }

        [Fact]
        public void BaseDispatcher_ValidateRequest_WithValidRequest_DoesNotThrow()
        {
            // Arrange
            var request = new object();

            // Act & Assert - Should not throw
            TestBaseDispatcher.ValidateRequestPublic(request);
        }

        [Fact]
        public void BaseDispatcher_ValidateHandlerName_WithNullName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => TestBaseDispatcher.ValidateHandlerNamePublic(null));
        }

        [Fact]
        public void BaseDispatcher_ValidateHandlerName_WithEmptyName_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => TestBaseDispatcher.ValidateHandlerNamePublic(""));
        }

        [Fact]
        public void BaseDispatcher_ValidateHandlerName_WithValidName_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            TestBaseDispatcher.ValidateHandlerNamePublic("validName");
        }

        [Fact]
        public void BaseRequestDispatcher_CreateHandlerNotFoundException_WithType_CreatesCorrectException()
        {
            // Arrange
            var requestType = typeof(TestRequest);

            // Act
            var exception = TestBaseRequestDispatcher.CreateHandlerNotFoundExceptionPublic(requestType);

            // Assert
            Assert.Equal(requestType.Name, exception.RequestType);
            Assert.Null(exception.HandlerName);
        }

        [Fact]
        public void BaseRequestDispatcher_CreateHandlerNotFoundException_WithTypeAndName_CreatesCorrectException()
        {
            // Arrange
            var requestType = typeof(TestRequest);
            var handlerName = "testHandler";

            // Act
            var exception = TestBaseRequestDispatcher.CreateHandlerNotFoundExceptionPublic(requestType, handlerName);

            // Assert
            Assert.Equal(requestType.Name, exception.RequestType);
            Assert.Equal(handlerName, exception.HandlerName);
        }

        [Fact]
        public async Task BaseStreamDispatcher_ThrowHandlerNotFound_ThrowsCorrectException()
        {
            // Arrange
            var requestType = typeof(TestStreamRequest);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
            {
                await foreach (var item in TestBaseStreamDispatcher.ThrowHandlerNotFoundPublic<string>(requestType))
                {
                    // Should not reach here
                }
            });

            Assert.Equal(requestType.Name, exception.RequestType);
            Assert.Null(exception.HandlerName);
        }

        [Fact]
        public async Task BaseStreamDispatcher_ThrowHandlerNotFound_WithHandlerName_ThrowsCorrectException()
        {
            // Arrange
            var requestType = typeof(TestStreamRequest);
            var handlerName = "testHandler";

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
            {
                await foreach (var item in TestBaseStreamDispatcher.ThrowHandlerNotFoundPublic<string>(requestType, handlerName))
                {
                    // Should not reach here
                }
            });

            Assert.Equal(requestType.Name, exception.RequestType);
            Assert.Equal(handlerName, exception.HandlerName);
        }

        [Fact]
        public async Task BaseNotificationDispatcher_ExecuteHandlersParallel_ExecutesAllHandlers()
        {
            // Arrange
            var counter = 0;
            var handlers = new[]
            {
                new ValueTask(Task.Run(() => Interlocked.Increment(ref counter))),
                new ValueTask(Task.Run(() => Interlocked.Increment(ref counter))),
                new ValueTask(Task.Run(() => Interlocked.Increment(ref counter)))
            };

            // Act
            await TestBaseNotificationDispatcher.ExecuteHandlersParallelPublic(handlers, CancellationToken.None);

            // Assert
            Assert.Equal(3, counter);
        }

        [Fact]
        public async Task BaseNotificationDispatcher_ExecuteHandlersSequential_ExecutesAllHandlers()
        {
            // Arrange
            var counter = 0;
            var handlers = new[]
            {
                new ValueTask(Task.Run(() => Interlocked.Increment(ref counter))),
                new ValueTask(Task.Run(() => Interlocked.Increment(ref counter))),
                new ValueTask(Task.Run(() => Interlocked.Increment(ref counter)))
            };

            // Act
            await TestBaseNotificationDispatcher.ExecuteHandlersSequentialPublic(handlers, CancellationToken.None);

            // Assert
            Assert.Equal(3, counter);
        }

        // Test classes
        private class TestService
        {
        }

        private class TestRequest : IRequest<string>
        {
        }

        private class TestStreamRequest : IStreamRequest<string>
        {
        }

        // Test wrapper classes to expose protected members
        private class TestBaseDispatcher : BaseDispatcher
        {
            public TestBaseDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            public T GetServicePublic<T>() where T : notnull => GetService<T>();
            public T? GetServiceOrNullPublic<T>() where T : class => GetServiceOrNull<T>();
            public IServiceScope CreateScopePublic() => CreateScope();
            public static RelayException HandleExceptionPublic(Exception exception, string requestType, string? handlerName = null) => HandleException(exception, requestType, handlerName);
            public static void ValidateRequestPublic(object? request, string parameterName = "request") => ValidateRequest(request, parameterName);
            public static void ValidateHandlerNamePublic(string? handlerName, string parameterName = "handlerName") => ValidateHandlerName(handlerName, parameterName);
        }

        private class TestBaseRequestDispatcher : BaseRequestDispatcher
        {
            public TestBaseRequestDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            public override ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public static HandlerNotFoundException CreateHandlerNotFoundExceptionPublic(Type requestType) => CreateHandlerNotFoundException(requestType);
            public static HandlerNotFoundException CreateHandlerNotFoundExceptionPublic(Type requestType, string handlerName) => CreateHandlerNotFoundException(requestType, handlerName);
        }

        private class TestBaseStreamDispatcher : BaseStreamDispatcher
        {
            public TestBaseStreamDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            public override IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public static IAsyncEnumerable<TResponse> ThrowHandlerNotFoundPublic<TResponse>(Type requestType) => ThrowHandlerNotFound<TResponse>(requestType);
            public static IAsyncEnumerable<TResponse> ThrowHandlerNotFoundPublic<TResponse>(Type requestType, string handlerName) => ThrowHandlerNotFound<TResponse>(requestType, handlerName);
        }

        private class TestBaseNotificationDispatcher : BaseNotificationDispatcher
        {
            public TestBaseNotificationDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
            {
            }

            public override ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public static ValueTask ExecuteHandlersParallelPublic(IEnumerable<ValueTask> handlers, CancellationToken cancellationToken) => ExecuteHandlersParallel(handlers, cancellationToken);
            public static ValueTask ExecuteHandlersSequentialPublic(IEnumerable<ValueTask> handlers, CancellationToken cancellationToken) => ExecuteHandlersSequential(handlers, cancellationToken);
        }
    }
}