using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Fallback;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Dispatchers;

/// <summary>
/// Tests for FallbackDispatcherBase functionality
/// </summary>
public class FallbackDispatcherBaseTests
{
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

    [Fact]
    public async Task ExecuteWithCache_WithHandlerExecutionException_ThrowsRelayException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<TestRequest, string>, ThrowingTestRequestHandler>();
        var serviceProvider = services.BuildServiceProvider();
        var request = new TestRequest { Message = "Test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RelayException>(() =>
            FallbackDispatcherBase.ExecuteWithCache(
                request,
                serviceProvider,
                rt => FallbackDispatcherBase.ResponseInvokerCache<string>.Create(rt)).AsTask());

        Assert.Equal("TestRequest", exception.RequestType);
        Assert.Contains("Test handler execution exception", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task ExecuteVoidWithCache_WithHandlerExecutionException_ThrowsRelayException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<TestVoidRequest>, ThrowingTestVoidRequestHandler>();
        var serviceProvider = services.BuildServiceProvider();
        var request = new TestVoidRequest { Message = "Test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RelayException>(() =>
            FallbackDispatcherBase.ExecuteVoidWithCache(
                request,
                serviceProvider,
                rt => FallbackDispatcherBase.VoidInvokerCache.Create(rt)).AsTask());

        Assert.Equal("TestVoidRequest", exception.RequestType);
        Assert.Contains("Test void handler execution exception", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
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

    private class ThrowingTestRequestHandler : IRequestHandler<TestRequest, string>
    {
        public ValueTask<string> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test handler execution exception");
        }
    }

    private class ThrowingTestVoidRequestHandler : IRequestHandler<TestVoidRequest>
    {
        public ValueTask HandleAsync(TestVoidRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test void handler execution exception");
        }
    }
}