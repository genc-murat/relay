using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline.Interfaces;

namespace Relay.Core.Pipeline.Behaviors
{
    /// <summary>
    /// Pipeline behavior that handles exceptions using registered exception handlers.
    /// This behavior wraps the pipeline execution and catches exceptions, allowing
    /// registered handlers to either handle the exception (and provide a response)
    /// or let it propagate to the next handler.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being handled.</typeparam>
    /// <typeparam name="TResponse">The type of response from the handler.</typeparam>
    public class RequestExceptionHandlerBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RequestExceptionHandlerBehavior<TRequest, TResponse>>? _logger;

        /// <summary>
        /// Initializes a new instance of the RequestExceptionHandlerBehavior class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving exception handlers.</param>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        public RequestExceptionHandlerBehavior(
            IServiceProvider serviceProvider,
            ILogger<RequestExceptionHandlerBehavior<TRequest, TResponse>>? logger = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;
        }

        /// <inheritdoc />
        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            try
            {
                // Execute the rest of the pipeline
                return await next().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(
                    "Exception occurred during request processing: {ExceptionType}. Checking for handlers.",
                    ex.GetType().Name);

                // Try to handle the exception with registered handlers
                var (handled, response) = await TryHandleExceptionAsync(request, ex, cancellationToken).ConfigureAwait(false);

                if (handled)
                {
                    _logger?.LogInformation(
                        "Exception {ExceptionType} was handled by exception handler. Returning response.",
                        ex.GetType().Name);

                    return response!;
                }

                // No handler processed the exception, rethrow
                _logger?.LogDebug(
                    "Exception {ExceptionType} was not handled by any exception handler. Rethrowing.",
                    ex.GetType().Name);

                throw;
            }
        }

        /// <summary>
        /// Tries to handle the exception using registered exception handlers.
        /// </summary>
        /// <param name="request">The request being processed.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A tuple indicating if handled and the response.</returns>
        private async ValueTask<(bool Handled, TResponse? Response)> TryHandleExceptionAsync(
            TRequest request,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var exceptionType = exception.GetType();

            // Try to get handlers for the specific exception type and its base types
            var handlerTypes = GetExceptionHandlerTypes(exceptionType);

            foreach (var handlerType in handlerTypes)
            {
                var handlers = _serviceProvider.GetServices(handlerType);

                foreach (var handler in handlers)
                {
                    if (handler == null) continue;

                    try
                    {
                        var (handled, response) = await InvokeHandlerAsync(handler, request, exception, cancellationToken)
                            .ConfigureAwait(false);

                        if (handled)
                        {
                            return (true, response);
                        }
                    }
                    catch (Exception handlerEx)
                    {
                        _logger?.LogError(
                            handlerEx,
                            "Exception handler {HandlerType} threw an exception while handling {ExceptionType}",
                            handler.GetType().Name,
                            exceptionType.Name);
                        // Continue to next handler
                    }
                }
            }

            return (false, default);
        }

        /// <summary>
        /// Gets the handler types for the exception and its base types.
        /// </summary>
        private IEnumerable<Type> GetExceptionHandlerTypes(Type exceptionType)
        {
            var currentType = exceptionType;

            while (currentType != null && typeof(Exception).IsAssignableFrom(currentType))
            {
                var handlerType = typeof(IRequestExceptionHandler<,,>)
                    .MakeGenericType(typeof(TRequest), typeof(TResponse), currentType);

                yield return handlerType;

                currentType = currentType.BaseType;
            }
        }

        /// <summary>
        /// Invokes the exception handler using reflection.
        /// </summary>
        private async ValueTask<(bool Handled, TResponse? Response)> InvokeHandlerAsync(
            object handler,
            TRequest request,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var handlerType = handler.GetType();
            var handleMethod = handlerType.GetMethod("HandleAsync");

            if (handleMethod == null)
            {
                return (false, default);
            }

            _logger?.LogTrace(
                "Invoking exception handler {HandlerType} for exception {ExceptionType}",
                handlerType.Name,
                exception.GetType().Name);

            try
            {
                var resultTask = handleMethod.Invoke(handler, new object[] { request, exception, cancellationToken });

                if (resultTask == null)
                {
                    return (false, default);
                }

                // Handle ValueTask<ExceptionHandlerResult<TResponse>>
                var resultTaskType = resultTask.GetType();
                if (resultTaskType.IsGenericType)
                {
                    var asTaskMethod = resultTaskType.GetMethod("AsTask");
                    if (asTaskMethod != null)
                    {
                        var task = (Task?)asTaskMethod.Invoke(resultTask, null);
                        if (task != null)
                        {
                            await task.ConfigureAwait(false);

                            var resultProperty = task.GetType().GetProperty("Result");
                            if (resultProperty != null)
                            {
                                var result = resultProperty.GetValue(task);
                                if (result != null)
                                {
                                    return ExtractHandlerResult(result);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to invoke exception handler");
            }

            return (false, default);
        }

        private (bool Handled, TResponse? Response) ExtractHandlerResult(object result)
        {
            // Use reflection to check if the result was handled
            var resultType = result.GetType();
            var handledProperty = resultType.GetProperty("Handled");
            var responseProperty = resultType.GetProperty("Response");

            if (handledProperty != null && responseProperty != null)
            {
                var handled = (bool?)handledProperty.GetValue(result);
                if (handled == true)
                {
                    return (true, (TResponse?)responseProperty.GetValue(result));
                }
            }

            return (false, default);
        }
    }
}
