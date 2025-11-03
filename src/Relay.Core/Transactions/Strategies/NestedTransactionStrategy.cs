using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline;

namespace Relay.Core.Transactions.Strategies
{
    /// <summary>
    /// Handles execution of nested transactions by reusing existing transaction context.
    /// </summary>
    public class NestedTransactionStrategy : ITransactionExecutionStrategy
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<NestedTransactionStrategy> _logger;
        private readonly NestedTransactionManager _nestedTransactionManager;
        private readonly TransactionLogger _transactionLogger;

        public NestedTransactionStrategy(
            IUnitOfWork unitOfWork,
            ILogger<NestedTransactionStrategy> logger,
            NestedTransactionManager nestedTransactionManager,
            TransactionLogger transactionLogger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nestedTransactionManager = nestedTransactionManager ?? throw new ArgumentNullException(nameof(nestedTransactionManager));
            _transactionLogger = transactionLogger ?? throw new ArgumentNullException(nameof(transactionLogger));
        }

        public async ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            ITransactionConfiguration configuration,
            string requestType,
            CancellationToken cancellationToken)
            where TRequest : notnull
        {
            var existingContext = _nestedTransactionManager.GetCurrentContext();

            if (existingContext == null)
            {
                throw new InvalidOperationException(
                    $"Transaction context was detected but is no longer available for nested request {requestType}");
            }

            _transactionLogger.LogNestedTransactionDetected(requestType, existingContext.TransactionId, existingContext.NestingLevel);

            ValidateNestedTransactionConfiguration(existingContext, configuration, requestType);

            _nestedTransactionManager.EnterNestedTransaction(requestType);

            try
            {
                var response = await next();

                _transactionLogger.LogSavingChanges(existingContext.TransactionId, requestType, isNested: true);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                _transactionLogger.LogNestedTransactionCompleted(requestType, existingContext.TransactionId);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Nested transaction {RequestName} failed in transaction {TransactionId}. Exception will propagate to outer transaction.",
                    requestType,
                    existingContext.TransactionId);

                throw;
            }
            finally
            {
                _nestedTransactionManager.ExitNestedTransaction(requestType);
            }
        }

        private void ValidateNestedTransactionConfiguration(
            ITransactionContext existingContext,
            ITransactionConfiguration configuration,
            string requestType)
        {
            try
            {
                _nestedTransactionManager.ValidateNestedTransactionConfiguration(
                    existingContext,
                    configuration,
                    requestType);
            }
            catch (NestedTransactionException ex)
            {
                _logger.LogError(ex,
                    "Nested transaction validation failed for {RequestName}: {ErrorMessage}",
                    requestType,
                    ex.Message);
                throw;
            }
        }
    }
}