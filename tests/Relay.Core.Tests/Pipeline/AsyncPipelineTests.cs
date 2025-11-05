using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Xunit;
using InputRequest = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.InputRequest;
using OutputResponse = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.OutputResponse;
using TestTransformationHandler = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.TestTransformationHandler;
using AsyncTransformationPipeline = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.AsyncTransformationPipeline;

namespace Relay.Core.Tests.Pipeline;

public class AsyncPipelineTests
{
    [Fact]
    public async Task Should_HandleAsyncTransformations()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>, AsyncTransformationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "test" };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal("ASYNC-TEST-ASYNC", result.Result);
    }
}
