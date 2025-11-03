using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Transactions;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Transactions;

/// <summary>
/// Tests for TransactionCoordinator.
/// </summary>
public class TransactionCoordinatorTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<TransactionCoordinator>> _loggerMock;
    private readonly Mock<IRelayDbTransaction> _transactionMock;
    private readonly Mock<ITransactionContext> _contextMock;
    private readonly TransactionCoordinator _coordinator;

    public TransactionCoordinatorTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<TransactionCoordinator>>();
        _transactionMock = new Mock<IRelayDbTransaction>();
        _contextMock = new Mock<ITransactionContext>();
        _coordinator = new TransactionCoordinator(_unitOfWorkMock.Object, _loggerMock.Object);

        // Setup default behavior
        _unitOfWorkMock.Setup(uow => uow.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);
        _unitOfWorkMock.Setup(uow => uow.CurrentTransactionContext)
            .Returns(_contextMock.Object);
        _contextMock.Setup(ctx => ctx.TransactionId)
            .Returns("test-tx-123");
        _contextMock.Setup(ctx => ctx.StartedAt)
            .Returns(DateTime.UtcNow);
        _contextMock.Setup(ctx => ctx.NestingLevel)
            .Returns(1);
    }

    #region BeginTransactionAsync Tests

    [Fact]
    public async Task BeginTransactionAsync_WithValidConfiguration_ShouldReturnTransactionAndContext()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.BeginTransactionAsync(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotNull(result.Transaction);
        Assert.NotNull(result.Context);
        Assert.NotNull(result.TimeoutCts);

        // Verify method calls
        _unitOfWorkMock.Verify(uow => uow.BeginTransactionAsync(configuration.IsolationLevel, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(uow => uow.CurrentTransactionContext, Times.Once);

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Beginning transaction") &&
                    v.ToString().Contains(requestType) &&
                    v.ToString().Contains(configuration.IsolationLevel.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("started successfully") &&
                    v.ToString().Contains("nesting level")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _coordinator.BeginTransactionAsync(null, requestType, cancellationToken));
    }

    [Fact]
    public async Task BeginTransactionAsync_WithTimeout_ShouldCreateTimeoutCts()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.BeginTransactionAsync(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotNull(result.TimeoutCts);
        Assert.False(result.TimeoutCts.IsCancellationRequested);

        // Verify timeout logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("configured with timeout") &&
                    v.ToString().Contains(configuration.Timeout.TotalSeconds.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithoutTimeout_ShouldNotCreateTimeoutCts()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.Zero,
            IsReadOnly = false
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.BeginTransactionAsync(configuration, requestType, cancellationToken);

        // Assert
        Assert.Null(result.TimeoutCts);

        // Verify no timeout logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("configured with no timeout")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithReadOnlyConfiguration_ShouldConfigureReadOnly()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = true
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.BeginTransactionAsync(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotNull(result.Transaction);

        // Verify read-only configuration logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Configuring transaction") &&
                    v.ToString().Contains("read-only")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Verify ReadOnlyTransactionEnforcer was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Beginning transaction") &&
                    v.ToString().Contains("(read-only)")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithNullContext_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        _unitOfWorkMock.Setup(uow => uow.CurrentTransactionContext)
            .Returns((ITransactionContext)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _coordinator.BeginTransactionAsync(configuration, requestType, cancellationToken));

        Assert.Contains("Transaction context was not created", exception.Message);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithTimeout_ShouldThrowTransactionTimeoutException()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromMilliseconds(100),
            IsReadOnly = false
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        _unitOfWorkMock.Setup(uow => uow.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .Returns(async (IsolationLevel isolationLevel, CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct); // Delay longer than timeout
                return _transactionMock.Object;
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TransactionTimeoutException>(() =>
            _coordinator.BeginTransactionAsync(configuration, requestType, cancellationToken));

        Assert.NotNull(exception.TransactionId);
        Assert.Equal(configuration.Timeout, exception.Timeout);
        Assert.Equal(requestType, exception.RequestType);
        Assert.NotNull(exception.InnerException);

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("timed out") &&
                    v.ToString().Contains(requestType)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithGeneralException_ShouldDisposeTimeoutCtsAndRethrow()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Database error");

        _unitOfWorkMock.Setup(uow => uow.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _coordinator.BeginTransactionAsync(configuration, requestType, cancellationToken));

        Assert.Equal(expectedException, exception);

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Failed to begin transaction") &&
                    v.ToString().Contains(requestType)),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(IsolationLevel.Unspecified)]
    [InlineData(IsolationLevel.Chaos)]
    [InlineData(IsolationLevel.ReadUncommitted)]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    [InlineData(IsolationLevel.Snapshot)]
    public async Task BeginTransactionAsync_WithDifferentIsolationLevels_ShouldWorkCorrectly(IsolationLevel isolationLevel)
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = isolationLevel,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.BeginTransactionAsync(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotNull(result.Transaction);
        Assert.NotNull(result.Context);

        _unitOfWorkMock.Verify(uow => uow.BeginTransactionAsync(isolationLevel, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-2)]
    public async Task BeginTransactionAsync_WithDisabledTimeout_ShouldNotCreateTimeoutCts(int timeoutSeconds)
    {
        // Arrange
        var timeout = timeoutSeconds switch
        {
            0 => TimeSpan.Zero,
            -1 => Timeout.InfiniteTimeSpan,
            -2 => TimeSpan.FromTicks(-1),
            _ => TimeSpan.FromSeconds(timeoutSeconds)
        };

        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = timeout,
            IsReadOnly = false
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.BeginTransactionAsync(configuration, requestType, cancellationToken);

        // Assert
        Assert.Null(result.TimeoutCts);

        // Verify no timeout logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("configured with no timeout")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    #endregion

    #region ExecuteWithTimeoutAsync Tests

    [Fact]
    public async Task ExecuteWithTimeoutAsync_WithValidOperation_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = "TestResult";
        var operation = new Func<CancellationToken, Task<string>>(_ => Task.FromResult(expectedResult));
        var timeoutCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromSeconds(30);
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.ExecuteWithTimeoutAsync(
            operation, _contextMock.Object, timeoutCts, timeout, requestType, cancellationToken);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        Func<CancellationToken, Task<string>> operation = null;
        var timeoutCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromSeconds(30);
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _coordinator.ExecuteWithTimeoutAsync(
                operation, _contextMock.Object, timeoutCts, timeout, requestType, cancellationToken));
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<string>>(_ => Task.FromResult("Result"));
        var timeoutCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromSeconds(30);
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _coordinator.ExecuteWithTimeoutAsync(
                operation, null, timeoutCts, timeout, requestType, cancellationToken));
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_WithTimeout_ShouldThrowTransactionTimeoutException()
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<string>>(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            return "Result";
        });
        var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var timeout = TimeSpan.FromMilliseconds(100);
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TransactionTimeoutException>(() =>
            _coordinator.ExecuteWithTimeoutAsync(
                operation, _contextMock.Object, timeoutCts, timeout, requestType, cancellationToken));

        Assert.Equal(_contextMock.Object.TransactionId, exception.TransactionId);
        Assert.Equal(timeout, exception.Timeout);
        Assert.Equal(requestType, exception.RequestType);
        Assert.NotNull(exception.InnerException);

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("timed out during execution") &&
                    v.ToString().Contains(_contextMock.Object.TransactionId) &&
                    v.ToString().Contains(requestType)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_WithoutTimeoutCts_ShouldUseOriginalCancellationToken()
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<string>>(_ => Task.FromResult("Result"));
        CancellationTokenSource? timeoutCts = null;
        var timeout = TimeSpan.FromSeconds(30);
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.ExecuteWithTimeoutAsync(
            operation, _contextMock.Object, timeoutCts, timeout, requestType, cancellationToken);

        // Assert
        Assert.Equal("Result", result);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_WithCancellation_ShouldPropagateCancellation()
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<string>>(ct =>
        {
            throw new OperationCanceledException();
        });
        var timeoutCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromSeconds(30);
        var requestType = "TestRequest";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _coordinator.ExecuteWithTimeoutAsync(
                operation, _contextMock.Object, timeoutCts, timeout, requestType, cts.Token));
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_WithGenericException_ShouldPropagateException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test error");
        var operation = new Func<CancellationToken, Task<string>>(_ => Task.FromException<string>(expectedException));
        var timeoutCts = new CancellationTokenSource();
        var timeout = TimeSpan.FromSeconds(30);
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _coordinator.ExecuteWithTimeoutAsync(
                operation, _contextMock.Object, timeoutCts, timeout, requestType, cancellationToken));

        Assert.Equal(expectedException, exception);
    }

    #endregion

    #region CommitTransactionAsync Tests

    [Fact]
    public async Task CommitTransactionAsync_WithValidParameters_ShouldCommitSuccessfully()
    {
        // Arrange
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        await _coordinator.CommitTransactionAsync(_transactionMock.Object, _contextMock.Object, requestType, cancellationToken);

        // Assert
        _transactionMock.Verify(tx => tx.CommitAsync(cancellationToken), Times.Once);

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Committing transaction") &&
                    v.ToString().Contains(_contextMock.Object.TransactionId) &&
                    v.ToString().Contains(requestType)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("committed successfully") &&
                    v.ToString().Contains(_contextMock.Object.TransactionId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task CommitTransactionAsync_WithNullTransaction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _coordinator.CommitTransactionAsync(null, _contextMock.Object, requestType, cancellationToken));
    }

    [Fact]
    public async Task CommitTransactionAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _coordinator.CommitTransactionAsync(_transactionMock.Object, null, requestType, cancellationToken));
    }

    [Fact]
    public async Task CommitTransactionAsync_WithCommitException_ShouldPropagateException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Commit failed");
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        _transactionMock.Setup(tx => tx.CommitAsync(cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _coordinator.CommitTransactionAsync(_transactionMock.Object, _contextMock.Object, requestType, cancellationToken));

        Assert.Equal(expectedException, exception);
    }

    #endregion

    #region RollbackTransactionAsync Tests

    [Fact]
    public async Task RollbackTransactionAsync_WithValidParameters_ShouldRollbackSuccessfully()
    {
        // Arrange
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        await _coordinator.RollbackTransactionAsync(_transactionMock.Object, _contextMock.Object, requestType, null, cancellationToken);

        // Assert
        _transactionMock.Verify(tx => tx.RollbackAsync(cancellationToken), Times.Once);

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Rolling back transaction") &&
                    v.ToString().Contains(_contextMock.Object.TransactionId) &&
                    v.ToString().Contains(requestType)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("rolled back successfully") &&
                    v.ToString().Contains(_contextMock.Object.TransactionId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithException_ShouldLogWarningAndRollback()
    {
        // Arrange
        var requestType = "TestRequest";
        var exception = new InvalidOperationException("Test exception");
        var cancellationToken = CancellationToken.None;

        // Act
        await _coordinator.RollbackTransactionAsync(_transactionMock.Object, _contextMock.Object, requestType, exception, cancellationToken);

        // Assert
        _transactionMock.Verify(tx => tx.RollbackAsync(cancellationToken), Times.Once);

        // Verify warning logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Rolling back transaction") &&
                    v.ToString().Contains(_contextMock.Object.TransactionId) &&
                    v.ToString().Contains(requestType) &&
                    v.ToString().Contains("due to exception")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithNullTransaction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _coordinator.RollbackTransactionAsync(null, _contextMock.Object, requestType, null, cancellationToken));
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Arrange
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _coordinator.RollbackTransactionAsync(_transactionMock.Object, null, requestType, null, cancellationToken));
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithRollbackException_ShouldPropagateException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Rollback failed");
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        _transactionMock.Setup(tx => tx.RollbackAsync(cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _coordinator.RollbackTransactionAsync(_transactionMock.Object, _contextMock.Object, requestType, null, cancellationToken));

        Assert.Equal(expectedException, exception);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
    {
        // Arrange
        IUnitOfWork unitOfWork = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TransactionCoordinator(unitOfWork, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        ILogger<TransactionCoordinator> logger = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TransactionCoordinator(_unitOfWorkMock.Object, logger));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Act
        var coordinator = new TransactionCoordinator(_unitOfWorkMock.Object, _loggerMock.Object);

        // Assert
        Assert.NotNull(coordinator);
    }

    #endregion

    #region Private Method Tests (through public interface)

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-2)]
    public async Task BeginTransactionAsync_WithDisabledTimeout_ShouldUseDefaultBehavior(int timeoutSeconds)
    {
        // Arrange
        var timeout = timeoutSeconds switch
        {
            0 => TimeSpan.Zero,
            -1 => Timeout.InfiniteTimeSpan,
            -2 => TimeSpan.FromTicks(-1),
            _ => TimeSpan.FromSeconds(timeoutSeconds)
        };

        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = timeout,
            IsReadOnly = false
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.BeginTransactionAsync(configuration, requestType, cancellationToken);

        // Assert
        Assert.Null(result.TimeoutCts);

        // Verify no timeout logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("configured with no timeout")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(300)]
    public async Task BeginTransactionAsync_WithEnabledTimeout_ShouldCreateTimeoutCts(int timeoutSeconds)
    {
        // Arrange
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = timeout,
            IsReadOnly = false
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.BeginTransactionAsync(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotNull(result.TimeoutCts);

        // Verify timeout logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("configured with timeout") &&
                    v.ToString().Contains(timeout.TotalSeconds.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithCancellation_ShouldPropagateCancellation()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false
        };
        var requestType = "TestRequest";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _unitOfWorkMock.Setup(uow => uow.BeginTransactionAsync(It.IsAny<IsolationLevel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _coordinator.BeginTransactionAsync(configuration, requestType, cts.Token));
    }

    #endregion

    #region Test Helper Classes

    private class TestTransactionConfiguration : ITransactionConfiguration
    {
        public IsolationLevel IsolationLevel { get; set; }
        public TimeSpan Timeout { get; set; }
        public bool IsReadOnly { get; set; }
        public bool UseDistributedTransaction { get; set; }
        public TransactionRetryPolicy? RetryPolicy { get; set; }
    }

    #endregion
}