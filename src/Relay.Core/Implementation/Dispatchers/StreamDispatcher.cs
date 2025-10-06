using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Relay.Core
{
    /// <summary>
    /// Default implementation of stream dispatcher that provides fallback behavior
    /// when no generated dispatcher is available.
    /// </summary>
    public class StreamDispatcher : BaseStreamDispatcher
    {
        private readonly PipelineExecutor _pipelineExecutor;

        /// <summary>
        /// Initializes a new instance of the StreamDispatcher class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for dependency resolution.</param>
        public StreamDispatcher(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _pipelineExecutor = new PipelineExecutor(serviceProvider);
        }

        /// <inheritdoc />
        public override IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken)
        {
            ValidateRequest(request);

            // Execute through pipeline
            return _pipelineExecutor.ExecuteStreamAsync<IStreamRequest<TResponse>, TResponse>(
                request,
                (req, ct) => ThrowHandlerNotFound<TResponse>(req.GetType()),
                cancellationToken);
        }

        /// <inheritdoc />
        public override IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(IStreamRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
        {
            ValidateRequest(request);
            ValidateHandlerName(handlerName);

            // Execute through pipeline
            return _pipelineExecutor.ExecuteStreamAsync<IStreamRequest<TResponse>, TResponse>(
                request,
                (req, ct) => ThrowHandlerNotFound<TResponse>(req.GetType(), handlerName),
                cancellationToken);
        }
    }
}