using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    /// <summary>
    /// Executes pipeline behaviors in the correct order with system modules having priority.
    /// </summary>
    public class PipelineExecutor
    {
        private readonly IServiceProvider _serviceProvider;

        public PipelineExecutor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Executes the pipeline for a request, running system modules first, then user pipelines, then the handler.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The request to process.</param>
        /// <param name="handler">The final handler to execute.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask containing the response.</returns>
        public ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(
            TRequest request, 
            Func<TRequest, CancellationToken, ValueTask<TResponse>> handler,
            CancellationToken cancellationToken)
        {
            // Get all system modules and sort by order
            var systemModules = GetSystemModules().OrderBy(m => m.Order).ToList();
            
            // Get all pipeline behaviors for this request type and de-duplicate by behavior type while preserving order
            var pipelineBehaviors = GetPipelineBehaviors<TRequest, TResponse>()
                .Aggregate(new List<IPipelineBehavior<TRequest, TResponse>>(), (acc, b) =>
                {
                    var idx = acc.FindIndex(existing => existing.GetType() == b.GetType());
                    if (idx >= 0) acc[idx] = b; else acc.Add(b);
                    return acc;
                });

            // Build the execution chain from the end backwards
            RequestHandlerDelegate<TResponse> next = () => handler(request, cancellationToken);

            // Add pipeline behaviors in reverse order
            for (int i = pipelineBehaviors.Count - 1; i >= 0; i--)
            {
                var behavior = pipelineBehaviors[i];
                var currentNext = next;
                next = () => behavior.HandleAsync(request, currentNext, cancellationToken);
            }

            // Add system modules in reverse order (they execute first)
            for (int i = systemModules.Count - 1; i >= 0; i--)
            {
                var module = systemModules[i];
                var currentNext = next;
                next = () => module.ExecuteAsync(request, currentNext, cancellationToken);
            }

            return next();
        }

        /// <summary>
        /// Executes the pipeline for a streaming request, running system modules first, then user pipelines, then the handler.
        /// </summary>
        /// <typeparam name="TRequest">The type of the streaming request.</typeparam>
        /// <typeparam name="TResponse">The type of the response items.</typeparam>
        /// <param name="request">The streaming request to process.</param>
        /// <param name="handler">The final streaming handler to execute.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An async enumerable of response items.</returns>
        public IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(
            TRequest request,
            Func<TRequest, CancellationToken, IAsyncEnumerable<TResponse>> handler,
            CancellationToken cancellationToken)
        {
            // Get all system modules and sort by order
            var systemModules = GetSystemModules().OrderBy(m => m.Order).ToList();
            
            // Get all stream pipeline behaviors for this request type and de-duplicate by behavior type while preserving order
            var streamBehaviors = GetStreamPipelineBehaviors<TRequest, TResponse>()
                .Aggregate(new List<IStreamPipelineBehavior<TRequest, TResponse>>(), (acc, b) =>
                {
                    var idx = acc.FindIndex(existing => existing.GetType() == b.GetType());
                    if (idx >= 0) acc[idx] = b; else acc.Add(b);
                    return acc;
                });

            // Build the execution chain from the end backwards
            StreamHandlerDelegate<TResponse> next = () => handler(request, cancellationToken);

            // Add stream pipeline behaviors in reverse order
            for (int i = streamBehaviors.Count - 1; i >= 0; i--)
            {
                var behavior = streamBehaviors[i];
                var currentNext = next;
                next = () => behavior.HandleAsync(request, currentNext, cancellationToken);
            }

            // Add system modules in reverse order (they execute first)
            for (int i = systemModules.Count - 1; i >= 0; i--)
            {
                var module = systemModules[i];
                var currentNext = next;
                next = () => module.ExecuteStreamAsync(request, currentNext, cancellationToken);
            }

            return next();
        }

        private IEnumerable<ISystemModule> GetSystemModules()
        {
            // Get system modules from DI container
            if (_serviceProvider.GetService(typeof(IEnumerable<ISystemModule>)) is IEnumerable<ISystemModule> modules)
            {
                return modules;
            }
            return Enumerable.Empty<ISystemModule>();
        }

        private IEnumerable<IPipelineBehavior<TRequest, TResponse>> GetPipelineBehaviors<TRequest, TResponse>()
        {
            // Prefer DI-registered pipeline behaviors if available
            try
            {
                var diResult = _serviceProvider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest, TResponse>>))
                    as IEnumerable<IPipelineBehavior<TRequest, TResponse>>;
                if (diResult != null && diResult.Any())
                {
                    return diResult;
                }
            }
            catch
            {
                // Ignore and fall back to generated registry
            }

            // Try to get pipeline behaviors from generated registry
            try
            {
                var registryType = Type.GetType("Relay.Generated.PipelineRegistry, " + GetType().Assembly.FullName);
                if (registryType != null)
                {
                    var method = registryType.GetMethod("GetPipelineBehaviors");
                    if (method != null)
                    {
                        var genericMethod = method.MakeGenericMethod(typeof(TRequest), typeof(TResponse));
                        var result = genericMethod.Invoke(null, new object[] { _serviceProvider });
                        if (result is IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
                        {
                            return behaviors;
                        }
                    }
                }
            }
            catch
            {
                // Fall back to empty collection if generated code is not available
            }
            
            return Enumerable.Empty<IPipelineBehavior<TRequest, TResponse>>();
        }

        private IEnumerable<IStreamPipelineBehavior<TRequest, TResponse>> GetStreamPipelineBehaviors<TRequest, TResponse>()
        {
            // Prefer DI-registered stream pipeline behaviors if available
            try
            {
                var diResult = _serviceProvider.GetService(typeof(IEnumerable<IStreamPipelineBehavior<TRequest, TResponse>>))
                    as IEnumerable<IStreamPipelineBehavior<TRequest, TResponse>>;
                if (diResult != null && diResult.Any())
                {
                    return diResult;
                }
            }
            catch
            {
                // Ignore and fall back to generated registry
            }

            // Try to get stream pipeline behaviors from generated registry
            try
            {
                var registryType = Type.GetType("Relay.Generated.PipelineRegistry, " + GetType().Assembly.FullName);
                if (registryType != null)
                {
                    var method = registryType.GetMethod("GetStreamPipelineBehaviors");
                    if (method != null)
                    {
                        var genericMethod = method.MakeGenericMethod(typeof(TRequest), typeof(TResponse));
                        var result = genericMethod.Invoke(null, new object[] { _serviceProvider });
                        if (result is IEnumerable<IStreamPipelineBehavior<TRequest, TResponse>> behaviors)
                        {
                            return behaviors;
                        }
                    }
                }
            }
            catch
            {
                // Fall back to empty collection if generated code is not available
            }
            
            return Enumerable.Empty<IStreamPipelineBehavior<TRequest, TResponse>>();
        }
    }
}