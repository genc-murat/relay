using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Xunit;
using InputRequest = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.InputRequest;
using OutputResponse = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.OutputResponse;
using TestTransformationHandler = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.TestTransformationHandler;
using DataValidationPipeline = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.DataValidationPipeline;
using ExceptionThrowingPipeline = Relay.Core.Tests.Pipeline.TransformationPipelineTestUtilities.ExceptionThrowingPipeline;

namespace Relay.Core.Tests.Pipeline;

public class ValidationPipelineTests
{
    [Fact]
    public async Task Should_ThrowOnInvalidData()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>, DataValidationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await executor.ExecuteAsync<InputRequest, OutputResponse>(
                request,
                (r, c) => handler.HandleAsync(r, c),
                CancellationToken.None);
        });
    }

    [Fact]
    public async Task Should_PropagateExceptionFromPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<Relay.Core.Contracts.Pipeline.IPipelineBehavior<InputRequest, OutputResponse>, ExceptionThrowingPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await executor.ExecuteAsync<InputRequest, OutputResponse>(
                request,
                (r, c) => handler.HandleAsync(r, c),
                CancellationToken.None);
        });
    }

    [Fact]
    public async Task Should_HandleCancellation()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "test" };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await executor.ExecuteAsync<InputRequest, OutputResponse>(
                request,
                async (r, c) =>
                {
                    c.ThrowIfCancellationRequested();
                    return await handler.HandleAsync(r, c);
                },
                cts.Token);
        });
    }
}