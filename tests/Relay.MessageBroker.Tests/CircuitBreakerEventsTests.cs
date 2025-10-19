using Relay.MessageBroker.CircuitBreaker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CircuitBreakerEventsTests
{
    [Fact]
    public void OnRejected_ShouldBeInvokedWhenCircuitOpen()
    {
        // Arrange
        var rejectedCount = 0;
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            Timeout = TimeSpan.FromMinutes(1),
            OnRejected = e =>
            {
                rejectedCount++;
                Assert.Equal(CircuitBreakerState.Open, e.CurrentState);
            }
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

        // Act - Try to execute when circuit is open
        try
        {
            circuitBreaker.ExecuteAsync(ct => ValueTask.FromResult("Success")).GetAwaiter().GetResult();
        }
        catch (CircuitBreakerOpenException)
        {
            // Expected
        }

        // Assert
        Assert.Equal(1, rejectedCount);
    }

    [Fact]
    public void OnStateChanged_ShouldBeInvokedWhenStateTransitions()
    {
        // Arrange
        var stateChanges = new List<(CircuitBreakerState Previous, CircuitBreakerState New, string Reason)>();
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            OnStateChanged = e =>
            {
                stateChanges.Add((e.PreviousState, e.NewState, e.Reason));
            }
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Open the circuit
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

        // Assert
        Assert.Single(stateChanges);
        Assert.Equal(CircuitBreakerState.Closed, stateChanges[0].Previous);
        Assert.Equal(CircuitBreakerState.Open, stateChanges[0].New);
        Assert.Contains("Failure threshold reached", stateChanges[0].Reason);
    }
}