using Relay.MessageBroker.CircuitBreaker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CircuitBreakerConcurrencyTests
{
    [Fact]
    public async Task ExecuteAsync_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 1000,
            SuccessThreshold = 1000,
            FailureRateThreshold = 1.0f
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);
        const int numTasks = 100;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < numTasks; i++)
        {
            if (i % 2 == 0)
            {
                tasks.Add(circuitBreaker.ExecuteAsync(ct => ValueTask.FromResult("Success")).AsTask());
            }
            else
            {
                tasks.Add(Task.Run(async () =>
                {
                    try { await circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException()); }
                    catch (InvalidOperationException) { }
                }));
            }
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(numTasks / 2, circuitBreaker.Metrics.SuccessfulCalls);
        Assert.Equal(numTasks / 2, circuitBreaker.Metrics.FailedCalls);
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_InHalfOpenState_ConcurrentCalls_ShouldHandleRaceConditions()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            SuccessThreshold = 3,
            Timeout = TimeSpan.FromMilliseconds(100)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Open the circuit
        for (int i = 0; i < 2; i++)
        {
            try { await circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException()); }
            catch (InvalidOperationException) { }
        }

        // Wait for timeout
        await Task.Delay(150);

        // Act - Multiple concurrent calls in HalfOpen state
        var tasks = new List<Task<string>>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(circuitBreaker.ExecuteAsync(ct => ValueTask.FromResult("Success")).AsTask());
        }

        var results = await Task.WhenAll(tasks);

        // Assert - All should succeed and circuit should close
        Assert.All(results, r => Assert.Equal("Success", r));
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 5
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);
        var cts = new CancellationTokenSource();

        // Act - Cancel immediately
        cts.Cancel();
        Func<Task> act = async () => await circuitBreaker.ExecuteAsync(async ct =>
        {
            await Task.Delay(1000, ct); // This should be cancelled
            return "Success";
        }, cts.Token);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(act);
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }
}