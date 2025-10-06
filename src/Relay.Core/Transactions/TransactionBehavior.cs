using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Pipeline behavior that wraps request handlers in a TransactionScope.
    /// Only applies to requests that implement ITransactionalRequest.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <remarks>
    /// This behavior provides automatic transaction management for requests marked with ITransactionalRequest.
    /// It uses System.Transactions.TransactionScope for distributed transaction support.
    ///
    /// Features:
    /// - Automatic commit on success
    /// - Automatic rollback on exception
    /// - Configurable isolation level and timeout
    /// - Compatible with Entity Framework Core and other transaction-aware data access layers
    ///
    /// Example usage:
    /// <code>
    /// public record CreateOrderCommand(int UserId, List&lt;OrderItem&gt; Items)
    ///     : IRequest&lt;Order&gt;, ITransactionalRequest&lt;Order&gt;;
    /// </code>
    /// </remarks>
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<TransactionBehavior<TRequest, TResponse>>? _logger;
        private readonly TransactionScopeOption _scopeOption;
        private readonly IsolationLevel _isolationLevel;
        private readonly TimeSpan _timeout;

        /// <summary>
        /// Initializes a new instance of TransactionBehavior.
        /// </summary>
        /// <param name="options">Configuration options for transaction behavior.</param>
        /// <param name="logger">Optional logger for transaction events.</param>
        public TransactionBehavior(
            IOptions<TransactionOptions>? options = null,
            ILogger<TransactionBehavior<TRequest, TResponse>>? logger = null)
        {
            var opts = options?.Value ?? new TransactionOptions();
            _scopeOption = opts.ScopeOption;
            _isolationLevel = opts.IsolationLevel;
            _timeout = opts.Timeout;
            _logger = logger;
        }

        /// <inheritdoc />
        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Only apply transaction scope if request implements ITransactionalRequest
            if (request is not ITransactionalRequest)
            {
                return await next();
            }

            var transactionOptions = new System.Transactions.TransactionOptions
            {
                IsolationLevel = _isolationLevel,
                Timeout = _timeout
            };

            using var transactionScope = new TransactionScope(
                _scopeOption,
                transactionOptions,
                TransactionScopeAsyncFlowOption.Enabled);

            try
            {
                _logger?.LogDebug(
                    "Beginning transaction for {RequestType} with isolation level {IsolationLevel}",
                    typeof(TRequest).Name,
                    _isolationLevel);

                var response = await next();

                transactionScope.Complete();

                _logger?.LogDebug(
                    "Transaction completed successfully for {RequestType}",
                    typeof(TRequest).Name);

                return response;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(
                    ex,
                    "Transaction rolled back for {RequestType} due to exception: {Message}",
                    typeof(TRequest).Name,
                    ex.Message);

                throw;
            }
        }
    }
}
