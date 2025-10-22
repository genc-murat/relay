using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Dispatchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable CS0162 // Unreachable code detected
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type

namespace Relay.Core.Tests.Implementation;

public class NamedRelayTests
{
    // Test request and response types
    public class TestRequest : IRequest<string> { }
    public class TestStreamRequest : IStreamRequest<int> { }
    public class TestStreamRequestString : IStreamRequest<string> { }

    // Mock implementations
    public class MockRelayImplementation : RelayImplementation
    {
        public MockRelayImplementation(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    public class MockRequestDispatcher : IRequestDispatcher
    {
        public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
        {
            // Default implementation - just call the named version with null
            return DispatchAsync(request, null!, cancellationToken);
        }

        public ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
        {
            // Default implementation - just call the named version with null
            return DispatchAsync(request, null!, cancellationToken);
        }

        public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
        {
            if (handlerName == "success")
                return ValueTask.FromResult((TResponse)(object)"Success");
            if (handlerName == "exception")
                throw new InvalidOperationException("Test exception");
            throw new HandlerNotFoundException(request.GetType().Name, handlerName);
        }

        public ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
        {
            if (handlerName == "success")
                return ValueTask.CompletedTask;
            if (handlerName == "exception")
                throw new InvalidOperationException("Test exception");
            throw new HandlerNotFoundException(request.GetType().Name, handlerName);
        }
    }

    public class MockStreamDispatcher : IStreamDispatcher
    {
        public IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
        {
            // Default implementation - just call the named version with null
            return DispatchAsync(request, null!, cancellationToken);
        }

        public IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
        {
            if (handlerName == "success")
                return CreateSuccessStream<TResponse>();
            if (handlerName == "exception")
                return CreateExceptionStream<TResponse>();
            return CreateNotFoundStream<TResponse>(request.GetType().Name, handlerName);
        }

        private async IAsyncEnumerable<T> CreateSuccessStream<T>()
        {
            yield return (T)(object)"Success";
        }

        private async IAsyncEnumerable<T> CreateExceptionStream<T>()
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("Test exception");
            yield break;
        }

        private async IAsyncEnumerable<T> CreateNotFoundStream<T>(string requestType, string handlerName)
        {
            await Task.CompletedTask;
            throw new HandlerNotFoundException(requestType, handlerName);
            yield break;
        }
    }

    private IServiceProvider CreateServiceProvider(bool includeDispatchers = true)
    {
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());

        services.AddSingleton(relay);

        if (includeDispatchers)
        {
            services.AddSingleton<IRequestDispatcher, MockRequestDispatcher>();
            services.AddSingleton<IStreamDispatcher, MockStreamDispatcher>();
        }

        return services.BuildServiceProvider();
    }

    [Fact]
    public void Constructor_WithNullRelay_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NamedRelay(null!, serviceProvider));
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());

        // Act & Assert
        var namedRelay = new NamedRelay(relay, null);
        Assert.NotNull(namedRelay);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeDispatchers()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();

        // Act
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);

        // Assert
        Assert.NotNull(namedRelay);
    }

    [Fact]
    public async Task SendAsync_Generic_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => namedRelay.SendAsync<string>(null!, "handler", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_Generic_WithNullHandlerName_ShouldThrowArgumentException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => namedRelay.SendAsync(request, null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_Generic_WithEmptyHandlerName_ShouldThrowArgumentException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => namedRelay.SendAsync(request, "", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_Generic_WithWhitespaceHandlerName_ShouldThrowArgumentException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => namedRelay.SendAsync(request, "   ", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_Generic_WithNoDispatcher_ShouldThrowHandlerNotFoundException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider(includeDispatchers: false);
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() => namedRelay.SendAsync(request, "handler", CancellationToken.None).AsTask());
        Assert.Contains("TestRequest", exception.Message);
        Assert.Contains("handler", exception.Message);
    }

    [Fact]
    public async Task SendAsync_Generic_WithSuccessHandler_ShouldReturnResult()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestRequest();

        // Act
        var result = await namedRelay.SendAsync(request, "success", CancellationToken.None);

        // Assert
        Assert.Equal("Success", result);
    }

    [Fact]
    public async Task SendAsync_Generic_WithExceptionHandler_ShouldThrowException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => namedRelay.SendAsync(request, "exception", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_Generic_WithNotFoundHandler_ShouldThrowHandlerNotFoundException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() => namedRelay.SendAsync(request, "notfound", CancellationToken.None).AsTask());
        Assert.Contains("TestRequest", exception.Message);
        Assert.Contains("notfound", exception.Message);
    }

    [Fact]
    public async Task SendAsync_NonGeneric_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => namedRelay.SendAsync(null!, "handler", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_NonGeneric_WithNullHandlerName_ShouldThrowArgumentException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => namedRelay.SendAsync(request, null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_NonGeneric_WithSuccessHandler_ShouldComplete()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        await namedRelay.SendAsync(request, "success", CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_NonGeneric_WithExceptionHandler_ShouldThrowException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => namedRelay.SendAsync(request, "exception", CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task StreamAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => namedRelay.StreamAsync<string>(null!, "handler", CancellationToken.None).GetAsyncEnumerator().MoveNextAsync().AsTask());
    }

    [Fact]
    public async Task StreamAsync_WithNullHandlerName_ShouldThrowArgumentException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestStreamRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => namedRelay.StreamAsync<int>(request, null!, CancellationToken.None).GetAsyncEnumerator().MoveNextAsync().AsTask());
    }

    [Fact]
    public async Task StreamAsync_WithEmptyHandlerName_ShouldThrowArgumentException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestStreamRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => namedRelay.StreamAsync<int>(request, "", CancellationToken.None).GetAsyncEnumerator().MoveNextAsync().AsTask());
    }

    [Fact]
    public async Task StreamAsync_WithNoDispatcher_ShouldThrowHandlerNotFoundException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider(includeDispatchers: false);
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestStreamRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() => namedRelay.StreamAsync<int>(request, "handler", CancellationToken.None).GetAsyncEnumerator().MoveNextAsync().AsTask());
        Assert.Contains("TestStreamRequest", exception.Message);
        Assert.Contains("handler", exception.Message);
    }

    [Fact]
    public async Task StreamAsync_WithSuccessHandler_ShouldReturnStream()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestStreamRequestString();

        // Act
        var results = new List<string>();
        await foreach (var item in namedRelay.StreamAsync<string>(request, "success", CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal("Success", results[0]);
    }

    [Fact]
    public async Task StreamAsync_WithExceptionHandler_ShouldThrowException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestStreamRequestString();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => namedRelay.StreamAsync<string>(request, "exception", CancellationToken.None).GetAsyncEnumerator().MoveNextAsync().AsTask());
    }

    [Fact]
    public async Task StreamAsync_WithNotFoundHandler_ShouldThrowHandlerNotFoundException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var services = new ServiceCollection();
        var relay = new MockRelayImplementation(services.BuildServiceProvider());
        var namedRelay = new NamedRelay(relay, serviceProvider);
        var request = new TestStreamRequestString();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() => namedRelay.StreamAsync<string>(request, "notfound", CancellationToken.None).GetAsyncEnumerator().MoveNextAsync().AsTask());
        Assert.Contains("TestStreamRequest", exception.Message);
        Assert.Contains("notfound", exception.Message);
    }
}