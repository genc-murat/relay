using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI
{
    /// <summary>
    /// Represents a batch item waiting for processing.
    /// </summary>
    internal sealed class BatchItem<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        public TRequest Request { get; init; } = default!;
        public RequestHandlerDelegate<TResponse> Handler { get; init; } = default!;
        public CancellationToken CancellationToken { get; init; }
        public DateTime EnqueueTime { get; init; }
        public Guid BatchId { get; init; }
        public TaskCompletionSource<BatchExecutionResult<TResponse>> CompletionSource { get; } = new();
    }
}
