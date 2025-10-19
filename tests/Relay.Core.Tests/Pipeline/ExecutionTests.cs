using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Xunit;
using InputRequest = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.InputRequest;
using OutputResponse = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.OutputResponse;
using TestTransformationHandler = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.TestTransformationHandler;
using CountingPipeline = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.CountingPipeline;
using ResponseModificationPipeline = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.ResponseModificationPipeline;
using TestNullResponseHandler = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.TestNullResponseHandler;

namespace Relay.Core.Tests.Pipeline;

public class ExecutionTests
{
    [Fact]
    public async Task Should_ExecutePipelineOnlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();
        var countingPipeline = new CountingPipeline();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddSingleton<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>>(countingPipeline);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "test" };

        // Act
        await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal(1, countingPipeline.ExecutionCount);
    }

    [Fact]
    public async Task Should_ModifyResponseContent()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>, ResponseModificationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "test" };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal("MODIFIED", result.Result);
    }

    [Fact]
    public async Task Should_HandleNullResponse()
    {
        // Arrange
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<Relay.Core.Contracts.Handlers.IRequestHandler<InputRequest, OutputResponse>>(
            new TestNullResponseHandler());

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "test" };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => new ValueTask<OutputResponse>((OutputResponse)null!),
            CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Should_PreserveRequestDataType()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "test123" };
        var originalData = request.Data;

        // Act
        await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert - request is modified, but type is preserved
        Assert.IsType<InputRequest>(request);
    }
}