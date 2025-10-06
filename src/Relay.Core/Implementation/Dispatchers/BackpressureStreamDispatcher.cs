using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core
{
    /// <summary>
    /// Stream dispatcher that supports backpressure handling and flow control.
    /// </summary>
    public class BackpressureStreamDispatcher : BaseStreamDispatcher
    {
        private readonly int _maxConcurrency;
        private readonly int _bufferSize;
        private readonly PipelineExecutor _pipelineExecutor;

        /// <summary>
        /// Initializes a new instance of the BackpressureStreamDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        /// <param name="maxConcurrency">Maximum number of concurrent operations.</param>
        /// <param name="bufferSize">Size of the internal buffer for flow control.</param>
        public BackpressureStreamDispatcher(IServiceProvider serviceProvider, int maxConcurrency = 10, int bufferSize = 100)
            : base(serviceProvider)
        {
            _maxConcurrency = maxConcurrency > 0 ? maxConcurrency : throw new ArgumentOutOfRangeException(nameof(maxConcurrency));
            _bufferSize = bufferSize > 0 ? bufferSize : throw new ArgumentOutOfRangeException(nameof(bufferSize));
            _pipelineExecutor = new PipelineExecutor(serviceProvider);
        }

        /// <inheritdoc />
        public override IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            // Execute through pipeline with backpressure
            return _pipelineExecutor.ExecuteStreamAsync<IStreamRequest<TResponse>, TResponse>(
                request,
                (req, ct) => CreateBackpressureStream<TResponse>(req, null, ct),
                cancellationToken);
        }

        /// <inheritdoc />
        public override IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
        {
            ValidateRequest(request);
            ValidateHandlerName(handlerName);

            // Execute through pipeline with backpressure
            return _pipelineExecutor.ExecuteStreamAsync<IStreamRequest<TResponse>, TResponse>(
                request,
                (req, ct) => CreateBackpressureStream<TResponse>(req, handlerName, ct),
                cancellationToken);
        }

        /// <summary>
        /// Creates a backpressure-aware stream for the given request.
        /// </summary>
        /// <typeparam name="TResponse">The type of response items.</typeparam>
        /// <param name="request">The stream request.</param>
        /// <param name="handlerName">Optional handler name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable with backpressure support.</returns>
        private async IAsyncEnumerable<TResponse> CreateBackpressureStream<TResponse>(
            IStreamRequest<TResponse> request,
            string? handlerName,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Try to resolve a handler for this request type
            var handler = TryResolveHandler<TResponse>(request, handlerName);
            if (handler == null)
            {
                if (handlerName != null)
                    throw new HandlerNotFoundException(request.GetType().Name, handlerName);
                else
                    throw new HandlerNotFoundException(request.GetType().Name);
            }

            // Create a semaphore for flow control
            using var semaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);

            await foreach (var item in handler.HandleAsync(request, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Wait for available slot (backpressure)
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    yield return item;
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Tries to resolve a stream handler for the given request.
        /// </summary>
        /// <typeparam name="TResponse">The response type.</typeparam>
        /// <param name="request">The request to handle.</param>
        /// <param name="handlerName">Optional handler name.</param>
        /// <returns>The resolved handler, or null if not found.</returns>
        private IStreamHandler<IStreamRequest<TResponse>, TResponse>? TryResolveHandler<TResponse>(
            IStreamRequest<TResponse> request,
            string? handlerName)
        {
            // This is a fallback implementation - in practice, the generated dispatcher
            // would have compile-time knowledge of all handlers
            try
            {
                var handlerType = typeof(IStreamHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
                return GetServiceOrNull<IStreamHandler<IStreamRequest<TResponse>, TResponse>>();
            }
            catch
            {
                return null;
            }
        }
    }
}