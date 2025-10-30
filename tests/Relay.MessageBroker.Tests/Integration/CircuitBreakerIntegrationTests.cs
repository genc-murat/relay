using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.MessageBroker.CircuitBreaker;
using Xunit;
using Xunit.Abstractions;

namespace Relay.MessageBroker.Tests.Integration;

/// <summary>
/// Integration tests for Circuit Breaker pattern with simulated failures.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Pattern", "CircuitBreaker")]
public class CircuitBreakerIntegrationTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<CircuitBreakerIntegrationTests> _logger;

    public CircuitBreakerIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        var services = new ServiceCollection();
        services.AddLogging();
        var serviceProvider = services.BuildServiceProvider();
        
        _logger = serviceProvider.GetRequiredService<ILogger<CircuitBreakerIntegrationTests>>();
    }

    [Fact]
    public async Task CircuitBreaker_ShouldTransitionToOpen_AfterFailureThreshold()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 3,
            Timeout = TimeSpan.FromSeconds(5),
            SuccessThreshold = 2
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);
        var failureCount = 0;

        // Act - Trigger failures
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    await Task.Delay(10, ct);
                    throw new InvalidOperationException("Simulated failure");
                }, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                failureCount++;
            }
        }

        // Assert
        Assert.Equal(3, failureCount);
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

        // Verify circuit is open
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
        {
            await circuitBreaker.ExecuteAsync(async ct =>
            {
                await Task.CompletedTask;
                return true;
            }, CancellationToken.None);
        });
    }

    [Fact]
    public async Task CircuitBreaker_ShouldTransitionToHalfOpen_AfterTimeout()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            Timeout = TimeSpan.FromMilliseconds(500),
            SuccessThreshold = 1
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Trigger failures to open circuit
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    throw new InvalidOperationException("Failure");
                }, CancellationToken.None);
            }
            catch { }
        }

        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

        // Act - Wait for timeout
        await Task.Delay(600);

        // Execute a successful operation
        var result = await circuitBreaker.ExecuteAsync(async ct =>
        {
            await Task.CompletedTask;
            return true;
        }, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_ShouldTransitionToClosed_AfterSuccessThreshold()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            Timeout = TimeSpan.FromMilliseconds(500),
            SuccessThreshold = 2
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Open the circuit
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    throw new InvalidOperationException("Failure");
                }, CancellationToken.None);
            }
            catch { }
        }

        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

        // Wait for timeout to enter half-open
        await Task.Delay(600);

        // Act - Execute successful operations
        for (int i = 0; i < 2; i++)
        {
            await circuitBreaker.ExecuteAsync(async ct =>
            {
                await Task.CompletedTask;
                return true;
            }, CancellationToken.None);
        }

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_ShouldReopenFromHalfOpen_OnFailure()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            Timeout = TimeSpan.FromMilliseconds(500),
            SuccessThreshold = 2
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Open the circuit
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    throw new InvalidOperationException("Failure");
                }, CancellationToken.None);
            }
            catch { }
        }

        // Wait for timeout to enter half-open
        await Task.Delay(600);

        // Act - Fail in half-open state
        try
        {
            await circuitBreaker.ExecuteAsync(async ct =>
            {
                throw new InvalidOperationException("Failure in half-open");
            }, CancellationToken.None);
        }
        catch { }

        // Assert
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_WithRetry_ShouldWorkTogether()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 5,
            Timeout = TimeSpan.FromSeconds(2),
            SuccessThreshold = 2
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);
        var attemptCount = 0;
        var maxRetries = 3;

        // Act - Simulate retry logic with circuit breaker
        async Task<bool> OperationWithRetry()
        {
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    return await circuitBreaker.ExecuteAsync(async ct =>
                    {
                        attemptCount++;
                        if (attemptCount < 3)
                        {
                            throw new InvalidOperationException("Transient failure");
                        }
                        return true;
                    }, CancellationToken.None);
                }
                catch (InvalidOperationException) when (retry < maxRetries - 1)
                {
                    await Task.Delay(100);
                    continue;
                }
                catch (CircuitBreakerOpenException)
                {
                    throw;
                }
            }
            return false;
        }

        var result = await OperationWithRetry();

        // Assert
        Assert.True(result);
        Assert.Equal(3, attemptCount);
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public async Task CircuitBreaker_ShouldTrackMetrics()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 10,
            Timeout = TimeSpan.FromSeconds(5)
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Act - Execute mix of successful and failed operations
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    await Task.CompletedTask;
                    return true;
                }, CancellationToken.None);
            }
            catch { }
        }

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

        // Assert
        var metrics = circuitBreaker.Metrics;
        Assert.Equal(8, metrics.TotalCalls);
        Assert.Equal(5, metrics.SuccessfulCalls);
        Assert.Equal(3, metrics.FailedCalls);
    }

    [Fact]
    public async Task CircuitBreaker_Reset_ShouldCloseCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 2,
            Timeout = TimeSpan.FromSeconds(5)
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);

        // Open the circuit
        for (int i = 0; i < 2; i++)
        {
            try
            {
                await circuitBreaker.ExecuteAsync(async ct =>
                {
                    throw new InvalidOperationException("Failure");
                }, CancellationToken.None);
            }
            catch { }
        }

        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

        // Act
        circuitBreaker.Reset();

        // Assert
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);

        // Should be able to execute operations
        var result = await circuitBreaker.ExecuteAsync(async ct =>
        {
            await Task.CompletedTask;
            return true;
        }, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task CircuitBreaker_Isolate_ShouldOpenCircuit()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            Enabled = true,
            FailureThreshold = 10,
            Timeout = TimeSpan.FromSeconds(5)
        };

        var circuitBreaker = new CircuitBreaker.CircuitBreaker(options);
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);

        // Act
        circuitBreaker.Isolate();

        // Assert
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

        await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
        {
            await circuitBreaker.ExecuteAsync(async ct =>
            {
                await Task.CompletedTask;
                return true;
            }, CancellationToken.None);
        });
    }

    [Fact]
    public async Task CircuitBreaker_WithExponentialBackoff_ShouldRecoverGracefully()
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
        var attemptCount = 0;

        // Simulate exponential backoff
        async Task<bool> OperationWithExponentialBackoff()
        {
            var delay = 100;
            var maxAttempts = 5;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    return await circuitBreaker.ExecuteAsync(async ct =>
                    {
                        attemptCount++;
                        if (attemptCount <= 3)
                        {
                            throw new InvalidOperationException("Failure");
                        }
                        return true;
                    }, CancellationToken.None);
                }
                catch (InvalidOperationException)
                {
                    await Task.Delay(delay);
                    delay *= 2; // Exponential backoff
                }
                catch (CircuitBreakerOpenException)
                {
                    // Wait for circuit to potentially enter half-open
                    await Task.Delay(600);
                }
            }

            return false;
        }

        // Act
        var result = await OperationWithExponentialBackoff();

        // Assert
        Assert.True(result);
        Assert.True(attemptCount >= 3);
    }
}
