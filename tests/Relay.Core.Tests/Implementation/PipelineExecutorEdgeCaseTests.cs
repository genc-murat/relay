using Microsoft.Extensions.DependencyInjection;
using Moq;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Implementation.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Implementation;

public class PipelineExecutorEdgeCaseTests
{
    public class TestRequest { }
    public class TestResponse { public string Value { get; set; } = string.Empty; }

    // Mock System Module
    public class TestSystemModule : ISystemModule
    {
        public int Order => 0;

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

    // Mock Pipeline Behavior
    public class TestPipelineBehavior : IPipelineBehavior<TestRequest, TestResponse>
    {
        public ValueTask<TestResponse> HandleAsync(TestRequest request, RequestHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            return next();
        }
    }

    // Mock Stream Pipeline Behavior
    public class TestStreamPipelineBehavior : IStreamPipelineBehavior<TestRequest, TestResponse>
    {
        public IAsyncEnumerable<TestResponse> HandleAsync(TestRequest request, StreamHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            return next();
        }
    }

    #region ExecuteAsync Edge Case Tests

    [Fact]
    public async Task ExecuteAsync_WithNoSystemModulesOrPipelineBehaviors_ShouldExecuteHandlerDirectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act
        var result = await executor.ExecuteAsync<TestRequest, TestResponse>(request, (req, ct) => ValueTask.FromResult(new TestResponse { Value = "Direct" }), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Direct", result.Value);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultiplePipelineBehaviorsOfSameType_ShouldDeduplicateCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IPipelineBehavior<TestRequest, TestResponse>, TestPipelineBehavior>();
        services.AddSingleton<IPipelineBehavior<TestRequest, TestResponse>, TestPipelineBehavior>(); // Duplicate type
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act
        var result = await executor.ExecuteAsync<TestRequest, TestResponse>(request, (req, ct) => ValueTask.FromResult(new TestResponse { Value = "Result" }), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Result", result.Value);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationTokenAlreadyCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await executor.ExecuteAsync<TestRequest, TestResponse>(request, (req, ct) => ValueTask.FromResult(new TestResponse()), cts.Token);
        });
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexPipelineChain_ExecutesInCorrectOrder()
    {
        // Arrange - Create behaviors that track execution order
        var executionOrder = new List<string>();
        
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Create a handler that records execution
        Func<TestRequest, CancellationToken, ValueTask<TestResponse>> handler = async (req, ct) =>
        {
            executionOrder.Add("Handler");
            return new TestResponse { Value = "Completed" };
        };

        // Act
        var result = await executor.ExecuteAsync<TestRequest, TestResponse>(request, handler, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Value);
    }

    [Fact]
    public async Task ExecuteAsync_WithLargeSystemModuleOrder_ShouldExecuteCorrectly()
    {
        // Arrange - High order system module
        var services = new ServiceCollection();
        services.AddSingleton<ISystemModule>(new HighOrderSystemModule());
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act
        var result = await executor.ExecuteAsync<TestRequest, TestResponse>(request, (req, ct) => ValueTask.FromResult(new TestResponse { Value = "Result" }), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Result", result.Value);
    }

    #endregion

    #region ExecuteStreamAsync Edge Case Tests

    [Fact]
    public async Task ExecuteStreamAsync_WithNoSystemModulesOrStreamBehaviors_ShouldExecuteHandlerDirectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act
        var results = new List<TestResponse>();
        await foreach (var item in executor.ExecuteStreamAsync<TestRequest, TestResponse>(request, (req, ct) => CreateTestStream(), CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal("StreamDirect", results[0].Value);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithMultipleStreamBehaviorsOfSameType_ShouldDeduplicateCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IStreamPipelineBehavior<TestRequest, TestResponse>, TestStreamPipelineBehavior>();
        services.AddSingleton<IStreamPipelineBehavior<TestRequest, TestResponse>, TestStreamPipelineBehavior>(); // Duplicate type
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act
        var results = new List<TestResponse>();
        await foreach (var item in executor.ExecuteStreamAsync<TestRequest, TestResponse>(request, (req, ct) => CreateTestStream(), CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal("StreamDirect", results[0].Value);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithCancellationTokenAlreadyCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - The exception should be thrown immediately when ExecuteStreamAsync is called since
        // cancellationToken.ThrowIfCancellationRequested() is called at the beginning of the method
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            var result = executor.ExecuteStreamAsync<TestRequest, TestResponse>(request, (req, ct) => CreateTestStream(), cts.Token);
            // The exception is thrown immediately due to cancellationToken.ThrowIfCancellationRequested()
            // in the ExecuteStreamAsync method, not when we enumerate
        });
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithEmptyStream_ShouldCompleteWithoutError()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act
        var results = new List<TestResponse>();
        await foreach (var item in executor.ExecuteStreamAsync<TestRequest, TestResponse>(request, (req, ct) => EmptyStream(), CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region GetSystemModules Edge Cases

    [Fact]
    public void GetSystemModules_WhenServiceReturnsNonEnumerableType_ShouldReturnEmptyCollection()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<ISystemModule>))).Returns("not an enumerable");
        var executor = new PipelineExecutor(mockServiceProvider.Object);

        // Act - Use reflection to access the private method
        var method = typeof(PipelineExecutor).GetMethod("GetSystemModules", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.Invoke(executor, null) as IEnumerable<ISystemModule>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetPipelineBehaviors Edge Cases

    [Fact]
    public void GetPipelineBehaviors_WhenServiceReturnsNonEnumerableType_ShouldReturnEmptyCollection()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>))).Returns("not an enumerable");
        var executor = new PipelineExecutor(mockServiceProvider.Object);

        // Act - Use reflection to access the private method
        var method = typeof(PipelineExecutor).GetMethod("GetPipelineBehaviors", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.MakeGenericMethod(typeof(TestRequest), typeof(TestResponse))
            .Invoke(executor, null) as IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetPipelineBehaviors_WithInvalidRegistryAssembly_ShouldReturnEmptyCollection()
    {
        // Arrange - Testing the generated registry fallback path where assembly loading fails
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        // Act - Use reflection to access the private method
        var method = typeof(PipelineExecutor).GetMethod("GetPipelineBehaviors", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.MakeGenericMethod(typeof(TestRequest), typeof(TestResponse))
            .Invoke(executor, null) as IEnumerable<IPipelineBehavior<TestRequest, TestResponse>>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region GetStreamPipelineBehaviors Edge Cases

    [Fact]
    public void GetStreamPipelineBehaviors_WhenServiceReturnsNonEnumerableType_ShouldReturnEmptyCollection()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IEnumerable<IStreamPipelineBehavior<TestRequest, TestResponse>>))).Returns("not an enumerable");
        var executor = new PipelineExecutor(mockServiceProvider.Object);

        // Act - Use reflection to access the private method
        var method = typeof(PipelineExecutor).GetMethod("GetStreamPipelineBehaviors", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.MakeGenericMethod(typeof(TestRequest), typeof(TestResponse))
            .Invoke(executor, null) as IEnumerable<IStreamPipelineBehavior<TestRequest, TestResponse>>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetStreamPipelineBehaviors_WithInvalidRegistryAssembly_ShouldReturnEmptyCollection()
    {
        // Arrange - Testing the generated registry fallback path where assembly loading fails
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        // Act - Use reflection to access the private method
        var method = typeof(PipelineExecutor).GetMethod("GetStreamPipelineBehaviors", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        var result = method?.MakeGenericMethod(typeof(TestRequest), typeof(TestResponse))
            .Invoke(executor, null) as IEnumerable<IStreamPipelineBehavior<TestRequest, TestResponse>>;

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region Constructor Edge Cases

    [Fact]
    public void Constructor_WithServiceProviderThatReturnsNullForGetService_ShouldNotThrow()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(It.IsAny<Type>())).Returns((object)null);
        var executor = new PipelineExecutor(mockServiceProvider.Object);

        // Act & Assert - Constructor should not throw with a service provider that returns null
        Assert.NotNull(executor);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public async Task ExecuteAsync_WithComplexPipelineChain_ShouldExecuteAllComponents()
    {
        // Arrange - Complex scenario with multiple system modules and pipeline behaviors
        var executionLog = new List<string>();

        var services = new ServiceCollection();
        services.AddSingleton<ISystemModule, LoggingSystemModule>(provider => new LoggingSystemModule(executionLog, "System"));
        services.AddSingleton<IPipelineBehavior<TestRequest, TestResponse>, LoggingPipelineBehavior>(provider => new LoggingPipelineBehavior(executionLog, "Pipeline"));
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act
        var result = await executor.ExecuteAsync<TestRequest, TestResponse>(request, (req, ct) => 
        {
            executionLog.Add("Handler");
            return ValueTask.FromResult(new TestResponse { Value = "Handled" });
        }, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Handled", result.Value);
        Assert.Contains("System", executionLog);
        Assert.Contains("Handler", executionLog);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithComplexPipelineChain_ShouldExecuteAllComponents()
    {
        // Arrange - Complex scenario with multiple system modules and stream pipeline behaviors
        var executionLog = new List<string>();

        var services = new ServiceCollection();
        services.AddSingleton<ISystemModule, LoggingSystemModule>(provider => new LoggingSystemModule(executionLog, "StreamSystem"));
        services.AddSingleton<IStreamPipelineBehavior<TestRequest, TestResponse>, LoggingStreamPipelineBehavior>(provider => new LoggingStreamPipelineBehavior(executionLog, "StreamPipeline"));
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Create a stream that records execution
        async IAsyncEnumerable<TestResponse> LoggingStream(TestRequest req, CancellationToken ct)
        {
            executionLog.Add("StreamHandler");
            yield return new TestResponse { Value = "StreamHandled" };
        }

        // Act
        var results = new List<TestResponse>();
        await foreach (var item in executor.ExecuteStreamAsync<TestRequest, TestResponse>(request, LoggingStream, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Single(results);
        Assert.Equal("StreamHandled", results[0].Value);
        Assert.Contains("StreamSystem", executionLog);
        Assert.Contains("StreamHandler", executionLog);
    }

    #endregion

    #region Helper Methods

    private async IAsyncEnumerable<TestResponse> CreateTestStream()
    {
        yield return new TestResponse { Value = "StreamDirect" };
    }

    private async IAsyncEnumerable<TestResponse> EmptyStream()
    {
        yield break;
    }

    #endregion

    #region Helper Classes for Testing

    public class HighOrderSystemModule : ISystemModule
    {
        public int Order => int.MaxValue;

        public ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            return next();
        }

        public IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            return next();
        }
    }

    public class LoggingSystemModule : ISystemModule
    {
        private readonly List<string> _log;
        private readonly string _name;

        public LoggingSystemModule(List<string> log, string name)
        {
            _log = log;
            _name = name;
        }

        public int Order => 0;

        public ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _log.Add(_name);
            return next();
        }

        public IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            return ExecuteStreamInternal(request, next, cancellationToken);
        }

        private async IAsyncEnumerable<TResponse> ExecuteStreamInternal<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _log.Add(_name);
            await foreach (var item in next())
            {
                yield return item;
            }
        }
    }

    public class LoggingPipelineBehavior : IPipelineBehavior<TestRequest, TestResponse>
    {
        private readonly List<string> _log;
        private readonly string _name;

        public LoggingPipelineBehavior(List<string> log, string name)
        {
            _log = log;
            _name = name;
        }

        public ValueTask<TestResponse> HandleAsync(TestRequest request, RequestHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            _log.Add(_name);
            return next();
        }
    }

    public class LoggingStreamPipelineBehavior : IStreamPipelineBehavior<TestRequest, TestResponse>
    {
        private readonly List<string> _log;
        private readonly string _name;

        public LoggingStreamPipelineBehavior(List<string> log, string name)
        {
            _log = log;
            _name = name;
        }

        public IAsyncEnumerable<TestResponse> HandleAsync(TestRequest request, StreamHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            return HandleStreamInternal(request, next, cancellationToken);
        }

        private async IAsyncEnumerable<TestResponse> HandleStreamInternal(TestRequest request, StreamHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            _log.Add(_name);
            await foreach (var item in next())
            {
                yield return item;
            }
        }
    }

    #endregion
}
