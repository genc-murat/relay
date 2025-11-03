using Relay.Core.Transactions.Strategies;

namespace Relay.Core.Transactions.Factories
{
    /// <summary>
    /// Factory for creating transaction execution strategies.
    /// </summary>
    public interface ITransactionStrategyFactory
    {
        /// <summary>
        /// Creates the appropriate transaction execution strategy based on the configuration.
        /// </summary>
        /// <param name="configuration">The transaction configuration.</param>
        /// <returns>The appropriate transaction execution strategy.</returns>
        ITransactionExecutionStrategy CreateStrategy(ITransactionConfiguration configuration);
    }
}