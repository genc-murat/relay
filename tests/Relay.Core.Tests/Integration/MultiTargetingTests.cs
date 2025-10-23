using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Tests.Testing;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Integration;

/// <summary>
/// Tests to validate multi-targeting support for netstandard2.0 and modern .NET
/// </summary>
[IntegrationTest(Category = "MultiTargeting")]
public class MultiTargetingTests
{
    [Fact]
    public void Framework_ShouldSupportNetStandard20()
    {
        // Arrange & Act
        var assembly = typeof(IRelay).Assembly;

        // Assert
        // This test validates that the assembly can be loaded and used
        // The actual target framework validation would be done at build time
        Assert.NotNull(assembly);
        Assert.True(typeof(IRelay).IsInterface);
    }

    [Fact]
    public void ValueTask_ShouldBeSupported()
    {
        // Arrange & Act
        var valueTaskType = typeof(ValueTask);
        var genericValueTaskType = typeof(ValueTask<>);

        // Assert
        Assert.NotNull(valueTaskType);
        Assert.NotNull(genericValueTaskType);

        // Verify ValueTask constructors and properties are available
        var closedGenericType = typeof(ValueTask<string>);
        var constructors = closedGenericType.GetConstructors();
        Assert.NotEmpty(constructors);

        // Verify ValueTask can be created from a result
        var valueTaskInstance = new ValueTask<string>("test");
        Assert.True(valueTaskInstance.IsCompleted);
    }

    [Fact]
    public async Task AsyncEnumerable_ShouldBeSupported()
    {
        // Arrange
        var handler = new MultiTargetStreamHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler);

        var relay = harness.Build();
        var request = new MultiTargetStreamRequest { Count = 3 };

        // Act
        var results = new System.Collections.Generic.List<int>();
        await foreach (var item in relay.StreamAsync(request))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(new[] { 0, 1, 2 }, results);
    }

    [Fact]
    public void CancellationToken_ShouldBeSupported()
    {
        // Arrange & Act
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Assert
        Assert.NotNull(token);
        Assert.True(token.CanBeCanceled);
    }

    [Fact]
    public async Task TaskCompletedTask_ShouldBeSupported()
    {
        // Arrange & Act
        await Task.CompletedTask;

        // Assert
        Assert.NotNull(Task.CompletedTask);
        Assert.True(Task.CompletedTask.IsCompleted);
    }

    [Fact]
    public void GenericConstraints_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var requestType = typeof(IRequest<>);
        var handlerType = typeof(IRequestHandler<,>);

        // Assert
        Assert.True(requestType.IsGenericTypeDefinition);
        Assert.True(handlerType.IsGenericTypeDefinition);

        // Verify constraints work at runtime
        var concreteRequestType = requestType.MakeGenericType(typeof(string));
        var concreteHandlerType = handlerType.MakeGenericType(typeof(MultiTargetRequest), typeof(string));

        Assert.NotNull(concreteRequestType);
        Assert.NotNull(concreteHandlerType);
    }

    [Fact]
    public async Task DependencyInjection_ShouldWorkAcrossTargets()
    {
        // Arrange
        var handler = new MultiTargetHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler);

        var relay = harness.Build();
        var request = new MultiTargetRequest { Value = "test" };

        // Act
        var result = await relay.SendAsync(request);

        // Assert
        Assert.Equal("Handled: test", result);
        Assert.True(handler.WasCalled);
    }

    [Fact]
    public void AttributeUsage_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var handleAttribute = new HandleAttribute { Name = "test", Priority = 1 };
        var notificationAttribute = new NotificationAttribute { DispatchMode = NotificationDispatchMode.Sequential };
        var pipelineAttribute = new PipelineAttribute { Order = 5, Scope = PipelineScope.Requests };

        // Assert
        Assert.Equal("test", handleAttribute.Name);
        Assert.Equal(1, handleAttribute.Priority);

        Assert.Equal(NotificationDispatchMode.Sequential, notificationAttribute.DispatchMode);

        Assert.Equal(5, pipelineAttribute.Order);
        Assert.Equal(PipelineScope.Requests, pipelineAttribute.Scope);
    }

    [Fact]
    public void ExceptionHandling_ShouldWorkAcrossTargets()
    {
        // Arrange & Act
        var relayException = new RelayException("TestRequest", "TestHandler", "Test message");
        var handlerNotFoundException = new HandlerNotFoundException("TestRequest");
        var multipleHandlersException = new MultipleHandlersException("TestRequest");

        // Assert
        Assert.Equal("TestRequest", relayException.RequestType);
        Assert.Equal("TestHandler", relayException.HandlerName);
        Assert.Equal("Test message", relayException.Message);

        Assert.IsType<RelayException>(handlerNotFoundException, exactMatch: false);
        Assert.IsType<RelayException>(multipleHandlersException, exactMatch: false);
    }

    [Fact]
    public async Task MemoryManagement_ShouldBeEfficient()
    {
        // Arrange
        var handler = new MultiTargetHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler)
            .WithoutTelemetry(); // Minimize allocations

        var relay = harness.Build();
        var request = new MultiTargetRequest { Value = "memory test" };

        // Act - Multiple calls to test memory efficiency
        for (int i = 0; i < 100; i++)
        {
            await relay.SendAsync(request);
        }

        // Assert
        Assert.Equal(100, handler.CallCount);

        // Force garbage collection to test for memory leaks
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // If we get here without OutOfMemoryException, memory management is working
        Assert.Equal(100, handler.CallCount);
    }

    [Fact]
    public async Task ThreadSafety_ShouldBeSupported()
    {
        // Arrange
        var handler = new MultiTargetHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler);

        var relay = harness.Build();

        // Act & Assert - Multiple threads should be able to use the same relay instance
        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var request = new MultiTargetRequest { Value = $"thread-{index}" };
                var result = await relay.SendAsync(request);
                Assert.Equal($"Handled: thread-{index}", result);
            });
        }

        await Task.WhenAll(tasks);
        Assert.Equal(10, handler.CallCount);
    }

    // Test classes
    private class MultiTargetRequest : IRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    private class MultiTargetStreamRequest : IStreamRequest<int>
    {
        public int Count { get; set; }
    }

    private class MultiTargetHandler : IRequestHandler<MultiTargetRequest, string>
    {
        private int _callCount;

        public bool WasCalled { get; private set; }
        public int CallCount => _callCount;

        public ValueTask<string> HandleAsync(MultiTargetRequest request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            Interlocked.Increment(ref _callCount);
            return ValueTask.FromResult($"Handled: {request.Value}");
        }
    }

    private class MultiTargetStreamHandler : IStreamHandler<MultiTargetStreamRequest, int>
    {
        public async System.Collections.Generic.IAsyncEnumerable<int> HandleAsync(
            MultiTargetStreamRequest request,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < request.Count; i++)
            {
                await Task.Delay(1, cancellationToken); // Minimal delay for async behavior
                yield return i;
            }
        }
    }
}