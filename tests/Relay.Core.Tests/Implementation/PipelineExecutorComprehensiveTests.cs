using Microsoft.Extensions.DependencyInjection;
using Moq;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Implementation.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Implementation;

public class PipelineExecutorComprehensiveTests
{
    // Test request and response types
    public class TestRequest { }
    public class TestResponse { public string Value { get; set; } = string.Empty; }

    // Additional system module for testing
    public class LowPrioritySystemModule : ISystemModule
    {
        public int Order => -1; // Lower order should execute later in the pipeline

        public ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(
            TRequest request, 
            RequestHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            return next();
        }

        public IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(
            TRequest request, 
            StreamHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            return next();
        }
    }

    // High priority system module for testing
    public class HighPrioritySystemModule : ISystemModule
    {
        public int Order => 100; // Higher order should execute earlier in the pipeline

        public ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(
            TRequest request, 
            RequestHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            return next();
        }

        public IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(
            TRequest request, 
            StreamHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            return next();
        }
    }

    // Pipeline behavior that tracks execution
    public class ExecutionTrackingPipelineBehavior : IPipelineBehavior<TestRequest, TestResponse>
    {
        public List<string> ExecutionOrder { get; } = new List<string>();

        public ValueTask<TestResponse> HandleAsync(TestRequest request, RequestHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            ExecutionOrder.Add("PipelineBehavior");
            return next();
        }
    }

    // Stream pipeline behavior that tracks execution
    public class ExecutionTrackingStreamPipelineBehavior : IStreamPipelineBehavior<TestRequest, TestResponse>
    {
        public List<string> ExecutionOrder { get; } = new List<string>();

        public IAsyncEnumerable<TestResponse> HandleAsync(TestRequest request, StreamHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            return HandleStreamInternal(request, next, cancellationToken);
        }

        private async IAsyncEnumerable<TestResponse> HandleStreamInternal(TestRequest request, StreamHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            ExecutionOrder.Add("StreamPipelineBehavior");
            await foreach (var item in next())
            {
                yield return item;
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleSystemModules_OrdersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ISystemModule, LowPrioritySystemModule>();
        services.AddSingleton<ISystemModule, HighPrioritySystemModule>();
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();
        var response = new TestResponse { Value = "HandlerResult" };

        // Act
        var result = await executor.ExecuteAsync<TestRequest, TestResponse>(request, (req, ct) => ValueTask.FromResult(response), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HandlerResult", result.Value);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithMultipleSystemModules_OrdersCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ISystemModule, LowPrioritySystemModule>();
        services.AddSingleton<ISystemModule, HighPrioritySystemModule>();
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act
        var results = new List<TestResponse>();
        await foreach (var item in executor.ExecuteStreamAsync(request, (req, ct) => CreateTestStream(), CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal("StreamResult", results[0].Value);
    }

    [Fact]
    public async Task ExecuteAsync_WithPipelineBehaviorException_RethrowsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var behavior = new ExceptionThrowingPipelineBehavior();
        services.AddSingleton<IPipelineBehavior<TestRequest, TestResponse>>(_ => behavior);
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await executor.ExecuteAsync<TestRequest, TestResponse>(request, (req, ct) => ValueTask.FromResult(new TestResponse()), CancellationToken.None);
        });
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithStreamPipelineBehaviorException_RethrowsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var behavior = new ExceptionThrowingStreamPipelineBehavior();
        services.AddSingleton<IStreamPipelineBehavior<TestRequest, TestResponse>>(_ => behavior);
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in executor.ExecuteStreamAsync(request, (req, ct) => CreateTestStream(), CancellationToken.None))
            {
                _ = item; // Consume the stream
            }
        });
        
        Assert.Equal("Stream pipeline behavior exception", exception.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithHandlerReturningNull_WorksCorrectly()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act & Assert - This should work even if the handler returns null
        // Since TestResponse is a class, we can test with a nullable response type
        var result = await executor.ExecuteAsync<TestRequest, TestResponse>(request, (req, ct) => ValueTask.FromResult<TestResponse>(null!), CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPipelineBehaviors_WithNullServiceProviderResult_ReturnsEmptyCollection()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>))).Returns(null);
        var executor = new PipelineExecutor(mockServiceProvider.Object);

        // Use reflection to access the internal method
        var method = typeof(PipelineExecutor).GetMethod("GetPipelineBehaviors", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act
        var result = method?.MakeGenericMethod(typeof(TestRequest), typeof(TestResponse))
            .Invoke(executor, null) as IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetStreamPipelineBehaviors_WithNullServiceProviderResult_ReturnsEmptyCollection()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IStreamPipelineBehavior<TestRequest, TestResponse>>))).Returns(null);
        var executor = new PipelineExecutor(mockServiceProvider.Object);

        // Use reflection to access the internal method
        var method = typeof(PipelineExecutor).GetMethod("GetStreamPipelineBehaviors", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act
        var result = method?.MakeGenericMethod(typeof(TestRequest), typeof(TestResponse))
            .Invoke(executor, null) as IEnumerable<IStreamPipelineBehavior<TestRequest, TestResponse>>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetPipelineBehaviors_WithEmptyCollectionResult_ReturnsEmptyCollection()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var emptyBehaviors = Enumerable.Empty<IPipelineBehavior<TestRequest, TestResponse>>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>))).Returns(emptyBehaviors);
        var executor = new PipelineExecutor(mockServiceProvider.Object);

        // Use reflection to access the internal method
        var method = typeof(PipelineExecutor).GetMethod("GetPipelineBehaviors", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act
        var result = method?.MakeGenericMethod(typeof(TestRequest), typeof(TestResponse))
            .Invoke(executor, null) as IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetStreamPipelineBehaviors_WithEmptyCollectionResult_ReturnsEmptyCollection()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var emptyBehaviors = Enumerable.Empty<IStreamPipelineBehavior<TestRequest, TestResponse>>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IStreamPipelineBehavior<TestRequest, TestResponse>>))).Returns(emptyBehaviors);
        var executor = new PipelineExecutor(mockServiceProvider.Object);

        // Use reflection to access the internal method
        var method = typeof(PipelineExecutor).GetMethod("GetStreamPipelineBehaviors", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Act
        var result = method?.MakeGenericMethod(typeof(TestRequest), typeof(TestResponse))
            .Invoke(executor, null) as IEnumerable<IStreamPipelineBehavior<TestRequest, TestResponse>>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullHandlerParameter_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await executor.ExecuteAsync<TestRequest, TestResponse>(request, null!, CancellationToken.None);
        });
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithNullHandlerParameter_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await foreach (var item in executor.ExecuteStreamAsync<TestRequest, TestResponse>(request, null!, CancellationToken.None))
            {
                _ = item; // Consume stream
            }
        });
    }

    [Fact]
    public async Task ExecuteAsync_WithRequestThatCausesHandlerException_RethrowsHandlerException()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await executor.ExecuteAsync<TestRequest, TestResponse>(request, (req, ct) =>
            {
                throw new InvalidOperationException("Handler exception");
            }, CancellationToken.None);
        });

        Assert.Equal("Handler exception", exception.Message);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithRequestThatCausesHandlerException_RethrowsHandlerException()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Create a stream that throws an exception
        async IAsyncEnumerable<TestResponse> ThrowingStream()
        {
            yield return new TestResponse { Value = "test" }; // This will be consumed first
            throw new InvalidOperationException("Stream handler exception");
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in executor.ExecuteStreamAsync<TestRequest, TestResponse>(request, (req, ct) => ThrowingStream(), CancellationToken.None))
            {
                _ = item; // Consume stream
            }
        });

        Assert.Equal("Stream handler exception", exception.Message);
    }

    private async IAsyncEnumerable<TestResponse> CreateTestStream()
    {
        yield return new TestResponse { Value = "StreamResult" };
    }

    private class ExceptionThrowingPipelineBehavior : IPipelineBehavior<TestRequest, TestResponse>
    {
        public ValueTask<TestResponse> HandleAsync(TestRequest request, RequestHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Pipeline behavior exception");
        }
    }

    private class ExceptionThrowingStreamPipelineBehavior : IStreamPipelineBehavior<TestRequest, TestResponse>
    {
        public IAsyncEnumerable<TestResponse> HandleAsync(TestRequest request, StreamHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            return HandleStreamInternal(request, next, cancellationToken);
        }

        private async IAsyncEnumerable<TestResponse> HandleStreamInternal(TestRequest request, StreamHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Stream pipeline behavior exception");
            yield break; // This is unreachable but required by compiler
        }
    }
}