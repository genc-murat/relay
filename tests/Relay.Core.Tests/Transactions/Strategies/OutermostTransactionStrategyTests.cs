using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Transactions;
using Relay.Core.Transactions.Factories;
using Relay.Core.Transactions.Strategies;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Transactions.Strategies;

public class OutermostTransactionStrategyTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITransactionCoordinator> _transactionCoordinatorMock;
    private readonly Mock<ITransactionEventPublisher> _eventPublisherMock;
    private readonly Mock<ITransactionMetricsCollector> _metricsCollectorMock;
    private readonly Mock<INestedTransactionManager> _nestedTransactionManagerMock;
    private readonly Mock<TransactionActivitySource> _activitySourceMock;
    private readonly TransactionLogger _transactionLogger;
    private readonly Mock<ITransactionEventContextFactory> _eventContextFactoryMock;
    private readonly OutermostTransactionStrategy _strategy;

    public OutermostTransactionStrategyTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.SetupProperty(x => x.IsReadOnly, false); // Mock the property with initial value
        _transactionCoordinatorMock = new Mock<ITransactionCoordinator>();
        _eventPublisherMock = new Mock<ITransactionEventPublisher>();
        _metricsCollectorMock = new Mock<ITransactionMetricsCollector>();
        _nestedTransactionManagerMock = new Mock<INestedTransactionManager>();
        _activitySourceMock = new Mock<TransactionActivitySource>();
        _transactionLogger = new TransactionLogger(NullLogger.Instance);
        _eventContextFactoryMock = new Mock<ITransactionEventContextFactory>();

        _strategy = new OutermostTransactionStrategy(
            _unitOfWorkMock.Object,
            NullLogger<OutermostTransactionStrategy>.Instance,
            _transactionCoordinatorMock.Object,
            _eventPublisherMock.Object,
            _metricsCollectorMock.Object,
            _nestedTransactionManagerMock.Object,
            _activitySourceMock.Object,
            _transactionLogger,
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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null,
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<string>> op, ITransactionContext ctx, CancellationTokenSource? timeoutCts, TimeSpan timeout, string reqType, CancellationToken ct) => op(ct));

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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null, // timeoutCts
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("success");

        _nestedTransactionManagerMock.Setup(x => x.ShouldCommitTransaction(mockContext.Object))
            .Returns(true);

        var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

        // Act
        await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

        // Assert
        _unitOfWorkMock.VerifySet(x => x.IsReadOnly = true, Times.Once);
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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null, // timeoutCts
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null, // timeoutCts
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<string>> op, ITransactionContext ctx, CancellationTokenSource? timeoutCts, TimeSpan timeout, string reqType, CancellationToken ct) => op(ct));

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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null, // timeoutCts
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<string>> op, ITransactionContext ctx, CancellationTokenSource? timeoutCts, TimeSpan timeout, string reqType, CancellationToken ct) => op(ct));

        _nestedTransactionManagerMock.Setup(x => x.ShouldCommitTransaction(mockContext.Object))
            .Returns(true);

        var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

        // Act
        await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

        // Assert
        _metricsCollectorMock.Verify(x => x.RecordTransactionSuccess(
            configuration.IsolationLevel, requestType, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimeout_HandlesTimeoutException()
    {
        // Arrange
        var request = new TestTransactionalRequest();
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            TimeSpan.FromSeconds(1));
        var requestType = "TestTransactionalRequest";
        var timeoutException = new TransactionTimeoutException("tx-123", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));

        var mockTransaction = new Mock<IRelayDbTransaction>();
        var mockContext = new Mock<ITransactionContext>();
        mockContext.Setup(x => x.TransactionId).Returns("tx-123");

        var eventContext = new TransactionEventContext
        {
            TransactionId = "tx-123",
            RequestType = requestType,
            IsolationLevel = configuration.IsolationLevel
        };

        _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
            .Returns(eventContext);

        _transactionCoordinatorMock.Setup(x => x.BeginTransactionAsync(
                configuration, requestType, It.IsAny<CancellationToken>()))
            .ReturnsAsync((mockTransaction.Object, mockContext.Object, new CancellationTokenSource()));

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                It.IsAny<CancellationTokenSource>(),
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(timeoutException);

        _nestedTransactionManagerMock.Setup(x => x.ShouldRollbackTransaction(mockContext.Object))
            .Returns(true);

        var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<TransactionTimeoutException>(
            async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

        Assert.Equal(timeoutException, actualException);

        _metricsCollectorMock.Verify(x => x.RecordTransactionTimeout(requestType, It.IsAny<TimeSpan>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishBeforeRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishAfterRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var request = new TestTransactionalRequest();
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            TimeSpan.FromMinutes(1));
        var requestType = "TestTransactionalRequest";
        using var cts = new CancellationTokenSource();

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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null,
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<string>> op, ITransactionContext ctx, CancellationTokenSource? timeoutCts, TimeSpan timeout, string reqType, CancellationToken ct) => op(ct));

        _nestedTransactionManagerMock.Setup(x => x.ShouldCommitTransaction(mockContext.Object))
            .Returns(true);

        var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

        // Act
        await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, cts.Token);

        // Assert
        _transactionCoordinatorMock.Verify(x => x.BeginTransactionAsync(
            configuration, requestType, cts.Token), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNestedTransactionManagerPreventsCommit_DoesNotCommit()
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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null,
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<string>> op, ITransactionContext ctx, CancellationTokenSource? timeoutCts, TimeSpan timeout, string reqType, CancellationToken ct) => op(ct));

        _nestedTransactionManagerMock.Setup(x => x.ShouldCommitTransaction(mockContext.Object))
            .Returns(false); // Prevent commit

        var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

        // Act
        var response = await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

        // Assert
        Assert.Equal("success", response);

        _transactionCoordinatorMock.Verify(x => x.CommitTransactionAsync(
            It.IsAny<IRelayDbTransaction>(), It.IsAny<ITransactionContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        _eventPublisherMock.Verify(x => x.PublishBeforeCommitAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishAfterCommitAsync(eventContext, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNestedTransactionManagerPreventsRollback_DoesNotRollback()
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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null,
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        _nestedTransactionManagerMock.Setup(x => x.ShouldRollbackTransaction(mockContext.Object))
            .Returns(false); // Prevent rollback

        var nextDelegate = new RequestHandlerDelegate<string>(() => throw expectedException);

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

        Assert.Equal(expectedException, actualException);

        _transactionCoordinatorMock.Verify(x => x.RollbackTransactionAsync(
            It.IsAny<IRelayDbTransaction>(), It.IsAny<ITransactionContext>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);

        _eventPublisherMock.Verify(x => x.PublishBeforeRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishAfterRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RestoresOriginalReadOnlyState()
    {
        // Arrange
        var request = new TestTransactionalRequest();
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            TimeSpan.FromMinutes(1),
            isReadOnly: true);
        var requestType = "TestTransactionalRequest";

        _unitOfWorkMock.SetupProperty(x => x.IsReadOnly, false); // Start with false
        var originalReadOnlyState = _unitOfWorkMock.Object.IsReadOnly;

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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null,
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("success");

        _nestedTransactionManagerMock.Setup(x => x.ShouldCommitTransaction(mockContext.Object))
            .Returns(true);

        var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

        // Act
        await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

        // Assert
        Assert.Equal(originalReadOnlyState, _unitOfWorkMock.Object.IsReadOnly);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullTransaction_DisposesGracefully()
    {
        // Arrange
        var request = new TestTransactionalRequest();
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            TimeSpan.FromMinutes(1));
        var requestType = "TestTransactionalRequest";

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
            .ReturnsAsync((null, mockContext.Object, null)); // Null transaction

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null,
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("success");

        _nestedTransactionManagerMock.Setup(x => x.ShouldCommitTransaction(mockContext.Object))
            .Returns(true);

        var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

        // Act & Assert - Should not throw
        var response = await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

        Assert.Equal("success", response);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsActivityOnSuccess()
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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null,
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<string>> op, ITransactionContext ctx, CancellationTokenSource? timeoutCts, TimeSpan timeout, string reqType, CancellationToken ct) => op(ct));

        _nestedTransactionManagerMock.Setup(x => x.ShouldCommitTransaction(mockContext.Object))
            .Returns(true);

        var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

        // Act
        await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

        // Assert
        _activitySourceMock.Verify(x => x.StartTransactionActivity(requestType, configuration), Times.Once);
        _activitySourceMock.Verify(x => x.RecordTransactionSuccess(
            It.IsAny<Activity>(), mockContext.Object, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsActivityOnFailure()
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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null,
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        _nestedTransactionManagerMock.Setup(x => x.ShouldRollbackTransaction(mockContext.Object))
            .Returns(true);

        var nextDelegate = new RequestHandlerDelegate<string>(() => throw expectedException);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

        // Assert
        _activitySourceMock.Verify(x => x.RecordTransactionFailure(
            It.IsAny<Activity>(), mockContext.Object, expectedException), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsActivityOnTimeout()
    {
        // Arrange
        var request = new TestTransactionalRequest();
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            TimeSpan.FromSeconds(1));
        var requestType = "TestTransactionalRequest";
        var timeoutException = new TransactionTimeoutException("tx-123", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));

        var mockTransaction = new Mock<IRelayDbTransaction>();
        var mockContext = new Mock<ITransactionContext>();
        mockContext.Setup(x => x.TransactionId).Returns("tx-123");

        var eventContext = new TransactionEventContext
        {
            TransactionId = "tx-123",
            RequestType = requestType,
            IsolationLevel = configuration.IsolationLevel
        };

        _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
            .Returns(eventContext);

        _transactionCoordinatorMock.Setup(x => x.BeginTransactionAsync(
                configuration, requestType, It.IsAny<CancellationToken>()))
            .ReturnsAsync((mockTransaction.Object, mockContext.Object, new CancellationTokenSource()));

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                It.IsAny<CancellationTokenSource>(),
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(timeoutException);

        _nestedTransactionManagerMock.Setup(x => x.ShouldRollbackTransaction(mockContext.Object))
            .Returns(true);

        var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

        // Act
        await Assert.ThrowsAsync<TransactionTimeoutException>(
            async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

        // Assert
        _activitySourceMock.Verify(x => x.RecordTransactionTimeout(
            It.IsAny<Activity>(), mockContext.Object, timeoutException), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithDifferentIsolationLevels_RecordsCorrectMetrics()
    {
        // Arrange
        var isolationLevels = new[]
        {
            IsolationLevel.ReadUncommitted,
            IsolationLevel.ReadCommitted,
            IsolationLevel.RepeatableRead,
            IsolationLevel.Serializable,
            IsolationLevel.Snapshot
        };

        foreach (var isolationLevel in isolationLevels)
        {
            // Arrange
            var request = new TestTransactionalRequest();
            var configuration = new TransactionConfiguration(
                isolationLevel,
                TimeSpan.FromMinutes(1));
            var requestType = $"TestRequest_{isolationLevel}";

            var mockTransaction = new Mock<IRelayDbTransaction>();
            var mockContext = new Mock<ITransactionContext>();
            mockContext.Setup(x => x.TransactionId).Returns($"test-transaction-id-{isolationLevel}");

            var eventContext = new TransactionEventContext
            {
                TransactionId = $"test-transaction-id-{isolationLevel}",
                RequestType = requestType,
                IsolationLevel = configuration.IsolationLevel
            };

            _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
                .Returns(eventContext);

            _transactionCoordinatorMock.Setup(x => x.BeginTransactionAsync(
                    configuration, requestType, It.IsAny<CancellationToken>()))
                .ReturnsAsync((mockTransaction.Object, mockContext.Object, null));

            _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                    It.IsAny<Func<CancellationToken, Task<string>>>(),
                    mockContext.Object,
                    null,
                    configuration.Timeout,
                    requestType,
                    It.IsAny<CancellationToken>()))
                .Returns((Func<CancellationToken, Task<string>> op, ITransactionContext ctx, CancellationTokenSource? timeoutCts, TimeSpan timeout, string reqType, CancellationToken ct) => op(ct));

            _nestedTransactionManagerMock.Setup(x => x.ShouldCommitTransaction(mockContext.Object))
                .Returns(true);

            var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

            // Act
            await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None);

            // Assert
            _metricsCollectorMock.Verify(x => x.RecordTransactionSuccess(
                isolationLevel, requestType, It.IsAny<TimeSpan>()), Times.Once);
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenEventPublisherThrowsInBeforeBegin_PropagatesException()
    {
        // Arrange
        var request = new TestTransactionalRequest();
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            TimeSpan.FromMinutes(1));
        var requestType = "TestTransactionalRequest";
        var eventException = new InvalidOperationException("Event publisher failed");

        var eventContext = new TransactionEventContext
        {
            TransactionId = "test-transaction-id",
            RequestType = requestType,
            IsolationLevel = configuration.IsolationLevel
        };

        _eventContextFactoryMock.Setup(x => x.CreateEventContext(requestType, configuration))
            .Returns(eventContext);

        _eventPublisherMock.Setup(x => x.PublishBeforeBeginAsync(eventContext, It.IsAny<CancellationToken>()))
            .ThrowsAsync(eventException);

        var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

        Assert.Equal(eventException, actualException);

        // Verify that transaction cleanup still happens
        _unitOfWorkMock.VerifySet(x => x.IsReadOnly = It.IsAny<bool>(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEventPublisherThrowsInAfterBegin_PropagatesException()
    {
        // Arrange
        var request = new TestTransactionalRequest();
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            TimeSpan.FromMinutes(1));
        var requestType = "TestTransactionalRequest";
        var eventException = new InvalidOperationException("Event publisher failed");

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

        _eventPublisherMock.Setup(x => x.PublishAfterBeginAsync(eventContext, It.IsAny<CancellationToken>()))
            .ThrowsAsync(eventException);

        var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

        Assert.Equal(eventException, actualException);

        // Verify rollback occurs
        _eventPublisherMock.Verify(x => x.PublishBeforeRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
        _eventPublisherMock.Verify(x => x.PublishAfterRollbackAsync(eventContext, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEventPublisherThrowsInAfterCommit_PropagatesException()
    {
        // Arrange
        var request = new TestTransactionalRequest();
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            TimeSpan.FromMinutes(1));
        var requestType = "TestTransactionalRequest";
        var eventException = new InvalidOperationException("Event publisher failed");

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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null,
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<string>> op, ITransactionContext ctx, CancellationTokenSource? timeoutCts, TimeSpan timeout, string reqType, CancellationToken ct) => op(ct));

        _nestedTransactionManagerMock.Setup(x => x.ShouldCommitTransaction(mockContext.Object))
            .Returns(true);

        _eventPublisherMock.Setup(x => x.PublishAfterCommitAsync(eventContext, It.IsAny<CancellationToken>()))
            .ThrowsAsync(eventException);

        var nextDelegate = new RequestHandlerDelegate<string>(() => new ValueTask<string>("success"));

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

        Assert.Equal(eventException, actualException);

        // Verify commit still happened
        _transactionCoordinatorMock.Verify(x => x.CommitTransactionAsync(
            mockTransaction.Object, mockContext.Object, requestType, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEventPublisherThrowsInRollback_HandlesGracefully()
    {
        // Arrange
        var request = new TestTransactionalRequest();
        var configuration = new TransactionConfiguration(
            IsolationLevel.ReadCommitted,
            TimeSpan.FromMinutes(1));
        var requestType = "TestTransactionalRequest";
        var handlerException = new InvalidOperationException("Handler failed");
        var rollbackException = new InvalidOperationException("Rollback event failed");

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

        _transactionCoordinatorMock.Setup(x => x.ExecuteWithTimeoutAsync(
                It.IsAny<Func<CancellationToken, Task<string>>>(),
                mockContext.Object,
                null,
                configuration.Timeout,
                requestType,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(handlerException);

        _nestedTransactionManagerMock.Setup(x => x.ShouldRollbackTransaction(mockContext.Object))
            .Returns(true);

        _eventPublisherMock.Setup(x => x.PublishAfterRollbackAsync(eventContext, It.IsAny<CancellationToken>()))
            .ThrowsAsync(rollbackException);

        var nextDelegate = new RequestHandlerDelegate<string>(() => throw handlerException);

        // Act & Assert - Should throw the rollback event exception when it fails during rollback
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _strategy.ExecuteAsync(request, nextDelegate, configuration, requestType, CancellationToken.None));

        Assert.Equal(rollbackException, actualException);

        // Verify rollback was attempted
        _transactionCoordinatorMock.Verify(x => x.RollbackTransactionAsync(
            mockTransaction.Object, mockContext.Object, requestType, handlerException, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Transaction(IsolationLevel.ReadCommitted)]
    private class TestTransactionalRequest : ITransactionalRequest
    {
        public string Data { get; set; } = "test";
    }
}