using Microsoft.Extensions.DependencyInjection;
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

public class PipelineExecutorTests
{
    // Test request and response types
    public class TestRequest { }
    public class TestResponse { public string Value { get; set; } = ""; }

    // Mock system module
    public class TestSystemModule : ISystemModule
    {
        public int Order => 1;
        public string Result { get; set; } = "";

        public ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            Result = "SystemModule";
            return next();
        }

        public IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            return ExecuteStreamInternal(request, next, cancellationToken);
        }

        private async IAsyncEnumerable<TResponse> ExecuteStreamInternal<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            Result = "SystemModule";
            await foreach (var item in next())
            {
                yield return item;
            }
        }
    }

    // Mock pipeline behavior
    public class TestPipelineBehavior : IPipelineBehavior<TestRequest, TestResponse>
    {
        public string Result { get; set; } = "";

        public ValueTask<TestResponse> HandleAsync(TestRequest request, RequestHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            Result = "PipelineBehavior";
            return next();
        }
    }

    // Mock stream pipeline behavior
    public class TestStreamPipelineBehavior : IStreamPipelineBehavior<TestRequest, TestResponse>
    {
        public string Result { get; set; } = "";

        public IAsyncEnumerable<TestResponse> HandleAsync(TestRequest request, StreamHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            return HandleStreamInternal(request, next, cancellationToken);
        }

        private async IAsyncEnumerable<TestResponse> HandleStreamInternal(TestRequest request, StreamHandlerDelegate<TestResponse> next, CancellationToken cancellationToken)
        {
            Result = "StreamPipelineBehavior";
            await foreach (var item in next())
            {
                yield return item;
            }
        }
    }

    private IServiceProvider CreateServiceProvider(bool includeSystemModules = true, bool includePipelineBehaviors = true)
    {
        var services = new ServiceCollection();

        if (includeSystemModules)
        {
            services.AddSingleton<ISystemModule, TestSystemModule>();
        }

        if (includePipelineBehaviors)
        {
            services.AddSingleton<IPipelineBehavior<TestRequest, TestResponse>, TestPipelineBehavior>();
            services.AddSingleton<IStreamPipelineBehavior<TestRequest, TestResponse>, TestStreamPipelineBehavior>();
        }

        return services.BuildServiceProvider();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PipelineExecutor(null!));
    }

    [Fact]
    public void Constructor_WithValidServiceProvider_ShouldSucceed()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();

        // Act
        var executor = new PipelineExecutor(serviceProvider);

        // Assert
        Assert.NotNull(executor);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync<TestRequest, TestResponse>(null!, (req, ct) => ValueTask.FromResult(new TestResponse()), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task ExecuteAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteAsync<TestRequest, TestResponse>(request, null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task ExecuteAsync_WithValidParameters_ShouldExecutePipeline()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
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
    public async Task ExecuteAsync_WithSystemModules_ShouldExecuteSystemModulesFirst()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider(includeSystemModules: true, includePipelineBehaviors: false);
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();
        var response = new TestResponse { Value = "HandlerResult" };

        // Act
        var result = await executor.ExecuteAsync<TestRequest, TestResponse>(request, (req, ct) => ValueTask.FromResult(response), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HandlerResult", result.Value);

        // Verify system module was executed
        var systemModule = serviceProvider.GetService<ISystemModule>() as TestSystemModule;
        Assert.NotNull(systemModule);
        Assert.Equal("SystemModule", systemModule.Result);
    }

    [Fact]
    public async Task ExecuteAsync_WithPipelineBehaviors_ShouldExecuteBehaviors()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider(includeSystemModules: false, includePipelineBehaviors: true);
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();
        var response = new TestResponse { Value = "HandlerResult" };

        // Act
        var result = await executor.ExecuteAsync<TestRequest, TestResponse>(request, (req, ct) => ValueTask.FromResult(response), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HandlerResult", result.Value);

        // Verify pipeline behavior was executed
        var pipelineBehavior = serviceProvider.GetService<IPipelineBehavior<TestRequest, TestResponse>>() as TestPipelineBehavior;
        Assert.NotNull(pipelineBehavior);
        Assert.Equal("PipelineBehavior", pipelineBehavior.Result);
    }

    [Fact]
    public async Task ExecuteAsync_WithSystemModulesAndPipelineBehaviors_ShouldExecuteInCorrectOrder()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider(includeSystemModules: true, includePipelineBehaviors: true);
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();
        var response = new TestResponse { Value = "HandlerResult" };

        // Act
        var result = await executor.ExecuteAsync<TestRequest, TestResponse>(request, (req, ct) => ValueTask.FromResult(response), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("HandlerResult", result.Value);

        // Verify both system module and pipeline behavior were executed
        var systemModule = serviceProvider.GetService<ISystemModule>() as TestSystemModule;
        var pipelineBehavior = serviceProvider.GetService<IPipelineBehavior<TestRequest, TestResponse>>() as TestPipelineBehavior;

        Assert.NotNull(systemModule);
        Assert.NotNull(pipelineBehavior);
        Assert.Equal("SystemModule", systemModule.Result);
        Assert.Equal("PipelineBehavior", pipelineBehavior.Result);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteStreamAsync<TestRequest, TestResponse>(null!, (req, ct) => CreateTestStream(), CancellationToken.None).GetAsyncEnumerator().MoveNextAsync().AsTask());
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => executor.ExecuteStreamAsync<TestRequest, TestResponse>(request, null!, CancellationToken.None).GetAsyncEnumerator().MoveNextAsync().AsTask());
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithValidParameters_ShouldExecutePipeline()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
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
    public async Task ExecuteStreamAsync_WithSystemModules_ShouldExecuteSystemModulesFirst()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider(includeSystemModules: true, includePipelineBehaviors: false);
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

        // Verify system module was executed
        var systemModule = serviceProvider.GetService<ISystemModule>() as TestSystemModule;
        Assert.NotNull(systemModule);
        Assert.Equal("SystemModule", systemModule.Result);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithStreamPipelineBehaviors_ShouldExecuteBehaviors()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider(includeSystemModules: false, includePipelineBehaviors: true);
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

        // Verify stream pipeline behavior was executed
        var streamBehavior = serviceProvider.GetService<IStreamPipelineBehavior<TestRequest, TestResponse>>() as TestStreamPipelineBehavior;
        Assert.NotNull(streamBehavior);
        Assert.Equal("StreamPipelineBehavior", streamBehavior.Result);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithSystemModulesAndStreamBehaviors_ShouldExecuteInCorrectOrder()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider(includeSystemModules: true, includePipelineBehaviors: true);
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

        // Verify both system module and stream behavior were executed
        var systemModule = serviceProvider.GetService<ISystemModule>() as TestSystemModule;
        var streamBehavior = serviceProvider.GetService<IStreamPipelineBehavior<TestRequest, TestResponse>>() as TestStreamPipelineBehavior;

        Assert.NotNull(systemModule);
        Assert.NotNull(streamBehavior);
        Assert.Equal("SystemModule", systemModule.Result);
        Assert.Equal("StreamPipelineBehavior", streamBehavior.Result);
    }

    [Fact]
    public async Task ExecuteStreamAsync_WithCancellation_ShouldRespectCancellation()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);
        var request = new TestRequest();
        var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => { executor.ExecuteStreamAsync(request, (req, ct) => CreateTestStream(), cts.Token); return Task.CompletedTask; });
    }

    private async IAsyncEnumerable<TestResponse> CreateTestStream()
    {
        yield return new TestResponse { Value = "StreamResult" };
    }
}