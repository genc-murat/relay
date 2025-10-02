using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Pipeline
{
    /// <summary>
    /// Pipeline behavior that executes all registered post-processors after the handler.
    /// This behavior should be registered late in the pipeline to ensure post-processors
    /// run after all other behaviors and the main handler complete successfully.
    /// Post-processors only execute if the handler completes without throwing an exception.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being handled.</typeparam>
    /// <typeparam name="TResponse">The type of response from the handler.</typeparam>
    public class RequestPostProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RequestPostProcessorBehavior<TRequest, TResponse>>? _logger;

        /// <summary>
        /// Initializes a new instance of the RequestPostProcessorBehavior class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving post-processors.</param>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        public RequestPostProcessorBehavior(
            IServiceProvider serviceProvider,
            ILogger<RequestPostProcessorBehavior<TRequest, TResponse>>? logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        /// <inheritdoc />
        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Execute the handler and get the response
            var response = await next().ConfigureAwait(false);

            // Get all registered post-processors for this request type
            var postProcessors = _serviceProvider
                .GetServices<IRequestPostProcessor<TRequest, TResponse>>()
                .ToList();

            if (postProcessors.Count > 0)
            {
                _logger?.LogDebug(
                    "Executing {Count} post-processor(s) for request type {RequestType}",
                    postProcessors.Count,
                    typeof(TRequest).Name);

                // Execute all post-processors in order
                foreach (var postProcessor in postProcessors)
                {
                    var processorType = postProcessor.GetType().Name;

                    _logger?.LogTrace(
                        "Executing post-processor {PostProcessorType} for request type {RequestType}",
                        processorType,
                        typeof(TRequest).Name);

                    try
                    {
                        await postProcessor.ProcessAsync(request, response, cancellationToken).ConfigureAwait(false);

                        _logger?.LogTrace(
                            "Post-processor {PostProcessorType} completed successfully",
                            processorType);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(
                            ex,
                            "Post-processor {PostProcessorType} failed for request type {RequestType}",
                            processorType,
                            typeof(TRequest).Name);
                        throw;
                    }
                }

                _logger?.LogDebug(
                    "All post-processors completed successfully for request type {RequestType}",
                    typeof(TRequest).Name);
            }

            return response;
        }
    }
}
