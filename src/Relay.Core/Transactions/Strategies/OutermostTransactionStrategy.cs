using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Transactions.Factories;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions.Strategies;

/// <summary>
/// Handles execution of outermost transactions with full feature support.
/// </summary>
public class OutermostTransactionStrategy : ITransactionExecutionStrategy
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OutermostTransactionStrategy> _logger;
    private readonly ITransactionCoordinator _transactionCoordinator;
    private readonly ITransactionEventPublisher _eventPublisher;
    private readonly ITransactionMetricsCollector _metricsCollector;
    private readonly INestedTransactionManager _nestedTransactionManager;
    private readonly TransactionActivitySource _activitySource;
    private readonly TransactionLogger _transactionLogger;
    private readonly ITransactionEventContextFactory _eventContextFactory;

    public OutermostTransactionStrategy(
        IUnitOfWork unitOfWork,
        ILogger<OutermostTransactionStrategy> logger,
        ITransactionCoordinator transactionCoordinator,
        ITransactionEventPublisher eventPublisher,
        ITransactionMetricsCollector metricsCollector,
        INestedTransactionManager nestedTransactionManager,
        TransactionActivitySource activitySource,
        TransactionLogger transactionLogger,
        ITransactionEventContextFactory eventContextFactory)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _transactionCoordinator = transactionCoordinator ?? throw new ArgumentNullException(nameof(transactionCoordinator));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
        _nestedTransactionManager = nestedTransactionManager ?? throw new ArgumentNullException(nameof(nestedTransactionManager));
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
        var previousReadOnlyState = _unitOfWork.IsReadOnly;
        
        _unitOfWork.IsReadOnly = configuration.IsReadOnly;

        using var activity = _activitySource.StartTransactionActivity(requestType, configuration);

        try
        {
            var eventContext = _eventContextFactory.CreateEventContext(requestType, configuration);

            await _eventPublisher.PublishBeforeBeginAsync(eventContext, cancellationToken);

            var (transaction, context, timeoutCts) = await _transactionCoordinator.BeginTransactionAsync(
                configuration,
                requestType,
                cancellationToken);

            eventContext.TransactionId = context.TransactionId;

            try
            {
                await _eventPublisher.PublishAfterBeginAsync(eventContext, cancellationToken);

                var response = await _transactionCoordinator.ExecuteWithTimeoutAsync(
                    async ct =>
                    {
                        var result = await next();
                        
                        _transactionLogger.LogSavingChanges(context.TransactionId, requestType, isNested: false);
                        await _unitOfWork.SaveChangesAsync(ct);
                        
                        return result;
                    },
                    context,
                    timeoutCts,
                    configuration.Timeout,
                    requestType,
                    cancellationToken);

                await _eventPublisher.PublishBeforeCommitAsync(eventContext, cancellationToken);

                if (_nestedTransactionManager.ShouldCommitTransaction(context))
                {
                    await _transactionCoordinator.CommitTransactionAsync(transaction, context, requestType, cancellationToken);
                    
                    RecordSuccessMetrics(configuration.IsolationLevel, requestType, stopwatch, activity, context);

                    await _eventPublisher.PublishAfterCommitAsync(eventContext, cancellationToken);
                }

                return response;
            }
            catch (TransactionTimeoutException ex)
            {
                await HandleTimeoutException(ex, stopwatch, activity, context, eventContext, transaction, requestType, cancellationToken);
                throw;
            }
            catch (TransactionEventHandlerException ex) when (ex.EventName == "BeforeCommit")
            {
                await HandleBeforeCommitException(ex, stopwatch, activity, context, eventContext, transaction, requestType, cancellationToken);
                throw;
            }
            catch (Exception ex)
            {
                await HandleGeneralException(ex, stopwatch, activity, context, eventContext, transaction, requestType, cancellationToken);
                throw;
            }
            finally
            {
                timeoutCts?.Dispose();
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }
        finally
        {
            _unitOfWork.IsReadOnly = previousReadOnlyState;
        }
    }

    private async ValueTask HandleTimeoutException(
        TransactionTimeoutException ex,
        Stopwatch stopwatch,
        System.Diagnostics.Activity activity,
        ITransactionContext context,
        TransactionEventContext eventContext,
        IRelayDbTransaction transaction,
        string requestType,
        CancellationToken cancellationToken)
    {
        stopwatch.Stop();
        _metricsCollector.RecordTransactionTimeout(requestType, stopwatch.Elapsed);
        _activitySource.RecordTransactionTimeout(activity, context, ex);

        await _eventPublisher.PublishBeforeRollbackAsync(eventContext, cancellationToken);

        if (_nestedTransactionManager.ShouldRollbackTransaction(context))
        {
            await _transactionCoordinator.RollbackTransactionAsync(transaction, context, requestType, ex, cancellationToken);
        }

        await _eventPublisher.PublishAfterRollbackAsync(eventContext, cancellationToken);
    }

    private async ValueTask HandleBeforeCommitException(
        TransactionEventHandlerException ex,
        Stopwatch stopwatch,
        System.Diagnostics.Activity activity,
        ITransactionContext context,
        TransactionEventContext eventContext,
        IRelayDbTransaction transaction,
        string requestType,
        CancellationToken cancellationToken)
    {
        stopwatch.Stop();
        _metricsCollector.RecordTransactionRollback(requestType, stopwatch.Elapsed);
        _activitySource.RecordTransactionRollback(activity, context, ex);

        _logger.LogWarning(ex,
            "Transaction {TransactionId} for {RequestName} rolling back due to BeforeCommit event handler failure",
            context.TransactionId,
            requestType);

        await _eventPublisher.PublishBeforeRollbackAsync(eventContext, cancellationToken);

        if (_nestedTransactionManager.ShouldRollbackTransaction(context))
        {
            await _transactionCoordinator.RollbackTransactionAsync(transaction, context, requestType, ex, cancellationToken);
        }

        await _eventPublisher.PublishAfterRollbackAsync(eventContext, cancellationToken);
    }

    private async ValueTask HandleGeneralException(
        Exception ex,
        Stopwatch stopwatch,
        System.Diagnostics.Activity activity,
        ITransactionContext context,
        TransactionEventContext eventContext,
        IRelayDbTransaction transaction,
        string requestType,
        CancellationToken cancellationToken)
    {
        stopwatch.Stop();
        _metricsCollector.RecordTransactionFailure(requestType, stopwatch.Elapsed);
        _activitySource.RecordTransactionFailure(activity, context, ex);

        await _eventPublisher.PublishBeforeRollbackAsync(eventContext, cancellationToken);

        if (_nestedTransactionManager.ShouldRollbackTransaction(context))
        {
            await _transactionCoordinator.RollbackTransactionAsync(transaction, context, requestType, ex, cancellationToken);
        }

        await _eventPublisher.PublishAfterRollbackAsync(eventContext, cancellationToken);
    }

    private void RecordSuccessMetrics(
        IsolationLevel isolationLevel,
        string requestType,
        Stopwatch stopwatch,
        System.Diagnostics.Activity activity,
        ITransactionContext context)
    {
        stopwatch.Stop();
        _metricsCollector.RecordTransactionSuccess(isolationLevel, requestType, stopwatch.Elapsed);
        _activitySource.RecordTransactionSuccess(activity, context, stopwatch.Elapsed);
    }
}