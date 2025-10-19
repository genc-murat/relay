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

public class DataHandlingTests
{
    [Fact]
    public async Task Should_HandleLongData()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>, TransformationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var longData = new string('a', 10000);
        var request = new InputRequest { Data = longData };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Contains("[PREFIX]", result.Result);
        Assert.Contains("[SUFFIX]", result.Result);
        Assert.True(result.Result.Length > 10000);
    }

    [Fact]
    public async Task Should_HandleSpecialCharacters()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>, TransformationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "!@#$%^&*()" };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal("[PREFIX]!@#$%^&*()[SUFFIX]", result.Result);
    }

    [Fact]
    public async Task Should_HandleUnicodeCharacters()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>, TransformationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "こんにちは😀" };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Contains("こんにちは😀", result.Result);
    }

    [Fact]
    public async Task Should_HandleWhitespaceData()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>, TransformationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "   " };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal("[PREFIX]   [SUFFIX]", result.Result);
    }
}