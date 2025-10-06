using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;

namespace Relay.Core.Pipeline
{
    /// <summary>
    /// Pipeline behavior that executes exception actions when exceptions occur.
    /// This behavior wraps the pipeline execution and catches exceptions, executing
    /// all registered exception actions before rethrowing the exception.
    /// Unlike exception handlers, actions cannot suppress exceptions.
    /// </summary>
    /// <typeparam name="TRequest">The type of request being handled.</typeparam>
    /// <typeparam name="TResponse">The type of response from the handler.</typeparam>
    public class RequestExceptionActionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RequestExceptionActionBehavior<TRequest, TResponse>>? _logger;

        /// <summary>
        /// Initializes a new instance of the RequestExceptionActionBehavior class.
        /// </summary>
        /// <param name="serviceProvider">The service provider for resolving exception actions.</param>
        /// <param name="logger">Optional logger for diagnostic information.</param>
        public RequestExceptionActionBehavior(
            IServiceProvider serviceProvider,
            ILogger<RequestExceptionActionBehavior<TRequest, TResponse>>? logger = null)
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
                    "Exception occurred during request processing: {ExceptionType}. Executing exception actions.",
                    ex.GetType().Name);

                // Execute all exception actions
                await ExecuteExceptionActionsAsync(request, ex, cancellationToken).ConfigureAwait(false);

                // Always rethrow the exception after actions execute
                throw;
            }
        }

        /// <summary>
        /// Executes all registered exception actions for the exception type.
        /// </summary>
        /// <param name="request">The request being processed.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private async ValueTask ExecuteExceptionActionsAsync(
            TRequest request,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var exceptionType = exception.GetType();

            // Get actions for the specific exception type and its base types
            var actionTypes = GetExceptionActionTypes(exceptionType);

            var executedCount = 0;

            foreach (var actionType in actionTypes)
            {
                var actions = _serviceProvider.GetServices(actionType);

                foreach (var action in actions)
                {
                    if (action == null) continue;

                    try
                    {
                        await InvokeActionAsync(action, request, exception, cancellationToken)
                            .ConfigureAwait(false);

                        executedCount++;
                    }
                    catch (Exception actionEx)
                    {
                        _logger?.LogError(
                            actionEx,
                            "Exception action {ActionType} threw an exception while processing {ExceptionType}",
                            action.GetType().Name,
                            exceptionType.Name);
                        // Continue to next action - don't let action failures stop other actions
                    }
                }
            }

            _logger?.LogDebug(
                "Executed {Count} exception action(s) for exception type {ExceptionType}",
                executedCount,
                exceptionType.Name);
        }

        /// <summary>
        /// Gets the action types for the exception and its base types.
        /// </summary>
        private IEnumerable<Type> GetExceptionActionTypes(Type exceptionType)
        {
            var currentType = exceptionType;

            while (currentType != null && typeof(Exception).IsAssignableFrom(currentType))
            {
                var actionType = typeof(IRequestExceptionAction<,>)
                    .MakeGenericType(typeof(TRequest), currentType);

                yield return actionType;

                currentType = currentType.BaseType;
            }
        }

        /// <summary>
        /// Invokes the exception action using reflection.
        /// </summary>
        private async ValueTask InvokeActionAsync(
            object action,
            TRequest request,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var actionType = action.GetType();
            var executeMethod = actionType.GetMethod("ExecuteAsync");

            if (executeMethod == null)
            {
                return;
            }

            _logger?.LogTrace(
                "Invoking exception action {ActionType} for exception {ExceptionType}",
                actionType.Name,
                exception.GetType().Name);

            var resultTask = executeMethod.Invoke(action, new object[] { request, exception, cancellationToken });

            if (resultTask is ValueTask valueTask)
            {
                await valueTask.ConfigureAwait(false);
            }
            else if (resultTask is Task task)
            {
                await task.ConfigureAwait(false);
            }
        }
    }
}
