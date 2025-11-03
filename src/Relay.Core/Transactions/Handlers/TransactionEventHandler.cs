using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Transactions;

namespace Relay.Core.Transactions.Handlers
{
    /// <summary>
    /// Handles transaction event publishing with error handling and logging.
    /// </summary>
    public class TransactionEventHandler
    {
        private readonly TransactionEventPublisher _eventPublisher;
        private readonly ILogger<TransactionEventHandler> _logger;

        public TransactionEventHandler(
            TransactionEventPublisher eventPublisher,
            ILogger<TransactionEventHandler> logger)
        {
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Publishes BeforeBegin event with error handling.
        /// </summary>
        public async ValueTask PublishBeforeBeginAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken)
        {
            await PublishEventSafelyAsync(
                () => _eventPublisher.PublishBeforeBeginAsync(context, cancellationToken),
                "BeforeBegin",
                context.TransactionId,
                context.RequestType);
        }

        /// <summary>
        /// Publishes AfterBegin event with error handling.
        /// </summary>
        public async ValueTask PublishAfterBeginAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken)
        {
            await PublishEventSafelyAsync(
                () => _eventPublisher.PublishAfterBeginAsync(context, cancellationToken),
                "AfterBegin",
                context.TransactionId,
                context.RequestType);
        }

        /// <summary>
        /// Publishes BeforeCommit event with error handling.
        /// </summary>
        public async ValueTask PublishBeforeCommitAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken)
        {
            await PublishEventWithExceptionPropagationAsync(
                () => _eventPublisher.PublishBeforeCommitAsync(context, cancellationToken),
                "BeforeCommit",
                context.TransactionId,
                context.RequestType);
        }

        /// <summary>
        /// Publishes AfterCommit event with error handling (errors are logged but don't affect transaction).
        /// </summary>
        public async ValueTask PublishAfterCommitAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken)
        {
            await PublishEventSafelyAsync(
                () => _eventPublisher.PublishAfterCommitAsync(context, cancellationToken),
                "AfterCommit",
                context.TransactionId,
                context.RequestType);
        }

        /// <summary>
        /// Publishes BeforeRollback event with error handling.
        /// </summary>
        public async ValueTask PublishBeforeRollbackAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken)
        {
            await PublishEventSafelyAsync(
                () => _eventPublisher.PublishBeforeRollbackAsync(context, cancellationToken),
                "BeforeRollback",
                context.TransactionId,
                context.RequestType);
        }

        /// <summary>
        /// Publishes AfterRollback event with error handling (errors are logged but don't affect transaction).
        /// </summary>
        public async ValueTask PublishAfterRollbackAsync(
            TransactionEventContext context,
            CancellationToken cancellationToken)
        {
            await PublishEventSafelyAsync(
                () => _eventPublisher.PublishAfterRollbackAsync(context, cancellationToken),
                "AfterRollback",
                context.TransactionId,
                context.RequestType);
        }

        /// <summary>
        /// Publishes an event safely, catching and logging any exceptions.
        /// </summary>
        private async ValueTask PublishEventSafelyAsync(
            Func<Task> publishAction,
            string eventName,
            string transactionId,
            string requestType)
        {
            try
            {
                await publishAction();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to publish {EventName} event for transaction {TransactionId} on request {RequestType}",
                    eventName,
                    transactionId,
                    requestType);
            }
        }

        /// <summary>
        /// Publishes an event with exception propagation for critical events.
        /// </summary>
        private async ValueTask PublishEventWithExceptionPropagationAsync(
            Func<Task> publishAction,
            string eventName,
            string transactionId,
            string requestType)
        {
            try
            {
                await publishAction();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Critical {EventName} event handler failed for transaction {TransactionId} on request {RequestType}",
                    eventName,
                    transactionId,
                    requestType);
                
                throw new TransactionEventHandlerException(
                    $"Transaction {eventName} event handler failed for transaction {transactionId}",
                    eventName,
                    transactionId,
                    ex);
            }
        }
    }
}