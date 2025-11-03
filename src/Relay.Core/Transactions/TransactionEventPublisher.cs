using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Publishes transaction lifecycle events to registered event handlers.
    /// </summary>
    /// <remarks>
    /// The TransactionEventPublisher is responsible for invoking all registered <see cref="ITransactionEventHandler"/>
    /// implementations at appropriate points in the transaction lifecycle. It handles event handler exceptions
    /// according to the event type:
    /// 
    /// <list type="bullet">
    /// <item><description>BeforeCommit: Exceptions cause transaction rollback</description></item>
    /// <item><description>AfterCommit/AfterRollback: Exceptions are logged but don't affect transaction outcome</description></item>
    /// <item><description>Other events: Exceptions are propagated</description></item>
    /// </list>
    /// 
    /// Event handlers are executed in parallel when possible to minimize performance impact.
    /// </remarks>
    public sealed class TransactionEventPublisher : ITransactionEventPublisher
    {
        private readonly IEnumerable<ITransactionEventHandler> _eventHandlers;
        private readonly ILogger<TransactionEventPublisher> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionEventPublisher"/> class.
        /// </summary>
        /// <param name="eventHandlers">The collection of registered event handlers.</param>
        /// <param name="logger">The logger for diagnostic output.</param>
        public TransactionEventPublisher(
            IEnumerable<ITransactionEventHandler> eventHandlers,
            ILogger<TransactionEventPublisher> logger)
        {
            _eventHandlers = eventHandlers ?? throw new ArgumentNullException(nameof(eventHandlers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Publishes the BeforeBegin event to all registered handlers.
        /// </summary>
        /// <param name="context">The transaction event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Exceptions thrown by event handlers are propagated to the caller.
        /// </remarks>
        public async Task PublishBeforeBeginAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var handlers = _eventHandlers.ToList();
            if (handlers.Count == 0)
            {
                _logger.LogTrace(
                    "No event handlers registered for BeforeBegin event (Transaction: {TransactionId})",
                    context.TransactionId);
                return;
            }

            _logger.LogDebug(
                "Publishing BeforeBegin event to {HandlerCount} handler(s) (Transaction: {TransactionId})",
                handlers.Count,
                context.TransactionId);

            await ExecuteHandlersAsync(
                handlers,
                handler => handler.OnBeforeBeginAsync(context, cancellationToken),
                "BeforeBegin",
                context.TransactionId,
                propagateExceptions: true);
        }

        /// <summary>
        /// Publishes the AfterBegin event to all registered handlers.
        /// </summary>
        /// <param name="context">The transaction event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Exceptions thrown by event handlers are propagated to the caller.
        /// </remarks>
        public async Task PublishAfterBeginAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var handlers = _eventHandlers.ToList();
            if (handlers.Count == 0)
            {
                _logger.LogTrace(
                    "No event handlers registered for AfterBegin event (Transaction: {TransactionId})",
                    context.TransactionId);
                return;
            }

            _logger.LogDebug(
                "Publishing AfterBegin event to {HandlerCount} handler(s) (Transaction: {TransactionId})",
                handlers.Count,
                context.TransactionId);

            await ExecuteHandlersAsync(
                handlers,
                handler => handler.OnAfterBeginAsync(context, cancellationToken),
                "AfterBegin",
                context.TransactionId,
                propagateExceptions: true);
        }

        /// <summary>
        /// Publishes the BeforeCommit event to all registered handlers.
        /// </summary>
        /// <param name="context">The transaction event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// <strong>IMPORTANT:</strong> If any event handler throws an exception, the transaction will be rolled back.
        /// This allows event handlers to prevent a commit if validation fails or other issues are detected.
        /// </remarks>
        /// <exception cref="TransactionEventHandlerException">
        /// Thrown when one or more event handlers fail, wrapping the original exceptions.
        /// </exception>
        public async Task PublishBeforeCommitAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var handlers = _eventHandlers.ToList();
            if (handlers.Count == 0)
            {
                _logger.LogTrace(
                    "No event handlers registered for BeforeCommit event (Transaction: {TransactionId})",
                    context.TransactionId);
                return;
            }

            _logger.LogDebug(
                "Publishing BeforeCommit event to {HandlerCount} handler(s) (Transaction: {TransactionId})",
                handlers.Count,
                context.TransactionId);

            await ExecuteHandlersAsync(
                handlers,
                handler => handler.OnBeforeCommitAsync(context, cancellationToken),
                "BeforeCommit",
                context.TransactionId,
                propagateExceptions: true);
        }

        /// <summary>
        /// Publishes the AfterCommit event to all registered handlers.
        /// </summary>
        /// <param name="context">The transaction event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// <strong>IMPORTANT:</strong> Exceptions thrown by event handlers are logged but do not affect
        /// the transaction outcome. The transaction has already been committed and cannot be rolled back.
        /// 
        /// This method will not throw exceptions even if handlers fail. All exceptions are logged.
        /// </remarks>
        public async Task PublishAfterCommitAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var handlers = _eventHandlers.ToList();
            if (handlers.Count == 0)
            {
                _logger.LogTrace(
                    "No event handlers registered for AfterCommit event (Transaction: {TransactionId})",
                    context.TransactionId);
                return;
            }

            _logger.LogDebug(
                "Publishing AfterCommit event to {HandlerCount} handler(s) (Transaction: {TransactionId})",
                handlers.Count,
                context.TransactionId);

            await ExecuteHandlersAsync(
                handlers,
                handler => handler.OnAfterCommitAsync(context, cancellationToken),
                "AfterCommit",
                context.TransactionId,
                propagateExceptions: false);
        }

        /// <summary>
        /// Publishes the BeforeRollback event to all registered handlers.
        /// </summary>
        /// <param name="context">The transaction event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Exceptions thrown by event handlers are propagated to the caller, but the rollback will still proceed.
        /// </remarks>
        public async Task PublishBeforeRollbackAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var handlers = _eventHandlers.ToList();
            if (handlers.Count == 0)
            {
                _logger.LogTrace(
                    "No event handlers registered for BeforeRollback event (Transaction: {TransactionId})",
                    context.TransactionId);
                return;
            }

            _logger.LogDebug(
                "Publishing BeforeRollback event to {HandlerCount} handler(s) (Transaction: {TransactionId})",
                handlers.Count,
                context.TransactionId);

            await ExecuteHandlersAsync(
                handlers,
                handler => handler.OnBeforeRollbackAsync(context, cancellationToken),
                "BeforeRollback",
                context.TransactionId,
                propagateExceptions: true);
        }

        /// <summary>
        /// Publishes the AfterRollback event to all registered handlers.
        /// </summary>
        /// <param name="context">The transaction event context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// <strong>IMPORTANT:</strong> Exceptions thrown by event handlers are logged but do not affect
        /// the transaction outcome. The transaction has already been rolled back.
        /// 
        /// This method will not throw exceptions even if handlers fail. All exceptions are logged.
        /// </remarks>
        public async Task PublishAfterRollbackAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken = default)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var handlers = _eventHandlers.ToList();
            if (handlers.Count == 0)
            {
                _logger.LogTrace(
                    "No event handlers registered for AfterRollback event (Transaction: {TransactionId})",
                    context.TransactionId);
                return;
            }

            _logger.LogDebug(
                "Publishing AfterRollback event to {HandlerCount} handler(s) (Transaction: {TransactionId})",
                handlers.Count,
                context.TransactionId);

            await ExecuteHandlersAsync(
                handlers,
                handler => handler.OnAfterRollbackAsync(context, cancellationToken),
                "AfterRollback",
                context.TransactionId,
                propagateExceptions: false);
        }

        /// <summary>
        /// Executes event handlers in parallel and handles exceptions according to the event type.
        /// </summary>
        /// <param name="handlers">The collection of event handlers to execute.</param>
        /// <param name="handlerAction">The action to execute for each handler.</param>
        /// <param name="eventName">The name of the event being published (for logging).</param>
        /// <param name="transactionId">The transaction ID (for logging).</param>
        /// <param name="propagateExceptions">Whether to propagate exceptions to the caller.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task ExecuteHandlersAsync(
            List<ITransactionEventHandler> handlers,
            Func<ITransactionEventHandler, Task> handlerAction,
            string eventName,
            string transactionId,
            bool propagateExceptions)
        {
            if (handlers.Count == 0)
                return;

            // Execute all handlers in parallel for better performance
            var tasks = handlers.Select(async handler =>
            {
                try
                {
                    await handlerAction(handler);
                    
                    _logger.LogTrace(
                        "Event handler {HandlerType} completed successfully for {EventName} event (Transaction: {TransactionId})",
                        handler.GetType().Name,
                        eventName,
                        transactionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Event handler {HandlerType} failed for {EventName} event (Transaction: {TransactionId})",
                        handler.GetType().Name,
                        eventName,
                        transactionId);

                    // Return the exception so we can handle it appropriately
                    return ex;
                }

                return null;
            });

            var results = await Task.WhenAll(tasks);

            // Collect all exceptions that occurred
            var exceptions = results.Where(ex => ex != null).Cast<Exception>().ToList();

            if (exceptions.Count > 0)
            {
                if (propagateExceptions)
                {
                    _logger.LogError(
                        "{ExceptionCount} event handler(s) failed for {EventName} event (Transaction: {TransactionId})",
                        exceptions.Count,
                        eventName,
                        transactionId);

                    // Throw an aggregate exception containing all handler exceptions
                    if (exceptions.Count == 1)
                    {
                        throw new TransactionEventHandlerException(
                            $"Event handler failed for {eventName} event",
                            eventName,
                            transactionId,
                            exceptions[0]);
                    }
                    else
                    {
                        throw new TransactionEventHandlerException(
                            $"{exceptions.Count} event handlers failed for {eventName} event",
                            eventName,
                            transactionId,
                            new AggregateException(exceptions));
                    }
                }
                else
                {
                    // Just log the errors, don't propagate
                    _logger.LogWarning(
                        "{ExceptionCount} event handler(s) failed for {EventName} event, but errors were suppressed (Transaction: {TransactionId})",
                        exceptions.Count,
                        eventName,
                        transactionId);
                }
            }
        }
    }
}
