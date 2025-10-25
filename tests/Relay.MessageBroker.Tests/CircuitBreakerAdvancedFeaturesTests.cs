using Relay.MessageBroker.CircuitBreaker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CircuitBreakerAdvancedFeaturesTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldTrackSlowCalls()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            TrackSlowCalls = true,
            SlowCallDurationThreshold = TimeSpan.FromMilliseconds(50)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Execute slow operation
        await circuitBreaker.ExecuteAsync(async ct =>
        {
            await Task.Delay(60, ct);
            return "Success";
        });

        // Assert
        Assert.Equal(1, circuitBreaker.Metrics.SlowCalls);
        Assert.Equal(1, circuitBreaker.Metrics.SuccessfulCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailureRateThreshold_ShouldOpenCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            MinimumThroughput = 5,
            FailureRateThreshold = 0.5, // 50% failure rate
            FailureThreshold = 100, // High value to test rate instead
            SamplingDuration = TimeSpan.FromSeconds(10)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - 3 successes, 3 failures = 50% failure rate with 6 total calls
        for (int i = 0; i < 3; i++)
        {
            await circuitBreaker.ExecuteAsync(ct => ValueTask.FromResult("Success"));
        }

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
        Assert.True(circuitBreaker.Metrics.TotalCalls >= 6);
    }

    [Fact]
    public async Task ExecuteAsync_WithSlowCallRateThreshold_ShouldOpenCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            MinimumThroughput = 2,
            SlowCallRateThreshold = 0.5f, // 50% slow call rate
            SlowCallDurationThreshold = TimeSpan.FromMilliseconds(50),
            TrackSlowCalls = true,
            FailureThreshold = 100, // High value to test rate instead
            SamplingDuration = TimeSpan.FromSeconds(10)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - 2 slow calls
        await circuitBreaker.ExecuteAsync(ct => new ValueTask(Task.Delay(100, ct)));
        await circuitBreaker.ExecuteAsync(ct => new ValueTask(Task.Delay(100, ct)));

        // Assert
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMinimumThroughputNotReached_ShouldNotOpenCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            MinimumThroughput = 5,
            FailureRateThreshold = 0.5f, // 50% failure rate
            FailureThreshold = 100, // High value to test rate instead
            SamplingDuration = TimeSpan.FromSeconds(10)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - 2 failures, less than MinimumThroughput
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException());
            }
            catch (InvalidOperationException) { }
        }

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldOpenCircuitOnSlowCallRateThreshold()
    {
        // Arrange - Configure to trigger slow call rate opening
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            MinimumThroughput = 3,
            SlowCallRateThreshold = 0.6f, // 60% slow call rate
            SlowCallDurationThreshold = TimeSpan.FromMilliseconds(50),
            TrackSlowCalls = true,
            FailureThreshold = 100, // High value to ensure slow call rate triggers opening
            SamplingDuration = TimeSpan.FromSeconds(10)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Execute operations where 2 out of 3 are slow (66% > 60% threshold)
        await circuitBreaker.ExecuteAsync(async ct => { await Task.Delay(10, ct); }); // Fast
        await circuitBreaker.ExecuteAsync(async ct => { await Task.Delay(60, ct); }); // Slow
        await circuitBreaker.ExecuteAsync(async ct => { await Task.Delay(60, ct); }); // Slow

        // Assert - Circuit should be open due to slow call rate threshold
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
        Assert.Equal(2, circuitBreaker.Metrics.SlowCalls);
        Assert.Equal(3, circuitBreaker.Metrics.TotalCalls);
    }
}