using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Xunit;
using InputRequest = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.InputRequest;
using OutputResponse = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.OutputResponse;
using TestTransformationHandler = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.TestTransformationHandler;
using TransformationPipeline = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.TransformationPipeline;

namespace Relay.Core.Tests.Pipeline;

public class BasicTransformationTests
{
    [Fact]
    public async Task Should_TransformRequestAndResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>, TransformationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "test" };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal("[PREFIX]TEST[SUFFIX]", result.Result);
    }

    [Fact]
    public async Task Should_PassThroughWithoutTransformation()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "hello" };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal("HELLO", result.Result);
    }

    [Fact]
    public async Task Should_HandleEmptyData()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>, TransformationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "" };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal("[PREFIX][SUFFIX]", result.Result);
    }
}
