using Relay.MessageBroker.CircuitBreaker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CircuitBreakerExceptionTests
{
    [Fact]
    public void CircuitBreakerOpenException_WithMessage_ShouldCreateException()
    {
        // Arrange
        var message = "Circuit breaker is open";

        // Act
        var exception = new CircuitBreakerOpenException(message);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void CircuitBreakerOpenException_WithMessageAndInnerException_ShouldCreateException()
    {
        // Arrange
        var message = "Circuit breaker is open";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new CircuitBreakerOpenException(message, innerException);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void CircuitBreakerOpenException_ShouldBeThrowable()
    {
        // Arrange
        var message = "Circuit breaker is open";

        // Act
        void ThrowException() => throw new CircuitBreakerOpenException(message);

        // Assert
        var exception = Assert.Throws<CircuitBreakerOpenException>(ThrowException);
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void CircuitBreakerOpenException_ShouldBeCatchableAsException()
    {
        // Arrange
        var message = "Circuit breaker is open";
        Exception? caughtException = null;

        // Act
        try
        {
            throw new CircuitBreakerOpenException(message);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert
        Assert.NotNull(caughtException);
        Assert.IsType<CircuitBreakerOpenException>(caughtException);
        Assert.Equal(message, caughtException!.Message);
    }

    [Fact]
    public void CircuitBreakerOpenException_WithInnerException_ShouldPreserveStackTrace()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var message = "Circuit breaker is open";

        // Act
        var exception = new CircuitBreakerOpenException(message, innerException);

        // Assert
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal("Inner error", exception.InnerException.Message);
    }

    [Fact]
    public async Task CircuitBreakerOpenException_ShouldBeThrownWhenCircuitIsOpen()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            Timeout = TimeSpan.FromSeconds(5)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Force circuit to open by causing failures
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync<string>(async ct =>
                {
                    await Task.Delay(10, ct);
                    throw new InvalidOperationException("Test failure");
                });
            }
            catch (Exception)
            {
                // Expected - catch both InvalidOperationException and CircuitBreakerOpenException
            }
        }

        // Assert - Circuit should be open now, next call should throw CircuitBreakerOpenException
        Func<Task> act = async () => await circuitBreaker.ExecuteAsync<string>(async ct =>
        {
            await Task.Delay(10, ct);
            return "Success";
        });

        var exception = await Assert.ThrowsAsync<CircuitBreakerOpenException>(act);
        Assert.Contains("Circuit breaker is open", exception.Message);
    }

    [Fact]
    public void CircuitBreakerOpenException_WithNullMessage_ShouldHandleGracefully()
    {
        // Arrange & Act
        var exception = new CircuitBreakerOpenException(null!);

        // Assert
        Assert.NotNull(exception);
    }

    [Fact]
    public void CircuitBreakerOpenException_WithEmptyMessage_ShouldHandleGracefully()
    {
        // Arrange & Act
        var exception = new CircuitBreakerOpenException(string.Empty);

        // Assert
        Assert.NotNull(exception);
        Assert.Equal(string.Empty, exception.Message);
    }
}

public class CircuitBreakerMetricsTests
{
    [Fact]
    public void CircuitBreakerMetrics_DefaultValues_ShouldBeZero()
    {
        // Act
        var metrics = new CircuitBreakerMetrics();

        // Assert
        Assert.Equal(0, metrics.TotalCalls);
        Assert.Equal(0, metrics.SuccessfulCalls);
        Assert.Equal(0, metrics.FailedCalls);
        Assert.Equal(0, metrics.SlowCalls);
    }

    [Fact]
    public void CircuitBreakerMetrics_WithValues_ShouldStoreCorrectly()
    {
        // Act
        var metrics = new CircuitBreakerMetrics
        {
            TotalCalls = 100,
            SuccessfulCalls = 80,
            FailedCalls = 20,
            SlowCalls = 10
        };

        // Assert
        Assert.Equal(100, metrics.TotalCalls);
        Assert.Equal(80, metrics.SuccessfulCalls);
        Assert.Equal(20, metrics.FailedCalls);
        Assert.Equal(10, metrics.SlowCalls);
        
        // Calculate rates manually
        var successRate = metrics.TotalCalls > 0 ? (double)metrics.SuccessfulCalls / metrics.TotalCalls : 0;
        var failureRate = metrics.TotalCalls > 0 ? (double)metrics.FailedCalls / metrics.TotalCalls : 0;
        Assert.Equal(0.8, successRate);
        Assert.Equal(0.2, failureRate);
    }

    [Fact]
    public async Task CircuitBreaker_ShouldTrackMetricsCorrectly()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 5
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Execute successful operations
        for (int i = 0; i < 3; i++)
        {
            await circuitBreaker.ExecuteAsync(async ct =>
            {
                await Task.Delay(10, ct);
            });
        }

        // Execute failed operations
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync<string>(async ct =>
                {
                    await Task.Delay(10, ct);
                    throw new InvalidOperationException("Test failure");
                });
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        // Assert
        var metrics = circuitBreaker.Metrics;
        Assert.Equal(5, metrics.TotalCalls);
        Assert.Equal(3, metrics.SuccessfulCalls);
        Assert.Equal(2, metrics.FailedCalls);
    }
}

public class CircuitBreakerAdditionalTests
{
    [Fact]
    public void CircuitBreaker_Reset_ShouldResetToClosedState()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 1
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Force to open state
        try
        {
            circuitBreaker.ExecuteAsync<string>(async ct =>
            {
                await Task.Delay(10, ct);
                throw new InvalidOperationException();
            }).AsTask().Wait();
        }
        catch { }

        try
        {
            circuitBreaker.ExecuteAsync<string>(async ct =>
            {
                await Task.Delay(10, ct);
                throw new InvalidOperationException();
            }).AsTask().Wait();
        }
        catch { }

        // Act
        circuitBreaker.Reset();

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public void CircuitBreaker_Isolate_ShouldOpenCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act
        circuitBreaker.Isolate();

        // Assert - Isolate should set circuit to Open state
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_WithDisabled_ShouldAllowAllOperations()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = false
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Even with failures, circuit should stay closed
        for (int i = 0; i < 10; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync<string>(async ct =>
                {
                    await Task.Delay(10, ct);
                    throw new InvalidOperationException();
                });
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public void CircuitBreakerOptions_Constructor_WithNull_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CircuitBreaker.CircuitBreaker(null!));
    }
}
