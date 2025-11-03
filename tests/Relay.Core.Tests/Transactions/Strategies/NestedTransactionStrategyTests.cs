using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Transactions;
using Relay.Core.Transactions.Strategies;
using Xunit;

namespace Relay.Core.Tests.Transactions.Strategies
{
    public class NestedTransactionStrategyTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<NestedTransactionManager> _nestedTransactionManagerMock;
        private readonly Mock<TransactionLogger> _transactionLoggerMock;
        private readonly NestedTransactionStrategy _strategy;

        public NestedTransactionStrategyTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _nestedTransactionManagerMock = new Mock<NestedTransactionManager>(NullLogger<NestedTransactionManager>.Instance);
            _transactionLoggerMock = new Mock<TransactionLogger>(NullLogger<TransactionLogger>.Instance);
            
            _strategy = new NestedTransactionStrategy(
                _unitOfWorkMock.Object,
                NullLogger<NestedTransactionStrategy>.Instance,
                _nestedTransactionManagerMock.Object,
                _transactionLoggerMock.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WhenNoActiveTransaction_ThrowsInvalidOperationException()
        {
            // Arrange
            _nestedTransactionManagerMock.Setup(x => x.GetCurrentContext()).Returns((ITransactionContext?)null);

            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(IsolationLevel.ReadCommitted, TimeSpan.FromMinutes(1));
            var requestType = "TestTransactionalRequest";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _strategy.ExecuteAsync(request, Mock.Of<RequestHandlerDelegate<string>>(), configuration, requestType, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_WithValidNestedTransaction_ExecutesSuccessfully()
        {
            // Arrange
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");
            mockContext.Setup(x => x.NestingLevel).Returns(1);

            _nestedTransactionManagerMock.Setup(x => x.GetCurrentContext()).Returns(mockContext.Object);

            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(IsolationLevel.ReadCommitted, TimeSpan.FromMinutes(1));
            var requestType = "TestTransactionalRequest";
            var expectedResponse = "success";

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>(expectedResponse));

            // Act
            var response = await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);
            
            _nestedTransactionManagerMock.Verify(x => x.EnterNestedTransaction(requestType), Times.Once);
            _nestedTransactionManagerMock.Verify(x => x.ExitNestedTransaction(requestType), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenHandlerThrows_PropagatesException()
        {
            // Arrange
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");
            mockContext.Setup(x => x.NestingLevel).Returns(1);

            _nestedTransactionManagerMock.Setup(x => x.GetCurrentContext()).Returns(mockContext.Object);

            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(IsolationLevel.ReadCommitted, TimeSpan.FromMinutes(1));
            var requestType = "TestTransactionalRequest";
            var expectedException = new InvalidOperationException("Test exception");

            var nextDelegate = new RequestHandlerDelegate<string>(() => throw expectedException);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

            Assert.Equal(expectedException, actualException);
            
            _nestedTransactionManagerMock.Verify(x => x.EnterNestedTransaction(requestType), Times.Once);
            _nestedTransactionManagerMock.Verify(x => x.ExitNestedTransaction(requestType), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenNestedTransactionValidationFails_ThrowsNestedTransactionException()
        {
            // Arrange
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");
            mockContext.Setup(x => x.NestingLevel).Returns(1);

            _nestedTransactionManagerMock.Setup(x => x.GetCurrentContext()).Returns(mockContext.Object);
            _nestedTransactionManagerMock.Setup(x => x.ValidateNestedTransactionConfiguration(
                It.IsAny<ITransactionContext>(),
                It.IsAny<ITransactionConfiguration>(),
                It.IsAny<string>()))
                .Throws(new NestedTransactionException("Validation failed", "test-id", 1, "TestTransactionalRequest"));

            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(IsolationLevel.ReadCommitted, TimeSpan.FromMinutes(1));
            var requestType = "TestTransactionalRequest";

            // Act & Assert
            await Assert.ThrowsAsync<NestedTransactionException>(
                async () => await _strategy.ExecuteAsync(request, Mock.Of<RequestHandlerDelegate<string>>(), configuration, requestType, CancellationToken.None));
        }

        [Fact]
        public async Task ExecuteAsync_LogsNestedTransactionEvents()
        {
            // Arrange
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");
            mockContext.Setup(x => x.NestingLevel).Returns(1);

            _nestedTransactionManagerMock.Setup(x => x.GetCurrentContext()).Returns(mockContext.Object);

            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(IsolationLevel.ReadCommitted, TimeSpan.FromMinutes(1));
            var requestType = "TestTransactionalRequest";
            var expectedResponse = "success";

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>(expectedResponse));

            // Act
            await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

            // Assert
            _transactionLoggerMock.Verify(x => x.LogNestedTransactionDetected(
                requestType, "test-transaction-id", 1), Times.Once);
            
            _transactionLoggerMock.Verify(x => x.LogSavingChanges(
                "test-transaction-id", requestType, true), Times.Once);
            
            _transactionLoggerMock.Verify(x => x.LogNestedTransactionCompleted(
                requestType, "test-transaction-id"), Times.Once);
        }

        [Transaction(IsolationLevel.ReadCommitted)]
        private class TestTransactionalRequest : ITransactionalRequest
        {
            public string Data { get; set; } = "test";
        }
    }
}