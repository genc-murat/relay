using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Xunit;

namespace Relay.Core.Tests.Pipeline;

public class ErrorHandlingTests
{
    [Fact]
    public async Task PipelineExecutor_Should_Propagate_Cancellation_Token()
    {
        // Arrange
        var services = new ServiceCollection();
        var cancellationTokenSource = new CancellationTokenSource();
        var receivedTokens = new List<CancellationToken>();

        var behavior = new TestPipelineBehaviorWithCancellationCheck(receivedTokens);
        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>>>(new Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>[] { behavior });
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            receivedTokens.Add(ct);
            return new ValueTask<string>("result");
        }

        // Act
        var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, cancellationTokenSource.Token);

        // Assert
        Assert.Equal("result_Modified", result);
        // All delegates should receive the same cancellation token
        Assert.All(receivedTokens, token => Assert.Equal(cancellationTokenSource.Token, token));
    }

    [Fact]
    public async Task PipelineExecutor_Should_Fallback_When_DI_Fails()
    {
        // Arrange
        var services = new ServiceCollection();
        // Don't register any pipeline behaviors in DI
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            return new ValueTask<string>("result");
        }

        // Act - This should not throw even if generated registry doesn't exist
        var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

        // Assert
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task PipelineExecutor_Should_Propagate_Exceptions_From_Pipeline_Behaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        var exceptionBehavior = new TestPipelineBehaviorThatThrows();
        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>>>(new Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>[] { exceptionBehavior });
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            return new ValueTask<string>("result");
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None));
        Assert.Equal("Pipeline behavior exception", exception.Message);
    }

    [Fact]
    public async Task PipelineExecutor_Should_Propagate_Exceptions_From_System_Modules()
    {
        // Arrange
        var services = new ServiceCollection();
        var exceptionModule = new TestSystemModuleThatThrows();
        services.AddSingleton<Relay.Core.Contracts.Core.ISystemModule>(exceptionModule);
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            return new ValueTask<string>("result");
        }

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None));
        Assert.Equal("System module exception", exception.Message);
    }

    [Fact]
    public async Task PipelineExecutor_Should_Handle_Cancellation_During_Execution()
    {
        // Arrange
        var services = new ServiceCollection();
        var cancellationTokenSource = new CancellationTokenSource();
        var slowBehavior = new TestPipelineBehaviorWithDelay();
        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>>>(new Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>[] { slowBehavior });
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            return new ValueTask<string>("result");
        }

        // Act - Cancel immediately
        cancellationTokenSource.Cancel();

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await executor.ExecuteAsync<TestRequest, string>(request, Handler, cancellationTokenSource.Token));
    }

    [Fact]
    public async Task PipelineExecutor_Should_Handle_Empty_System_Modules_Collection()
    {
        // Arrange
        var services = new ServiceCollection();
        // Register empty collection of system modules
        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Core.ISystemModule>>(Array.Empty<Relay.Core.Contracts.Core.ISystemModule>());
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            return new ValueTask<string>("result");
        }

        // Act
        var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

        // Assert
        Assert.Equal("result", result);
    }

    [Fact]
    public async Task PipelineExecutor_Should_Handle_Empty_Pipeline_Behaviors_Collection()
    {
        // Arrange
        var services = new ServiceCollection();
        // Register empty collection of pipeline behaviors
        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>>>(Array.Empty<Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>>());
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            return new ValueTask<string>("result");
        }

        // Act
        var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

        // Assert
        Assert.Equal("result", result);
    }

    [Fact]
    public void PipelineExecutor_Should_Handle_Empty_Stream_Pipeline_Behaviors_Collection()
    {
        // Arrange
        var services = new ServiceCollection();
        // Register empty collection of stream pipeline behaviors
        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IStreamPipelineBehavior<TestStreamRequest, string>>>(Array.Empty<Relay.Core.Contracts.Pipeline.IStreamPipelineBehavior<TestStreamRequest, string>>());
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestStreamRequest();

        IAsyncEnumerable<string> Handler(TestStreamRequest req, CancellationToken ct)
        {
            return GenerateStreamItems();
        }

        static async IAsyncEnumerable<string> GenerateStreamItems()
        {
            await Task.CompletedTask;
            yield return "item1";
        }

        // Act
        var result = executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }
}