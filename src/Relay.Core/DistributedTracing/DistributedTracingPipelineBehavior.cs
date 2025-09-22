using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Configuration;

namespace Relay.Core.DistributedTracing
{
    /// <summary>
    /// A pipeline behavior that implements distributed tracing for requests.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class DistributedTracingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IDistributedTracingProvider _tracingProvider;
        private readonly ILogger<DistributedTracingPipelineBehavior<TRequest, TResponse>> _logger;
        private readonly IOptions<RelayOptions> _options;
        private readonly string _handlerKey;

        public DistributedTracingPipelineBehavior(
            IDistributedTracingProvider tracingProvider,
            ILogger<DistributedTracingPipelineBehavior<TRequest, TResponse>> logger,
            IOptions<RelayOptions> options)
        {
            _tracingProvider = tracingProvider ?? throw new ArgumentNullException(nameof(tracingProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _handlerKey = typeof(TRequest).FullName ?? typeof(TRequest).Name;
        }

        public async ValueTask<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Get distributed tracing configuration
            var tracingOptions = GetDistributedTracingOptions();
            var traceAttribute = typeof(TRequest).GetCustomAttribute<TraceAttribute>();

            // Check if distributed tracing is enabled for this request
            if (!IsDistributedTracingEnabled(tracingOptions, traceAttribute))
            {
                return await next();
            }

            // Get tracing parameters
            var (traceRequest, traceResponse, operationName) = GetTracingParameters(tracingOptions, traceAttribute);

            // Start tracing activity
            var tags = new Dictionary<string, object?>
            {
                ["request.type"] = typeof(TRequest).FullName ?? typeof(TRequest).Name,
                ["handler.key"] = _handlerKey
            };

            using var activity = _tracingProvider.StartActivity(operationName, typeof(TRequest), null, tags);
            
            if (activity == null)
            {
                // Tracing is not enabled, just proceed
                return await next();
            }

            try
            {
                _logger.LogDebug("Starting distributed trace for {OperationName}", operationName);

                // Add request information to trace if enabled
                if (traceRequest)
                {
                    AddRequestInfoToTrace(request);
                }

                // Execute the handler
                var response = await next();

                // Add response information to trace if enabled
                if (traceResponse)
                {
                    AddResponseInfoToTrace(response);
                }

                // Set activity status to OK
                _tracingProvider.SetActivityStatus(ActivityStatusCode.Ok);

                _logger.LogDebug("Completed distributed trace for {OperationName}", operationName);

                return response;
            }
            catch (Exception ex)
            {
                // Record exception in trace
                if (tracingOptions.RecordExceptions)
                {
                    _tracingProvider.RecordException(ex);
                    _tracingProvider.SetActivityStatus(ActivityStatusCode.Error, ex.Message);
                }

                _logger.LogError(ex, "Distributed trace failed for {OperationName}", operationName);

                throw;
            }
        }

        private DistributedTracingOptions GetDistributedTracingOptions()
        {
            // Check for handler-specific overrides
            if (_options.Value.DistributedTracingOverrides.TryGetValue(_handlerKey, out var handlerOptions))
            {
                return handlerOptions;
            }

            // Return default options
            return _options.Value.DefaultDistributedTracingOptions;
        }

        private static bool IsDistributedTracingEnabled(DistributedTracingOptions tracingOptions, TraceAttribute? traceAttribute)
        {
            // If distributed tracing is explicitly disabled globally, return false
            if (!tracingOptions.EnableAutomaticDistributedTracing && traceAttribute == null)
            {
                return false;
            }

            // If distributed tracing is enabled globally or explicitly enabled with TraceAttribute, return true
            return tracingOptions.EnableAutomaticDistributedTracing || traceAttribute != null;
        }

        private static (bool traceRequest, bool traceResponse, string operationName) GetTracingParameters(
            DistributedTracingOptions tracingOptions, TraceAttribute? traceAttribute)
        {
            if (traceAttribute != null)
            {
                var operationName = traceAttribute.OperationName ?? $"Process {typeof(TRequest).Name}";
                return (traceAttribute.TraceRequest, traceAttribute.TraceResponse, operationName);
            }
            
            var defaultOperationName = $"Process {typeof(TRequest).Name}";
            return (tracingOptions.TraceRequests, tracingOptions.TraceResponses, defaultOperationName);
        }

        private void AddRequestInfoToTrace(TRequest request)
        {
            var tags = new Dictionary<string, object?>
            {
                ["request.info"] = request?.ToString() ?? "null"
            };

            // In a real implementation, you would add more detailed request information here
            _tracingProvider.AddActivityTags(tags);
        }

        private void AddResponseInfoToTrace(TResponse response)
        {
            var tags = new Dictionary<string, object?>
            {
                ["response.info"] = response?.ToString() ?? "null"
            };

            // In a real implementation, you would add more detailed response information here
            _tracingProvider.AddActivityTags(tags);
        }
    }
}