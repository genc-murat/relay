using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Pipeline;
using Relay.Core.Transactions.Factories;
using Relay.Core.Transactions.Template;

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
        private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;
        private readonly TransactionExecutionTemplate _executionTemplate;

        public TransactionBehavior(
            ILogger<TransactionBehavior<TRequest, TResponse>> logger,
            DefaultTransactionExecutionTemplate executionTemplate)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _executionTemplate = executionTemplate ?? throw new ArgumentNullException(nameof(executionTemplate));
        }

public async ValueTask<TResponse> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            return await _executionTemplate.ExecuteAsync(request, next, cancellationToken);
        }


    }
}