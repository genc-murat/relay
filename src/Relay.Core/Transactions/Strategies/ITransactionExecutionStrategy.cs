using System;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Pipeline;

namespace Relay.Core.Transactions.Strategies
{
    /// <summary>
    /// Defines the contract for transaction execution strategies.
    /// </summary>
    public interface ITransactionExecutionStrategy
    {
        /// <summary>
        /// Executes a transactional request using the specific strategy.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The request to execute.</param>
        /// <param name="next">The next handler in the pipeline.</param>
        /// <param name="configuration">The transaction configuration.</param>
        /// <param name="requestType">The name of the request type.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The response from the handler.</returns>
        ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            ITransactionConfiguration configuration,
            string requestType,
            CancellationToken cancellationToken)
            where TRequest : notnull;
    }
}