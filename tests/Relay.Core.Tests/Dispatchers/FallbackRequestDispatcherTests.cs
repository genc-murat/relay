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

namespace Relay.Core.Tests.Dispatchers;

/// <summary>
/// Tests for FallbackRequestDispatcher functionality
/// </summary>
public class FallbackRequestDispatcherTests
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
    public async Task FallbackRequestDispatcher_DispatchAsync_WithHandlerException_WrapsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<TestRequest, string>, ThrowingTestRequestHandler>();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackRequestDispatcher(serviceProvider);
        var request = new TestRequest { Message = "Test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RelayException>(() =>
            dispatcher.DispatchAsync(request, CancellationToken.None).AsTask());

        Assert.Equal("TestRequest", exception.RequestType);
        Assert.Contains("Test handler exception", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task FallbackRequestDispatcher_DispatchAsync_VoidRequest_WithHandlerException_WrapsException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IRequestHandler<TestVoidRequest>, ThrowingTestVoidRequestHandler>();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackRequestDispatcher(serviceProvider);
        var request = new TestVoidRequest { Message = "Test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RelayException>(() =>
            dispatcher.DispatchAsync(request, CancellationToken.None).AsTask());

        Assert.Equal("TestVoidRequest", exception.RequestType);
        Assert.Contains("Test void handler exception", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task FallbackRequestDispatcher_DispatchAsync_WithEmptyHandlerName_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackRequestDispatcher(serviceProvider);
        var request = new TestRequest { Message = "Test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            dispatcher.DispatchAsync(request, "", CancellationToken.None).AsTask());

        Assert.Contains("handlerName", exception.Message);
    }

    [Fact]
    public async Task FallbackRequestDispatcher_DispatchAsync_VoidRequest_WithEmptyHandlerName_ThrowsArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = new FallbackRequestDispatcher(serviceProvider);
        var request = new TestVoidRequest { Message = "Test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            dispatcher.DispatchAsync(request, "", CancellationToken.None).AsTask());

        Assert.Contains("handlerName", exception.Message);
    }

    [Fact]
    public void FallbackRequestDispatcher_Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FallbackRequestDispatcher(null!));
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
            throw new InvalidOperationException("Test handler exception");
        }
    }

    private class ThrowingTestVoidRequestHandler : IRequestHandler<TestVoidRequest>
    {
        public ValueTask HandleAsync(TestVoidRequest request, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test void handler exception");
        }
    }
}