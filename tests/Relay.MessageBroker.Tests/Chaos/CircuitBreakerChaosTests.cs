using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.MessageBroker.CircuitBreaker;
using Xunit;
using Xunit.Abstractions;

namespace Relay.MessageBroker.Tests.Chaos;

/// <summary>
/// Chaos engineering tests for Circuit Breaker pattern.
/// Tests circuit breaker behavior under failure conditions and sustained load.
/// </summary>
[Trait("Category", "Chaos")]
[Trait("Pattern", "CircuitBreaker")]
public class CircuitBreakerChaosTests
{
    private readonly ITestOutputHelper _output;

    public CircuitBreakerChaosTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task CircuitBreaker_OpensAfterFailureThreshold_UnderSustainedLoad()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 5,
            Timeout = TimeSpan.FromSeconds(2),
            SuccessThreshold = 3
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);
        var failureCount = 0;
        var successCount = 0;

        // Act - Simulate sustained load with failures
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await circuitBreaker.ExecuteAsync(async ct =>
                    {
                        await Task.Delay(10, ct);
                        throw new InvalidOperationException("Simulated failure");
                    }, CancellationToken.None);
                    Interlocked.Increment(ref successCount);
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failureCount);
                }
                catch (CircuitBreakerOpenException)
                {
                    // Circuit opened, expected
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
        Assert.True(failureCount >= options.FailureThreshold);
        _output.WriteLine($"Failures: {failureCount}, Circuit State: {circuitBreaker.State}");
    }

    [Fact]
    public async Task CircuitBreaker_TransitionsToHalfOpen_AfterTimeout()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 3,
            Timeout = TimeSpan.FromMilliseconds(500),
            SuccessThreshold = 2
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Trigger failures to open circuit
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    throw new InvalidOperationException("Failure");
                }, CancellationToken.None);
            }
            catch (InvalidOperationException) { }
        }

        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
        _output.WriteLine($"Circuit opened at: {DateTimeOffset.UtcNow}");

        // Wait for timeout
        await Task.Delay(600);

        // Execute operation to transition to half-open
        var halfOpenDetected = false;
        try
        {
            await circuitBreaker.ExecuteAsync(async ct =>
            {
                halfOpenDetected = true;
                await Task.CompletedTask;
                return true;
            }, CancellationToken.None);
        }
        catch { }

        // Assert
        Assert.True(halfOpenDetected, "Circuit should have transitioned to half-open");
        _output.WriteLine($"Circuit transitioned to half-open at: {DateTimeOffset.UtcNow}");
    }

    [Fact]
    public async Task CircuitBreaker_ClosesAfterSuccessThreshold_InHalfOpenState()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 3,
            Timeout = TimeSpan.FromMilliseconds(500),
            SuccessThreshold = 3
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Open the circuit
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    throw new InvalidOperationException("Failure");
                }, CancellationToken.None);
            }
            catch (InvalidOperationException) { }
        }

        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

        // Wait for timeout to enter half-open
        await Task.Delay(600);

        // Act - Execute successful operations to close circuit
        for (int i = 0; i < 3; i++)
        {
            await circuitBreaker.ExecuteAsync(async ct =>
            {
                await Task.CompletedTask;
                return true;
            }, CancellationToken.None);
        }

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        _output.WriteLine($"Circuit closed after {options.SuccessThreshold} successful operations");
    }

    [Fact]
    public async Task CircuitBreaker_BehavesCorrectly_UnderSustainedLoad()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 10,
            Timeout = TimeSpan.FromSeconds(1),
            SuccessThreshold = 5
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);
        var totalOperations = 100;
        var successCount = 0;
        var failureCount = 0;
        var circuitOpenCount = 0;

        // Act - Simulate sustained load with mix of success and failures
        var tasks = new List<Task>();
        for (int i = 0; i < totalOperations; i++)
        {
            var operationIndex = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await circuitBreaker.ExecuteAsync(async ct =>
                    {
                        await Task.Delay(5, ct);
                        // Fail 30% of operations
                        if (operationIndex % 10 < 3)
                        {
                            throw new InvalidOperationException("Simulated failure");
                        }
                        return true;
                    }, CancellationToken.None);
                    Interlocked.Increment(ref successCount);
                }
                catch (InvalidOperationException)
                {
                    Interlocked.Increment(ref failureCount);
                }
                catch (CircuitBreakerOpenException)
                {
                    Interlocked.Increment(ref circuitOpenCount);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var metrics = circuitBreaker.Metrics;
        Assert.True(metrics.TotalCalls > 0);
        Assert.True(successCount > 0);
        _output.WriteLine($"Total: {totalOperations}, Success: {successCount}, Failures: {failureCount}, Circuit Open Rejections: {circuitOpenCount}");
        _output.WriteLine($"Metrics - Total: {metrics.TotalCalls}, Success: {metrics.SuccessfulCalls}, Failed: {metrics.FailedCalls}");
    }

    [Fact]
    public async Task CircuitBreaker_HandlesRapidStateTransitions()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            Timeout = TimeSpan.FromMilliseconds(200),
            SuccessThreshold = 2
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);
        var stateChanges = new List<CircuitBreakerState>();

        // Act - Rapidly cycle through states
        for (int cycle = 0; cycle < 3; cycle++)
        {
            // Open circuit
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    await circuitBreaker.ExecuteAsync(async ct =>
                    {
                        throw new InvalidOperationException("Failure");
                    }, CancellationToken.None);
                }
                catch (InvalidOperationException) { }
            }
            stateChanges.Add(circuitBreaker.State);

            // Wait for half-open
            await Task.Delay(250);

            // Close circuit
            for (int i = 0; i < 2; i++)
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    await Task.CompletedTask;
                    return true;
                }, CancellationToken.None);
            }
            stateChanges.Add(circuitBreaker.State);
        }

        // Assert
        Assert.Contains(CircuitBreakerState.Open, stateChanges);
        Assert.Contains(CircuitBreakerState.Closed, stateChanges);
        _output.WriteLine($"State transitions: {string.Join(" -> ", stateChanges)}");
    }

    [Fact]
    public async Task CircuitBreaker_MaintainsCorrectMetrics_UnderConcurrentLoad()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 50,
            Timeout = TimeSpan.FromSeconds(5),
            SuccessThreshold = 10
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);
        var concurrentOperations = 50;
        var expectedSuccesses = 0;
        var expectedFailures = 0;

        // Act - Execute concurrent operations
        var tasks = new List<Task>();
        for (int i = 0; i < concurrentOperations; i++)
        {
            var shouldFail = i % 3 == 0;
            if (shouldFail)
                expectedFailures++;
            else
                expectedSuccesses++;

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await circuitBreaker.ExecuteAsync(async ct =>
                    {
                        await Task.Delay(10, ct);
                        if (shouldFail)
                        {
                            throw new InvalidOperationException("Failure");
                        }
                        return true;
                    }, CancellationToken.None);
                }
                catch (InvalidOperationException) { }
                catch (CircuitBreakerOpenException) { }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var metrics = circuitBreaker.Metrics;
        Assert.Equal(concurrentOperations, metrics.TotalCalls);
        Assert.Equal(expectedSuccesses, metrics.SuccessfulCalls);
        Assert.Equal(expectedFailures, metrics.FailedCalls);
        _output.WriteLine($"Metrics - Total: {metrics.TotalCalls}, Success: {metrics.SuccessfulCalls}, Failed: {metrics.FailedCalls}");
    }

    [Fact]
    public async Task CircuitBreaker_RecoversProperly_AfterExtendedOutage()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 3,
            Timeout = TimeSpan.FromMilliseconds(500),
            SuccessThreshold = 2
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Simulate extended outage
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    throw new InvalidOperationException("Outage");
                }, CancellationToken.None);
            }
            catch (InvalidOperationException) { }
        }

        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
        _output.WriteLine("Circuit opened due to outage");

        // Simulate extended outage period
        await Task.Delay(1000);

        // Service recovers
        var recoverySuccessful = true;
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    await Task.CompletedTask;
                    return true;
                }, CancellationToken.None);
            }
            catch
            {
                recoverySuccessful = false;
            }
        }

        // Assert
        Assert.True(recoverySuccessful);
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        _output.WriteLine("Circuit recovered and closed");
    }

    [Fact]
    public async Task CircuitBreaker_HandlesIntermittentFailures_UnderLoad()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 5,
            Timeout = TimeSpan.FromMilliseconds(500),
            SuccessThreshold = 3
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);
        var operationCount = 0;

        // Act - Simulate intermittent failures
        var tasks = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var opIndex = Interlocked.Increment(ref operationCount);
                try
                {
                    await circuitBreaker.ExecuteAsync(async ct =>
                    {
                        await Task.Delay(10, ct);
                        // Intermittent failures (every 4th operation fails)
                        if (opIndex % 4 == 0)
                        {
                            throw new InvalidOperationException("Intermittent failure");
                        }
                        return true;
                    }, CancellationToken.None);
                }
                catch (InvalidOperationException) { }
                catch (CircuitBreakerOpenException) { }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var metrics = circuitBreaker.Metrics;
        Assert.True(metrics.SuccessfulCalls > 0);
        Assert.True(metrics.FailedCalls > 0);
        _output.WriteLine($"Handled intermittent failures - Success: {metrics.SuccessfulCalls}, Failed: {metrics.FailedCalls}, State: {circuitBreaker.State}");
    }
}
