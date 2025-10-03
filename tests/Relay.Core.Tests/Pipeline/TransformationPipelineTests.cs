using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Relay.Core.Tests.Pipeline;

public class TransformationPipelineTests
{
    public class InputRequest : IRequest<OutputResponse>
    {
        public string Data { get; set; } = "";
    }

    public class OutputResponse
    {
        public string Result { get; set; } = "";
    }

    public class TestTransformationHandler : IRequestHandler<InputRequest, OutputResponse>
    {
        public ValueTask<OutputResponse> HandleAsync(InputRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<OutputResponse>(new OutputResponse 
            { 
                Result = request.Data.ToUpperInvariant() 
            });
        }
    }

    public class TransformationPipeline : IPipelineBehavior<InputRequest, OutputResponse>
    {
        public async ValueTask<OutputResponse> HandleAsync(
            InputRequest request,
            RequestHandlerDelegate<OutputResponse> next,
            CancellationToken cancellationToken)
        {
            // Pre-process: Add prefix
            request.Data = $"[PREFIX]{request.Data}";
            
            var response = await next();
            
            // Post-process: Add suffix
            response.Result = $"{response.Result}[SUFFIX]";
            
            return response;
        }
    }

    [Fact]
    public async Task Should_TransformRequestAndResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, TransformationPipeline>();

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

    public class MultiplePipeline1 : IPipelineBehavior<InputRequest, OutputResponse>
    {
        public async ValueTask<OutputResponse> HandleAsync(
            InputRequest request,
            RequestHandlerDelegate<OutputResponse> next,
            CancellationToken cancellationToken)
        {
            request.Data = $"1{request.Data}";
            var response = await next();
            response.Result = $"{response.Result}1";
            return response;
        }
    }

    public class MultiplePipeline2 : IPipelineBehavior<InputRequest, OutputResponse>
    {
        public async ValueTask<OutputResponse> HandleAsync(
            InputRequest request,
            RequestHandlerDelegate<OutputResponse> next,
            CancellationToken cancellationToken)
        {
            request.Data = $"2{request.Data}";
            var response = await next();
            response.Result = $"{response.Result}2";
            return response;
        }
    }

    [Fact]
    public async Task Should_ExecuteMultiplePipelinesInOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, MultiplePipeline1>();
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, MultiplePipeline2>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "X" };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        // Pipelines execute in order: 1->2->handler->2->1
        // Input: X -> 1X -> 21X -> 21X (uppercase)
        // Output: 21X -> 21X2 -> 21X21
        Assert.Contains("21X", result.Result);
    }
}
