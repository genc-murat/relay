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

    public class ConditionalTransformationPipeline : IPipelineBehavior<InputRequest, OutputResponse>
    {
        public async ValueTask<OutputResponse> HandleAsync(
            InputRequest request,
            RequestHandlerDelegate<OutputResponse> next,
            CancellationToken cancellationToken)
        {
            if (request.Data.StartsWith("SKIP"))
            {
                return new OutputResponse { Result = "SKIPPED" };
            }

            return await next();
        }
    }

    [Fact]
    public async Task Should_ShortCircuitWithConditionalPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, ConditionalTransformationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new InputRequest { Data = "SKIP_ME" };

        // Act
        var result = await executor.ExecuteAsync<InputRequest, OutputResponse>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal("SKIPPED", result.Result);
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
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, TransformationPipeline>();

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

    public class DataValidationPipeline : IPipelineBehavior<InputRequest, OutputResponse>
    {
        public async ValueTask<OutputResponse> HandleAsync(
            InputRequest request,
            RequestHandlerDelegate<OutputResponse> next,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Data))
            {
                throw new ArgumentException("Data cannot be empty");
            }

            return await next();
        }
    }

    [Fact]
    public async Task Should_ThrowOnInvalidData()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, DataValidationPipeline>();

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

    public class CountingPipeline : IPipelineBehavior<InputRequest, OutputResponse>
    {
        public int ExecutionCount { get; private set; }

        public async ValueTask<OutputResponse> HandleAsync(
            InputRequest request,
            RequestHandlerDelegate<OutputResponse> next,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return await next();
        }
    }

    [Fact]
    public async Task Should_ExecutePipelineOnlyOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();
        var countingPipeline = new CountingPipeline();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddSingleton<IPipelineBehavior<InputRequest, OutputResponse>>(countingPipeline);

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

    public class ResponseModificationPipeline : IPipelineBehavior<InputRequest, OutputResponse>
    {
        public async ValueTask<OutputResponse> HandleAsync(
            InputRequest request,
            RequestHandlerDelegate<OutputResponse> next,
            CancellationToken cancellationToken)
        {
            var response = await next();
            response.Result = response.Result.Replace("TEST", "MODIFIED");
            return response;
        }
    }

    [Fact]
    public async Task Should_ModifyResponseContent()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, ResponseModificationPipeline>();

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
        services.AddSingleton<IRequestHandler<InputRequest, OutputResponse>>(
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

    private class TestNullResponseHandler : IRequestHandler<InputRequest, OutputResponse>
    {
        public ValueTask<OutputResponse> HandleAsync(InputRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<OutputResponse>((OutputResponse)null!);
        }
    }

    public class ChainedTransformationPipeline : IPipelineBehavior<InputRequest, OutputResponse>
    {
        private readonly int _number;

        public ChainedTransformationPipeline(int number)
        {
            _number = number;
        }

        public async ValueTask<OutputResponse> HandleAsync(
            InputRequest request,
            RequestHandlerDelegate<OutputResponse> next,
            CancellationToken cancellationToken)
        {
            request.Data = $"{request.Data}-{_number}";
            var response = await next();
            response.Result = $"{_number}-{response.Result}";
            return response;
        }
    }

    [Fact]
    public async Task Should_HandleLongData()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, TransformationPipeline>();

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
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, TransformationPipeline>();

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
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, TransformationPipeline>();

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

    public class ExceptionThrowingPipeline : IPipelineBehavior<InputRequest, OutputResponse>
    {
        public ValueTask<OutputResponse> HandleAsync(
            InputRequest request,
            RequestHandlerDelegate<OutputResponse> next,
            CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Pipeline error");
        }
    }

    [Fact]
    public async Task Should_PropagateExceptionFromPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, ExceptionThrowingPipeline>();

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

    public class AsyncTransformationPipeline : IPipelineBehavior<InputRequest, OutputResponse>
    {
        public async ValueTask<OutputResponse> HandleAsync(
            InputRequest request,
            RequestHandlerDelegate<OutputResponse> next,
            CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            request.Data = $"ASYNC-{request.Data}";
            var response = await next();
            await Task.Delay(10, cancellationToken);
            response.Result = $"{response.Result}-ASYNC";
            return response;
        }
    }

    [Fact]
    public async Task Should_HandleAsyncTransformations()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, AsyncTransformationPipeline>();

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

    [Fact]
    public async Task Should_HandleWhitespaceData()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransformationHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<InputRequest, OutputResponse>, TransformationPipeline>();

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
