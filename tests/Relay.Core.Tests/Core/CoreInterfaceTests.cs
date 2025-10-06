using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Relay.Core;
using Relay.Core.Tests.Testing;
using static Relay.Core.Tests.Testing.FluentAssertionsExtensions;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Pipeline;

namespace Relay.Core.Tests.Core;

/// <summary>
/// Comprehensive tests for core Relay interfaces
/// </summary>
public class CoreInterfaceTests
{
    [Fact]
    public void IRequest_ShouldBeMarkerInterface()
    {
        // Arrange & Act
        var requestType = typeof(IRequest);

        // Assert
        requestType.Should().BeInterface();
        requestType.GetMethods().Should().BeEmpty();
        requestType.GetProperties().Should().BeEmpty();
    }

    [Fact]
    public void IRequestWithResponse_ShouldBeMarkerInterface()
    {
        // Arrange & Act
        var requestType = typeof(IRequest<>);

        // Assert
        requestType.Should().BeInterface();
        requestType.IsGenericTypeDefinition.Should().BeTrue();
        requestType.GetMethods().Should().BeEmpty();
        requestType.GetProperties().Should().BeEmpty();
    }

    [Fact]
    public void IStreamRequest_ShouldBeMarkerInterface()
    {
        // Arrange & Act
        var requestType = typeof(IStreamRequest<>);

        // Assert
        requestType.Should().BeInterface();
        requestType.IsGenericTypeDefinition.Should().BeTrue();
        requestType.GetMethods().Should().BeEmpty();
        requestType.GetProperties().Should().BeEmpty();
    }

    [Fact]
    public void INotification_ShouldBeMarkerInterface()
    {
        // Arrange & Act
        var notificationType = typeof(INotification);

        // Assert
        notificationType.Should().BeInterface();
        notificationType.GetMethods().Should().BeEmpty();
        notificationType.GetProperties().Should().BeEmpty();
    }

    [Fact]
    public void IRelay_ShouldHaveCorrectMethods()
    {
        // Arrange & Act
        var relayType = typeof(IRelay);

        // Assert
        relayType.Should().BeInterface();

        var methods = relayType.GetMethods();
        methods.Should().HaveCount(4);

        // Check SendAsync with response
        methods.Should().Contain(m =>
            m.Name == "SendAsync" &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 2 &&
            m.ReturnType.IsGenericType &&
            m.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>));

        // Check SendAsync without response
        methods.Should().Contain(m =>
            m.Name == "SendAsync" &&
            !m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 2 &&
            m.ReturnType == typeof(ValueTask));

        // Check StreamAsync
        methods.Should().Contain(m =>
            m.Name == "StreamAsync" &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 2);

        // Check PublishAsync
        methods.Should().Contain(m =>
            m.Name == "PublishAsync" &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 2 &&
            m.ReturnType == typeof(ValueTask));
    }

    [Fact]
    public void IRequestHandler_ShouldHaveCorrectSignature()
    {
        // Arrange & Act
        var handlerType = typeof(IRequestHandler<,>);

        // Assert
        handlerType.Should().BeInterface();
        handlerType.IsGenericTypeDefinition.Should().BeTrue();
        handlerType.GetGenericArguments().Should().HaveCount(2);

        var methods = handlerType.GetMethods();
        methods.Should().HaveCount(1);

        var handleMethod = methods[0];
        handleMethod.Name.Should().Be("HandleAsync");
        handleMethod.GetParameters().Should().HaveCount(2);
        handleMethod.ReturnType.IsGenericType.Should().BeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(ValueTask<>));
    }

    [Fact]
    public void IStreamHandler_ShouldHaveCorrectSignature()
    {
        // Arrange & Act
        var handlerType = typeof(IStreamHandler<,>);

        // Assert
        handlerType.Should().BeInterface();
        handlerType.IsGenericTypeDefinition.Should().BeTrue();
        handlerType.GetGenericArguments().Should().HaveCount(2);

        var methods = handlerType.GetMethods();
        methods.Should().HaveCount(1);

        var handleMethod = methods[0];
        handleMethod.Name.Should().Be("HandleAsync");
        handleMethod.GetParameters().Should().HaveCount(2);
        handleMethod.ReturnType.IsGenericType.Should().BeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(IAsyncEnumerable<>));
    }

    [Fact]
    public void INotificationHandler_ShouldHaveCorrectSignature()
    {
        // Arrange & Act
        var handlerType = typeof(INotificationHandler<>);

        // Assert
        handlerType.Should().BeInterface();
        handlerType.IsGenericTypeDefinition.Should().BeTrue();
        handlerType.GetGenericArguments().Should().HaveCount(1);

        var methods = handlerType.GetMethods();
        methods.Should().HaveCount(1);

        var handleMethod = methods[0];
        handleMethod.Name.Should().Be("HandleAsync");
        handleMethod.GetParameters().Should().HaveCount(2);
        handleMethod.ReturnType.Should().Be(typeof(ValueTask));
    }

    [Fact]
    public void IPipelineBehavior_ShouldHaveCorrectSignature()
    {
        // Arrange & Act
        var pipelineType = typeof(IPipelineBehavior<,>);

        // Assert
        pipelineType.Should().BeInterface();
        pipelineType.IsGenericTypeDefinition.Should().BeTrue();
        pipelineType.GetGenericArguments().Should().HaveCount(2);

        var methods = pipelineType.GetMethods();
        methods.Should().HaveCount(1);

        var handleMethod = methods[0];
        handleMethod.Name.Should().Be("HandleAsync");
        handleMethod.GetParameters().Should().HaveCount(3);
        handleMethod.ReturnType.IsGenericType.Should().BeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(ValueTask<>));
    }

    [Fact]
    public void IStreamPipelineBehavior_ShouldHaveCorrectSignature()
    {
        // Arrange & Act
        var pipelineType = typeof(IStreamPipelineBehavior<,>);

        // Assert
        pipelineType.Should().BeInterface();
        pipelineType.IsGenericTypeDefinition.Should().BeTrue();
        pipelineType.GetGenericArguments().Should().HaveCount(2);

        var methods = pipelineType.GetMethods();
        methods.Should().HaveCount(1);

        var handleMethod = methods[0];
        handleMethod.Name.Should().Be("HandleAsync");
        handleMethod.GetParameters().Should().HaveCount(3);
        handleMethod.ReturnType.IsGenericType.Should().BeTrue();
        handleMethod.ReturnType.GetGenericTypeDefinition().Should().Be(typeof(IAsyncEnumerable<>));
    }

    [Fact]
    public void ConcreteRequest_ShouldImplementInterface()
    {
        // Arrange & Act
        var request = new TestConcreteRequest();

        // Assert
        request.Should().BeAssignableTo<IRequest<string>>();
    }

    [Fact]
    public void ConcreteVoidRequest_ShouldImplementInterface()
    {
        // Arrange & Act
        var request = new TestConcreteVoidRequest();

        // Assert
        request.Should().BeAssignableTo<IRequest>();
    }

    [Fact]
    public void ConcreteStreamRequest_ShouldImplementInterface()
    {
        // Arrange & Act
        var request = new TestConcreteStreamRequest();

        // Assert
        request.Should().BeAssignableTo<IStreamRequest<int>>();
    }

    [Fact]
    public void ConcreteNotification_ShouldImplementInterface()
    {
        // Arrange & Act
        var notification = new TestConcreteNotification();

        // Assert
        notification.Should().BeAssignableTo<INotification>();
    }

    [Fact]
    public void ConcreteHandler_ShouldImplementInterface()
    {
        // Arrange & Act
        var handler = new TestConcreteHandler();

        // Assert
        handler.Should().BeAssignableTo<IRequestHandler<TestConcreteRequest, string>>();
    }

    [Fact]
    public void ConcreteStreamHandler_ShouldImplementInterface()
    {
        // Arrange & Act
        var handler = new TestConcreteStreamHandler();

        // Assert
        handler.Should().BeAssignableTo<IStreamHandler<TestConcreteStreamRequest, int>>();
    }

    [Fact]
    public void ConcreteNotificationHandler_ShouldImplementInterface()
    {
        // Arrange & Act
        var handler = new TestConcreteNotificationHandler();

        // Assert
        handler.Should().BeAssignableTo<INotificationHandler<TestConcreteNotification>>();
    }

    [Fact]
    public void ConcretePipelineBehavior_ShouldImplementInterface()
    {
        // Arrange & Act
        var pipeline = new TestConcretePipelineBehavior();

        // Assert
        pipeline.Should().BeAssignableTo<IPipelineBehavior<TestConcreteRequest, string>>();
    }

    [Fact]
    public void ConcreteStreamPipelineBehavior_ShouldImplementInterface()
    {
        // Arrange & Act
        var pipeline = new TestConcreteStreamPipelineBehavior();

        // Assert
        pipeline.Should().BeAssignableTo<IStreamPipelineBehavior<TestConcreteStreamRequest, int>>();
    }

    // Test implementations
    private class TestConcreteRequest : IRequest<string>
    {
        public string Value { get; set; } = "test";
    }

    private class TestConcreteVoidRequest : IRequest
    {
        public string Value { get; set; } = "test";
    }

    private class TestConcreteStreamRequest : IStreamRequest<int>
    {
        public int Count { get; set; } = 5;
    }

    private class TestConcreteNotification : INotification
    {
        public string Message { get; set; } = "test";
    }

    private class TestConcreteHandler : IRequestHandler<TestConcreteRequest, string>
    {
        public ValueTask<string> HandleAsync(TestConcreteRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult($"Handled: {request.Value}");
        }
    }

    private class TestConcreteStreamHandler : IStreamHandler<TestConcreteStreamRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(TestConcreteStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < request.Count; i++)
            {
                await Task.Delay(1, cancellationToken);
                yield return i;
            }
        }
    }

    private class TestConcreteNotificationHandler : INotificationHandler<TestConcreteNotification>
    {
        public ValueTask HandleAsync(TestConcreteNotification notification, CancellationToken cancellationToken)
        {
            // Handle notification
            return ValueTask.CompletedTask;
        }
    }

    private class TestConcretePipelineBehavior : IPipelineBehavior<TestConcreteRequest, string>
    {
        public async ValueTask<string> HandleAsync(TestConcreteRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            var result = await next();
            return $"Pipeline: {result}";
        }
    }

    private class TestConcreteStreamPipelineBehavior : IStreamPipelineBehavior<TestConcreteStreamRequest, int>
    {
        public async IAsyncEnumerable<int> HandleAsync(TestConcreteStreamRequest request, StreamHandlerDelegate<int> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in next().WithCancellation(cancellationToken))
            {
                yield return item * 2; // Transform each item
            }
        }
    }
}