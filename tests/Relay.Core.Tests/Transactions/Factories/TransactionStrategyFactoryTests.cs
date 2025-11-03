using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.Transactions;
using Relay.Core.Transactions.Factories;
using Relay.Core.Transactions.Strategies;
using Xunit;
using IRelayDbTransaction = Relay.Core.Transactions.IRelayDbTransaction;

namespace Relay.Core.Tests.Transactions.Factories
{
    public class TransactionStrategyFactoryTests
    {
        private class MockDbTransaction : IRelayDbTransaction
        {
            public IDbConnection? Connection => null;
            public IsolationLevel IsolationLevel => System.Data.IsolationLevel.ReadCommitted;

            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void Commit() { }
            public void Rollback() { }
            public void Dispose() { }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        private readonly NestedTransactionManager _nestedTransactionManager;
        private readonly TransactionCoordinator _transactionCoordinator;
        private readonly DistributedTransactionCoordinator _distributedTransactionCoordinator;
        private readonly TransactionLogger _transactionLogger;
        private readonly TransactionActivitySource _transactionActivitySource;
        private readonly TransactionEventPublisher _transactionEventPublisher;
        private readonly TransactionMetricsCollector _transactionMetricsCollector;
        private readonly ITransactionEventContextFactory _transactionEventContextFactory;
        private readonly TransactionStrategyFactory _factory;

        public TransactionStrategyFactoryTests()
        {
            // Initialize real instances for sealed classes
            _nestedTransactionManager = new NestedTransactionManager(NullLogger<NestedTransactionManager>.Instance);
            _transactionCoordinator = new TransactionCoordinator(Mock.Of<IUnitOfWork>(), NullLogger<TransactionCoordinator>.Instance);
            _distributedTransactionCoordinator = new DistributedTransactionCoordinator(NullLogger<DistributedTransactionCoordinator>.Instance);
            _transactionLogger = new TransactionLogger(NullLogger.Instance);
            _transactionActivitySource = new TransactionActivitySource();
            _transactionEventPublisher = new TransactionEventPublisher(
                new List<ITransactionEventHandler>(), 
                NullLogger<TransactionEventPublisher>.Instance);
            _transactionMetricsCollector = new TransactionMetricsCollector();
            _transactionEventContextFactory = Mock.Of<ITransactionEventContextFactory>();

            // Create real strategy instances
            var nestedTransactionStrategy = new NestedTransactionStrategy(
                Mock.Of<IUnitOfWork>(),
                NullLogger<NestedTransactionStrategy>.Instance,
                _nestedTransactionManager,
                _transactionLogger);

            var outermostTransactionStrategy = new OutermostTransactionStrategy(
                Mock.Of<IUnitOfWork>(),
                NullLogger<OutermostTransactionStrategy>.Instance,
                _transactionCoordinator,
                _transactionEventPublisher,
                _transactionMetricsCollector,
                _nestedTransactionManager,
                _transactionActivitySource,
                _transactionLogger,
                _transactionEventContextFactory);

            var distributedTransactionStrategy = new DistributedTransactionStrategy(
                Mock.Of<IUnitOfWork>(),
                NullLogger<DistributedTransactionStrategy>.Instance,
                _distributedTransactionCoordinator,
                _transactionEventPublisher,
                _transactionMetricsCollector,
                _transactionActivitySource,
                _transactionLogger,
                _transactionEventContextFactory);

            _factory = new TransactionStrategyFactory(
                nestedTransactionStrategy,
                outermostTransactionStrategy,
                distributedTransactionStrategy,
                _nestedTransactionManager);
        }

        [Fact]
        public void CreateStrategy_WithDistributedTransaction_ReturnsDistributedTransactionStrategy()
        {
            // Arrange
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                useDistributedTransaction: true);

            // Note: Since NestedTransactionManager is not mockable (sealed class),
            // we need to set up the actual transaction context for testing
            TransactionContextAccessor.Clear();

            // Act
            var strategy = _factory.CreateStrategy(configuration);

            // Assert
            Assert.IsType<DistributedTransactionStrategy>(strategy);
        }

        [Fact]
        public void Constructor_WithNullNestedTransactionStrategy_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionStrategyFactory(
                null,
                new OutermostTransactionStrategy(
                    Mock.Of<IUnitOfWork>(),
                    NullLogger<OutermostTransactionStrategy>.Instance,
                    _transactionCoordinator,
                    _transactionEventPublisher,
                    _transactionMetricsCollector,
                    _nestedTransactionManager,
                    _transactionActivitySource,
                    _transactionLogger,
                    _transactionEventContextFactory),
                new DistributedTransactionStrategy(
                    Mock.Of<IUnitOfWork>(),
                    NullLogger<DistributedTransactionStrategy>.Instance,
                    _distributedTransactionCoordinator,
                    _transactionEventPublisher,
                    _transactionMetricsCollector,
                    _transactionActivitySource,
                    _transactionLogger,
                    _transactionEventContextFactory),
                _nestedTransactionManager));
        }

        [Fact]
        public void Constructor_WithNullOutermostTransactionStrategy_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionStrategyFactory(
                new NestedTransactionStrategy(
                    Mock.Of<IUnitOfWork>(),
                    NullLogger<NestedTransactionStrategy>.Instance,
                    _nestedTransactionManager,
                    _transactionLogger),
                null,
                new DistributedTransactionStrategy(
                    Mock.Of<IUnitOfWork>(),
                    NullLogger<DistributedTransactionStrategy>.Instance,
                    _distributedTransactionCoordinator,
                    _transactionEventPublisher,
                    _transactionMetricsCollector,
                    _transactionActivitySource,
                    _transactionLogger,
                    _transactionEventContextFactory),
                _nestedTransactionManager));
        }

        [Fact]
        public void Constructor_WithNullDistributedTransactionStrategy_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionStrategyFactory(
                new NestedTransactionStrategy(
                    Mock.Of<IUnitOfWork>(),
                    NullLogger<NestedTransactionStrategy>.Instance,
                    _nestedTransactionManager,
                    _transactionLogger),
                new OutermostTransactionStrategy(
                    Mock.Of<IUnitOfWork>(),
                    NullLogger<OutermostTransactionStrategy>.Instance,
                    _transactionCoordinator,
                    _transactionEventPublisher,
                    _transactionMetricsCollector,
                    _nestedTransactionManager,
                    _transactionActivitySource,
                    _transactionLogger,
                    _transactionEventContextFactory),
                null,
                _nestedTransactionManager));
        }

        [Fact]
        public void Constructor_WithNullNestedTransactionManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionStrategyFactory(
                new NestedTransactionStrategy(
                    Mock.Of<IUnitOfWork>(),
                    NullLogger<NestedTransactionStrategy>.Instance,
                    _nestedTransactionManager,
                    _transactionLogger),
                new OutermostTransactionStrategy(
                    Mock.Of<IUnitOfWork>(),
                    NullLogger<OutermostTransactionStrategy>.Instance,
                    _transactionCoordinator,
                    _transactionEventPublisher,
                    _transactionMetricsCollector,
                    _nestedTransactionManager,
                    _transactionActivitySource,
                    _transactionLogger,
                    _transactionEventContextFactory),
                new DistributedTransactionStrategy(
                    Mock.Of<IUnitOfWork>(),
                    NullLogger<DistributedTransactionStrategy>.Instance,
                    _distributedTransactionCoordinator,
                    _transactionEventPublisher,
                    _transactionMetricsCollector,
                    _transactionActivitySource,
                    _transactionLogger,
                    _transactionEventContextFactory),
                null));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void CreateStrategy_VariousCombinations_ReturnsCorrectStrategy(
            bool useDistributedTransaction,
            bool hasActiveTransaction)
        {
            // Arrange
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                useDistributedTransaction: useDistributedTransaction);

            // Note: Since NestedTransactionManager is not mockable (sealed class),
            // we need to set up the actual transaction context for testing
            TransactionContextAccessor.Clear();
            if (hasActiveTransaction)
            {
                var mockTransaction = new MockDbTransaction();
                var context = new TransactionContext(mockTransaction, IsolationLevel.ReadCommitted);
                TransactionContextAccessor.Current = context;
            }

            // Act
            var strategy = _factory.CreateStrategy(configuration);

            // Assert
            if (useDistributedTransaction)
            {
                Assert.IsType<DistributedTransactionStrategy>(strategy);
            }
            else if (hasActiveTransaction)
            {
                Assert.IsType<NestedTransactionStrategy>(strategy);
            }
            else
            {
                Assert.IsType<OutermostTransactionStrategy>(strategy);
            }
        }
    }
}