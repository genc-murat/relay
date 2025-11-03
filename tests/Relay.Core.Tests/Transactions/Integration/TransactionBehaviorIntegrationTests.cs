using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Extensions;
using Relay.Core.Transactions;
using Relay.Core.Transactions.Factories;
using Relay.Core.Transactions.Strategies;
using Relay.Core.Transactions.Template;
using Xunit;

namespace Relay.Core.Tests.Transactions.Integration
{
    public class TransactionBehaviorIntegrationTests
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly TransactionCoordinator _transactionCoordinator;
        private readonly NestedTransactionManager _nestedTransactionManager;
        private readonly DistributedTransactionCoordinator _distributedTransactionCoordinator;
        private readonly TransactionEventPublisher _transactionEventPublisher;
        private readonly TransactionRetryHandler _transactionRetryHandler;
        private readonly TransactionLogger _transactionLogger;

        public TransactionBehaviorIntegrationTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _transactionCoordinator = new TransactionCoordinator(_unitOfWorkMock.Object, NullLogger<TransactionCoordinator>.Instance);
            _nestedTransactionManager = new NestedTransactionManager(NullLogger<NestedTransactionManager>.Instance);
            _distributedTransactionCoordinator = new DistributedTransactionCoordinator(NullLogger<DistributedTransactionCoordinator>.Instance);
            _transactionEventPublisher = new TransactionEventPublisher(new List<ITransactionEventHandler>(), NullLogger<TransactionEventPublisher>.Instance);
            _transactionRetryHandler = new TransactionRetryHandler(NullLogger<TransactionRetryHandler>.Instance);
            _transactionLogger = new TransactionLogger(NullLogger.Instance);

            var services = new ServiceCollection();
            
            // Register all required services for refactored TransactionBehavior
            services.AddSingleton(NullLogger<TransactionBehavior<TestRequest, string>>.Instance);
            services.AddSingleton(NullLogger<TransactionBehavior<OutermostRequest, string>>.Instance);
            services.AddSingleton(NullLogger<TransactionBehavior<NestedRequest, string>>.Instance);
            services.AddSingleton(NullLogger<TransactionBehavior<DistributedRequest, string>>.Instance);
            services.AddSingleton(NullLogger<TransactionBehavior<RetryRequest, string>>.Instance);
            services.AddSingleton(NullLogger<TransactionBehavior<UnspecifiedRequest, string>>.Instance);
            services.AddSingleton(NullLogger<TransactionBehavior<NoAttributeRequest, string>>.Instance);
            services.AddSingleton(NullLogger<TransactionBehavior<ReadOnlyRequest, string>>.Instance);
            services.AddSingleton(NullLogger<TransactionBehavior<NonTransactionalRequest, string>>.Instance);
            
            // Add the logger for DefaultTransactionExecutionTemplate
            services.AddSingleton<ILogger<DefaultTransactionExecutionTemplate>>(NullLogger<DefaultTransactionExecutionTemplate>.Instance);
            
            // Add generic logging factory
            services.AddLogging();
            services.AddSingleton(_unitOfWorkMock.Object);
            
            // Register TransactionConfigurationResolver directly with required dependencies
            services.AddOptions();
            services.Configure<TransactionOptions>(options => { });
            services.AddSingleton<ITransactionConfigurationResolver, TransactionConfigurationResolver>();
            services.AddSingleton<TransactionConfigurationResolver>();
            
            // Register TransactionCoordinator as both concrete type and interface
            services.AddSingleton<ITransactionCoordinator>(_transactionCoordinator);
            services.AddSingleton(_transactionCoordinator);
            services.AddSingleton<ITransactionEventPublisher>(_transactionEventPublisher);
            services.AddSingleton(_transactionEventPublisher);
            services.AddSingleton<ITransactionRetryHandler>(_transactionRetryHandler);
            services.AddSingleton(_transactionRetryHandler);
            var metricsCollector = Mock.Of<TransactionMetricsCollector>();
            services.AddSingleton<ITransactionMetricsCollector>(metricsCollector);
            services.AddSingleton(metricsCollector);
            services.AddSingleton<INestedTransactionManager>(_nestedTransactionManager);
            services.AddSingleton(_nestedTransactionManager);
            services.AddSingleton(_distributedTransactionCoordinator);
            services.AddSingleton(Mock.Of<TransactionActivitySource>());
            services.AddSingleton(_transactionLogger);
            services.AddSingleton<ITransactionEventContextFactory, TransactionEventContextFactory>();
            
            // Register strategies
            services.AddSingleton<NestedTransactionStrategy>();
            services.AddSingleton<OutermostTransactionStrategy>();
            services.AddSingleton<DistributedTransactionStrategy>();
            
            // Register factories
            services.AddSingleton<ITransactionStrategyFactory, TransactionStrategyFactory>();
            
            // Register template
            services.AddSingleton<TransactionExecutionTemplate, DefaultTransactionExecutionTemplate>();
            services.AddSingleton<DefaultTransactionExecutionTemplate>();
            
            // Register TransactionBehavior for all test request types
            services.AddSingleton<TransactionBehavior<TestRequest, string>>();
            services.AddSingleton<TransactionBehavior<OutermostRequest, string>>();
            services.AddSingleton<TransactionBehavior<NestedRequest, string>>();
            services.AddSingleton<TransactionBehavior<DistributedRequest, string>>();
            services.AddSingleton<TransactionBehavior<RetryRequest, string>>();
            services.AddSingleton<TransactionBehavior<UnspecifiedRequest, string>>();
            services.AddSingleton<TransactionBehavior<NoAttributeRequest, string>>();
            services.AddSingleton<TransactionBehavior<ReadOnlyRequest, string>>();
            services.AddSingleton<TransactionBehavior<NonTransactionalRequest, string>>();

            _serviceProvider = services.BuildServiceProvider();
        }

        private void SetupUnitOfWorkForTransaction(bool isReadOnly = false)
        {
            // Set up the UnitOfWork mock to return a proper transaction context
            var mockTransaction = new Mock<IRelayDbTransaction>();
            mockTransaction.Setup(x => x.IsolationLevel).Returns(IsolationLevel.ReadCommitted);
            
            var transactionContext = new TransactionContext(mockTransaction.Object, IsolationLevel.ReadCommitted, isReadOnly);
            
            // Set up CurrentTransactionContext to return context
            _unitOfWorkMock.Setup(x => x.CurrentTransactionContext).Returns(transactionContext);
            
            // Set up BeginTransactionAsync to create transaction
            _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockTransaction.Object);
            
            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            
            // Set IsReadOnly property - this should be set by IUnitOfWork implementation
            // when it detects read-only transaction context
            _unitOfWorkMock.Setup(x => x.IsReadOnly).Returns(isReadOnly);
        }

        [Fact]
        public async Task HandleAsync_WithNonTransactionalRequest_ExecutesWithoutTransaction()
        {
            // Arrange
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<NonTransactionalRequest, string>>();
            var request = new NonTransactionalRequest { Data = "test" };
            var expectedResponse = "success";

            // Act
            var response = await behavior.HandleAsync(
                request,
                () => new ValueTask<string>(expectedResponse),
                CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);
            
            // For non-transactional requests, SaveChangesAsync should not be called
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithOutermostTransaction_UsesOutermostStrategy()
        {
            // Arrange
            SetupUnitOfWorkForTransaction();
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<OutermostRequest, string>>();
            var request = new OutermostRequest { Data = "test" };
            var expectedResponse = "success";

            // Act
            var response = await behavior.HandleAsync(
                request,
                () => new ValueTask<string>(expectedResponse),
                CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            // Verify that a transaction was created and committed
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithNestedTransaction_UsesNestedStrategy()
        {
            // Arrange
            SetupUnitOfWorkForTransaction();
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<NestedRequest, string>>();
            var request = new NestedRequest { Data = "test" };
            var expectedResponse = "success";

            // Act
            var response = await behavior.HandleAsync(
                request,
                () => new ValueTask<string>(expectedResponse),
                CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            // Verify that the operation completed successfully
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithDistributedTransaction_UsesDistributedStrategy()
        {
            // Arrange
            SetupUnitOfWorkForTransaction();
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<DistributedRequest, string>>();
            var request = new DistributedRequest { Data = "test" };
            var expectedResponse = "success";

            // Act
            var response = await behavior.HandleAsync(
                request,
                () => new ValueTask<string>(expectedResponse),
                CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            // Verify that the operation completed successfully
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WithRetryPolicy_ExecutesWithRetry()
        {
            // Arrange
            SetupUnitOfWorkForTransaction();
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<RetryRequest, string>>();
            var request = new RetryRequest { Data = "test" };
            var expectedResponse = "success";

            // Act
            var response = await behavior.HandleAsync(
                request,
                () => new ValueTask<string>(expectedResponse),
                CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);

            // Verify that the operation completed successfully
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenTransactionFails_RollsBackCorrectly()
        {
            // Arrange
            SetupUnitOfWorkForTransaction();
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<OutermostRequest, string>>();
            var request = new OutermostRequest { Data = "test" };
            var expectedException = new InvalidOperationException("Test failure");

            // Act & Assert
            var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await behavior.HandleAsync(
                    request,
                    () => throw expectedException,
                    CancellationToken.None));

            Assert.Equal(expectedException, actualException);

            // Verify that SaveChangesAsync was not called due to the exception
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task HandleAsync_WithUnspecifiedIsolationLevel_ThrowsConfigurationException()
        {
            // Arrange
            var behavior = _serviceProvider.GetRequiredService<TransactionBehavior<UnspecifiedRequest, string>>();
            var request = new UnspecifiedRequest { Data = "test" };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await behavior.HandleAsync(
                    request,
                    () => new ValueTask<string>("success"),
                    CancellationToken.None));

            Assert.Contains("cannot be Unspecified", exception.Message);
        }

        [Fact]
        public async Task HandleAsync_WithMissingTransactionAttribute_ThrowsConfigurationException()
        {
            // Arrange - Create a separate service provider with explicit options
            var services = new ServiceCollection();
            services.AddSingleton(NullLogger<TransactionBehavior<NoAttributeRequest, string>>.Instance);
            
            // Create a fresh unit of work mock with no setup
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            services.AddSingleton(unitOfWorkMock.Object);
            
            // Create options with RequireExplicitTransactionAttribute = true
            var options = new TransactionOptions { RequireExplicitTransactionAttribute = true };
            services.AddOptions();
            services.Configure<TransactionOptions>(opt =>
            {
                opt.RequireExplicitTransactionAttribute = options.RequireExplicitTransactionAttribute;
            });
            ServiceRegistrationHelper.TryAddTransient<ITransactionConfigurationResolver, TransactionConfigurationResolver>(services);
            
            // Register services with interfaces as well as concrete types to ensure DI can resolve all dependencies
            services.AddSingleton<ITransactionCoordinator>(_transactionCoordinator);
            services.AddSingleton(_transactionCoordinator);
            services.AddSingleton<ITransactionEventPublisher>(_transactionEventPublisher);
            services.AddSingleton(_transactionEventPublisher);
            services.AddSingleton<ITransactionRetryHandler>(_transactionRetryHandler);
            services.AddSingleton(_transactionRetryHandler);
            var metricsCollector = Mock.Of<TransactionMetricsCollector>();
            services.AddSingleton<ITransactionMetricsCollector>(metricsCollector);
            services.AddSingleton(metricsCollector);
            services.AddSingleton<INestedTransactionManager>(_nestedTransactionManager);
            services.AddSingleton(_nestedTransactionManager);
            services.AddSingleton(_distributedTransactionCoordinator);
            services.AddSingleton(Mock.Of<TransactionActivitySource>());
            services.AddSingleton(_transactionLogger);
            services.AddSingleton<ITransactionEventContextFactory, TransactionEventContextFactory>();
            
            // Register strategies
            services.AddSingleton<NestedTransactionStrategy>();
            services.AddSingleton<OutermostTransactionStrategy>();
            services.AddSingleton<DistributedTransactionStrategy>();
            
            // Register factories
            services.AddSingleton<ITransactionStrategyFactory, TransactionStrategyFactory>();
            
            // Register template
            services.AddSingleton<TransactionExecutionTemplate, DefaultTransactionExecutionTemplate>();
            services.AddSingleton<DefaultTransactionExecutionTemplate>();
            
            // Register TransactionBehavior
            services.AddSingleton<TransactionBehavior<NoAttributeRequest, string>>();
            services.AddLogging();
            
            var serviceProvider = services.BuildServiceProvider();
            var behavior = serviceProvider.GetRequiredService<TransactionBehavior<NoAttributeRequest, string>>();
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
            var mockTransaction = new Mock<IRelayDbTransaction>();
            mockTransaction.Setup(x => x.IsolationLevel).Returns(IsolationLevel.ReadCommitted);
            
            var transactionContext = new TransactionContext(mockTransaction.Object, IsolationLevel.ReadCommitted, true);
            
            var testUnitOfWork = new TestUnitOfWork
            {
                IsReadOnly = true,
                CurrentTransactionContext = transactionContext
            };
            
            var services = new ServiceCollection();
            services.AddSingleton(NullLogger<TransactionBehavior<ReadOnlyRequest, string>>.Instance);
            services.AddSingleton<IUnitOfWork>(testUnitOfWork);
            services.AddOptions();
            services.Configure<TransactionOptions>(options => { });
            ServiceRegistrationHelper.TryAddTransient<ITransactionConfigurationResolver, TransactionConfigurationResolver>(services);
            
            var transactionCoordinator = new TransactionCoordinator(testUnitOfWork, NullLogger<TransactionCoordinator>.Instance);
            services.AddSingleton<ITransactionCoordinator>(transactionCoordinator);
            services.AddSingleton(transactionCoordinator);
            
            services.AddSingleton<ITransactionEventPublisher>(_transactionEventPublisher);
            services.AddSingleton(_transactionEventPublisher);
            services.AddSingleton<ITransactionRetryHandler>(_transactionRetryHandler);
            services.AddSingleton(_transactionRetryHandler);
            var metricsCollector = Mock.Of<TransactionMetricsCollector>();
            services.AddSingleton<ITransactionMetricsCollector>(metricsCollector);
            services.AddSingleton(metricsCollector);
            services.AddSingleton<INestedTransactionManager>(_nestedTransactionManager);
            services.AddSingleton(_nestedTransactionManager);
            services.AddSingleton(_distributedTransactionCoordinator);
            services.AddSingleton(Mock.Of<TransactionActivitySource>());
            services.AddSingleton(_transactionLogger);
            services.AddSingleton<ITransactionEventContextFactory, TransactionEventContextFactory>();
            
            // Register strategies
            services.AddSingleton<NestedTransactionStrategy>();
            services.AddSingleton<OutermostTransactionStrategy>(sp => 
                new OutermostTransactionStrategy(
                    testUnitOfWork,
                    NullLogger<OutermostTransactionStrategy>.Instance,
                    sp.GetRequiredService<ITransactionCoordinator>(),
                    sp.GetRequiredService<ITransactionEventPublisher>(),
                    sp.GetRequiredService<ITransactionMetricsCollector>(),
                    sp.GetRequiredService<INestedTransactionManager>(),
                    sp.GetRequiredService<TransactionActivitySource>(),
                    sp.GetRequiredService<TransactionLogger>(),
                    sp.GetRequiredService<ITransactionEventContextFactory>()));
            services.AddSingleton<DistributedTransactionStrategy>();
            
            // Register factories
            services.AddSingleton<ITransactionStrategyFactory, TransactionStrategyFactory>();
            
            // Register template
            services.AddSingleton<TransactionExecutionTemplate, DefaultTransactionExecutionTemplate>();
            services.AddSingleton<DefaultTransactionExecutionTemplate>();
            
            // Register TransactionBehavior
            services.AddSingleton<TransactionBehavior<ReadOnlyRequest, string>>();
            services.AddLogging();
            
            var serviceProvider = services.BuildServiceProvider();
            var behavior = serviceProvider.GetRequiredService<TransactionBehavior<ReadOnlyRequest, string>>();
            var request = new ReadOnlyRequest { Data = "test" };
            var expectedResponse = "success";

            // Act
            var response = await behavior.HandleAsync(
                request,
                () => new ValueTask<string>(expectedResponse),
                CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, response);
            Assert.True(testUnitOfWork.IsReadOnly);
        }

        [Transaction(IsolationLevel.ReadCommitted)]
        private class TestRequest
        {
            public string Data { get; set; } = "";
        }

        private class NonTransactionalRequest
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

        private class NoAttributeRequest : ITransactionalRequest
        {
            public string Data { get; set; } = "";
        }

        [Transaction(IsolationLevel.ReadCommitted, IsReadOnly = true)]
        private class ReadOnlyRequest : ITransactionalRequest
        {
            public string Data { get; set; } = "";
        }

        private class TestUnitOfWork : IUnitOfWork
        {
            public bool IsReadOnly { get; set; }
            public ITransactionContext? CurrentTransactionContext { get; set; }

            public Task<IRelayDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
            {
                var mockTransaction = new Mock<IRelayDbTransaction>();
                mockTransaction.Setup(x => x.IsolationLevel).Returns(isolationLevel);
                return Task.FromResult(mockTransaction.Object);
            }

            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(1);
            }

            public Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }

            public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }
    }
}