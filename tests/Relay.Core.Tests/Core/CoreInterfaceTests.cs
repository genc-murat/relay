using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Pipeline;
using Xunit;

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
        Assert.True(requestType.IsInterface);
        Assert.Empty(requestType.GetMethods());
        Assert.Empty(requestType.GetProperties());
    }

    [Fact]
    public void IRequestWithResponse_ShouldBeMarkerInterface()
    {
        // Arrange & Act
        var requestType = typeof(IRequest<>);

        // Assert
        Assert.True(requestType.IsInterface);
        Assert.True(requestType.IsGenericTypeDefinition);
        Assert.Empty(requestType.GetMethods());
        Assert.Empty(requestType.GetProperties());
    }

    [Fact]
    public void IStreamRequest_ShouldBeMarkerInterface()
    {
        // Arrange & Act
        var requestType = typeof(IStreamRequest<>);

        // Assert
        Assert.True(requestType.IsInterface);
        Assert.True(requestType.IsGenericTypeDefinition);
        Assert.Empty(requestType.GetMethods());
        Assert.Empty(requestType.GetProperties());
    }

    [Fact]
    public void INotification_ShouldBeMarkerInterface()
    {
        // Arrange & Act
        var notificationType = typeof(INotification);

        // Assert
        Assert.True(notificationType.IsInterface);
        Assert.Empty(notificationType.GetMethods());
        Assert.Empty(notificationType.GetProperties());
    }

    [Fact]
    public void IRelay_ShouldHaveCorrectMethods()
    {
        // Arrange & Act
        var relayType = typeof(IRelay);

        // Assert
        Assert.True(relayType.IsInterface);

        var methods = relayType.GetMethods();
        Assert.Equal(4, methods.Length);

        // Check SendAsync with response
        Assert.Contains(methods, m =>
            m.Name == "SendAsync" &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 2 &&
            m.ReturnType.IsGenericType &&
            m.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>));

        // Check SendAsync without response
        Assert.Contains(methods, m =>
            m.Name == "SendAsync" &&
            !m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 2 &&
            m.ReturnType == typeof(ValueTask));

        // Check StreamAsync
        Assert.Contains(methods, m =>
            m.Name == "StreamAsync" &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 2);

        // Check PublishAsync
        Assert.Contains(methods, m =>
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
        Assert.True(handlerType.IsInterface);
        Assert.True(handlerType.IsGenericTypeDefinition);
        Assert.Equal(2, handlerType.GetGenericArguments().Length);

        var methods = handlerType.GetMethods();
        Assert.Single(methods);

        var handleMethod = methods[0];
        Assert.Equal("HandleAsync", handleMethod.Name);
        Assert.Equal(2, handleMethod.GetParameters().Length);
        Assert.True(handleMethod.ReturnType.IsGenericType);
        Assert.Equal(typeof(ValueTask<>), handleMethod.ReturnType.GetGenericTypeDefinition());
    }

    [Fact]
    public void IStreamHandler_ShouldHaveCorrectSignature()
    {
        // Arrange & Act
        var handlerType = typeof(IStreamHandler<,>);

        // Assert
        Assert.True(handlerType.IsInterface);
        Assert.True(handlerType.IsGenericTypeDefinition);
        Assert.Equal(2, handlerType.GetGenericArguments().Length);

        var methods = handlerType.GetMethods();
        Assert.Single(methods);

        var handleMethod = methods[0];
        Assert.Equal("HandleAsync", handleMethod.Name);
        Assert.Equal(2, handleMethod.GetParameters().Length);
        Assert.True(handleMethod.ReturnType.IsGenericType);
        Assert.Equal(typeof(IAsyncEnumerable<>), handleMethod.ReturnType.GetGenericTypeDefinition());
    }

    [Fact]
    public void INotificationHandler_ShouldHaveCorrectSignature()
    {
        // Arrange & Act
        var handlerType = typeof(INotificationHandler<>);

        // Assert
        Assert.True(handlerType.IsInterface);
        Assert.True(handlerType.IsGenericTypeDefinition);
        Assert.Single(handlerType.GetGenericArguments());

        var methods = handlerType.GetMethods();
        Assert.Single(methods);

        var handleMethod = methods[0];
        Assert.Equal("HandleAsync", handleMethod.Name);
        Assert.Equal(2, handleMethod.GetParameters().Length);
        Assert.Equal(typeof(ValueTask), handleMethod.ReturnType);
    }

    [Fact]
    public void IPipelineBehavior_ShouldHaveCorrectSignature()
    {
        // Arrange & Act
        var pipelineType = typeof(IPipelineBehavior<,>);

        // Assert
        Assert.True(pipelineType.IsInterface);
        Assert.True(pipelineType.IsGenericTypeDefinition);
        Assert.Equal(2, pipelineType.GetGenericArguments().Length);

        var methods = pipelineType.GetMethods();
        Assert.Single(methods);

        var handleMethod = methods[0];
        Assert.Equal("HandleAsync", handleMethod.Name);
        Assert.Equal(3, handleMethod.GetParameters().Length);
        Assert.True(handleMethod.ReturnType.IsGenericType);
        Assert.Equal(typeof(ValueTask<>), handleMethod.ReturnType.GetGenericTypeDefinition());
    }

    [Fact]
    public void IStreamPipelineBehavior_ShouldHaveCorrectSignature()
    {
        // Arrange & Act
        var pipelineType = typeof(IStreamPipelineBehavior<,>);

        // Assert
        Assert.True(pipelineType.IsInterface);
        Assert.True(pipelineType.IsGenericTypeDefinition);
        Assert.Equal(2, pipelineType.GetGenericArguments().Length);

        var methods = pipelineType.GetMethods();
        Assert.Single(methods);

        var handleMethod = methods[0];
        Assert.Equal("HandleAsync", handleMethod.Name);
        Assert.Equal(3, handleMethod.GetParameters().Length);
        Assert.True(handleMethod.ReturnType.IsGenericType);
        Assert.Equal(typeof(IAsyncEnumerable<>), handleMethod.ReturnType.GetGenericTypeDefinition());
    }

    [Fact]
    public void ConcreteRequest_ShouldImplementInterface()
    {
        // Arrange & Act
        var request = new TestConcreteRequest();

        // Assert
        Assert.IsAssignableFrom<IRequest<string>>(request);
    }

    [Fact]
    public void ConcreteVoidRequest_ShouldImplementInterface()
    {
        // Arrange & Act
        var request = new TestConcreteVoidRequest();

        // Assert
        Assert.IsAssignableFrom<IRequest>(request);
    }

    [Fact]
    public void ConcreteStreamRequest_ShouldImplementInterface()
    {
        // Arrange & Act
        var request = new TestConcreteStreamRequest();

        // Assert
        Assert.IsAssignableFrom<IStreamRequest<int>>(request);
    }

    [Fact]
    public void ConcreteNotification_ShouldImplementInterface()
    {
        // Arrange & Act
        var notification = new TestConcreteNotification();

        // Assert
        Assert.IsAssignableFrom<INotification>(notification);
    }

    [Fact]
    public void ConcreteHandler_ShouldImplementInterface()
    {
        // Arrange & Act
        var handler = new TestConcreteHandler();

        // Assert
        Assert.IsAssignableFrom<IRequestHandler<TestConcreteRequest, string>>(handler);
    }

    [Fact]
    public void ConcreteStreamHandler_ShouldImplementInterface()
    {
        // Arrange & Act
        var handler = new TestConcreteStreamHandler();

        // Assert
        Assert.IsAssignableFrom<IStreamHandler<TestConcreteStreamRequest, int>>(handler);
    }

    [Fact]
    public void ConcreteNotificationHandler_ShouldImplementInterface()
    {
        // Arrange & Act
        var handler = new TestConcreteNotificationHandler();

        // Assert
        Assert.IsAssignableFrom<INotificationHandler<TestConcreteNotification>>(handler);
    }

    [Fact]
    public void ConcretePipelineBehavior_ShouldImplementInterface()
    {
        // Arrange & Act
        var pipeline = new TestConcretePipelineBehavior();

        // Assert
        Assert.IsAssignableFrom<IPipelineBehavior<TestConcreteRequest, string>>(pipeline);
    }

    [Fact]
    public void ConcreteStreamPipelineBehavior_ShouldImplementInterface()
    {
        // Arrange & Act
        var pipeline = new TestConcreteStreamPipelineBehavior();

        // Assert
        Assert.IsAssignableFrom<IStreamPipelineBehavior<TestConcreteStreamRequest, int>>(pipeline);
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
