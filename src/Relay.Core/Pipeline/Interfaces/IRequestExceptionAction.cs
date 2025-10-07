using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Pipeline.Interfaces
{
    /// <summary>
    /// Defines an action to perform when an exception occurs during request processing.
    /// Unlike IRequestExceptionHandler, actions cannot suppress exceptions - they are primarily
    /// used for side effects like logging, metrics, or notifications.
    /// The exception will continue to propagate after all actions execute.
    /// Multiple actions can be registered for the same exception type and will all execute.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being handled.</typeparam>
    /// <typeparam name="TException">The type of exception to handle.</typeparam>
    public interface IRequestExceptionAction<in TRequest, in TException>
        where TException : Exception
    {
        /// <summary>
        /// Executes an action in response to an exception during request processing.
        /// This method is called before the exception propagates, but cannot suppress the exception.
        /// Use this for logging, metrics collection, notifications, or other side effects.
        /// </summary>
        /// <param name="request">The request that was being processed when the exception occurred.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask representing the completion of the action.</returns>
        ValueTask ExecuteAsync(TRequest request, TException exception, CancellationToken cancellationToken);
    }
}
