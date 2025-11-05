using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Tests.Pipeline;

/// <summary>
/// Shared test utilities for TransformationPipeline tests
/// </summary>
public static class TransformationPipelineTestUtilities
{
    // Test classes
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

    public class TestNullResponseHandler : IRequestHandler<InputRequest, OutputResponse>
    {
        public ValueTask<OutputResponse> HandleAsync(InputRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<OutputResponse>((OutputResponse)null!);
        }
    }

    // Pipeline behaviors
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
}
