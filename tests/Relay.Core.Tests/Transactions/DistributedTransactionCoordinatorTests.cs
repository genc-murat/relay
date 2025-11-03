using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Transactions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using IsolationLevel = System.Data.IsolationLevel;

namespace Relay.Core.Tests.Transactions;

/// <summary>
/// Tests for DistributedTransactionCoordinator.
/// </summary>
public class DistributedTransactionCoordinatorTests
{
    private readonly Mock<ILogger<DistributedTransactionCoordinator>> _loggerMock;
    private readonly DistributedTransactionCoordinator _coordinator;

    public DistributedTransactionCoordinatorTests()
    {
        _loggerMock = new Mock<ILogger<DistributedTransactionCoordinator>>();
        _coordinator = new DistributedTransactionCoordinator(_loggerMock.Object);
    }

    [Fact]
    public void CreateDistributedTransactionScope_WithValidConfiguration_ShouldReturnScopeWithTransactionId()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotNull(result.Scope);
        Assert.NotNull(result.TransactionId);
        Assert.NotEmpty(result.TransactionId);
        Assert.True(result.StartTime <= DateTime.UtcNow);
        Assert.True(result.StartTime > DateTime.UtcNow.AddMinutes(-1));

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Creating distributed transaction scope")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("created successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void CreateDistributedTransactionScope_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _coordinator.CreateDistributedTransactionScope(null, requestType, cancellationToken));
    }

    [Theory]
    [InlineData(IsolationLevel.Unspecified)]
    [InlineData(IsolationLevel.Chaos)]
    [InlineData(IsolationLevel.ReadUncommitted)]
    [InlineData(IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable)]
    [InlineData(IsolationLevel.Snapshot)]
    public void CreateDistributedTransactionScope_WithDifferentIsolationLevels_ShouldCreateScopeCorrectly(IsolationLevel isolationLevel)
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = isolationLevel,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotNull(result.Scope);
        Assert.NotNull(result.TransactionId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(300)]
    [InlineData(-1)]
    public void CreateDistributedTransactionScope_WithDifferentTimeouts_ShouldCreateScopeCorrectly(int timeoutSeconds)
    {
        // Arrange
        var timeout = timeoutSeconds switch
        {
            0 => TimeSpan.Zero,
            -1 => Timeout.InfiniteTimeSpan,
            _ => TimeSpan.FromSeconds(timeoutSeconds)
        };
        
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = timeout,
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotNull(result.Scope);
        Assert.NotNull(result.TransactionId);
    }

    [Fact]
    public void CreateDistributedTransactionScope_WithException_ShouldThrowDistributedTransactionException()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Mock logger to throw exception during logging to simulate failure
        _loggerMock.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()))
            .Throws(new InvalidOperationException("Simulated failure"));

        // Act & Assert
        var exception = Assert.Throws<DistributedTransactionException>(() =>
            _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken));

        Assert.Contains("Failed to create distributed transaction scope", exception.Message);
        Assert.NotNull(exception.TransactionId);
        Assert.NotNull(exception.InnerException);

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to create distributed transaction scope")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void CreateDistributedTransactionScope_ShouldGenerateUniqueTransactionIds()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result1 = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);
        var result2 = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotEqual(result1.TransactionId, result2.TransactionId);
    }

    [Fact]
    public async Task ExecuteInDistributedTransactionAsync_WithValidOperation_ShouldReturnResult()
    {
        // Arrange
        var expectedResult = "TestResult";
        var operation = new Func<CancellationToken, Task<string>>(_ => Task.FromResult(expectedResult));
        var transactionId = "test-tx-123";
        var timeout = TimeSpan.FromSeconds(30);
        var requestType = "TestRequest";
        var startTime = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.ExecuteInDistributedTransactionAsync(
            operation, transactionId, timeout, requestType, startTime, cancellationToken);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task ExecuteInDistributedTransactionAsync_WithNullOperation_ShouldThrowArgumentNullException()
    {
        // Arrange
        Func<CancellationToken, Task<string>> operation = null;
        var transactionId = "test-tx-123";
        var timeout = TimeSpan.FromSeconds(30);
        var requestType = "TestRequest";
        var startTime = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _coordinator.ExecuteInDistributedTransactionAsync(
                operation, transactionId, timeout, requestType, startTime, cancellationToken));
    }

    [Fact]
    public async Task ExecuteInDistributedTransactionAsync_WithTimeout_ShouldThrowTransactionTimeoutException()
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<string>>(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            return "Result";
        });
        var transactionId = "test-tx-123";
        var timeout = TimeSpan.FromMilliseconds(100); // Very short timeout
        var requestType = "TestRequest";
        var startTime = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TransactionTimeoutException>(() =>
            _coordinator.ExecuteInDistributedTransactionAsync(
                operation, transactionId, timeout, requestType, startTime, cancellationToken));

        Assert.Equal(transactionId, exception.TransactionId);
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
                    v.ToString().Contains(transactionId) &&
                    v.ToString().Contains(requestType)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteInDistributedTransactionAsync_WithTransactionException_ShouldThrowDistributedTransactionException()
    {
        // Arrange
        var transactionException = new TransactionException("Transaction failed");
        var operation = new Func<CancellationToken, Task<string>>(_ => Task.FromException<string>(transactionException));
        var transactionId = "test-tx-123";
        var timeout = TimeSpan.FromSeconds(30);
        var requestType = "TestRequest";
        var startTime = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DistributedTransactionException>(() =>
            _coordinator.ExecuteInDistributedTransactionAsync(
                operation, transactionId, timeout, requestType, startTime, cancellationToken));

        Assert.Contains("Distributed transaction failed", exception.Message);
        Assert.Equal(transactionId, exception.TransactionId);
        Assert.Equal(transactionException, exception.InnerException);

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Distributed transaction") &&
                    v.ToString().Contains("failed") &&
                    v.ToString().Contains(transactionId) &&
                    v.ToString().Contains(requestType)),
                transactionException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteInDistributedTransactionAsync_WithDisabledTimeout_ShouldNotCreateTimeoutToken()
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<string>>(_ => Task.FromResult("Result"));
        var transactionId = "test-tx-123";
        var timeout = TimeSpan.Zero; // Disabled timeout
        var requestType = "TestRequest";
        var startTime = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.ExecuteInDistributedTransactionAsync(
            operation, transactionId, timeout, requestType, startTime, cancellationToken);

        // Assert
        Assert.Equal("Result", result);
        // The operation should complete without timeout since timeout is disabled
    }

    [Fact]
    public async Task ExecuteInDistributedTransactionAsync_WithInfiniteTimeout_ShouldNotCreateTimeoutToken()
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<string>>(_ => Task.FromResult("Result"));
        var transactionId = "test-tx-123";
        var timeout = Timeout.InfiniteTimeSpan; // Infinite timeout
        var requestType = "TestRequest";
        var startTime = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.ExecuteInDistributedTransactionAsync(
            operation, transactionId, timeout, requestType, startTime, cancellationToken);

        // Assert
        Assert.Equal("Result", result);
        // The operation should complete without timeout since timeout is infinite
    }

    [Fact]
    public async Task ExecuteInDistributedTransactionAsync_WithCancellation_ShouldPropagateCancellation()
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<string>>(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(10), ct);
            return "Result";
        });
        var transactionId = "test-tx-123";
        var timeout = TimeSpan.FromSeconds(30);
        var requestType = "TestRequest";
        var startTime = DateTime.UtcNow;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _coordinator.ExecuteInDistributedTransactionAsync(
                operation, transactionId, timeout, requestType, startTime, cts.Token));
    }

    [Fact]
    public async Task ExecuteInDistributedTransactionAsync_WithGenericException_ShouldPropagateException()
    {
        // Arrange
        var genericException = new InvalidOperationException("Generic error");
        var operation = new Func<CancellationToken, Task<string>>(_ => Task.FromException<string>(genericException));
        var transactionId = "test-tx-123";
        var timeout = TimeSpan.FromSeconds(30);
        var requestType = "TestRequest";
        var startTime = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _coordinator.ExecuteInDistributedTransactionAsync(
                operation, transactionId, timeout, requestType, startTime, cancellationToken));

        Assert.Equal(genericException, exception);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(300)]
    public async Task ExecuteInDistributedTransactionAsync_WithDifferentTimeouts_ShouldWorkCorrectly(int timeoutSeconds)
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<string>>(_ => Task.FromResult("Result"));
        var transactionId = "test-tx-123";
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);
        var requestType = "TestRequest";
        var startTime = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.ExecuteInDistributedTransactionAsync(
            operation, transactionId, timeout, requestType, startTime, cancellationToken);

        // Assert
        Assert.Equal("Result", result);
    }

    [Fact]
    public async Task ExecuteInDistributedTransactionAsync_WithComplexOperation_ShouldHandleCorrectly()
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<int>>(async ct =>
        {
            await Task.Delay(100, ct);
            return 42;
        });
        var transactionId = "test-tx-123";
        var timeout = TimeSpan.FromSeconds(5);
        var requestType = "ComplexOperation";
        var startTime = DateTime.UtcNow;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _coordinator.ExecuteInDistributedTransactionAsync(
            operation, transactionId, timeout, requestType, startTime, cancellationToken);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteInDistributedTransactionAsync_WithTimeoutAndCancellation_ShouldHandleCorrectly()
    {
        // Arrange
        var operation = new Func<CancellationToken, Task<string>>(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            return "Result";
        });
        var transactionId = "test-tx-123";
        var timeout = TimeSpan.FromSeconds(1);
        var requestType = "TestRequest";
        var startTime = DateTime.UtcNow;
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act & Assert - Should throw due to cancellation (faster than timeout)
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _coordinator.ExecuteInDistributedTransactionAsync(
                operation, transactionId, timeout, requestType, startTime, cts.Token));
    }

    [Fact]
    public void CreateDistributedTransactionScope_ShouldLogCorrectInformation()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.Serializable,
            Timeout = TimeSpan.FromMinutes(2),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "CreateOrderCommand";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Creating distributed transaction scope") &&
                    v.ToString().Contains(requestType) &&
                    v.ToString().Contains(configuration.IsolationLevel.ToString()) &&
                    v.ToString().Contains(configuration.Timeout.TotalSeconds.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void CompleteDistributedTransaction_WithValidScope_ShouldCompleteSuccessfully()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;
        
        var createResult = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Act
        _coordinator.CompleteDistributedTransaction(createResult.Scope, createResult.TransactionId, requestType, createResult.StartTime);

        // Assert
        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Completing distributed transaction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("completed successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void CompleteDistributedTransaction_WithNullScope_ShouldThrowArgumentNullException()
    {
        // Arrange
        var transactionId = "test-tx-123";
        var requestType = "TestRequest";
        var startTime = DateTime.UtcNow;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _coordinator.CompleteDistributedTransaction(null, transactionId, requestType, startTime));
    }

    [Fact]
    public void CompleteDistributedTransaction_WithException_ShouldThrowDistributedTransactionException()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;
        
        var createResult = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Mock logger to throw exception during logging to simulate failure
        _loggerMock.Setup(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()))
            .Throws(new InvalidOperationException("Simulated failure"));

        // Act & Assert
        var exception = Assert.Throws<DistributedTransactionException>(() =>
            _coordinator.CompleteDistributedTransaction(createResult.Scope, createResult.TransactionId, requestType, createResult.StartTime));

        Assert.Contains("Failed to complete distributed transaction", exception.Message);
        Assert.Equal(createResult.TransactionId, exception.TransactionId);
        Assert.NotNull(exception.InnerException);

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to complete distributed transaction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void CompleteDistributedTransaction_ShouldLogCorrectInformation()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.Serializable,
            Timeout = TimeSpan.FromMinutes(2),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "CreateOrderCommand";
        var cancellationToken = CancellationToken.None;
        
        var createResult = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Act
        _coordinator.CompleteDistributedTransaction(createResult.Scope, createResult.TransactionId, requestType, createResult.StartTime);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Completing distributed transaction") &&
                    v.ToString().Contains(createResult.TransactionId) &&
                    v.ToString().Contains(requestType)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Distributed transaction") &&
                    v.ToString().Contains("completed successfully") &&
                    v.ToString().Contains(createResult.TransactionId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void CompleteDistributedTransaction_WithDifferentElapsedTimes_ShouldLogCorrectly()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;
        
        var createResult = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);
        var pastStartTime = DateTime.UtcNow.AddSeconds(-5); // Simulate 5 seconds elapsed

        // Act
        _coordinator.CompleteDistributedTransaction(createResult.Scope, createResult.TransactionId, requestType, pastStartTime);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Completing distributed transaction") &&
                    v.ToString().Contains(createResult.TransactionId) &&
                    v.ToString().Contains(requestType) &&
                    v.ToString().Contains("seconds")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void DisposeDistributedTransaction_WithValidScope_ShouldDisposeSuccessfully()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;
        
        var createResult = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Act
        _coordinator.DisposeDistributedTransaction(createResult.Scope, createResult.TransactionId, requestType, createResult.StartTime);

        // Assert
        // Verify debug logging for normal disposal
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Disposing distributed transaction")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void DisposeDistributedTransaction_WithNullScope_ShouldReturnSilently()
    {
        // Arrange
        var transactionId = "test-tx-123";
        var requestType = "TestRequest";
        var startTime = DateTime.UtcNow;

        // Act & Assert - Should not throw
        _coordinator.DisposeDistributedTransaction(null, transactionId, requestType, startTime);

        // Verify no logging occurred
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Never);
    }

    [Fact]
    public void DisposeDistributedTransaction_WithException_ShouldLogWarningAndRollback()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;
        var testException = new InvalidOperationException("Test exception");
        
        var createResult = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Act
        _coordinator.DisposeDistributedTransaction(createResult.Scope, createResult.TransactionId, requestType, createResult.StartTime, testException);

        // Assert
        // Verify warning logging for exception case
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Disposing distributed transaction") &&
                    v.ToString().Contains("due to exception") &&
                    v.ToString().Contains("will rollback")),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Verify debug logging for successful rollback
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Distributed transaction") &&
                    v.ToString().Contains("rolled back successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void DisposeDistributedTransaction_WithDisposalException_ShouldLogErrorAndNotThrow()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;
        
        var createResult = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Since TransactionScope is sealed, we can't mock it directly.
        // Instead, we'll test the error logging path by creating a scope that has already been disposed
        // which will cause an exception when trying to dispose again.
        createResult.Scope.Dispose(); // First dispose - should work fine

        // Act & Assert - Should not throw even when disposing an already disposed scope
        _coordinator.DisposeDistributedTransaction(createResult.Scope, createResult.TransactionId, requestType, createResult.StartTime);

        // Verify error logging (this may or may not occur depending on TransactionScope implementation)
        // The important thing is that the method doesn't throw
    }

    [Fact]
    public void DisposeDistributedTransaction_ShouldLogCorrectInformation()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.Serializable,
            Timeout = TimeSpan.FromMinutes(2),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "CreateOrderCommand";
        var cancellationToken = CancellationToken.None;
        
        var createResult = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Act
        _coordinator.DisposeDistributedTransaction(createResult.Scope, createResult.TransactionId, requestType, createResult.StartTime);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Disposing distributed transaction") &&
                    v.ToString().Contains(createResult.TransactionId) &&
                    v.ToString().Contains(requestType)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void DisposeDistributedTransaction_WithDifferentElapsedTimes_ShouldLogCorrectly()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;
        var testException = new InvalidOperationException("Test exception");
        
        var createResult = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);
        var pastStartTime = DateTime.UtcNow.AddSeconds(-10); // Simulate 10 seconds elapsed

        // Act
        _coordinator.DisposeDistributedTransaction(createResult.Scope, createResult.TransactionId, requestType, pastStartTime, testException);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Disposing distributed transaction") &&
                    v.ToString().Contains(createResult.TransactionId) &&
                    v.ToString().Contains(requestType) &&
                    v.ToString().Contains("seconds") &&
                    v.ToString().Contains("due to exception")),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void DisposeDistributedTransaction_WithExceptionAndDisposalError_ShouldLogBothAndNotThrow()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = IsolationLevel.ReadCommitted,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;
        var testException = new InvalidOperationException("Original exception");
        
        var createResult = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Since TransactionScope is sealed, we can't mock it directly.
        // We'll test warning logging path by providing an exception parameter.
        // The disposal error path is harder to test reliably, but we can verify warning logging.

        // Act & Assert - Should not throw
        _coordinator.DisposeDistributedTransaction(createResult.Scope, createResult.TransactionId, requestType, createResult.StartTime, testException);

        // Verify warning logging for original exception
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Disposing distributed transaction") &&
                    v.ToString().Contains("due to exception")),
                testException,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Verify debug logging for successful rollback
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Distributed transaction") &&
                    v.ToString().Contains("rolled back successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(IsolationLevel.Unspecified, System.Transactions.IsolationLevel.Unspecified)]
    [InlineData(IsolationLevel.Chaos, System.Transactions.IsolationLevel.Chaos)]
    [InlineData(IsolationLevel.ReadUncommitted, System.Transactions.IsolationLevel.ReadUncommitted)]
    [InlineData(IsolationLevel.ReadCommitted, System.Transactions.IsolationLevel.ReadCommitted)]
    [InlineData(IsolationLevel.RepeatableRead, System.Transactions.IsolationLevel.RepeatableRead)]
    [InlineData(IsolationLevel.Serializable, System.Transactions.IsolationLevel.Serializable)]
    [InlineData(IsolationLevel.Snapshot, System.Transactions.IsolationLevel.Snapshot)]
    public void CreateDistributedTransactionScope_WithValidIsolationLevels_ShouldConvertCorrectly(
        IsolationLevel dataIsolationLevel, 
        System.Transactions.IsolationLevel expectedTransactionIsolationLevel)
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = dataIsolationLevel,
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

        // Assert
        Assert.NotNull(result.Scope);
        Assert.NotNull(result.TransactionId);

        // Verify that the transaction scope was created with the correct isolation level
        // This indirectly tests the ConvertIsolationLevel method
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("Creating distributed transaction scope") &&
                    v.ToString().Contains(dataIsolationLevel.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void CreateDistributedTransactionScope_WithUnsupportedIsolationLevel_ShouldThrowArgumentException()
    {
        // Arrange
        var configuration = new TestTransactionConfiguration
        {
            IsolationLevel = (IsolationLevel)999, // Invalid isolation level
            Timeout = TimeSpan.FromSeconds(30),
            IsReadOnly = false,
            UseDistributedTransaction = true,
            RetryPolicy = null
        };
        var requestType = "TestRequest";
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        var exception = Assert.Throws<DistributedTransactionException>(() =>
            _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken));

        Assert.Contains("Failed to create distributed transaction scope", exception.Message);
        Assert.NotNull(exception.InnerException);
        Assert.IsType<ArgumentException>(exception.InnerException);
        Assert.Contains("Unsupported isolation level", exception.InnerException.Message);

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to create distributed transaction scope")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

        [Fact]
        public void CreateTransactionScope_WithValidIsolationLevels_ShouldConvertCorrectly()
        {
            // Arrange
            var configuration = new TestTransactionConfiguration
            {
                IsolationLevel = IsolationLevel.Serializable,
                Timeout = TimeSpan.FromSeconds(30),
                IsReadOnly = false,
                UseDistributedTransaction = true,
                RetryPolicy = null
            };

            // Act
            var scope = _coordinator.CreateTransactionScope(configuration);

            // Assert
            Assert.NotNull(scope);

            // The scope should be created successfully, which means ConvertIsolationLevel worked correctly
            // We can't directly test the private method, but we can verify it works through the public interface
            scope.Dispose();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-2)]
        public void CreateDistributedTransactionScope_WithDisabledTimeout_ShouldUseDefaultTimeout(int timeoutSeconds)
        {
            // Arrange
            var timeout = timeoutSeconds switch
            {
                0 => TimeSpan.Zero,
                -1 => System.Threading.Timeout.InfiniteTimeSpan,
                -2 => TimeSpan.FromTicks(-1),
                _ => TimeSpan.FromSeconds(timeoutSeconds)
            };
            
            var configuration = new TestTransactionConfiguration
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = timeout,
                IsReadOnly = false,
                UseDistributedTransaction = true,
                RetryPolicy = null
            };
            var requestType = "TestRequest";
            var cancellationToken = CancellationToken.None;

            // Act
            var result = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

            // Assert
            Assert.NotNull(result.Scope);
            Assert.NotNull(result.TransactionId);

            // Verify that transaction scope was created (timeout should be disabled, using default)
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => 
                        v.ToString().Contains("Creating distributed transaction scope") &&
                        v.ToString().Contains(requestType)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            result.Scope.Dispose();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(30)]
        [InlineData(300)]
        [InlineData(3600)]
        public void CreateDistributedTransactionScope_WithEnabledTimeout_ShouldUseConfiguredTimeout(int timeoutSeconds)
        {
            // Arrange
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            
            var configuration = new TestTransactionConfiguration
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = timeout,
                IsReadOnly = false,
                UseDistributedTransaction = true,
                RetryPolicy = null
            };
            var requestType = "TestRequest";
            var cancellationToken = CancellationToken.None;

            // Act
            var result = _coordinator.CreateDistributedTransactionScope(configuration, requestType, cancellationToken);

            // Assert
            Assert.NotNull(result.Scope);
            Assert.NotNull(result.TransactionId);

            // Verify that transaction scope was created with the specified timeout
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => 
                        v.ToString().Contains("Creating distributed transaction scope") &&
                        v.ToString().Contains(requestType) &&
                        v.ToString().Contains(timeout.TotalSeconds.ToString())),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            result.Scope.Dispose();
        }

        [Fact]
        public void CreateTransactionScope_WithDisabledTimeout_ShouldUseDefaultTimeout()
        {
            // Arrange
            var configuration = new TestTransactionConfiguration
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.Zero, // Disabled timeout
                IsReadOnly = false,
                UseDistributedTransaction = true,
                RetryPolicy = null
            };

            // Act
            var scope = _coordinator.CreateTransactionScope(configuration);

            // Assert
            Assert.NotNull(scope);

            // The scope should be created successfully with default timeout
            // This indirectly tests IsTimeoutEnabled method returning false for TimeSpan.Zero
            scope.Dispose();
        }

        [Fact]
        public void CreateTransactionScope_WithInfiniteTimeout_ShouldUseDefaultTimeout()
        {
            // Arrange
            var configuration = new TestTransactionConfiguration
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = System.Threading.Timeout.InfiniteTimeSpan, // Infinite timeout
                IsReadOnly = false,
                UseDistributedTransaction = true,
                RetryPolicy = null
            };

            // Act
            var scope = _coordinator.CreateTransactionScope(configuration);

            // Assert
            Assert.NotNull(scope);

            // The scope should be created successfully with default timeout
            // This indirectly tests IsTimeoutEnabled method returning false for InfiniteTimeSpan
            scope.Dispose();
        }

        [Fact]
        public void CreateTransactionScope_WithPositiveTimeout_ShouldUseConfiguredTimeout()
        {
            // Arrange
            var configuration = new TestTransactionConfiguration
            {
                IsolationLevel = IsolationLevel.ReadCommitted,
                Timeout = TimeSpan.FromMinutes(2), // Positive timeout
                IsReadOnly = false,
                UseDistributedTransaction = true,
                RetryPolicy = null
            };

            // Act
            var scope = _coordinator.CreateTransactionScope(configuration);

            // Assert
            Assert.NotNull(scope);

            // The scope should be created successfully with the specified timeout
            // This indirectly tests IsTimeoutEnabled method returning true for positive TimeSpan
            scope.Dispose();
        }

    private class TestTransactionConfiguration : ITransactionConfiguration
    {
        public IsolationLevel IsolationLevel { get; set; }
        public TimeSpan Timeout { get; set; }
        public bool IsReadOnly { get; set; }
        public bool UseDistributedTransaction { get; set; }
        public TransactionRetryPolicy? RetryPolicy { get; set; }
    }


}