using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline.Interfaces;

namespace Relay.Core.Pipeline.Behaviors
{
    /// <summary>
    /// Pipeline behavior that executes all registered pre-processors before the handler.
    /// This behavior should be registered early in the pipeline to ensure pre-processors
    /// run before other behaviors like validation, caching, etc.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being handled.</typeparam>
    /// <typeparam name="TResponse">The type of response from the handler.</typeparam>
    public class RequestPreProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RequestPreProcessorBehavior<TRequest, TResponse>>? _logger;

        /// <summary>
        /// Initializes a new instance of the RequestPreProcessorBehavior class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving pre-processors.</param>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        public RequestPreProcessorBehavior(
            IServiceProvider serviceProvider,
            ILogger<RequestPreProcessorBehavior<TRequest, TResponse>>? logger = null)
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
            // Get all registered pre-processors for this request type
            var preProcessors = _serviceProvider
                .GetServices<IRequestPreProcessor<TRequest>>()
                .ToList();

            if (preProcessors.Count > 0)
            {
                _logger?.LogDebug(
                    "Executing {Count} pre-processor(s) for request type {RequestType}",
                    preProcessors.Count,
                    typeof(TRequest).Name);

                // Execute all pre-processors in order
                foreach (var preProcessor in preProcessors)
                {
                    var processorType = preProcessor.GetType().Name;

                    _logger?.LogTrace(
                        "Executing pre-processor {PreProcessorType} for request type {RequestType}",
                        processorType,
                        typeof(TRequest).Name);

                    try
                    {
                        await preProcessor.ProcessAsync(request, cancellationToken).ConfigureAwait(false);

                        _logger?.LogTrace(
                            "Pre-processor {PreProcessorType} completed successfully",
                            processorType);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(
                            ex,
                            "Pre-processor {PreProcessorType} failed for request type {RequestType}",
                            processorType,
                            typeof(TRequest).Name);
                        throw;
                    }
                }

                _logger?.LogDebug(
                    "All pre-processors completed successfully for request type {RequestType}",
                    typeof(TRequest).Name);
            }

            // Continue with the pipeline
            return await next().ConfigureAwait(false);
        }
    }
}
