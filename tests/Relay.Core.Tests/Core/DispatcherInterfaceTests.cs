using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Requests;
using Relay.Core.Tests.Testing;
using Xunit;

namespace Relay.Core.Tests.Core;

/// <summary>
/// Comprehensive tests for dispatcher interfaces
/// </summary>
public class DispatcherInterfaceTests
{
    [Fact]
    public void IRequestDispatcher_ShouldHaveCorrectMethods()
    {
        // Arrange & Act
        var dispatcherType = typeof(IRequestDispatcher);

        // Assert
        Assert.True(dispatcherType.IsInterface);

        var methods = dispatcherType.GetMethods();
        Assert.Equal(4, methods.Length);

        // Check DispatchAsync with response
        Assert.Contains(methods, m =>
            m.Name == "DispatchAsync" &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 2 &&
            m.ReturnType.IsGenericType &&
            m.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>));

        // Check DispatchAsync without response
        Assert.Contains(methods, m =>
            m.Name == "DispatchAsync" &&
            !m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 2 &&
            m.ReturnType == typeof(ValueTask));

        // Check named handler variants
        Assert.Contains(methods, m =>
            m.Name == "DispatchAsync" &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 3);

        Assert.Contains(methods, m =>
            m.Name == "DispatchAsync" &&
            !m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 3);
    }

    [Fact]
    public void IStreamDispatcher_ShouldHaveCorrectMethods()
    {
        // Arrange & Act
        var dispatcherType = typeof(IStreamDispatcher);

        // Assert
        Assert.True(dispatcherType.IsInterface);

        var methods = dispatcherType.GetMethods();
        Assert.Equal(2, methods.Length);

        // Check DispatchAsync methods
        Assert.Contains(methods, m =>
            m.Name == "DispatchAsync" &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 2 &&
            m.ReturnType.IsGenericType &&
            m.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));

        Assert.Contains(methods, m =>
            m.Name == "DispatchAsync" &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 3);
    }

    [Fact]
    public void INotificationDispatcher_ShouldHaveCorrectMethods()
    {
        // Arrange & Act
        var dispatcherType = typeof(INotificationDispatcher);

        // Assert
        Assert.True(dispatcherType.IsInterface);

        var methods = dispatcherType.GetMethods();
        Assert.Equal(1, methods.Length);

        var method = methods[0];
        Assert.Equal("DispatchAsync", method.Name);
        Assert.True(method.IsGenericMethodDefinition);
        Assert.Equal(2, method.GetParameters().Length);
        Assert.Equal(typeof(ValueTask), method.ReturnType);
    }

    [Fact]
    public async Task TestRequestDispatcher_ShouldDispatchCorrectly()
    {
        // Arrange
        var handler = new TestHandler<TestRequest<string>, string>("test response");
        var harness = new RelayTestHarness()
            .AddHandler(handler);

        var dispatcher = harness.GetService<IRequestDispatcher>();
        var request = new TestRequest<string> { ExpectedResponse = "test response" };

        // Act
        var result = await dispatcher.DispatchAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("test response", result);
        Assert.True(handler.WasCalled);
    }

    [Fact]
    public async Task TestRequestDispatcher_WithVoidRequest_ShouldDispatchCorrectly()
    {
        // Arrange
        var handler = new TestVoidHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler);

        var dispatcher = harness.GetService<IRequestDispatcher>();
        var request = new TestVoidRequest();

        // Act
        await dispatcher.DispatchAsync(request, CancellationToken.None);

        // Assert
        Assert.True(handler.WasCalled);
    }

    [Fact]
    public async Task TestStreamDispatcher_ShouldDispatchCorrectly()
    {
        // Arrange
        var handler = new TestStreamHandler<TestStreamRequest<int>, int>();
        var harness = new RelayTestHarness()
            .AddHandler(handler);

        var dispatcher = harness.GetService<IStreamDispatcher>();
        var request = new TestStreamRequest<int> { Items = new List<int> { 1, 2, 3 } };

        // Act
        var results = new List<int>();
        await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(new List<int> { 1, 2, 3 }, results);
        Assert.True(handler.WasCalled);
    }

    [Fact]
    public async Task TestNotificationDispatcher_ShouldDispatchCorrectly()
    {
        // Arrange
        var handler = new TestNotificationHandler<Relay.Core.Tests.Testing.TestNotification>();
        var harness = new RelayTestHarness()
            .AddHandler(handler);

        var dispatcher = harness.GetService<INotificationDispatcher>();
        var notification = new Relay.Core.Tests.Testing.TestNotification { Message = "test message" };

        // Act
        await dispatcher.DispatchAsync(notification, CancellationToken.None);

        // Assert
        Assert.True(handler.WasCalled);
        Assert.Same(notification, handler.LastNotification);
    }

    [Fact]
    public async Task TestRequestDispatcher_WithMissingHandler_ShouldThrowException()
    {
        // Arrange
        var harness = new RelayTestHarness();
        var dispatcher = harness.GetService<IRequestDispatcher>();
        var request = new TestRequest<string>();

        // Act & Assert
        await Assert.ThrowsAsync<HandlerNotFoundException>(() =>
            dispatcher.DispatchAsync(request, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task TestStreamDispatcher_WithMissingHandler_ShouldThrowException()
    {
        // Arrange
        var harness = new RelayTestHarness();
        var dispatcher = harness.GetService<IStreamDispatcher>();
        var request = new TestStreamRequest<int>();

        // Act & Assert
        await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
        {
            await foreach (var item in dispatcher.DispatchAsync(request, CancellationToken.None))
            {
                // Should not reach here
            }
        });
    }

    [Fact]
    public async Task TestNotificationDispatcher_WithNoHandlers_ShouldCompleteSuccessfully()
    {
        // Arrange
        var harness = new RelayTestHarness();
        var dispatcher = harness.GetService<INotificationDispatcher>();
        var notification = new TestNotification();

        // Act & Assert - Should not throw
        await dispatcher.DispatchAsync(notification, CancellationToken.None);
    }

    [Fact]
    public async Task TestRequestDispatcher_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var handler = new TestDelayHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler);

        var dispatcher = harness.GetService<IRequestDispatcher>();
        var request = new TestDelayRequest { DelayMs = 1000 };

        using var cts = new CancellationTokenSource(100);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            dispatcher.DispatchAsync(request, cts.Token).AsTask());
    }

    [Fact]
    public async Task TestStreamDispatcher_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var handler = new TestDelayStreamHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler);

        var dispatcher = harness.GetService<IStreamDispatcher>();
        var request = new TestDelayStreamRequest { ItemCount = 10, DelayMs = 100 };

        using var cts = new CancellationTokenSource(250);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in dispatcher.DispatchAsync(request, cts.Token))
            {
                // Should be cancelled before completing all items
            }
        });
    }

    // Test helper classes
    private class TestVoidRequest : IRequest<Unit>
    {
    }

    private class TestVoidHandler : IRequestHandler<TestVoidRequest, Unit>
    {
        public bool WasCalled { get; private set; }

        public ValueTask<Unit> HandleAsync(TestVoidRequest request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return ValueTask.FromResult(Unit.Value);
        }
    }

    private class TestDelayRequest : IRequest<string>
    {
        public int DelayMs { get; set; }
    }

    private class TestDelayHandler : IRequestHandler<TestDelayRequest, string>
    {
        public async ValueTask<string> HandleAsync(TestDelayRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(request.DelayMs, cancellationToken);
            return "completed";
        }
    }

    private class TestDelayStreamRequest : IStreamRequest<int>
    {
        public int ItemCount { get; set; }
        public int DelayMs { get; set; }
    }

    private class TestDelayStreamHandler : IStreamHandler<TestDelayStreamRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(TestDelayStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < request.ItemCount; i++)
            {
                await Task.Delay(request.DelayMs, cancellationToken);
                yield return i;
            }
        }
    }
}