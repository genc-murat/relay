using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Xunit;
using InputRequest = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.InputRequest;
using OutputResponse = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.OutputResponse;
using TestTransformationHandler = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.TestTransformationHandler;
using MultiplePipeline1 = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.MultiplePipeline1;
using MultiplePipeline2 = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.MultiplePipeline2;

namespace Relay.Core.Tests.Pipeline;

public class MultiplePipelineTests
{
    [Fact]
    public async Task Should_ExecuteMultiplePipelinesInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>, MultiplePipeline1>();
        services.AddTransient<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>, MultiplePipeline2>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "X" };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        // Pipelines execute in order: 2->1->handler->1->2
        // Input: X -> 2X -> 12X -> 12X (uppercase)
        // Output: 12X -> 12X1 -> 12X12
        Assert.Contains("12X", result.Result);
    }
}