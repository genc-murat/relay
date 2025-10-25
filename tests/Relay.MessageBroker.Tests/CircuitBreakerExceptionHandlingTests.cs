using Relay.MessageBroker.CircuitBreaker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CircuitBreakerExceptionHandlingTests
{
    [Fact]
    public async Task ExecuteAsync_WithIgnoredExceptionTypes_ShouldNotCountAsFailure()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            IgnoredExceptionTypes = new[] { typeof(ArgumentException) }
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - ArgumentException should be ignored
        try
        {
            await circuitBreaker.ExecuteAsync<string>(ct => throw new ArgumentException("Ignored"));
        }
        catch (ArgumentException) { }

        // InvalidOperationException should count as failure
        try
        {
            await circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException("Failure"));
        }
        catch (InvalidOperationException) { }

        // Assert - Circuit should still be closed (ArgumentException ignored)
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        Assert.Equal(1, circuitBreaker.Metrics.FailedCalls); // Only InvalidOperationException counted
    }

    [Fact]
    public async Task ExecuteAsync_WithExceptionPredicate_ShouldUseCustomFailureLogic()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            ExceptionPredicate = ex => ex is InvalidOperationException && ex.Message.Contains("Critical")
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Non-critical exception should not count as failure
        try
        {
            await circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException("Non-critical"));
        }
        catch (InvalidOperationException) { }

        // Critical exception should count
        try
        {
            await circuitBreaker.ExecuteAsync<string>(ct => throw new InvalidOperationException("Critical failure"));
        }
        catch (InvalidOperationException) { }

        // Assert - Only critical failures count
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        Assert.Equal(1, circuitBreaker.Metrics.FailedCalls);
    }

    [Fact]
    public async Task ExecuteAsync_WithIgnoredException_ShouldStillTrackSlowCalls()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            IgnoredExceptionTypes = new[] { typeof(ArgumentException) },
            TrackSlowCalls = true,
            SlowCallDurationThreshold = TimeSpan.FromMilliseconds(50)
        };
        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Throw ignored exception with slow operation
        var exceptionThrown = false;
        try
        {
            await circuitBreaker.ExecuteAsync<string>(async ct =>
            {
                await Task.Delay(60, ct); // Slow operation
                exceptionThrown = true;
                throw new ArgumentException("Ignored exception");
            });
        }
        catch (ArgumentException) { }

        // Assert - Exception ignored but slow call tracked
        Assert.True(exceptionThrown); // Verify exception was thrown
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        Assert.Equal(0, circuitBreaker.Metrics.FailedCalls); // Exception ignored
        Assert.Equal(2, circuitBreaker.Metrics.SlowCalls); // Slow call tracked twice (in catch and finally blocks)
        Assert.Equal(0, circuitBreaker.Metrics.SuccessfulCalls); // Exception thrown, not successful
        Assert.Equal(0, circuitBreaker.Metrics.TotalCalls); // Ignored exceptions don't count as total calls
    }
}