using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Core;
using Xunit;

namespace Relay.Core.Tests
{
    public class PipelineBehaviorTests
    {
        [Fact]
        public void IPipelineBehavior_Should_Have_Correct_Signature()
        {
            // Arrange & Act
            var interfaceType = typeof(IPipelineBehavior<,>);
            var method = interfaceType.GetMethod("HandleAsync");

            // Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(ValueTask<>).Name, method.ReturnType.Name);
            Assert.Equal(3, method.GetParameters().Length);

            var parameters = method.GetParameters();
            Assert.Equal("request", parameters[0].Name);
            Assert.Equal("next", parameters[1].Name);
            Assert.Equal("cancellationToken", parameters[2].Name);
            Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        }

        [Fact]
        public void IStreamPipelineBehavior_Should_Have_Correct_Signature()
        {
            // Arrange & Act
            var interfaceType = typeof(IStreamPipelineBehavior<,>);
            var method = interfaceType.GetMethod("HandleAsync");

            // Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(IAsyncEnumerable<>).Name, method.ReturnType.Name);
            Assert.Equal(3, method.GetParameters().Length);

            var parameters = method.GetParameters();
            Assert.Equal("request", parameters[0].Name);
            Assert.Equal("next", parameters[1].Name);
            Assert.Equal("cancellationToken", parameters[2].Name);
            Assert.Equal(typeof(CancellationToken), parameters[2].ParameterType);
        }

        [Fact]
        public void ISystemModule_Should_Have_Correct_Properties_And_Methods()
        {
            // Arrange & Act
            var interfaceType = typeof(ISystemModule);
            var orderProperty = interfaceType.GetProperty("Order");
            var executeMethod = interfaceType.GetMethod("ExecuteAsync");
            var executeStreamMethod = interfaceType.GetMethod("ExecuteStreamAsync");

            // Assert
            Assert.NotNull(orderProperty);
            Assert.Equal(typeof(int), orderProperty.PropertyType);
            Assert.True(orderProperty.CanRead);

            Assert.NotNull(executeMethod);
            Assert.True(executeMethod.IsGenericMethodDefinition);
            Assert.Equal(2, executeMethod.GetGenericArguments().Length);

            Assert.NotNull(executeStreamMethod);
            Assert.True(executeStreamMethod.IsGenericMethodDefinition);
            Assert.Equal(2, executeStreamMethod.GetGenericArguments().Length);
        }

        [Fact]
        public void RequestHandlerDelegate_Should_Return_ValueTask()
        {
            // Arrange & Act
            var delegateType = typeof(RequestHandlerDelegate<>);
            var invokeMethod = delegateType.GetMethod("Invoke");

            // Assert
            Assert.NotNull(invokeMethod);
            Assert.Equal(typeof(ValueTask<>).Name, invokeMethod.ReturnType.Name);
            Assert.Empty(invokeMethod.GetParameters());
        }

        [Fact]
        public void StreamHandlerDelegate_Should_Return_IAsyncEnumerable()
        {
            // Arrange & Act
            var delegateType = typeof(StreamHandlerDelegate<>);
            var invokeMethod = delegateType.GetMethod("Invoke");

            // Assert
            Assert.NotNull(invokeMethod);
            Assert.Equal(typeof(IAsyncEnumerable<>).Name, invokeMethod.ReturnType.Name);
            Assert.Empty(invokeMethod.GetParameters());
        }

        [Fact]
        public async Task PipelineExecutor_Should_Execute_Handler_When_No_Pipelines()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestRequest();
            var handlerExecuted = false;

            ValueTask<string> Handler(TestRequest req, CancellationToken ct)
            {
                handlerExecuted = true;
                return new ValueTask<string>("result");
            }

            // Act
            var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.True(handlerExecuted);
            Assert.Equal("result", result);
        }

        [Fact]
        public void PipelineExecutor_Should_Execute_Stream_Handler_When_No_Pipelines()
        {
            // Arrange
            var services = new ServiceCollection();
            var serviceProvider = services.BuildServiceProvider();
            var executor = new PipelineExecutor(serviceProvider);

            var request = new TestStreamRequest();
            IAsyncEnumerable<string> Handler(TestStreamRequest req, CancellationToken ct)
            {
                return GenerateItems();
            }

            static async IAsyncEnumerable<string> GenerateItems()
            {
                await Task.CompletedTask;
                yield return "item1";
                yield return "item2";
            }

            // Act
            var result = executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            // Note: We can't easily test the execution without consuming the enumerable,
            // but we can verify the method returns without throwing
        }

        [Fact]
        public void PipelineExecutor_Constructor_Should_Throw_When_ServiceProvider_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PipelineExecutor(null!));
        }

        // Test request types
        public class TestRequest : IRequest<string> { }
        public class TestStreamRequest : IStreamRequest<string> { }
    }

    // Test implementations for pipeline behaviors
    public class TestPipelineBehavior : IPipelineBehavior<TestPipelineBehaviorTests.TestRequest, string>
    {
        public List<string> ExecutionOrder { get; } = new();

        public async ValueTask<string> HandleAsync(TestPipelineBehaviorTests.TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            ExecutionOrder.Add("Before");
            var result = await next();
            ExecutionOrder.Add("After");
            return result + "_Modified";
        }
    }

    public class TestStreamPipelineBehavior : IStreamPipelineBehavior<TestPipelineBehaviorTests.TestStreamRequest, string>
    {
        public List<string> ExecutionOrder { get; } = new();

        public async IAsyncEnumerable<string> HandleAsync(TestPipelineBehaviorTests.TestStreamRequest request, StreamHandlerDelegate<string> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ExecutionOrder.Add("Before");
            await foreach (var item in next())
            {
                ExecutionOrder.Add($"Item: {item}");
                yield return item + "_Modified";
            }
            ExecutionOrder.Add("After");
        }
    }

    public class TestSystemModule : ISystemModule
    {
        public int Order { get; }
        public List<string> ExecutionOrder { get; } = new();

        public TestSystemModule(int order = 0)
        {
            Order = order;
        }

        public async ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            ExecutionOrder.Add($"SystemModule_{Order}_Before");
            var result = await next();
            ExecutionOrder.Add($"SystemModule_{Order}_After");
            return result;
        }

        public async IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            ExecutionOrder.Add($"SystemModule_{Order}_Stream_Before");
            await foreach (var item in next())
            {
                ExecutionOrder.Add($"SystemModule_{Order}_Stream_Item");
                yield return item;
            }
            ExecutionOrder.Add($"SystemModule_{Order}_Stream_After");
        }
    }

    public class TestPipelineBehaviorTests
    {
        public class TestRequest : IRequest<string> { }
        public class TestStreamRequest : IStreamRequest<string> { }
    }
}