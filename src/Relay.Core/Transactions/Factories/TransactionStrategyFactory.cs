using Relay.Core.Transactions.Strategies;

namespace Relay.Core.Transactions.Factories
{
    /// <summary>
    /// Default implementation of transaction strategy factory.
    /// </summary>
    public class TransactionStrategyFactory : ITransactionStrategyFactory
    {
        private readonly NestedTransactionStrategy _nestedTransactionStrategy;
        private readonly OutermostTransactionStrategy _outermostTransactionStrategy;
        private readonly DistributedTransactionStrategy _distributedTransactionStrategy;
        private readonly NestedTransactionManager _nestedTransactionManager;

        public TransactionStrategyFactory(
            NestedTransactionStrategy nestedTransactionStrategy,
            OutermostTransactionStrategy outermostTransactionStrategy,
            DistributedTransactionStrategy distributedTransactionStrategy,
            NestedTransactionManager nestedTransactionManager)
        {
            _nestedTransactionStrategy = nestedTransactionStrategy ?? throw new System.ArgumentNullException(nameof(nestedTransactionStrategy));
            _outermostTransactionStrategy = outermostTransactionStrategy ?? throw new System.ArgumentNullException(nameof(outermostTransactionStrategy));
            _distributedTransactionStrategy = distributedTransactionStrategy ?? throw new System.ArgumentNullException(nameof(distributedTransactionStrategy));
            _nestedTransactionManager = nestedTransactionManager ?? throw new System.ArgumentNullException(nameof(nestedTransactionManager));
        }

        public Strategies.ITransactionExecutionStrategy CreateStrategy(ITransactionConfiguration configuration)
        {
            if (configuration.UseDistributedTransaction)
            {
                return _distributedTransactionStrategy;
            }

            if (_nestedTransactionManager.IsTransactionActive())
            {
                return _nestedTransactionStrategy;
            }

            return _outermostTransactionStrategy;
        }
    }
}