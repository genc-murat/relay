using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline;
using Relay.Core.Transactions.Factories;
using Relay.Core.Transactions.Strategies;

namespace Relay.Core.Transactions.Template
{
    /// <summary>
    /// Default implementation of transaction execution template using strategy pattern.
    /// </summary>
    public class DefaultTransactionExecutionTemplate : TransactionExecutionTemplate
    {
        private readonly ITransactionStrategyFactory _strategyFactory;

        public DefaultTransactionExecutionTemplate(
            ILogger<DefaultTransactionExecutionTemplate> logger,
            ITransactionConfigurationResolver configurationResolver,
            ITransactionRetryHandler retryHandler,
            ITransactionStrategyFactory strategyFactory)
            : base(logger, configurationResolver, retryHandler)
        {
            _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
        }

        protected override async ValueTask<TResponse> ExecuteTransactionAsync<TRequest, TResponse>(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            ITransactionConfiguration configuration,
            string requestType,
            CancellationToken cancellationToken)
        {
            var strategy = _strategyFactory.CreateStrategy(configuration);
            return await strategy.ExecuteAsync(request, next, configuration, requestType, cancellationToken);
        }
    }
}