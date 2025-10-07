using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Pipeline.Interfaces
{
    /// <summary>
    /// Defines a request post-processor for a handler.
    /// Post-processors are executed after the main handler and all pipeline behaviors complete successfully.
    /// Multiple post-processors can be registered and will execute in registration order.
    /// Post-processors do not execute if an exception is thrown during handler execution.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being handled.</typeparam>
    /// <typeparam name="TResponse">The type of response from the handler.</typeparam>
    public interface IRequestPostProcessor<in TRequest, in TResponse>
    {
        /// <summary>
        /// Processes the request after the handler executes successfully.
        /// This method is called after all pipeline behaviors and the main handler complete.
        /// Use this for operations like logging, cleanup, notifications, or response transformation tracking.
        /// </summary>
        /// <param name="request">The request that was handled.</param>
        /// <param name="response">The response returned by the handler.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of post-processing.</returns>
        ValueTask ProcessAsync(TRequest request, TResponse response, CancellationToken cancellationToken);
    }
}
