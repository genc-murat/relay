using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline;
using Relay.Core.Transactions.Factories;

namespace Relay.Core.Transactions.Strategies
{
    /// <summary>
    /// Handles execution of distributed transactions with coordination across multiple resources.
    /// </summary>
    public class DistributedTransactionStrategy : ITransactionExecutionStrategy
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DistributedTransactionStrategy> _logger;
        private readonly DistributedTransactionCoordinator _distributedTransactionCoordinator;
        private readonly TransactionEventPublisher _eventPublisher;
        private readonly TransactionMetricsCollector _metricsCollector;
        private readonly TransactionActivitySource _activitySource;
        private readonly TransactionLogger _transactionLogger;
        private readonly ITransactionEventContextFactory _eventContextFactory;

        public DistributedTransactionStrategy(
            IUnitOfWork unitOfWork,
            ILogger<DistributedTransactionStrategy> logger,
            DistributedTransactionCoordinator distributedTransactionCoordinator,
            TransactionEventPublisher eventPublisher,
            TransactionMetricsCollector metricsCollector,
            TransactionActivitySource activitySource,
            TransactionLogger transactionLogger,
            ITransactionEventContextFactory eventContextFactory)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _distributedTransactionCoordinator = distributedTransactionCoordinator ?? throw new ArgumentNullException(nameof(distributedTransactionCoordinator));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _transactionLogger = transactionLogger ?? throw new ArgumentNullException(nameof(transactionLogger));
            _eventContextFactory = eventContextFactory ?? throw new ArgumentNullException(nameof(eventContextFactory));
        }

        public async ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            ITransactionConfiguration configuration,
            string requestType,
            CancellationToken cancellationToken)
            where TRequest : notnull
        {
            var stopwatch = Stopwatch.StartNew();

            using var activity = _activitySource.StartTransactionActivity(requestType, configuration);

            var (scope, transactionId, startTime) = _distributedTransactionCoordinator.CreateDistributedTransactionScope(
                configuration,
                requestType,
                cancellationToken);

            _transactionLogger.LogDistributedTransactionCreated(transactionId, requestType, configuration.IsolationLevel);

            var eventContext = _eventContextFactory.CreateEventContext(requestType, configuration);
            eventContext.TransactionId = transactionId;

            try
            {
                await _eventPublisher.PublishBeforeBeginAsync(eventContext, cancellationToken);
                await _eventPublisher.PublishAfterBeginAsync(eventContext, cancellationToken);

                var response = await _distributedTransactionCoordinator.ExecuteInDistributedTransactionAsync(
                    async (ct) =>
                    {
                        var result = await next();

                        _transactionLogger.LogSavingChanges(transactionId, requestType, isNested: false);
                        await _unitOfWork.SaveChangesAsync(ct);

                        return result;
                    },
                    transactionId,
                    configuration.Timeout,
                    requestType,
                    startTime,
                    cancellationToken);

                await _eventPublisher.PublishBeforeCommitAsync(eventContext, cancellationToken);

                _distributedTransactionCoordinator.CompleteDistributedTransaction(
                    scope,
                    transactionId,
                    requestType,
                    startTime);

                RecordSuccessMetrics(configuration.IsolationLevel, requestType, stopwatch, transactionId);

                await _eventPublisher.PublishAfterCommitAsync(eventContext, cancellationToken);

                return response;
            }
            catch (TransactionEventHandlerException ex) when (ex.EventName == "BeforeCommit")
            {
                await HandleBeforeCommitException(ex, stopwatch, eventContext, scope, transactionId, requestType, startTime, cancellationToken);
                throw;
            }
            catch (Exception ex)
            {
                await HandleGeneralException(ex, stopwatch, eventContext, scope, transactionId, requestType, startTime, configuration.IsolationLevel, cancellationToken);
                throw;
            }
            finally
            {
                scope?.Dispose();
            }
        }

        private async ValueTask HandleBeforeCommitException(
            TransactionEventHandlerException ex,
            Stopwatch stopwatch,
            TransactionEventContext eventContext,
            IDisposable scope,
            string transactionId,
            string requestType,
            DateTime startTime,
            CancellationToken cancellationToken)
        {
            stopwatch.Stop();
            _metricsCollector.RecordTransactionRollback(requestType, stopwatch.Elapsed);

            _logger.LogWarning(ex,
                "Distributed transaction {TransactionId} for {RequestName} rolling back due to BeforeCommit event handler failure",
                transactionId,
                requestType);

            await _eventPublisher.PublishBeforeRollbackAsync(eventContext, cancellationToken);

            scope.Dispose();

            await _eventPublisher.PublishAfterRollbackAsync(eventContext, cancellationToken);
        }

        private async ValueTask HandleGeneralException(
            Exception ex,
            Stopwatch stopwatch,
            TransactionEventContext eventContext,
            IDisposable scope,
            string transactionId,
            string requestType,
            DateTime startTime,
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            stopwatch.Stop();
            _metricsCollector.RecordTransactionFailure(requestType, stopwatch.Elapsed);

            _logger.LogError(ex,
                "Distributed transaction {TransactionId} for {RequestName} with isolation level {IsolationLevel} failed. Rolling back.",
                transactionId,
                requestType,
                isolationLevel);

            await _eventPublisher.PublishBeforeRollbackAsync(eventContext, cancellationToken);

            scope.Dispose();

            await _eventPublisher.PublishAfterRollbackAsync(eventContext, cancellationToken);
        }

        private void RecordSuccessMetrics(
            IsolationLevel isolationLevel,
            string requestType,
            Stopwatch stopwatch,
            string transactionId)
        {
            stopwatch.Stop();
            _metricsCollector.RecordTransactionSuccess(isolationLevel, requestType, stopwatch.Elapsed);
            _transactionLogger.LogDistributedTransactionCommitted(transactionId, requestType, isolationLevel);
        }
    }
}