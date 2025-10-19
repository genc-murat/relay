using Relay.MessageBroker.CircuitBreaker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CircuitBreakerBasicExecutionTests
{
    [Fact]
    public async Task ExecuteAsync_WhenOperationSucceeds_ShouldRecordSuccess()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 5
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act
        var result = await circuitBreaker.ExecuteAsync(async ct =>
        {
            await Task.Delay(10, ct);
            return "Success";
        });

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        Assert.Equal(1, circuitBreaker.Metrics.SuccessfulCalls);
        Assert.Equal(0, circuitBreaker.Metrics.FailedCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationFails_ShouldRecordFailure()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 5
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act
        Func<Task> act = async () => await circuitBreaker.ExecuteAsync<string>(async ct =>
        {
            await Task.Delay(10, ct);
            throw new InvalidOperationException("Test failure");
        });

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        Assert.Equal(1, circuitBreaker.Metrics.FailedCalls);
    }

    [Fact]
    public async Task ExecuteAsync_NonGeneric_WhenOperationSucceeds_ShouldRecordSuccess()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 5
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act
        await circuitBreaker.ExecuteAsync(async ct =>
        {
            await Task.Delay(10, ct);
        });

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        Assert.Equal(1, circuitBreaker.Metrics.SuccessfulCalls);
        Assert.Equal(0, circuitBreaker.Metrics.FailedCalls);
    }
}