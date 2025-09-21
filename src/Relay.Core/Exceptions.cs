using System;

namespace Relay.Core
{
    /// <summary>
    /// Base exception for Relay framework errors.
    /// </summary>
    public class RelayException : Exception
    {
        /// <summary>
        /// Gets the type name of the request that caused the exception.
        /// </summary>
        public string RequestType { get; }

        /// <summary>
        /// Gets the name of the handler, if applicable.
        /// </summary>
        public string? HandlerName { get; }

        /// <summary>
        /// Initializes a new instance of the RelayException class.
        /// </summary>
        /// <param name="requestType">The type name of the request.</param>
        /// <param name="handlerName">The name of the handler, if applicable.</param>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception, if any.</param>
        public RelayException(string requestType, string? handlerName, string message, Exception? innerException = null)
            : base(message, innerException)
        {
            RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
            HandlerName = handlerName;
        }

        /// <summary>
        /// Returns a string representation of the exception including request type and handler name.
        /// </summary>
        public override string ToString()
        {
            var result = base.ToString();
            if (!string.IsNullOrEmpty(RequestType))
            {
                result = $"RequestType: {RequestType}\n{result}";
            }
            if (!string.IsNullOrEmpty(HandlerName))
            {
                result = $"HandlerName: {HandlerName}\n{result}";
            }
            return result;
        }
    }

    /// <summary>
    /// Exception thrown when no handler is found for a request.
    /// </summary>
    public class HandlerNotFoundException : RelayException
    {
        /// <summary>
        /// Initializes a new instance of the HandlerNotFoundException class.
        /// </summary>
        /// <param name="requestType">The type name of the request.</param>
        public HandlerNotFoundException(string requestType)
            : base(requestType, null, $"No handler found for request type '{requestType}'")
        {
        }

        /// <summary>
        /// Initializes a new instance of the HandlerNotFoundException class with a handler name.
        /// </summary>
        /// <param name="requestType">The type name of the request.</param>
        /// <param name="handlerName">The name of the requested handler.</param>
        public HandlerNotFoundException(string requestType, string handlerName)
            : base(requestType, handlerName, $"No handler named '{handlerName}' found for request type '{requestType}'")
        {
        }
    }

    /// <summary>
    /// Exception thrown when multiple handlers are found for a request that expects a single handler.
    /// </summary>
    public class MultipleHandlersException : RelayException
    {
        /// <summary>
        /// Initializes a new instance of the MultipleHandlersException class.
        /// </summary>
        /// <param name="requestType">The type name of the request.</param>
        public MultipleHandlersException(string requestType)
            : base(requestType, null, $"Multiple handlers found for request type '{requestType}'. Use named handlers or ensure only one handler is registered.")
        {
        }
    }
}