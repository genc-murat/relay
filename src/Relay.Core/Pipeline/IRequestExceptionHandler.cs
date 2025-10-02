using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Pipeline
{
    /// <summary>
    /// Defines the result of exception handling, indicating whether the exception was handled
    /// and optionally providing a response value.
    /// </summary>
    /// <typeparam name="TResponse">The type of response.</typeparam>
    public readonly struct ExceptionHandlerResult<TResponse>
    {
        /// <summary>
        /// Gets a value indicating whether the exception was handled.
        /// If true, the response value will be returned and the exception will not propagate.
        /// </summary>
        public bool Handled { get; }

        /// <summary>
        /// Gets the response value to return when the exception is handled.
        /// Only valid when Handled is true.
        /// </summary>
        public TResponse? Response { get; }

        private ExceptionHandlerResult(bool handled, TResponse? response)
        {
            Handled = handled;
            Response = response;
        }

        /// <summary>
        /// Creates a result indicating the exception was handled with the given response.
        /// The exception will not propagate and the response will be returned.
        /// </summary>
        /// <param name="response">The response to return.</param>
        /// <returns>An exception handler result.</returns>
        public static ExceptionHandlerResult<TResponse> Handle(TResponse response)
        {
            return new ExceptionHandlerResult<TResponse>(true, response);
        }

        /// <summary>
        /// Creates a result indicating the exception was not handled.
        /// The exception will continue to propagate.
        /// </summary>
        /// <returns>An exception handler result.</returns>
        public static ExceptionHandlerResult<TResponse> Unhandled()
        {
            return new ExceptionHandlerResult<TResponse>(false, default);
        }
    }

    /// <summary>
    /// Defines a handler for exceptions of a specific type that occur during request processing.
    /// Exception handlers can catch and handle exceptions, optionally providing a response value
    /// to return instead of propagating the exception.
    /// Multiple handlers can be registered for the same exception type and will execute in order.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being handled.</typeparam>
    /// <typeparam name="TResponse">The type of response from the handler.</typeparam>
    /// <typeparam name="TException">The type of exception to handle.</typeparam>
    public interface IRequestExceptionHandler<in TRequest, TResponse, in TException>
        where TException : Exception
    {
        /// <summary>
        /// Handles the exception that occurred during request processing.
        /// Return ExceptionHandlerResult.Handle(response) to suppress the exception and return a response.
        /// Return ExceptionHandlerResult.Unhandled() to let the exception propagate to the next handler.
        /// </summary>
        /// <param name="request">The request that was being processed when the exception occurred.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A ValueTask containing the exception handling result.</returns>
        ValueTask<ExceptionHandlerResult<TResponse>> HandleAsync(
            TRequest request,
            TException exception,
            CancellationToken cancellationToken);
    }
}
