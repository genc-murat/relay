using System;
using System.Data;
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
    public class DistributedTransactionStrategyTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<DistributedTransactionCoordinator> _distributedTransactionCoordinatorMock;
        private readonly Mock<TransactionEventPublisher> _eventPublisherMock;
        private readonly Mock<TransactionMetricsCollector> _metricsCollectorMock;
        private readonly Mock<TransactionActivitySource> _activitySourceMock;
        private readonly Mock<TransactionLogger> _transactionLoggerMock;
        private readonly Mock<ITransactionEventContextFactory> _eventContextFactoryMock;
        private readonly DistributedTransactionStrategy _strategy;

        public DistributedTransactionStrategyTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _distributedTransactionCoordinatorMock = new Mock<DistributedTransactionCoordinator>(NullLogger<DistributedTransactionCoordinator>.Instance);
            _eventPublisherMock = new Mock<TransactionEventPublisher>();
            _metricsCollectorMock = new Mock<TransactionMetricsCollector>();
            _activitySourceMock = new Mock<TransactionActivitySource>();
            _transactionLoggerMock = new Mock<TransactionLogger>(NullLogger<TransactionLogger>.Instance);
            _eventContextFactoryMock = new Mock<ITransactionEventContextFactory>();

            _strategy = new DistributedTransactionStrategy(
                _unitOfWorkMock.Object,
                NullLogger<DistributedTransactionStrategy>.Instance,
                _distributedTransactionCoordinatorMock.Object,
                _eventPublisherMock.Object,
                _metricsCollectorMock.Object,
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
                useDistributedTransaction: true);
            var requestType = "TestTransactionalRequest";
            var expectedResponse = "success";
            var transactionId = "distributed-transaction-id";
            var startTime = DateTime.UtcNow;

            var mockScope = new Mock<System.Transactions.TransactionScope>();
            var eventContext = new TransactionEventContext
            {
                TransactionId = transactionId,
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel
            };

            _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
                .Returns(eventContext);

            _distributedTransactionCoordinatorMock.Setup(x => x.CreateDistributedTransactionScope(
                    configuration, requestType, It.IsAny<CancellationToken>()))
                .Returns((mockScope.Object, transactionId, startTime));

            _distributedTransactionCoordinatorMock.Setup(x => x.ExecuteInDistributedTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task<string>>>(),
                    transactionId,
                    configuration.Timeout,
                    requestType,
                    startTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>(expectedResponse));

            // Act
            var response = await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            _eventPublisherMock.Verify(x => x.PublishBeforeBeginAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishAfterBeginAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishBeforeCommitAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishAfterCommitAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);

            _distributedTransactionCoordinatorMock.Verify(x => x.CompleteDistributedTransaction(
                mockScope.Object, transactionId, requestType, startTime), Times.Once);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenHandlerThrows_RollsBackTransaction()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                useDistributedTransaction: true);
            var requestType = "TestTransactionalRequest";
            var expectedException = new InvalidOperationException("Test exception");
            var transactionId = "distributed-transaction-id";
            var startTime = DateTime.UtcNow;

            var mockScope = new Mock<System.Transactions.TransactionScope>();
            var eventContext = new TransactionEventContext
            {
                TransactionId = transactionId,
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel
            };

            _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
                .Returns(eventContext);

            _distributedTransactionCoordinatorMock.Setup(x => x.CreateDistributedTransactionScope(
                    configuration, requestType, It.IsAny<CancellationToken>()))
                .Returns((mockScope.Object, transactionId, startTime));

            _distributedTransactionCoordinatorMock.Setup(x => x.ExecuteInDistributedTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task<string>>>(),
                    transactionId,
                    configuration.Timeout,
                    requestType,
                    startTime,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var nextDelegate = new RequestHandlerDelegate<string>(() => throw expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

            Assert.Equal(expectedException, actualException);

            _eventPublisherMock.Verify(x => x.PublishBeforeRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishAfterRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);

            mockScope.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenBeforeCommitEventFails_RollsBackTransaction()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                useDistributedTransaction: true);
            var requestType = "TestTransactionalRequest";
            var eventException = new TransactionEventHandlerException("Event failed", "BeforeCommit", "test-id");
            var transactionId = "distributed-transaction-id";
            var startTime = DateTime.UtcNow;

            var mockScope = new Mock<System.Transactions.TransactionScope>();
            var eventContext = new TransactionEventContext
            {
                TransactionId = transactionId,
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel
            };

            _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
                .Returns(eventContext);

            _distributedTransactionCoordinatorMock.Setup(x => x.CreateDistributedTransactionScope(
                    configuration, requestType, It.IsAny<CancellationToken>()))
                .Returns((mockScope.Object, transactionId, startTime));

            _distributedTransactionCoordinatorMock.Setup(x => x.ExecuteInDistributedTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task<string>>>(),
                    transactionId,
                    configuration.Timeout,
                    requestType,
                    startTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("success");

            _eventPublisherMock.Setup(x => x.PublishBeforeCommitAsync(eventContext, It.IsAny<CancellationToken>()))
                .ThrowsAsync(eventException);

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<TransactionEventHandlerException>(
                async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

            Assert.Equal(eventException, actualException);

            _eventPublisherMock.Verify(x => x.PublishBeforeRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishAfterRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);

            mockScope.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_LogsDistributedTransactionEvents()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                useDistributedTransaction: true);
            var requestType = "TestTransactionalRequest";
            var transactionId = "distributed-transaction-id";
            var startTime = DateTime.UtcNow;

            var mockScope = new Mock<System.Transactions.TransactionScope>();
            var eventContext = new TransactionEventContext
            {
                TransactionId = transactionId,
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel
            };

            _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
                .Returns(eventContext);

            _distributedTransactionCoordinatorMock.Setup(x => x.CreateDistributedTransactionScope(
                    configuration, requestType, It.IsAny<CancellationToken>()))
                .Returns((mockScope.Object, transactionId, startTime));

            _distributedTransactionCoordinatorMock.Setup(x => x.ExecuteInDistributedTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task<string>>>(),
                    transactionId,
                    configuration.Timeout,
                    requestType,
                    startTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("success");

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

            // Act
            await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

            // Assert
            _transactionLoggerMock.Verify(x => x.LogDistributedTransactionCreated(
                transactionId, requestType, configuration.IsolationLevel), Times.Once);
            
            _transactionLoggerMock.Verify(x => x.LogSavingChanges(
                transactionId, requestType, false), Times.Once);
            
            _transactionLoggerMock.Verify(x => x.LogDistributedTransactionCommitted(
                transactionId, requestType, configuration.IsolationLevel), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_RecordsMetricsOnSuccess()
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                IsolationLevel.ReadCommitted,
                TimeSpan.FromMinutes(1),
                useDistributedTransaction: true);
            var requestType = "TestTransactionalRequest";
            var transactionId = "distributed-transaction-id";
            var startTime = DateTime.UtcNow;

            var mockScope = new Mock<System.Transactions.TransactionScope>();
            var eventContext = new TransactionEventContext
            {
                TransactionId = transactionId,
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel
            };

            _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
                .Returns(eventContext);

            _distributedTransactionCoordinatorMock.Setup(x => x.CreateDistributedTransactionScope(
                    configuration, requestType, It.IsAny<CancellationToken>()))
                .Returns((mockScope.Object, transactionId, startTime));

            _distributedTransactionCoordinatorMock.Setup(x => x.ExecuteInDistributedTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task<string>>>(),
                    transactionId,
                    configuration.Timeout,
                    requestType,
                    startTime,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("success");

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