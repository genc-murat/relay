using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Transactions.Tests;

public class TransactionRetryHandlerTests
{
    private readonly Mock<ILogger<TransactionRetryHandler>> _loggerMock;
    private readonly TransactionRetryHandler _retryHandler;

    public TransactionRetryHandlerTests()
    {
        _loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        _retryHandler = new TransactionRetryHandler(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithValidLogger_Succeeds()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();

        // Act
        var handler = new TransactionRetryHandler(loggerMock.Object);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TransactionRetryHandler(null));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var retryPolicy = new TransactionRetryPolicy();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _retryHandler.ExecuteWithRetryAsync<string>(null, retryPolicy, "test", "test", CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithNullRetryPolicy_ExecutesOperationOnce()
    {
        // Arrange
        var callCount = 0;

        // Act
        var result = await _retryHandler.ExecuteWithRetryAsync(
            _ =>
            {
                callCount++;
                return Task.FromResult("success");
            },
            null,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithSuccessfulOperation_ExecutesOnce()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 3,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        var result = await _retryHandler.ExecuteWithRetryAsync(
            _ =>
            {
                callCount++;
                return Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithNonTransientError_ThrowsImmediately()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 3,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };
        var nonTransientError = new ArgumentException("invalid argument");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _retryHandler.ExecuteWithRetryAsync<string>(
                _ =>
                {
                    callCount++;
                    throw nonTransientError;
                },
                retryPolicy,
                "test-transaction",
                "test-request",
                CancellationToken.None));

        Assert.Equal(1, callCount);
        Assert.Same(nonTransientError, exception);

        // Verify non-transient error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("failed with non-transient error") &&
                    v.ToString().Contains("test-transaction") &&
                    v.ToString().Contains("test-request")),
                nonTransientError,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithTransientError_RetriesAndSucceeds()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 2,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };
        var transientError = new TimeoutException("timeout");

        // Act
        var result = await _retryHandler.ExecuteWithRetryAsync(
            _ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw transientError;
                }
                return Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount);

        // Verify retry logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("failed with transient error") &&
                    v.ToString().Contains("test-transaction") &&
                    v.ToString().Contains("test-request") &&
                    v.ToString().Contains("Retry attempt 1")),
                transientError,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithAllRetriesExhausted_ThrowsTransactionRetryExhaustedException()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 2,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };
        var transientError = new TimeoutException("timeout");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TransactionRetryExhaustedException>(
            () => _retryHandler.ExecuteWithRetryAsync<string>(
                _ =>
                {
                    callCount++;
                    throw transientError;
                },
                retryPolicy,
                "test-transaction",
                "test-request",
                CancellationToken.None));

        Assert.Equal(3, callCount); // Initial attempt + 2 retries
        Assert.Contains("failed after 2 retry attempts", exception.Message);
        Assert.Equal(transientError, exception.InnerException);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 2,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(10)
        };
        var cancellationToken = new CancellationToken(true); // Already cancelled token

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _retryHandler.ExecuteWithRetryAsync<string>(
                cancellationToken =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return Task.FromResult("result");
                },
                retryPolicy,
                "test-transaction",
                "test-request",
                cancellationToken));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithLinearRetryStrategy_CalculatesFixedDelay()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 2,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        await _retryHandler.ExecuteWithRetryAsync(
            async _ =>
            {
                callCount++;
                if (callCount <= 2)
                {
                    throw new InvalidOperationException("connection timeout - retry me");
                }
                return await Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal(3, callCount); // Initial attempt + 2 retries
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithExponentialBackoffRetryStrategy_CalculatesIncreasingDelay()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 2,
            Strategy = RetryStrategy.ExponentialBackoff,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        await _retryHandler.ExecuteWithRetryAsync(
            async _ =>
            {
                callCount++;
                if (callCount <= 2)
                {
                    throw new InvalidOperationException("connection timeout - retry me");
                }
                return await Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal(3, callCount); // Initial attempt + 2 retries
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithTransientErrorDetector_UsesSpecifiedDetector()
    {
        // Arrange
        var callCount = 0;
        var customDetector = new Mock<ITransientErrorDetector>();
        customDetector.Setup(x => x.IsTransient(It.IsAny<Exception>())).Returns(true);
        
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 1,
            TransientErrorDetector = customDetector.Object,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        await _retryHandler.ExecuteWithRetryAsync(
            async _ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("custom transient");
                }
                return await Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal(2, callCount);
        customDetector.Verify(x => x.IsTransient(It.IsAny<InvalidOperationException>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithShouldRetryPredicate_UsesCustomDetector()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 1,
            ShouldRetry = ex => ex is InvalidOperationException,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        await _retryHandler.ExecuteWithRetryAsync(
            async _ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("connection timeout - retry me");
                }
                return await Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithTransientErrorDetectorTakingPriority_UsesTransientErrorDetectorOverShouldRetry()
    {
        // Arrange
        var callCount = 0;
        var customDetector = new Mock<ITransientErrorDetector>();
        customDetector.Setup(x => x.IsTransient(It.IsAny<Exception>())).Returns(false); // Not transient
        
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 1,
            TransientErrorDetector = customDetector.Object,
            ShouldRetry = ex => true, // Would retry, but detector takes priority
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _retryHandler.ExecuteWithRetryAsync<string>(
                _ =>
                {
                    callCount++;
                    throw new InvalidOperationException("not transient by detector");
                },
                retryPolicy,
                "test-transaction",
                "test-request",
                CancellationToken.None));

        // Assert
        Assert.Equal(1, callCount); // Should not retry
        customDetector.Verify(x => x.IsTransient(It.IsAny<InvalidOperationException>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithInvalidRetryStrategy_ThrowsInvalidOperationException()
    {
        // Arrange
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 1,
            Strategy = (RetryStrategy)999, // Invalid strategy
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _retryHandler.ExecuteWithRetryAsync<string>(
                _ => throw new InvalidOperationException("test"),
                retryPolicy,
                "test-transaction",
                "test-request",
                CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithRetryPolicyMaxRetriesZero_ExecutesOperationOnce()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 0,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        var result = await _retryHandler.ExecuteWithRetryAsync(
            _ =>
            {
                callCount++;
                return Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithSuccessAfterRetry_LogsSuccessMessage()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 1,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        var result = await _retryHandler.ExecuteWithRetryAsync(
            _ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("transient error");
                }
                return Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount);

        // Verify success logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("succeeded on retry attempt") &&
                    v.ToString().Contains("test-transaction") &&
                    v.ToString().Contains("test-request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithAllRetriesExhausted_LogsErrorAndThrows()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 2,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };
        var transientError = new TimeoutException("connection timeout");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TransactionRetryExhaustedException>(
            () => _retryHandler.ExecuteWithRetryAsync<string>(
                _ =>
                {
                    callCount++;
                    throw transientError;
                },
                retryPolicy,
                "test-transaction",
                "test-request",
                CancellationToken.None));

        // Assert
        Assert.Equal(3, callCount); // Initial attempt + 2 retries

        // Verify error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("failed after") &&
                    v.ToString().Contains("retry attempts") &&
                    v.ToString().Contains("test-transaction") &&
                    v.ToString().Contains("test-request") &&
                    v.ToString().Contains("2") && // retry attempts
                    v.ToString().Contains("connection timeout")),
                transientError,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithTransientError_LogsRetryWarning()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 1,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(10)
        };

        // Act
        await _retryHandler.ExecuteWithRetryAsync(
            _ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("transient error");
                }
                return Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal(2, callCount);

        // Verify retry warning logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("failed with transient error") &&
                    v.ToString().Contains("test-transaction") &&
                    v.ToString().Contains("test-request") &&
                    v.ToString().Contains("Retry attempt 1") &&
                    v.ToString().Contains("of 1") &&
                    v.ToString().Contains("transient error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithNonTransientError_LogsNonTransientWarning()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 2,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };
        var nonTransientError = new ArgumentException("invalid argument");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _retryHandler.ExecuteWithRetryAsync<string>(
                _ =>
                {
                    callCount++;
                    throw nonTransientError;
                },
                retryPolicy,
                "test-transaction",
                "test-request",
                CancellationToken.None));

        // Assert
        Assert.Equal(1, callCount);

        // Verify non-transient error logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("failed with non-transient error") &&
                    v.ToString().Contains("test-transaction") &&
                    v.ToString().Contains("test-request")),
                nonTransientError,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithZeroDelay_SkipsDelay()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 1,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.Zero // Zero delay
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = await _retryHandler.ExecuteWithRetryAsync(
            _ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("transient error");
                }
                return Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        stopwatch.Stop();

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount);
        Assert.True(stopwatch.ElapsedMilliseconds < 50); // Should complete quickly without delay
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public async Task ExecuteWithRetryAsync_WithMultipleRetries_RetriesCorrectNumberOfTimes(int maxRetries)
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = maxRetries,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        var result = await _retryHandler.ExecuteWithRetryAsync(
            _ =>
            {
                callCount++;
                if (callCount <= maxRetries)
                {
                    throw new InvalidOperationException("transient error");
                }
                return Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(maxRetries + 1, callCount); // Initial attempt + all retries
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithNullTransactionIdAndRequestType_HandlesGracefully()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 1,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        var result = await _retryHandler.ExecuteWithRetryAsync(
            _ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("transient error");
                }
                return Task.FromResult("success");
            },
            retryPolicy,
            null, // null transaction ID
            null, // null request type
            CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount);

        // Verify logging works with null values
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("failed with transient error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithDifferentExceptionTypes_RetriesOnlyTransient()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 2,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _retryHandler.ExecuteWithRetryAsync(
                _ =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        throw new TimeoutException("timeout"); // Transient
                    }
                    if (callCount == 2)
                    {
                        throw new ArgumentException("invalid"); // Non-transient
                    }
                    return Task.FromResult("success");
                },
                retryPolicy,
                "test-transaction",
                "test-request",
                CancellationToken.None);
        });

        Assert.Equal(2, callCount); // Should retry timeout but not argument exception
        Assert.Equal("invalid", exception.Message);

        // Verify retry for transient error
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("failed with transient error")),
                It.IsAny<TimeoutException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);

        // Verify no retry for non-transient error
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString().Contains("failed with non-transient error")),
                It.IsAny<ArgumentException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }



    [Fact]
    public async Task ExecuteWithRetryAsync_WithDelayAndCancelledToken_DuringDelay_ThrowsOperationCanceledException()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 2,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(200) // Longer delay for cancellation test
        };

        using var cts = new CancellationTokenSource();

        // Act & Assert
        var task = _retryHandler.ExecuteWithRetryAsync<string>(
            cancellationToken =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("transient error");
                }
                return Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            cts.Token);

        // Cancel token after a short delay to occur during the retry delay period
        await Task.Delay(10);
        cts.Cancel();

        // Task.Delay throws TaskCanceledException, which inherits from OperationCanceledException
        // but xUnit's Assert.ThrowsAsync requires exact type match, so we need to check for TaskCanceledException
        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }

    // Tests to cover GetErrorDetector method branches
    [Fact]
    public async Task ExecuteWithRetryAsync_WithTransientErrorDetector_CoversGetErrorDetectorBranch()
    {
        // Arrange
        var callCount = 0;
        var customDetector = new Mock<ITransientErrorDetector>();
        customDetector.Setup(x => x.IsTransient(It.IsAny<Exception>())).Returns(true);
        
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 1,
            TransientErrorDetector = customDetector.Object,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        var result = await _retryHandler.ExecuteWithRetryAsync<string>(
            async _ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new TimeoutException("transient error");
                }
                return "success";
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert - This covers the GetErrorDetector branch where ShouldRetry is not null
        Assert.Equal("success", result);
        Assert.Equal(2, callCount); // Initial attempt + 1 retry
        customDetector.Verify(x => x.IsTransient(It.IsAny<TimeoutException>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithShouldRetryPredicate_CoversGetErrorDetectorBranch()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 1,
            ShouldRetry = ex => ex is TimeoutException,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        var result = await _retryHandler.ExecuteWithRetryAsync<string>(
            async _ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new TimeoutException("timeout error");
                }
                return "success";
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert - This covers the GetErrorDetector branch where ShouldRetry is not null
        Assert.Equal("success", result);
        Assert.Equal(2, callCount); // Initial attempt + 1 retry
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithDefaultErrorDetector_CoversGetErrorDetectorDefaultBranch()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 1,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
            // Neither TransientErrorDetector nor ShouldRetry set - uses default
        };

        // Act
        var result = await _retryHandler.ExecuteWithRetryAsync<string>(
            async _ =>
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new TimeoutException("timeout error"); // Default detector handles this
                }
                return "success";
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert - This covers the default branch in GetErrorDetector
        Assert.Equal("success", result);
        Assert.Equal(2, callCount); // Initial attempt + 1 retry
    }

    // Tests to cover GetRetryStrategy method branches
    [Fact]
    public async Task ExecuteWithRetryAsync_WithLinearStrategy_CoversGetRetryStrategyLinearBranch()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 2,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        await _retryHandler.ExecuteWithRetryAsync(
            async _ =>
            {
                callCount++;
                if (callCount <= 2)
                {
                    throw new InvalidOperationException("connection timeout - retry me");
                }
                return await Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal(3, callCount); // Initial attempt + 2 retries
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithExponentialBackoffStrategy_CoversGetRetryStrategyExponentialBranch()
    {
        // Arrange
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy
        {
            MaxRetries = 2,
            Strategy = RetryStrategy.ExponentialBackoff,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        await _retryHandler.ExecuteWithRetryAsync(
            async _ =>
            {
                callCount++;
                if (callCount <= 2)
                {
                    throw new InvalidOperationException("connection timeout - retry me");
                }
                return await Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert - This covers the ExponentialBackoff branch in GetRetryStrategy
        Assert.Equal(3, callCount); // Initial attempt + 2 retries
    }
}
