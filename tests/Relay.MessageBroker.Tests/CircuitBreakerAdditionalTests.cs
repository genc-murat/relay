using FluentAssertions;
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
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
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
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void CircuitBreakerOpenException_ShouldBeThrowable()
    {
        // Arrange
        var message = "Circuit breaker is open";

        // Act
        Action act = () => throw new CircuitBreakerOpenException(message);

        // Assert
        act.Should().Throw<CircuitBreakerOpenException>()
            .WithMessage(message);
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
        caughtException.Should().NotBeNull();
        caughtException.Should().BeOfType<CircuitBreakerOpenException>();
        caughtException!.Message.Should().Be(message);
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
        exception.InnerException.Should().Be(innerException);
        exception.InnerException.Message.Should().Be("Inner error");
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

        await act.Should().ThrowAsync<CircuitBreakerOpenException>()
            .WithMessage("*Circuit breaker is open*");
    }

    [Fact]
    public void CircuitBreakerOpenException_WithNullMessage_ShouldHandleGracefully()
    {
        // Arrange & Act
        var exception = new CircuitBreakerOpenException(null!);

        // Assert
        exception.Should().NotBeNull();
    }

    [Fact]
    public void CircuitBreakerOpenException_WithEmptyMessage_ShouldHandleGracefully()
    {
        // Arrange & Act
        var exception = new CircuitBreakerOpenException(string.Empty);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(string.Empty);
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
        metrics.TotalCalls.Should().Be(0);
        metrics.SuccessfulCalls.Should().Be(0);
        metrics.FailedCalls.Should().Be(0);
        metrics.SlowCalls.Should().Be(0);
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
        metrics.TotalCalls.Should().Be(100);
        metrics.SuccessfulCalls.Should().Be(80);
        metrics.FailedCalls.Should().Be(20);
        metrics.SlowCalls.Should().Be(10);
        
        // Calculate rates manually
        var successRate = metrics.TotalCalls > 0 ? (double)metrics.SuccessfulCalls / metrics.TotalCalls : 0;
        var failureRate = metrics.TotalCalls > 0 ? (double)metrics.FailedCalls / metrics.TotalCalls : 0;
        successRate.Should().Be(0.8);
        failureRate.Should().Be(0.2);
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
        metrics.TotalCalls.Should().Be(5);
        metrics.SuccessfulCalls.Should().Be(3);
        metrics.FailedCalls.Should().Be(2);
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
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
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
        circuitBreaker.State.Should().Be(CircuitBreakerState.Open);
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
        circuitBreaker.State.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void CircuitBreakerOptions_Constructor_WithNull_ShouldThrow()
    {
        // Act
        Action act = () => new CircuitBreaker.CircuitBreaker(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
