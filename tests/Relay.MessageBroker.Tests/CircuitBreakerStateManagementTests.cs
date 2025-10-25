using Relay.MessageBroker.CircuitBreaker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CircuitBreakerStateManagementTests
{
    [Fact]
    public async Task ExecuteAsync_WhenFailureThresholdReached_ShouldOpenCircuit()
    {
        // Arrange
        var stateChangeCount = 0;
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 3,
            OnStateChanged = e => stateChangeCount++
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Exceed failure threshold
        for (int i = 0; i < 3; i++)
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

        // Assert
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
        Assert.Equal(3, circuitBreaker.Metrics.FailedCalls);
        Assert.Equal(1, stateChangeCount); // One transition to Open
    }

    [Fact]
    public async Task ExecuteAsync_WhenCircuitOpen_ShouldThrowCircuitBreakerOpenException()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            Timeout = TimeSpan.FromMinutes(1)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Open the circuit
        for (int i = 0; i < 2; i++)
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

        // Act
        Func<Task> act = async () => await circuitBreaker.ExecuteAsync(async ct =>
        {
            await Task.Delay(10, ct);
            return "Success";
        });

        // Assert
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(act);
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_AfterTimeout_ShouldTransitionToHalfOpen()
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
            try
            {
                await circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException("Test failure"));
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

        // Wait for timeout
        await Task.Delay(150);

        // Act - This should transition to HalfOpen
        var result = await circuitBreaker.ExecuteAsync(async ct =>
        {
            await Task.Delay(10, ct);
            return "Success";
        });

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(CircuitBreakerState.HalfOpen, circuitBreaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_InHalfOpenState_WhenSuccessThresholdReached_ShouldClosCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            SuccessThreshold = 2,
            Timeout = TimeSpan.FromMilliseconds(100),
            HalfOpenDuration = TimeSpan.FromMilliseconds(50)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Open the circuit
        for (int i = 0; i < 2; i++)
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

        // Wait for timeout to enter HalfOpen
        await Task.Delay(150);

        // Execute first successful call (enters HalfOpen)
        await circuitBreaker.ExecuteAsync(async ct =>
        {
            await Task.Delay(10, ct);
            return "Success";
        });

        await Task.Delay(60); // Wait for HalfOpenDuration

        // Act - Execute second successful call (should close circuit)
        await circuitBreaker.ExecuteAsync(async ct =>
        {
            await Task.Delay(10, ct);
            return "Success";
        });

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_InHalfOpenState_WhenFailureOccurs_ShouldReopenCircuit()
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
            try
            {
                await circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException("Test failure"));
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        // Wait for timeout
        await Task.Delay(150);

        // Act - Failure in HalfOpen state
        try
        {
            await circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException("Test failure"));
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        // Assert
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_WithVeryShortTimeout_ShouldTransitionQuickly()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            Timeout = TimeSpan.FromMilliseconds(10)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Open the circuit
        for (int i = 0; i < 2; i++)
        {
            try { await circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException()); }
            catch (InvalidOperationException) { }
        }

        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

        // Wait for very short timeout
        await Task.Delay(20);

        // Act - Should transition to HalfOpen
        var result = await circuitBreaker.ExecuteAsync(ct => ValueTask.FromResult("Success"));

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(CircuitBreakerState.HalfOpen, circuitBreaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleStateTransitions_ShouldTrackCorrectly()
    {
        // Arrange
        var stateChanges = new List<CircuitBreakerState>();
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            SuccessThreshold = 2,
            Timeout = TimeSpan.FromMilliseconds(50),
            OnStateChanged = e => stateChanges.Add(e.NewState)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Open circuit
        for (int i = 0; i < 2; i++)
        {
            try { await circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException()); }
            catch (InvalidOperationException) { }
        }

        // Wait for timeout and transition to HalfOpen
        await Task.Delay(60);
        await circuitBreaker.ExecuteAsync(ct => ValueTask.FromResult("Success"));

        // Wait for HalfOpen duration and close circuit
        await Task.Delay(60);
        await circuitBreaker.ExecuteAsync(ct => ValueTask.FromResult("Success"));

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        Assert.Contains(CircuitBreakerState.Open, stateChanges);
        Assert.Contains(CircuitBreakerState.HalfOpen, stateChanges);
        Assert.Contains(CircuitBreakerState.Closed, stateChanges);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldOpenCircuitOnConsecutiveFailures_WithoutRateCheck()
    {
        // Arrange - Use high minimum throughput to ensure consecutive failures path is taken
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 3,
            MinimumThroughput = 100, // High value to prevent rate-based opening
            SamplingDuration = TimeSpan.FromHours(1) // Long duration to prevent rate calculation
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Exceed consecutive failure threshold
        for (int i = 0; i < 3; i++)
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

        // Assert - Circuit should be open due to consecutive failures
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
        Assert.Equal(3, circuitBreaker.Metrics.FailedCalls);
    }
}