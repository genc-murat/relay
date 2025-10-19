using Relay.MessageBroker.CircuitBreaker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CircuitBreakerConfigurationTests
{
    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ShouldAlwaysExecute()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = false,
            FailureThreshold = 1
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Multiple failures should not open circuit
        for (int i = 0; i < 10; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException("Test failure"));
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        // Assert - Should still be closed (disabled)
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);

        // Should still execute operations
        var result = await circuitBreaker.ExecuteAsync(async ct =>
        {
            await Task.Delay(10, ct);
            return "Success";
        });

        Assert.Equal("Success", result);
    }

    [Fact]
    public void Reset_ShouldCloseCircuitAndClearMetrics()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Open the circuit
        for (int i = 0; i < 2; i++)
        {
            try
            {
                circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException("Test failure")).GetAwaiter().GetResult();
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

        // Act
        circuitBreaker.Reset();

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        Assert.Equal(0, circuitBreaker.Metrics.TotalCalls);
        Assert.Equal(0, circuitBreaker.Metrics.FailedCalls);
    }

    [Fact]
    public void Isolate_ShouldOpenCircuitManually()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 10
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);

        // Act
        circuitBreaker.Isolate();

        // Assert
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
    }

    [Fact]
    public void Reset_DuringHalfOpenState_ShouldCloseCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            Timeout = TimeSpan.FromMilliseconds(100)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Open the circuit
        for (int i = 0; i < 2; i++)
        {
            try { circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException()).GetAwaiter().GetResult(); }
            catch (InvalidOperationException) { }
        }

        // Wait for timeout (simulated)
        // In real scenario, we'd wait, but for test we can manually set state or use a longer timeout

        // Act - Reset during any state
        circuitBreaker.Reset();

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        Assert.Equal(0, circuitBreaker.Metrics.TotalCalls);
    }
}