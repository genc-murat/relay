using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Transactions;
using Relay.Core.Transactions.Factories;
using Relay.Core.Transactions.Strategies;
using Xunit;

namespace Relay.Core.Tests.Transactions.Strategies
{
    public class OutermostTransactionStrategyTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<TransactionCoordinator> _transactionCoordinatorMock;
        private readonly Mock<TransactionEventPublisher> _eventPublisherMock;
        private readonly Mock<TransactionMetricsCollector> _metricsCollectorMock;
        private readonly Mock<NestedTransactionManager> _nestedTransactionManagerMock;
        private readonly Mock<TransactionActivitySource> _activitySourceMock;
        private readonly Mock<TransactionLogger> _transactionLoggerMock;
        private readonly Mock<ITransactionEventContextFactory> _eventContextFactoryMock;
        private readonly OutermostTransactionStrategy _strategy;

        public OutermostTransactionStrategyTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _transactionCoordinatorMock = new Mock<TransactionCoordinator>(NullLogger<TransactionCoordinator>.Instance);
            _eventPublisherMock = new Mock<TransactionEventPublisher>();
            _metricsCollectorMock = new Mock<TransactionMetricsCollector>();
            _nestedTransactionManagerMock = new Mock<NestedTransactionManager>(NullLogger<NestedTransactionManager>.Instance);
            _activitySourceMock = new Mock<TransactionActivitySource>();
            _transactionLoggerMock = new Mock<TransactionLogger>(NullLogger<TransactionLogger>.Instance);
            _eventContextFactoryMock = new Mock<ITransactionEventContextFactory>();

            _strategy = new OutermostTransactionStrategy(
                _unitOfWorkMock.Object,
                NullLogger<OutermostTransactionStrategy>.Instance,
                _transactionCoordinatorMock.Object,
                _eventPublisherMock.Object,
                _metricsCollectorMock.Object,
                _nestedTransactionManagerMock.Object,
                _activitySourceMock.Object,
                _transactionLoggerMock.Object,
                _eventContextFactoryMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidConfiguration_ExecutesSuccessfully()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                isReadOnly: false);
            var requestType = "TestTransactionalRequest";
            var expectedResponse = "success";

            var mockTransaction = new Mock<IRelayDbTransaction>();
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");

            var eventContext = new TransactionEventContext
            {
                TransactionId = "test-transaction-id",
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel
            };

            _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
                .Returns(eventContext);

            _transactionCoordinatorMock.Setup(x => x.BeginTransactionAsync(
                    configuration, requestType, It.IsAny<CancellationToken>()))
                .ReturnsAsync((mockTransaction.Object, mockContext.Object, null));

            _nestedTransactionManagerMock.Setup(x => x.ShouldCommitTransaction(mockContext.Object))
                .Returns(true);

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>(expectedResponse));

            // Act
            var response = await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            _eventPublisherMock.Verify(x => x.PublishBeforeBeginAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishAfterBeginAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishBeforeCommitAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishAfterCommitAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);

            _transactionCoordinatorMock.Verify(x => x.CommitTransactionAsync(
                mockTransaction.Object, mockContext.Object, requestType, It.IsAny<CancellationToken>()), Times.Once);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithReadOnlyConfiguration_SetsReadOnlyMode()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                isReadOnly: true);
            var requestType = "TestTransactionalRequest";

            var mockTransaction = new Mock<IRelayDbTransaction>();
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");

            var eventContext = new TransactionEventContext
            {
                TransactionId = "test-transaction-id",
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel
            };

            _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
                .Returns(eventContext);

            _transactionCoordinatorMock.Setup(x => x.BeginTransactionAsync(
                    configuration, requestType, It.IsAny<CancellationToken>()))
                .ReturnsAsync((mockTransaction.Object, mockContext.Object, null));

            _nestedTransactionManagerMock.Setup(x => x.ShouldCommitTransaction(mockContext.Object))
                .Returns(true);

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

            // Act
            await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

            // Assert
            Assert.True(_unitOfWorkMock.Object.IsReadOnly);
        }

        [Fact]
        public async Task ExecuteAsync_WhenHandlerThrows_RollsBackTransaction()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1));
            var requestType = "TestTransactionalRequest";
            var expectedException = new InvalidOperationException("Test exception");

            var mockTransaction = new Mock<IRelayDbTransaction>();
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");

            var eventContext = new TransactionEventContext
            {
                TransactionId = "test-transaction-id",
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel
            };

            _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
                .Returns(eventContext);

            _transactionCoordinatorMock.Setup(x => x.BeginTransactionAsync(
                    configuration, requestType, It.IsAny<CancellationToken>()))
                .ReturnsAsync((mockTransaction.Object, mockContext.Object, null));

            _nestedTransactionManagerMock.Setup(x => x.ShouldRollbackTransaction(mockContext.Object))
                .Returns(true);

            var nextDelegate = new RequestHandlerDelegate<string>(() => throw expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

            Assert.Equal(expectedException, actualException);

            _eventPublisherMock.Verify(x => x.PublishBeforeRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishAfterRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);

            _transactionCoordinatorMock.Verify(x => x.RollbackTransactionAsync(
                mockTransaction.Object, mockContext.Object, requestType, expectedException, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenBeforeCommitEventFails_RollsBackTransaction()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1));
            var requestType = "TestTransactionalRequest";
            var eventException = new TransactionEventHandlerException("Event failed", "BeforeCommit", "test-id");

            var mockTransaction = new Mock<IRelayDbTransaction>();
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");

            var eventContext = new TransactionEventContext
            {
                TransactionId = "test-transaction-id",
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel
            };

            _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
                .Returns(eventContext);

            _transactionCoordinatorMock.Setup(x => x.BeginTransactionAsync(
                    configuration, requestType, It.IsAny<CancellationToken>()))
                .ReturnsAsync((mockTransaction.Object, mockContext.Object, null));

            _nestedTransactionManagerMock.Setup(x => x.ShouldRollbackTransaction(mockContext.Object))
                .Returns(true);

            _eventPublisherMock.Setup(x => x.PublishBeforeCommitAsync(eventContext, It.IsAny<CancellationToken>()))
                .ThrowsAsync(eventException);

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TransactionEventHandlerException>(
                async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

            Assert.Equal(eventException, actualException);

            _eventPublisherMock.Verify(x => x.PublishBeforeRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishAfterRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);

            _transactionCoordinatorMock.Verify(x => x.RollbackTransactionAsync(
                mockTransaction.Object, mockContext.Object, requestType, eventException, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_RecordsMetricsOnSuccess()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1));
            var requestType = "TestTransactionalRequest";

            var mockTransaction = new Mock<IRelayDbTransaction>();
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");

            var eventContext = new TransactionEventContext
            {
                TransactionId = "test-transaction-id",
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel
            };

            _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
                .Returns(eventContext);

            _transactionCoordinatorMock.Setup(x => x.BeginTransactionAsync(
                    configuration, requestType, It.IsAny<CancellationToken>()))
                .ReturnsAsync((mockTransaction.Object, mockContext.Object, null));

            _nestedTransactionManagerMock.Setup(x => x.ShouldCommitTransaction(mockContext.Object))
                .Returns(true);

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

            // Act
            await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

            // Assert
            _metricsCollectorMock.Verify(x => x.RecordTransactionSuccess(
                configuration.IsolationLevel, requestType, It.IsAny<TimeSpan>()), Times.Once);
        }

        [Transaction(IsolationLevel.ReadCommitted)]
        private class TestTransactionalRequest : ITransactionalRequest
        {
            public string Data { get; set; } = "test";
        }
    }
}