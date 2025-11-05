using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.Contracts
{
    public class BaseRequestDispatcherTests
    {
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly TestRequestDispatcher _dispatcher;

        public BaseRequestDispatcherTests()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _dispatcher = new TestRequestDispatcher(_serviceProviderMock.Object);
        }

        [Fact]
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestRequestDispatcher(null!));
        }

        [Fact]
        public void Constructor_WithValidServiceProvider_SetsServiceProvider()
        {
            // Act
            var dispatcher = new TestRequestDispatcher(_serviceProviderMock.Object);

            // Assert
            Assert.NotNull(dispatcher.GetServiceProvider());
        }

        [Fact]
        public void ValidateRequest_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _dispatcher.ValidateRequest<TestRequest>(null!));
        }

        [Fact]
        public void ValidateRequest_WithValidRequest_DoesNotThrow()
        {
            // Arrange
            var request = new TestRequest();

            // Act & Assert
            Assert.NotNull(request); // Just to use the request
            _dispatcher.ValidateRequest(request);
        }

        [Fact]
        public void ValidateHandlerName_WithNullHandlerName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _dispatcher.ValidateHandlerName(null!));
            Assert.Contains("Handler name cannot be null or empty", exception.Message);
        }

        [Fact]
        public void ValidateHandlerName_WithEmptyHandlerName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _dispatcher.ValidateHandlerName(string.Empty));
            Assert.Contains("Handler name cannot be null or empty", exception.Message);
        }

        [Fact]
        public void ValidateHandlerName_WithWhitespaceHandlerName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _dispatcher.ValidateHandlerName("   "));
            Assert.Contains("Handler name cannot be null or empty", exception.Message);
        }

        [Fact]
        public void ValidateHandlerName_WithValidHandlerName_DoesNotThrow()
        {
            // Act & Assert
            _dispatcher.ValidateHandlerName("validHandler");
        }

        [Fact]
        public void CreateHandlerNotFoundException_WithoutHandlerName_CreatesCorrectException()
        {
            // Act
            var exception = _dispatcher.CreateHandlerNotFoundException(typeof(TestRequest));

            // Assert
            Assert.IsType<HandlerNotFoundException>(exception);
            Assert.Contains("No handler found for request type 'TestRequest'", exception.Message);
        }

        [Fact]
        public void CreateHandlerNotFoundException_WithHandlerName_CreatesCorrectException()
        {
            // Act
            var exception = _dispatcher.CreateHandlerNotFoundException(typeof(TestRequest), "testHandler");

            // Assert
            Assert.IsType<HandlerNotFoundException>(exception);
            Assert.Contains("No handler named 'testHandler' found for request type 'TestRequest'", exception.Message);
        }

        // Test concrete implementation for abstract methods
        private class TestRequestDispatcher : BaseRequestDispatcher
        {
            public TestRequestDispatcher(IServiceProvider serviceProvider) : base(serviceProvider) { }

            public override ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            {
                ValidateRequest(request);
                return ValueTask.FromResult(default(TResponse)!);
            }

            public override ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken = default)
            {
                ValidateRequest(request);
                return ValueTask.CompletedTask;
            }

            public override ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken = default)
            {
                ValidateRequest(request);
                ValidateHandlerName(handlerName);
                return ValueTask.FromResult(default(TResponse)!);
            }

            public override ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken = default)
            {
                ValidateRequest(request);
                ValidateHandlerName(handlerName);
                return ValueTask.CompletedTask;
            }

            // Expose protected methods for testing
            public new void ValidateRequest<T>(T request) where T : class => base.ValidateRequest(request);
            public new void ValidateHandlerName(string handlerName) => base.ValidateHandlerName(handlerName);
            public new Exception CreateHandlerNotFoundException(Type requestType, string? handlerName = null) =>
                base.CreateHandlerNotFoundException(requestType, handlerName);
            public IServiceProvider GetServiceProvider() => base.ServiceProvider;
        }

        private class TestRequest : IRequest<string> { }
    }
}

