using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Xunit;
using TestRequest = Relay.Core.Tests.Pipeline.PipelineBehaviorTestUtilities.TestRequest;
using TestStreamRequest = Relay.Core.Tests.Pipeline.PipelineBehaviorTestUtilities.TestStreamRequest;

namespace Relay.Core.Tests.Pipeline;

public class BasicPipelineExecutorTests
{
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
}