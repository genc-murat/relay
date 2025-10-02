using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Pipeline
{
    /// <summary>
    /// Defines a request pre-processor for a handler.
    /// Pre-processors are executed before the main handler and all pipeline behaviors.
    /// Multiple pre-processors can be registered and will execute in registration order.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being handled.</typeparam>
    public interface IRequestPreProcessor<in TRequest>
    {
        /// <summary>
        /// Processes the request before the handler executes.
        /// This method is called before any pipeline behaviors and the main handler.
        /// Use this for operations like logging, validation preparation, or request enrichment.
        /// </summary>
        /// <param name="request">The request being processed.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of pre-processing.</returns>
        ValueTask ProcessAsync(TRequest request, CancellationToken cancellationToken);
    }
}
