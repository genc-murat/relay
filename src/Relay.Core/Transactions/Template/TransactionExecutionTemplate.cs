using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline;

namespace Relay.Core.Transactions.Template
{
    /// <summary>
    /// Template method pattern implementation for transaction execution lifecycle.
    /// </summary>
    public abstract class TransactionExecutionTemplate
    {
        protected readonly ILogger Logger;
        protected readonly ITransactionConfigurationResolver ConfigurationResolver;
        protected readonly ITransactionRetryHandler RetryHandler;

        protected TransactionExecutionTemplate(
            ILogger logger,
            ITransactionConfigurationResolver configurationResolver,
            ITransactionRetryHandler retryHandler)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ConfigurationResolver = configurationResolver ?? throw new ArgumentNullException(nameof(configurationResolver));
            RetryHandler = retryHandler ?? throw new ArgumentNullException(nameof(retryHandler));
        }

        /// <summary>
        /// Executes the transaction using the template method pattern.
        /// </summary>
        public async ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
            where TRequest : notnull
        {
            if (!ShouldProcessTransaction(request))
            {
                return await next();
            }

            var requestType = typeof(TRequest).Name;
            var configuration = await ResolveAndValidateConfigurationAsync(request, requestType);

            PreProcess(request, configuration, requestType);

            try
            {
                if (ShouldUseRetry(configuration))
                {
                    return await ExecuteWithRetryAsync(request, next, configuration, requestType, cancellationToken);
                }

                return await ExecuteTransactionAsync(request, next, configuration, requestType, cancellationToken);
            }
            finally
            {
                PostProcess(request, configuration, requestType);
            }
        }

        /// <summary>
        /// Determines if the request should be processed as a transaction.
        /// </summary>
        protected virtual bool ShouldProcessTransaction<TRequest>(TRequest request) where TRequest : notnull
        {
            return request is ITransactionalRequest;
        }

        /// <summary>
        /// Resolves and validates the transaction configuration.
        /// </summary>
        protected virtual async ValueTask<ITransactionConfiguration> ResolveAndValidateConfigurationAsync<TRequest>(
            TRequest request,
            string requestType)
            where TRequest : notnull
        {
            ITransactionConfiguration configuration;
            try
            {
                configuration = ConfigurationResolver.Resolve(request);
            }
            catch (TransactionConfigurationException ex)
            {
                Logger.LogError(ex,
                    "Transaction configuration error for {RequestName}: {ErrorMessage}",
                    requestType,
                    ex.Message);
                throw;
            }

            if (configuration.IsolationLevel == IsolationLevel.Unspecified)
            {
                var errorMessage = $"Transaction isolation level cannot be Unspecified for request type '{requestType}'. " +
                    "You must explicitly specify an isolation level in the [Transaction] attribute.";
                Logger.LogError("{ErrorMessage}", errorMessage);
                throw new TransactionConfigurationException(errorMessage, typeof(TRequest));
            }

            return await ValueTask.FromResult(configuration);
        }

        /// <summary>
        /// Pre-processing hook before transaction execution.
        /// </summary>
        protected virtual void PreProcess<TRequest>(
            TRequest request,
            ITransactionConfiguration configuration,
            string requestType)
            where TRequest : notnull
        {
            // Default implementation does nothing
        }

        /// <summary>
        /// Post-processing hook after transaction execution.
        /// </summary>
        protected virtual void PostProcess<TRequest>(
            TRequest request,
            ITransactionConfiguration configuration,
            string requestType)
            where TRequest : notnull
        {
            // Default implementation does nothing
        }

        /// <summary>
        /// Determines if retry should be used for this transaction.
        /// </summary>
        protected virtual bool ShouldUseRetry(ITransactionConfiguration configuration)
        {
            return configuration.RetryPolicy != null && configuration.RetryPolicy.MaxRetries > 0;
        }

        /// <summary>
        /// Executes the transaction with retry logic.
        /// </summary>
        protected virtual async ValueTask<TResponse> ExecuteWithRetryAsync<TRequest, TResponse>(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            ITransactionConfiguration configuration,
            string requestType,
            CancellationToken cancellationToken)
            where TRequest : notnull
        {
            return await RetryHandler.ExecuteWithRetryAsync(
                async ct => await ExecuteTransactionAsync(request, next, configuration, requestType, ct),
                configuration.RetryPolicy,
                null,
                requestType,
                cancellationToken);
        }

        /// <summary>
        /// Abstract method for executing the actual transaction.
        /// Must be implemented by concrete templates.
        /// </summary>
        protected abstract ValueTask<TResponse> ExecuteTransactionAsync<TRequest, TResponse>(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            ITransactionConfiguration configuration,
            string requestType,
            CancellationToken cancellationToken)
            where TRequest : notnull;
    }
}