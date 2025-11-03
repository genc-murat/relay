using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// A pipeline behavior that wraps the request handler in a database transaction with comprehensive features.
    /// </summary>
    /// <remarks>
    /// <para><strong>Breaking Change:</strong> This behavior now requires all transactional requests
    /// to have a <see cref="TransactionAttribute"/> with an explicit isolation level specified.
    /// Requests without the attribute or with <see cref="IsolationLevel.Unspecified"/> will throw
    /// a <see cref="TransactionConfigurationException"/>.</para>
    /// 
    /// <para>This behavior provides the following features:</para>
    /// <list type="bullet">
    /// <item><description>Mandatory isolation level specification</description></item>
    /// <item><description>Transaction timeout enforcement</description></item>
    /// <item><description>Automatic retry on transient failures</description></item>
    /// <item><description>Transaction event hooks</description></item>
    /// <item><description>Nested transaction support</description></item>
    /// <item><description>Read-only transaction optimization</description></item>
    /// <item><description>Distributed transaction coordination</description></item>
    /// <item><description>Comprehensive metrics and telemetry</description></item>
    /// </list>
    /// </remarks>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;
        private readonly TransactionConfigurationResolver _configurationResolver;
        private readonly TransactionCoordinator _transactionCoordinator;
        private readonly TransactionEventPublisher _eventPublisher;
        private readonly TransactionRetryHandler _retryHandler;
        private readonly TransactionMetricsCollector _metricsCollector;
        private readonly NestedTransactionManager _nestedTransactionManager;
        private readonly DistributedTransactionCoordinator _distributedTransactionCoordinator;
        private readonly TransactionActivitySource _activitySource;
        private readonly TransactionLogger _transactionLogger;

        public TransactionBehavior(
            IUnitOfWork unitOfWork,
            ILogger<TransactionBehavior<TRequest, TResponse>> logger,
            TransactionConfigurationResolver configurationResolver,
            TransactionCoordinator transactionCoordinator,
            TransactionEventPublisher eventPublisher,
            TransactionRetryHandler retryHandler,
            TransactionMetricsCollector metricsCollector,
            NestedTransactionManager nestedTransactionManager,
            DistributedTransactionCoordinator distributedTransactionCoordinator,
            TransactionActivitySource activitySource,
            TransactionLogger transactionLogger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configurationResolver = configurationResolver ?? throw new ArgumentNullException(nameof(configurationResolver));
            _transactionCoordinator = transactionCoordinator ?? throw new ArgumentNullException(nameof(transactionCoordinator));
            _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
            _retryHandler = retryHandler ?? throw new ArgumentNullException(nameof(retryHandler));
            _metricsCollector = metricsCollector ?? throw new ArgumentNullException(nameof(metricsCollector));
            _nestedTransactionManager = nestedTransactionManager ?? throw new ArgumentNullException(nameof(nestedTransactionManager));
            _distributedTransactionCoordinator = distributedTransactionCoordinator ?? throw new ArgumentNullException(nameof(distributedTransactionCoordinator));
            _activitySource = activitySource ?? throw new ArgumentNullException(nameof(activitySource));
            _transactionLogger = transactionLogger ?? throw new ArgumentNullException(nameof(transactionLogger));
        }

        public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Fast path: Skip transaction logic for non-transactional requests
            if (request is not ITransactionalRequest)
            {
                return await next();
            }

            var requestType = typeof(TRequest).Name;

            // Resolve transaction configuration - this will throw if attribute is missing or invalid
            ITransactionConfiguration configuration;
            try
            {
                configuration = _configurationResolver.Resolve(request);
            }
            catch (TransactionConfigurationException ex)
            {
                _logger.LogError(ex,
                    "Transaction configuration error for {RequestName}: {ErrorMessage}",
                    requestType,
                    ex.Message);
                throw;
            }

            // Validate isolation level is not Unspecified
            if (configuration.IsolationLevel == IsolationLevel.Unspecified)
            {
                var errorMessage = $"Transaction isolation level cannot be Unspecified for request type '{requestType}'. " +
                    "You must explicitly specify an isolation level in the [Transaction] attribute.";
                _logger.LogError("{ErrorMessage}", errorMessage);
                throw new TransactionConfigurationException(errorMessage, typeof(TRequest));
            }

            // Check if distributed transaction is requested
            if (configuration.UseDistributedTransaction)
            {
                _logger.LogInformation(
                    "Distributed transaction requested for {RequestName}",
                    requestType);

                return await HandleDistributedTransactionAsync(request, next, configuration, requestType, cancellationToken);
            }

            // Check if we're in a nested transaction scenario
            if (_nestedTransactionManager.IsTransactionActive())
            {
                return await HandleNestedTransactionAsync(request, next, configuration, requestType, cancellationToken);
            }

            // Execute with retry if configured
            if (configuration.RetryPolicy != null && configuration.RetryPolicy.MaxRetries > 0)
            {
                return await _retryHandler.ExecuteWithRetryAsync(
                    async ct => await HandleOutermostTransactionAsync(request, next, configuration, requestType, ct),
                    configuration.RetryPolicy,
                    null, // Transaction ID will be generated in each attempt
                    requestType,
                    cancellationToken);
            }

            // Begin new transaction (outermost transaction) without retry
            return await HandleOutermostTransactionAsync(request, next, configuration, requestType, cancellationToken);
        }

        /// <summary>
        /// Handles a nested transactional request by reusing the existing transaction.
        /// </summary>
        private async ValueTask<TResponse> HandleNestedTransactionAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            ITransactionConfiguration configuration,
            string requestType,
            CancellationToken cancellationToken)
        {
            var existingContext = _nestedTransactionManager.GetCurrentContext();

            if (existingContext == null)
            {
                throw new InvalidOperationException(
                    $"Transaction context was detected but is no longer available for nested request {requestType}");
            }

            _transactionLogger.LogNestedTransactionDetected(requestType, existingContext.TransactionId, existingContext.NestingLevel);

            // Validate that the nested transaction configuration is compatible with the outer transaction
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

            // Enter the nested transaction (increment nesting level)
            _nestedTransactionManager.EnterNestedTransaction(requestType);

            try
            {
                // Execute the handler within the existing transaction
                var response = await next();

                // Save changes (but don't commit - that's handled by the outermost transaction)
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

                // Don't rollback here - let the exception propagate to the outermost transaction
                // which will handle the rollback
                throw;
            }
            finally
            {
                // Exit the nested transaction (decrement nesting level)
                _nestedTransactionManager.ExitNestedTransaction(requestType);
            }
        }

        /// <summary>
        /// Handles the outermost transactional request by creating a new transaction with full feature support.
        /// </summary>
        private async ValueTask<TResponse> HandleOutermostTransactionAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            ITransactionConfiguration configuration,
            string requestType,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var previousReadOnlyState = _unitOfWork.IsReadOnly;
            
            // Configure read-only mode before beginning transaction
            _unitOfWork.IsReadOnly = configuration.IsReadOnly;

            // Start distributed tracing activity
            using var activity = _activitySource.StartTransactionActivity(requestType, configuration);

            try
            {
                // Create event context for lifecycle events
                var eventContext = new TransactionEventContext
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    RequestType = requestType,
                    IsolationLevel = configuration.IsolationLevel,
                    NestingLevel = 0,
                    Timestamp = DateTime.UtcNow,
                    Metadata = new System.Collections.Generic.Dictionary<string, object>
                    {
                        ["IsReadOnly"] = configuration.IsReadOnly,
                        ["Timeout"] = configuration.Timeout,
                        ["UseDistributedTransaction"] = configuration.UseDistributedTransaction
                    }
                };

                // Publish BeforeBegin event
                await _eventPublisher.PublishBeforeBeginAsync(eventContext, cancellationToken);

                // Begin transaction with timeout enforcement
                var (transaction, context, timeoutCts) = await _transactionCoordinator.BeginTransactionAsync(
                    configuration,
                    requestType,
                    cancellationToken);

                // Update event context with actual transaction ID
                eventContext.TransactionId = context.TransactionId;

                try
                {
                    // Publish AfterBegin event
                    await _eventPublisher.PublishAfterBeginAsync(eventContext, cancellationToken);

                    // Execute the handler with timeout enforcement
                    var response = await _transactionCoordinator.ExecuteWithTimeoutAsync(
                        async ct =>
                        {
                            var result = await next();
                            
                            // Save changes
                            _transactionLogger.LogSavingChanges(context.TransactionId, requestType, isNested: false);
                            await _unitOfWork.SaveChangesAsync(ct);
                            
                            return result;
                        },
                        context,
                        timeoutCts,
                        configuration.Timeout,
                        requestType,
                        cancellationToken);

                    // Publish BeforeCommit event - failures here will cause rollback
                    await _eventPublisher.PublishBeforeCommitAsync(eventContext, cancellationToken);

                    // Commit the transaction
                    if (_nestedTransactionManager.ShouldCommitTransaction(context))
                    {
                        await _transactionCoordinator.CommitTransactionAsync(transaction, context, requestType, cancellationToken);
                        
                        stopwatch.Stop();
                        _metricsCollector.RecordTransactionSuccess(
                            configuration.IsolationLevel,
                            requestType,
                            stopwatch.Elapsed);

                        _activitySource.RecordTransactionSuccess(activity, context, stopwatch.Elapsed);

                        // Publish AfterCommit event - failures here are logged but don't affect transaction
                        await _eventPublisher.PublishAfterCommitAsync(eventContext, cancellationToken);
                    }

                    return response;
                }
                catch (TransactionTimeoutException ex)
                {
                    stopwatch.Stop();
                    _metricsCollector.RecordTransactionTimeout(requestType, stopwatch.Elapsed);
                    _activitySource.RecordTransactionTimeout(activity, context, ex);

                    // Publish BeforeRollback event
                    await _eventPublisher.PublishBeforeRollbackAsync(eventContext, cancellationToken);

                    // Rollback due to timeout
                    if (_nestedTransactionManager.ShouldRollbackTransaction(context))
                    {
                        await _transactionCoordinator.RollbackTransactionAsync(transaction, context, requestType, ex, cancellationToken);
                    }

                    // Publish AfterRollback event
                    await _eventPublisher.PublishAfterRollbackAsync(eventContext, cancellationToken);

                    throw;
                }
                catch (TransactionEventHandlerException ex) when (ex.EventName == "BeforeCommit")
                {
                    // BeforeCommit handler failed - rollback the transaction
                    stopwatch.Stop();
                    _metricsCollector.RecordTransactionRollback(requestType, stopwatch.Elapsed);
                    _activitySource.RecordTransactionRollback(activity, context, ex);

                    _logger.LogWarning(ex,
                        "Transaction {TransactionId} for {RequestName} rolling back due to BeforeCommit event handler failure",
                        context.TransactionId,
                        requestType);

                    // Publish BeforeRollback event
                    await _eventPublisher.PublishBeforeRollbackAsync(eventContext, cancellationToken);

                    if (_nestedTransactionManager.ShouldRollbackTransaction(context))
                    {
                        await _transactionCoordinator.RollbackTransactionAsync(transaction, context, requestType, ex, cancellationToken);
                    }

                    // Publish AfterRollback event
                    await _eventPublisher.PublishAfterRollbackAsync(eventContext, cancellationToken);

                    throw;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _metricsCollector.RecordTransactionFailure(requestType, stopwatch.Elapsed);
                    _activitySource.RecordTransactionFailure(activity, context, ex);

                    // Publish BeforeRollback event
                    await _eventPublisher.PublishBeforeRollbackAsync(eventContext, cancellationToken);

                    // Rollback due to exception
                    if (_nestedTransactionManager.ShouldRollbackTransaction(context))
                    {
                        await _transactionCoordinator.RollbackTransactionAsync(transaction, context, requestType, ex, cancellationToken);
                    }

                    // Publish AfterRollback event
                    await _eventPublisher.PublishAfterRollbackAsync(eventContext, cancellationToken);

                    throw;
                }
                finally
                {
                    // Dispose timeout cancellation token source
                    timeoutCts?.Dispose();
                    
                    // Dispose transaction
                    if (transaction != null)
                    {
                        await transaction.DisposeAsync();
                    }
                }
            }
            finally
            {
                // Restore previous read-only state
                _unitOfWork.IsReadOnly = previousReadOnlyState;
            }
        }

        /// <summary>
        /// Handles a transactional request using distributed transaction coordination.
        /// </summary>
        private async ValueTask<TResponse> HandleDistributedTransactionAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            ITransactionConfiguration configuration,
            string requestType,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            // Start distributed tracing activity
            using var activity = _activitySource.StartTransactionActivity(requestType, configuration);

            // Create distributed transaction scope
            var (scope, transactionId, startTime) = _distributedTransactionCoordinator.CreateDistributedTransactionScope(
                configuration,
                requestType,
                cancellationToken);

            _transactionLogger.LogDistributedTransactionCreated(transactionId, requestType, configuration.IsolationLevel);

            // Create event context for lifecycle events
            var eventContext = new TransactionEventContext
            {
                TransactionId = transactionId,
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel,
                NestingLevel = 0,
                Timestamp = DateTime.UtcNow,
                Metadata = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["IsReadOnly"] = configuration.IsReadOnly,
                    ["Timeout"] = configuration.Timeout,
                    ["UseDistributedTransaction"] = true
                }
            };

            try
            {
                // Publish BeforeBegin event
                await _eventPublisher.PublishBeforeBeginAsync(eventContext, cancellationToken);

                // Publish AfterBegin event
                await _eventPublisher.PublishAfterBeginAsync(eventContext, cancellationToken);

                // Execute the handler within the distributed transaction scope
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

                // Publish BeforeCommit event - failures here will cause rollback
                await _eventPublisher.PublishBeforeCommitAsync(eventContext, cancellationToken);

                // Complete the distributed transaction (commit)
                _distributedTransactionCoordinator.CompleteDistributedTransaction(
                    scope,
                    transactionId,
                    requestType,
                    startTime);

                stopwatch.Stop();
                _metricsCollector.RecordTransactionSuccess(
                    configuration.IsolationLevel,
                    requestType,
                    stopwatch.Elapsed);

                _transactionLogger.LogDistributedTransactionCommitted(transactionId, requestType, configuration.IsolationLevel);

                // Publish AfterCommit event - failures here are logged but don't affect transaction
                await _eventPublisher.PublishAfterCommitAsync(eventContext, cancellationToken);

                return response;
            }
            catch (TransactionEventHandlerException ex) when (ex.EventName == "BeforeCommit")
            {
                // BeforeCommit handler failed - rollback the transaction
                stopwatch.Stop();
                _metricsCollector.RecordTransactionRollback(requestType, stopwatch.Elapsed);

                _logger.LogWarning(ex,
                    "Distributed transaction {TransactionId} for {RequestName} rolling back due to BeforeCommit event handler failure",
                    transactionId,
                    requestType);

                // Publish BeforeRollback event
                await _eventPublisher.PublishBeforeRollbackAsync(eventContext, cancellationToken);

                // Dispose will cause rollback since Complete was not called
                _distributedTransactionCoordinator.DisposeDistributedTransaction(
                    scope,
                    transactionId,
                    requestType,
                    startTime,
                    ex);

                // Publish AfterRollback event
                await _eventPublisher.PublishAfterRollbackAsync(eventContext, cancellationToken);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _metricsCollector.RecordTransactionFailure(requestType, stopwatch.Elapsed);

                _logger.LogError(ex,
                    "Distributed transaction {TransactionId} for {RequestName} with isolation level {IsolationLevel} failed. Rolling back.",
                    transactionId,
                    requestType,
                    configuration.IsolationLevel);

                // Publish BeforeRollback event
                await _eventPublisher.PublishBeforeRollbackAsync(eventContext, cancellationToken);

                // Dispose will cause rollback since Complete was not called
                _distributedTransactionCoordinator.DisposeDistributedTransaction(
                    scope,
                    transactionId,
                    requestType,
                    startTime,
                    ex);

                // Publish AfterRollback event
                await _eventPublisher.PublishAfterRollbackAsync(eventContext, cancellationToken);

                throw;
            }
            finally
            {
                // Ensure scope is disposed
                scope?.Dispose();
            }
        }
    }
}