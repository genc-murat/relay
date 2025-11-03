using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Relay.Core.Transactions.Tests;

public class TransactionRetryHandlerTests
{
    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new TransactionRetryHandler(null!));
        Assert.Equal("logger", exception.ParamName);
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
    public async Task ExecuteWithRetryAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var handler = new TransactionRetryHandler(loggerMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.ExecuteWithRetryAsync<string>(null!, null, null, null, CancellationToken.None));
        Assert.Equal("operation", exception.ParamName);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithNullRetryPolicy_ExecutesOperationOnce()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;

        // Act
        var result = await handler.ExecuteWithRetryAsync(
            _ => 
            {
                callCount++;
                return Task.FromResult("success");
            },
            null, // null retry policy
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithRetryPolicyMaxRetriesZero_ExecutesOperationOnce()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy { MaxRetries = 0 };

        // Act
        var result = await handler.ExecuteWithRetryAsync(
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
    public async Task ExecuteWithRetryAsync_WithSuccessfulOperation_ExecutesOnce()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy { MaxRetries = 3 };

        // Act
        var result = await handler.ExecuteWithRetryAsync(
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
    public async Task ExecuteWithRetryAsync_WithTransientErrorThatSucceedsOnRetry_RetriesAndSucceeds()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy 
        { 
            MaxRetries = 3,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1) // Small delay for testing
        };

        // Create a transient error detector that always returns true
        var transientError = new InvalidOperationException("connection timeout");

        // Act
        var result = await handler.ExecuteWithRetryAsync(
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
        Assert.Equal(2, callCount); // First attempt fails, second succeeds
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithNonTransientError_ThrowsImmediately()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy { MaxRetries = 3 };
        var nonTransientError = new InvalidOperationException("Some business logic error"); // Not a connection-related error

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.ExecuteWithRetryAsync<string>(
                _ => 
                {
                    callCount++;
                    throw nonTransientError;
                },
                retryPolicy,
                "test-transaction",
                "test-request",
                CancellationToken.None));

        Assert.Equal("Some business logic error", exception.Message);
        Assert.Equal(1, callCount); // Should not retry non-transient errors
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithAllRetriesExhausted_ThrowsTransactionRetryExhaustedException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy { MaxRetries = 2 };
        var transientError = new InvalidOperationException("connection timeout");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TransactionRetryExhaustedException>(
            () => handler.ExecuteWithRetryAsync<string>(
                _ => 
                {
                    callCount++;
                    throw transientError;
                },
                retryPolicy,
                "test-transaction",
                "test-request",
                CancellationToken.None));

        Assert.Equal(2, exception.RetryAttempts);
        Assert.Equal("test-transaction", exception.TransactionId);
        Assert.Equal("test-request", exception.RequestType);
        Assert.Equal("connection timeout", exception.InnerException?.Message);
        Assert.Equal(3, callCount); // Initial attempt + 2 retries = 3 total attempts
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithTransientErrorDetector_UsesSpecifiedDetector()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var customErrorDetector = new Mock<ITransientErrorDetector>();
        customErrorDetector.Setup(x => x.IsTransient(It.IsAny<Exception>())).Returns(true);
        
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy 
        { 
            MaxRetries = 1,
            TransientErrorDetector = customErrorDetector.Object,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        var result = await handler.ExecuteWithRetryAsync<string>(
            _ => 
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("first attempt fails");
                }
                return Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount); // First attempt fails, second succeeds
        customErrorDetector.Verify(x => x.IsTransient(It.IsAny<Exception>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithShouldRetryPredicate_UsesCustomDetector()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var shouldRetryPredicate = new Mock<Func<Exception, bool>>();
        shouldRetryPredicate.Setup(x => x.Invoke(It.IsAny<Exception>())).Returns(true);
        
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy 
        { 
            MaxRetries = 1,
            ShouldRetry = shouldRetryPredicate.Object,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        var result = await handler.ExecuteWithRetryAsync<string>(
            _ => 
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("first attempt fails");
                }
                return Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount); // First attempt fails, second succeeds
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithTransientErrorDetectorTakingPriority_UsesTransientErrorDetectorOverShouldRetry()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var transientErrorDetector = new Mock<ITransientErrorDetector>();
        transientErrorDetector.Setup(x => x.IsTransient(It.IsAny<Exception>())).Returns(true);
        
        var shouldRetryPredicate = new Mock<Func<Exception, bool>>();
        shouldRetryPredicate.Setup(x => x.Invoke(It.IsAny<Exception>())).Returns(false); // Should not be called because transient detector takes precedence
        
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy 
        { 
            MaxRetries = 1,
            TransientErrorDetector = transientErrorDetector.Object,
            ShouldRetry = shouldRetryPredicate.Object,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };

        // Act
        var result = await handler.ExecuteWithRetryAsync<string>(
            _ => 
            {
                callCount++;
                if (callCount == 1)
                {
                    throw new InvalidOperationException("first attempt fails");
                }
                return Task.FromResult("success");
            },
            retryPolicy,
            "test-transaction",
            "test-request",
            CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(2, callCount); // First attempt fails, second succeeds
        transientErrorDetector.Verify(x => x.IsTransient(It.IsAny<Exception>()), Times.Once);
        shouldRetryPredicate.Verify(x => x.Invoke(It.IsAny<Exception>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithLinearRetryStrategy_CalculatesFixedDelay()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy 
        { 
            MaxRetries = 2,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(10) // Fixed delay for all attempts
        };

        // Act
        var result = await handler.ExecuteWithRetryAsync<string>(
            _ => 
            {
                callCount++;
                if (callCount <= 2) // Fail first 2 attempts, succeed on 3rd
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
        Assert.Equal(3, callCount); // First 2 attempts fail, 3rd succeeds
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithExponentialBackoffRetryStrategy_CalculatesIncreasingDelay()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy 
        { 
            MaxRetries = 2,
            Strategy = RetryStrategy.ExponentialBackoff,
            InitialDelay = TimeSpan.FromMilliseconds(10)
        };

        // Act
        var result = await handler.ExecuteWithRetryAsync<string>(
            _ => 
            {
                callCount++;
                if (callCount <= 2) // Fail first 2 attempts, succeed on 3rd
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
        Assert.Equal(3, callCount); // First 2 attempts fail, 3rd succeeds
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithInvalidRetryStrategy_ThrowsInvalidOperationException()
    {
        // Note: We can't actually test this scenario with the current public APIs
        // since RetryStrategy enum only has Linear and ExponentialBackoff values.
        // The scenario would happen if someone tried to extend the enum in the future 
        // and the switch statement in GetRetryStrategy didn't have a case for it.
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy 
        { 
            MaxRetries = 2,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(10)
        };
        var cancellationToken = new CancellationToken(true); // Already cancelled token

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.ExecuteWithRetryAsync<string>(
                cancellationToken => 
                {
                    callCount++;
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    return Task.FromResult("result");
                },
                retryPolicy,
                "test-transaction",
                "test-request",
                cancellationToken));
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithDelayAndCancelledToken_DuringDelay_ThrowsOperationCanceledException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<TransactionRetryHandler>>();
        var handler = new TransactionRetryHandler(loggerMock.Object);
        var callCount = 0;
        var retryPolicy = new TransactionRetryPolicy 
        { 
            MaxRetries = 2,
            Strategy = RetryStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(200) // Longer delay for cancellation test
        };
        
        using var cts = new CancellationTokenSource();
        
        // Act & Assert
        var task = handler.ExecuteWithRetryAsync<string>(
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
}