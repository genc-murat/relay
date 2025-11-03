using System;
using System.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.Transactions;
using Relay.Core.Transactions.Factories;
using Relay.Core.Transactions.Strategies;
using Xunit;

namespace Relay.Core.Tests.Transactions.Factories
{
    public class TransactionStrategyFactoryTests
    {
        private readonly Mock<NestedTransactionStrategy> _nestedTransactionStrategyMock;
        private readonly Mock<OutermostTransactionStrategy> _outermostTransactionStrategyMock;
        private readonly Mock<DistributedTransactionStrategy> _distributedTransactionStrategyMock;
        private readonly Mock<NestedTransactionManager> _nestedTransactionManagerMock;
        private readonly TransactionStrategyFactory _factory;

        public TransactionStrategyFactoryTests()
        {
            _nestedTransactionStrategyMock = new Mock<NestedTransactionStrategy>(
                Mock.Of<IUnitOfWork>(),
                NullLogger<NestedTransactionStrategy>.Instance,
                Mock.Of<NestedTransactionManager>(),
                Mock.Of<TransactionLogger>());

            _outermostTransactionStrategyMock = new Mock<OutermostTransactionStrategy>(
                Mock.Of<IUnitOfWork>(),
                NullLogger<OutermostTransactionStrategy>.Instance,
                Mock.Of<TransactionCoordinator>(),
                Mock.Of<TransactionEventPublisher>(),
                Mock.Of<TransactionMetricsCollector>(),
                Mock.Of<NestedTransactionManager>(),
                Mock.Of<TransactionActivitySource>(),
                Mock.Of<TransactionLogger>(),
                Mock.Of<ITransactionEventContextFactory>());

            _distributedTransactionStrategyMock = new Mock<DistributedTransactionStrategy>(
                Mock.Of<IUnitOfWork>(),
                NullLogger<DistributedTransactionStrategy>.Instance,
                Mock.Of<DistributedTransactionCoordinator>(),
                Mock.Of<TransactionEventPublisher>(),
                Mock.Of<TransactionMetricsCollector>(),
                Mock.Of<TransactionActivitySource>(),
                Mock.Of<TransactionLogger>(),
                Mock.Of<ITransactionEventContextFactory>());

            _nestedTransactionManagerMock = new Mock<NestedTransactionManager>(NullLogger<NestedTransactionManager>.Instance);

            _factory = new TransactionStrategyFactory(
                _nestedTransactionStrategyMock.Object,
                _outermostTransactionStrategyMock.Object,
                _distributedTransactionStrategyMock.Object,
                _nestedTransactionManagerMock.Object);
        }

        [Fact]
        public void CreateStrategy_WithDistributedTransaction_ReturnsDistributedTransactionStrategy()
        {
            // Arrange
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                useDistributedTransaction: true);

            _nestedTransactionManagerMock.Setup(x => x.IsTransactionActive()).Returns(true);

            // Act
            var strategy = _factory.CreateStrategy(configuration);

            // Assert
            Assert.Equal(_distributedTransactionStrategyMock.Object, strategy);
        }

        [Fact]
        public void Constructor_WithNullNestedTransactionStrategy_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionStrategyFactory(
                null,
                _outermostTransactionStrategyMock.Object,
                _distributedTransactionStrategyMock.Object,
                _nestedTransactionManagerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullOutermostTransactionStrategy_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionStrategyFactory(
                _nestedTransactionStrategyMock.Object,
                null,
                _distributedTransactionStrategyMock.Object,
                _nestedTransactionManagerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullDistributedTransactionStrategy_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionStrategyFactory(
                _nestedTransactionStrategyMock.Object,
                _outermostTransactionStrategyMock.Object,
                null,
                _nestedTransactionManagerMock.Object));
        }

        [Fact]
        public void Constructor_WithNullNestedTransactionManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TransactionStrategyFactory(
                _nestedTransactionStrategyMock.Object,
                _outermostTransactionStrategyMock.Object,
                _distributedTransactionStrategyMock.Object,
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

            _nestedTransactionManagerMock.Setup(x => x.IsTransactionActive()).Returns(hasActiveTransaction);

            // Act
            var strategy = _factory.CreateStrategy(configuration);

            // Assert
            if (useDistributedTransaction)
            {
                Assert.Equal(_distributedTransactionStrategyMock.Object, strategy);
            }
            else if (hasActiveTransaction)
            {
                Assert.Equal(_nestedTransactionStrategyMock.Object, strategy);
            }
            else
            {
                Assert.Equal(_outermostTransactionStrategyMock.Object, strategy);
            }
        }
    }
}