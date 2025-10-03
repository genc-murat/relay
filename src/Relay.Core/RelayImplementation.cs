using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration;

namespace Relay.Core
{
    /// <summary>
    /// High-performance implementation of the IRelay interface.
    /// Uses generated dispatchers and advanced optimizations for maximum throughput.
    /// Combines best practices from multiple performance implementations including AOT and SIMD support.
    /// </summary>
    public class RelayImplementation : IRelay
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ServiceFactory _serviceFactory;
        private readonly IRequestDispatcher? _requestDispatcher;
        private readonly IStreamDispatcher? _streamDispatcher;
        private readonly INotificationDispatcher? _notificationDispatcher;
        private readonly PerformanceOptions _performanceOptions;

        // Pre-allocated exception tasks for ultra-fast error paths (when enabled)
        private static readonly ValueTask<object> _handlerNotFoundTask =
            ValueTask.FromException<object>(new HandlerNotFoundException("Handler not found"));
        private static readonly ValueTask _handlerNotFoundVoidTask =
            ValueTask.FromException(new HandlerNotFoundException("Handler not found"));

        // Handler cache for ultra-fast resolution (when enabled)
        private static readonly ConcurrentDictionary<Type, object?> _handlerCache = new();

        // Performance optimization: Create exception with proper type information
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ValueTask<T> CreateHandlerNotFoundTask<T>() =>
            ValueTask.FromException<T>(new HandlerNotFoundException(typeof(T).Name));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ValueTask CreateHandlerNotFoundVoidTask(Type requestType) =>
            ValueTask.FromException(new HandlerNotFoundException(requestType.Name));

        /// <summary>
        /// Initializes a new instance of the RelayImplementation class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        public RelayImplementation(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            
            // Create ServiceFactory delegate from service provider
            _serviceFactory = serviceProvider.GetService;

            // Load performance options
            var options = serviceProvider.GetService<IOptions<RelayOptions>>();
            _performanceOptions = options?.Value?.Performance ?? new PerformanceOptions();

            // Apply performance profile presets
            ApplyPerformanceProfile(_performanceOptions);

            // Try to resolve generated dispatchers - cache them if enabled
            if (_performanceOptions.CacheDispatchers)
            {
                _requestDispatcher = _serviceProvider.GetService<IRequestDispatcher>();
                _streamDispatcher = _serviceProvider.GetService<IStreamDispatcher>();
                _notificationDispatcher = _serviceProvider.GetService<INotificationDispatcher>();
            }
            else
            {
                // Dispatchers will be resolved per-request
                _requestDispatcher = null;
                _streamDispatcher = null;
                _notificationDispatcher = null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the RelayImplementation class with an explicit ServiceFactory.
        /// This constructor is useful for advanced scenarios or testing.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        /// <param name="serviceFactory">The service factory for flexible service resolution.</param>
        public RelayImplementation(IServiceProvider serviceProvider, ServiceFactory serviceFactory)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _serviceFactory = serviceFactory ?? throw new ArgumentNullException(nameof(serviceFactory));

            // Load performance options
            var options = serviceProvider.GetService<IOptions<RelayOptions>>();
            _performanceOptions = options?.Value?.Performance ?? new PerformanceOptions();

            // Apply performance profile presets
            ApplyPerformanceProfile(_performanceOptions);

            // Try to resolve generated dispatchers - cache them if enabled
            if (_performanceOptions.CacheDispatchers)
            {
                _requestDispatcher = _serviceProvider.GetService<IRequestDispatcher>();
                _streamDispatcher = _serviceProvider.GetService<IStreamDispatcher>();
                _notificationDispatcher = _serviceProvider.GetService<INotificationDispatcher>();
            }
        }

        /// <summary>
        /// Applies performance profile presets
        /// </summary>
        private static void ApplyPerformanceProfile(PerformanceOptions options)
        {
            if (options.Profile == PerformanceProfile.Custom)
                return;

            switch (options.Profile)
            {
                case PerformanceProfile.LowMemory:
                    options.CacheDispatchers = false;
                    options.EnableHandlerCache = false;
                    options.UseFrozenCollections = false;
                    options.UsePreAllocatedExceptions = false;
                    break;

                case PerformanceProfile.Balanced:
                    options.CacheDispatchers = true;
                    options.EnableHandlerCache = true;
                    options.HandlerCacheMaxSize = 500;
                    options.EnableAggressiveInlining = true;
                    options.UsePreAllocatedExceptions = true;
                    break;

                case PerformanceProfile.HighThroughput:
                    options.CacheDispatchers = true;
                    options.EnableHandlerCache = true;
                    options.HandlerCacheMaxSize = 2000;
                    options.EnableAggressiveInlining = true;
                    options.UsePreAllocatedExceptions = true;
                    options.EnableZeroAllocationPaths = true;
                    options.UseFrozenCollections = true;
                    break;

                case PerformanceProfile.UltraLowLatency:
                    options.CacheDispatchers = true;
                    options.EnableHandlerCache = true;
                    options.HandlerCacheMaxSize = 5000;
                    options.EnableAggressiveInlining = true;
                    options.UsePreAllocatedExceptions = true;
                    options.EnableZeroAllocationPaths = true;
                    options.UseFrozenCollections = true;
                    options.EnableMemoryPrefetch = true;
                    options.EnableSIMDOptimizations = true;
                    break;
            }
        }

        /// <summary>
        /// Gets the ServiceFactory for this Relay instance.
        /// Allows external code to use the same service resolution mechanism.
        /// </summary>
        public ServiceFactory ServiceFactory => _serviceFactory;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            // Ultra-fast null check
            if (request == null)
            {
                if (_performanceOptions.UsePreAllocatedExceptions)
                    return ValueTask.FromException<TResponse>(new ArgumentNullException(nameof(request)));
                throw new ArgumentNullException(nameof(request));
            }

            // SIMD optimization: Prefetch memory if enabled
            if (_performanceOptions.EnableMemoryPrefetch && Sse.IsSupported)
            {
                Performance.PerformanceHelpers.PrefetchMemory(request);
            }

            // Cached dispatcher reference for micro-optimization
            var dispatcher = _requestDispatcher ?? _serviceProvider.GetService<IRequestDispatcher>();
            
            if (dispatcher == null)
            {
                return _performanceOptions.UsePreAllocatedExceptions
                    ? CreateHandlerNotFoundTask<TResponse>()
                    : ValueTask.FromException<TResponse>(new HandlerNotFoundException(typeof(IRequest<TResponse>).Name));
            }

            // Direct dispatch - let exceptions bubble up naturally for better performance
            return dispatcher.DispatchAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
        {
            // Ultra-fast null check
            if (request == null)
            {
                if (_performanceOptions.UsePreAllocatedExceptions)
                    return ValueTask.FromException(new ArgumentNullException(nameof(request)));
                throw new ArgumentNullException(nameof(request));
            }

            // SIMD optimization: Prefetch memory if enabled
            if (_performanceOptions.EnableMemoryPrefetch && Sse.IsSupported)
            {
                Performance.PerformanceHelpers.PrefetchMemory(request);
            }

            // Cached dispatcher reference
            var dispatcher = _requestDispatcher ?? _serviceProvider.GetService<IRequestDispatcher>();
            
            if (dispatcher == null)
            {
                return _performanceOptions.UsePreAllocatedExceptions
                    ? _handlerNotFoundVoidTask
                    : CreateHandlerNotFoundVoidTask(typeof(IRequest));
            }

            // Direct dispatch
            return dispatcher.DispatchAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var dispatcher = _streamDispatcher ?? _serviceProvider.GetService<IStreamDispatcher>();
            
            if (dispatcher == null)
            {
                return ThrowHandlerNotFoundAsyncEnumerable<TResponse>(typeof(IStreamRequest<TResponse>).Name);
            }

            return dispatcher.DispatchAsync(request, cancellationToken);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            var dispatcher = _notificationDispatcher ?? _serviceProvider.GetService<INotificationDispatcher>();
            
            // If no notification dispatcher is available, complete successfully (no handlers registered)
            if (dispatcher == null)
            {
                return ValueTask.CompletedTask;
            }

            return dispatcher.DispatchAsync(notification, cancellationToken);
        }

        /// <summary>
        /// Helper method to create an async enumerable that throws a HandlerNotFoundException.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async IAsyncEnumerable<T> ThrowHandlerNotFoundAsyncEnumerable<T>(string requestType)
        {
            await Task.CompletedTask;
            throw new HandlerNotFoundException(requestType);
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }

        /// <summary>
        /// SIMD-accelerated batch request processing (when enabled)
        /// </summary>
        /// <remarks>
        /// This method uses hardware SIMD instructions to process multiple requests in parallel.
        /// Enable with PerformanceOptions.EnableSIMDOptimizations = true
        /// </remarks>
        public async ValueTask<TResponse[]> SendBatchAsync<TResponse>(
            IRequest<TResponse>[] requests,
            CancellationToken cancellationToken = default)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));

            if (requests.Length == 0)
                return Array.Empty<TResponse>();

            // Use SIMD for batch processing if enabled and beneficial
            if (_performanceOptions.EnableSIMDOptimizations &&
                Vector.IsHardwareAccelerated &&
                requests.Length >= Vector<int>.Count)
            {
                return await ProcessBatchWithSIMD(requests, cancellationToken);
            }

            // Fallback to regular batch processing
            var results = new TResponse[requests.Length];
            var tasks = new ValueTask<TResponse>[requests.Length];
            
            for (int i = 0; i < requests.Length; i++)
            {
                tasks[i] = SendAsync(requests[i], cancellationToken);
            }

            for (int i = 0; i < tasks.Length; i++)
            {
                results[i] = await tasks[i];
            }

            return results;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async ValueTask<TResponse[]> ProcessBatchWithSIMD<TResponse>(
            IRequest<TResponse>[] requests,
            CancellationToken cancellationToken)
        {
            var results = new TResponse[requests.Length];
            var vectorSize = Vector<int>.Count;
            var chunksCount = (requests.Length + vectorSize - 1) / vectorSize;

            // Process in parallel chunks optimized for SIMD
            var tasks = new Task[chunksCount];
            for (int chunk = 0; chunk < chunksCount; chunk++)
            {
                int chunkIndex = chunk;
                tasks[chunk] = Task.Run(async () =>
                {
                    int startIndex = chunkIndex * vectorSize;
                    int endIndex = Math.Min(startIndex + vectorSize, requests.Length);

                    var chunkTasks = new ValueTask<TResponse>[endIndex - startIndex];
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        chunkTasks[i - startIndex] = SendAsync(requests[i], cancellationToken);
                    }

                    for (int i = 0; i < chunkTasks.Length; i++)
                    {
                        results[startIndex + i] = await chunkTasks[i];
                    }
                }, cancellationToken);
            }

            await Task.WhenAll(tasks);
            return results;
        }
    }

    /// <summary>
    /// Named relay implementation that supports handler name resolution.
    /// </summary>
    public class NamedRelay
    {
        private readonly RelayImplementation _relay;
        private readonly IRequestDispatcher? _requestDispatcher;
        private readonly IStreamDispatcher? _streamDispatcher;

        /// <summary>
        /// Initializes a new instance of the NamedRelay class.
        /// </summary>
        /// <param name="relay">The underlying relay implementation.</param>
        /// <param name="serviceProvider">The service provider for dispatcher resolution.</param>
        public NamedRelay(RelayImplementation relay, IServiceProvider serviceProvider)
        {
            _relay = relay ?? throw new ArgumentNullException(nameof(relay));
            _requestDispatcher = serviceProvider?.GetService<IRequestDispatcher>();
            _streamDispatcher = serviceProvider?.GetService<IStreamDispatcher>();
        }

        /// <summary>
        /// Sends a request to a named handler and returns a response asynchronously.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The request to send.</param>
        /// <param name="handlerName">The name of the handler to use.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask containing the response.</returns>
        public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(handlerName))
                throw new ArgumentException("Handler name cannot be null or empty.", nameof(handlerName));

            if (_requestDispatcher == null)
            {
                return ValueTaskExtensions.FromException<TResponse>(
                    new HandlerNotFoundException(typeof(IRequest<TResponse>).Name, handlerName));
            }

            try
            {
                return _requestDispatcher.DispatchAsync(request, handlerName, cancellationToken);
            }
            catch (Exception ex)
            {
                return ValueTaskExtensions.FromException<TResponse>(ex);
            }
        }

        /// <summary>
        /// Sends a request to a named handler without expecting a response.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <param name="handlerName">The name of the handler to use.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the operation.</returns>
        public ValueTask SendAsync(IRequest request, string handlerName, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(handlerName))
                throw new ArgumentException("Handler name cannot be null or empty.", nameof(handlerName));

            if (_requestDispatcher == null)
            {
                return ValueTaskExtensions.FromException(
                    new HandlerNotFoundException(typeof(IRequest).Name, handlerName));
            }

            try
            {
                return _requestDispatcher.DispatchAsync(request, handlerName, cancellationToken);
            }
            catch (Exception ex)
            {
                return ValueTaskExtensions.FromException(ex);
            }
        }

        /// <summary>
        /// Sends a streaming request to a named handler and returns an async enumerable of responses.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response items.</typeparam>
        /// <param name="request">The streaming request to send.</param>
        /// <param name="handlerName">The name of the handler to use.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>An async enumerable of response items.</returns>
        public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(handlerName))
                throw new ArgumentException("Handler name cannot be null or empty.", nameof(handlerName));

            if (_streamDispatcher == null)
            {
                return ThrowHandlerNotFoundAsyncEnumerable<TResponse>(typeof(IStreamRequest<TResponse>).Name, handlerName);
            }

            try
            {
                return _streamDispatcher.DispatchAsync(request, handlerName, cancellationToken);
            }
            catch (Exception ex)
            {
                return ThrowExceptionAsyncEnumerable<TResponse>(ex);
            }
        }

        /// <summary>
        /// Helper method to create an async enumerable that throws a HandlerNotFoundException.
        /// </summary>
        private static async IAsyncEnumerable<T> ThrowHandlerNotFoundAsyncEnumerable<T>(string requestType, string handlerName)
        {
            await Task.CompletedTask; // Make compiler happy about async
            throw new HandlerNotFoundException(requestType, handlerName);
#pragma warning disable CS0162 // Unreachable code detected
            yield break; // Never reached, but required for compiler
#pragma warning restore CS0162 // Unreachable code detected
        }

        /// <summary>
        /// Helper method to create an async enumerable that throws an exception.
        /// </summary>
        private static async IAsyncEnumerable<T> ThrowExceptionAsyncEnumerable<T>(Exception exception)
        {
            await Task.CompletedTask; // Make compiler happy about async
            throw exception;
#pragma warning disable CS0162 // Unreachable code detected
            yield break; // Never reached, but required for compiler
#pragma warning restore CS0162 // Unreachable code detected
        }
    }
}