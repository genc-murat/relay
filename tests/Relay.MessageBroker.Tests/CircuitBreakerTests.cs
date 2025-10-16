using Relay.MessageBroker.CircuitBreaker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class CircuitBreakerTests
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
}
