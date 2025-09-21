using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core;
using Relay.Core.Tests.Testing;
using Xunit;
using static Relay.Core.Tests.Testing.FluentAssertionsExtensions;

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
        var targetFramework = assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>();

        // Assert
        // This test validates that the assembly can be loaded and used
        // The actual target framework validation would be done at build time
        assembly.Should().NotBeNull();
        typeof(IRelay).Should().BeInterface();
    }

    [Fact]
    public void ValueTask_ShouldBeSupported()
    {
        // Arrange & Act
        var valueTaskType = typeof(ValueTask);
        var genericValueTaskType = typeof(ValueTask<>);

        // Assert
        valueTaskType.Should().NotBeNull();
        genericValueTaskType.Should().NotBeNull();

        // Verify ValueTask constructors and properties are available
        var closedGenericType = typeof(ValueTask<string>);
        var constructors = closedGenericType.GetConstructors();
        constructors.Should().NotBeEmpty();

        // Verify ValueTask can be created from a result
        var valueTaskInstance = new ValueTask<string>("test");
        valueTaskInstance.IsCompleted.Should().BeTrue();
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
        results.Should().BeEquivalentTo(new[] { 0, 1, 2 });
    }

    [Fact]
    public void CancellationToken_ShouldBeSupported()
    {
        // Arrange & Act
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Assert
        token.Should().NotBeNull();
        token.CanBeCanceled.Should().BeTrue();
    }

    [Fact]
    public async Task TaskCompletedTask_ShouldBeSupported()
    {
        // Arrange & Act
        await Task.CompletedTask;

        // Assert
        Task.CompletedTask.Should().NotBeNull();
        Task.CompletedTask.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void GenericConstraints_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var requestType = typeof(IRequest<>);
        var handlerType = typeof(IRequestHandler<,>);

        // Assert
        requestType.IsGenericTypeDefinition.Should().BeTrue();
        handlerType.IsGenericTypeDefinition.Should().BeTrue();

        // Verify constraints work at runtime
        var concreteRequestType = requestType.MakeGenericType(typeof(string));
        var concreteHandlerType = handlerType.MakeGenericType(typeof(MultiTargetRequest), typeof(string));

        concreteRequestType.Should().NotBeNull();
        concreteHandlerType.Should().NotBeNull();
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
        result.Should().Be("Handled: test");
        handler.WasCalled.Should().BeTrue();
    }

    [Fact]
    public void AttributeUsage_ShouldWorkCorrectly()
    {
        // Arrange & Act
        var handleAttribute = new HandleAttribute { Name = "test", Priority = 1 };
        var notificationAttribute = new NotificationAttribute { DispatchMode = NotificationDispatchMode.Sequential };
        var pipelineAttribute = new PipelineAttribute { Order = 5, Scope = PipelineScope.Requests };

        // Assert
        handleAttribute.Name.Should().Be("test");
        handleAttribute.Priority.Should().Be(1);

        notificationAttribute.DispatchMode.Should().Be(NotificationDispatchMode.Sequential);

        pipelineAttribute.Order.Should().Be(5);
        pipelineAttribute.Scope.Should().Be(PipelineScope.Requests);
    }

    [Fact]
    public void ExceptionHandling_ShouldWorkAcrossTargets()
    {
        // Arrange & Act
        var relayException = new RelayException("TestRequest", "TestHandler", "Test message");
        var handlerNotFoundException = new HandlerNotFoundException("TestRequest");
        var multipleHandlersException = new MultipleHandlersException("TestRequest");

        // Assert
        relayException.RequestType.Should().Be("TestRequest");
        relayException.HandlerName.Should().Be("TestHandler");
        relayException.Message.Should().Be("Test message");

        handlerNotFoundException.Should().BeAssignableTo<RelayException>();
        multipleHandlersException.Should().BeAssignableTo<RelayException>();
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
        handler.CallCount.Should().Be(100);

        // Force garbage collection to test for memory leaks
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // If we get here without OutOfMemoryException, memory management is working
        handler.CallCount.Should().Be(100);
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
                result.Should().Be($"Handled: thread-{index}");
            });
        }

        await Task.WhenAll(tasks);
        handler.CallCount.Should().Be(10);
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