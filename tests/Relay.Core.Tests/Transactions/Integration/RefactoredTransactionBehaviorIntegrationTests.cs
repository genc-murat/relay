using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Transactions;
using Relay.Core.Transactions.Factories;
using Relay.Core.Transactions.Strategies;
using Relay.Core.Transactions.Template;
using Xunit;

namespace Relay.Core.Tests.Transactions.Integration
{
    public class RefactoredTransactionBehaviorIntegrationTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<TransactionCoordinator> _transactionCoordinatorMock;
        private readonly Mock<NestedTransactionManager> _nestedTransactionManagerMock;
        private readonly Mock<DistributedTransactionCoordinator> _distributedTransactionCoordinatorMock;

        public RefactoredTransactionBehaviorIntegrationTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _transactionCoordinatorMock = new Mock<TransactionCoordinator>(NullLogger<TransactionCoordinator>.Instance);
            _nestedTransactionManagerMock = new Mock<NestedTransactionManager>(NullLogger<NestedTransactionManager>.Instance);
            _distributedTransactionCoordinatorMock = new Mock<DistributedTransactionCoordinator>(NullLogger<DistributedTransactionCoordinator>.Instance);

            var services = new ServiceCollection();
            
            // Register all required services for the refactored TransactionBehavior
            services.AddSingleton(NullLogger<TransactionBehavior<TestRequest, string>>.Instance);
            services.AddSingleton(_unitOfWorkMock.Object);
            services.AddSingleton(new TransactionConfigurationResolver(Options.Create(new TransactionOptions())));
            services.AddSingleton(_transactionCoordinatorMock.Object);
            services.AddSingleton(Mock.Of<TransactionEventPublisher>());
            services.AddSingleton(Mock.Of<TransactionRetryHandler>());
            services.AddSingleton(Mock.Of<TransactionMetricsCollector>());
            services.AddSingleton(_nestedTransactionManagerMock.Object);
            services.AddSingleton(_distributedTransactionCoordinatorMock.Object);
            services.AddSingleton(Mock.Of<TransactionActivitySource>());
            services.AddSingleton(Mock.Of<TransactionLogger>());
            services.AddSingleton<ITransactionEventContextFactory, TransactionEventContextFactory>();
            
            // Register strategies
            services.AddSingleton<NestedTransactionStrategy>();
            services.AddSingleton<OutermostTransactionStrategy>();
            services.AddSingleton<DistributedTransactionStrategy>();
            
            // Register factories
            services.AddSingleton<ITransactionStrategyFactory, TransactionStrategyFactory>();
            
            // Register template
            services.AddSingleton<TransactionExecutionTemplate, DefaultTransactionExecutionTemplate>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task HandleAsync_WithNonTransactionalRequest_ExecutesWithoutTransaction()
        {
            // Arrange
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<TestRequest, string>>();
            var request = new TestRequest { Data = "test" };
            var expectedResponse = "success";

            // Act
            var response = await behavior.HandleAsync(
                request,
                () => new ValueTask<string>(expectedResponse),
                CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);
            
            _transactionCoordinatorMock.Verify(x => x.BeginTransactionAsync(
                It.IsAny<ITransactionConfiguration>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithOutermostTransaction_UsesOutermostStrategy()
        {
            // Arrange
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<OutermostRequest, string>>();
            var request = new OutermostRequest { Data = "test" };
            var expectedResponse = "success";

            var mockTransaction = new Mock<IRelayDbTransaction>();
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");

            _transactionCoordinatorMock.Setup(x => x.BeginTransactionAsync(
                    It.IsAny<ITransactionConfiguration>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((mockTransaction.Object, mockContext.Object, null));

            _nestedTransactionManagerMock.Setup(x => x.IsTransactionActive()).Returns(false);

            // Act
            var response = await behavior.HandleAsync(
                request,
                () => new ValueTask<string>(expectedResponse),
                CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            _transactionCoordinatorMock.Verify(x => x.BeginTransactionAsync(
                It.IsAny<ITransactionConfiguration>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithNestedTransaction_UsesNestedStrategy()
        {
            // Arrange
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<NestedRequest, string>>();
            var request = new NestedRequest { Data = "test" };
            var expectedResponse = "success";

            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");

            _nestedTransactionManagerMock.Setup(x => x.IsTransactionActive()).Returns(true);
            _nestedTransactionManagerMock.Setup(x => x.GetCurrentContext()).Returns(mockContext.Object);

            // Act
            var response = await behavior.HandleAsync(
                request,
                () => new ValueTask<string>(expectedResponse),
                CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            _nestedTransactionManagerMock.Verify(x => x.EnterNestedTransaction("NestedRequest"), Times.Once);
            _nestedTransactionManagerMock.Verify(x => x.ExitNestedTransaction("NestedRequest"), Times.Once);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _transactionCoordinatorMock.Verify(x => x.BeginTransactionAsync(
                It.IsAny<ITransactionConfiguration>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithDistributedTransaction_UsesDistributedStrategy()
        {
            // Arrange
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<DistributedRequest, string>>();
            var request = new DistributedRequest { Data = "test" };
            var expectedResponse = "success";

            var mockScope = new Mock<System.Transactions.TransactionScope>();

            _distributedTransactionCoordinatorMock.Setup(x => x.CreateDistributedTransactionScope(
                    It.IsAny<ITransactionConfiguration>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .Returns((mockScope.Object, "distributed-transaction-id", DateTime.UtcNow));

            _distributedTransactionCoordinatorMock.Setup(x => x.ExecuteInDistributedTransactionAsync(
                    It.IsAny<Func<CancellationToken, Task<string>>>(),
                    It.IsAny<string>(),
                    It.IsAny<TimeSpan>(),
                    It.IsAny<string>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var response = await behavior.HandleAsync(
                request,
                () => new ValueTask<string>(expectedResponse),
                CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            _distributedTransactionCoordinatorMock.Verify(x => x.CreateDistributedTransactionScope(
                It.IsAny<ITransactionConfiguration>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _distributedTransactionCoordinatorMock.Verify(x => x.CompleteDistributedTransaction(
                It.IsAny<System.Transactions.TransactionScope>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>()), Times.Once);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithRetryPolicy_ExecutesWithRetry()
        {
            // Arrange
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<RetryRequest, string>>();
            var request = new RetryRequest { Data = "test" };
            var expectedResponse = "success";

            var mockTransaction = new Mock<IRelayDbTransaction>();
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");

            _transactionCoordinatorMock.Setup(x => x.BeginTransactionAsync(
                    It.IsAny<ITransactionConfiguration>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((mockTransaction.Object, mockContext.Object, null));

            _nestedTransactionManagerMock.Setup(x => x.IsTransactionActive()).Returns(false);

            // Act
            var response = await behavior.HandleAsync(
                request,
                () => new ValueTask<string>(expectedResponse),
                CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            _transactionCoordinatorMock.Verify(x => x.BeginTransactionAsync(
                It.IsAny<ITransactionConfiguration>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);

            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenTransactionFails_RollsBackCorrectly()
        {
            // Arrange
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<OutermostRequest, string>>();
            var request = new OutermostRequest { Data = "test" };
            var expectedException = new InvalidOperationException("Test failure");

            var mockTransaction = new Mock<IRelayDbTransaction>();
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");

            _transactionCoordinatorMock.Setup(x => x.BeginTransactionAsync(
                    It.IsAny<ITransactionConfiguration>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((mockTransaction.Object, mockContext.Object, null));

            _nestedTransactionManagerMock.Setup(x => x.IsTransactionActive()).Returns(false);

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await behavior.HandleAsync(
                    request,
                    () => throw expectedException,
                    CancellationToken.None));

            Assert.Equal(expectedException, actualException);

            _transactionCoordinatorMock.Verify(x => x.RollbackTransactionAsync(
                mockTransaction.Object,
                mockContext.Object,
                It.IsAny<string>(),
                expectedException,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithUnspecifiedIsolationLevel_ThrowsConfigurationException()
        {
            // Arrange
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<UnspecifiedRequest, string>>();
            var request = new UnspecifiedRequest { Data = "test" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TransactionConfigurationException>(
                async () => await behavior.HandleAsync(
                    request,
                    () => new ValueTask<string>("success"),
                    CancellationToken.None));

            Assert.Contains("cannot be Unspecified", exception.Message);
        }

        [Fact]
        public async Task HandleAsync_WithMissingTransactionAttribute_ThrowsConfigurationException()
        {
            // Arrange
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<NoAttributeRequest, string>>();
            var request = new NoAttributeRequest { Data = "test" };

            // Act & Assert
            await Assert.ThrowsAsync<TransactionConfigurationException>(
                async () => await behavior.HandleAsync(
                    request,
                    () => new ValueTask<string>("success"),
                    CancellationToken.None));
        }

        [Fact]
        public async Task HandleAsync_WithReadOnlyTransaction_SetsReadOnlyMode()
        {
            // Arrange
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<ReadOnlyRequest, string>>();
            var request = new ReadOnlyRequest { Data = "test" };
            var expectedResponse = "success";

            var mockTransaction = new Mock<IRelayDbTransaction>();
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns("test-transaction-id");

            _transactionCoordinatorMock.Setup(x => x.BeginTransactionAsync(
                    It.IsAny<ITransactionConfiguration>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((mockTransaction.Object, mockContext.Object, null));

            _nestedTransactionManagerMock.Setup(x => x.IsTransactionActive()).Returns(false);

            // Act
            var response = await behavior.HandleAsync(
                request,
                () => new ValueTask<string>(expectedResponse),
                CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);
            Assert.True(_unitOfWorkMock.Object.IsReadOnly);
        }

        [Transaction(IsolationLevel.ReadCommitted)]
        private class TestRequest
        {
            public string Data { get; set; } = "";
        }

        [Transaction(IsolationLevel.ReadCommitted)]
        private class OutermostRequest : ITransactionalRequest
        {
            public string Data { get; set; } = "";
        }

        [Transaction(IsolationLevel.ReadCommitted)]
        private class NestedRequest : ITransactionalRequest
        {
            public string Data { get; set; } = "";
        }

        [Transaction(IsolationLevel.ReadCommitted, UseDistributedTransaction = true)]
        private class DistributedRequest : ITransactionalRequest
        {
            public string Data { get; set; } = "";
        }

        [Transaction(IsolationLevel.ReadCommitted)]
        private class RetryRequest : ITransactionalRequest
        {
            public string Data { get; set; } = "";
        }

        [Transaction(IsolationLevel.Unspecified)]
        private class UnspecifiedRequest : ITransactionalRequest
        {
            public string Data { get; set; } = "";
        }

        private class NoAttributeRequest
        {
            public string Data { get; set; } = "";
        }

        [Transaction(IsolationLevel.ReadCommitted, IsReadOnly = true)]
        private class ReadOnlyRequest : ITransactionalRequest
        {
            public string Data { get; set; } = "";
        }
    }
}