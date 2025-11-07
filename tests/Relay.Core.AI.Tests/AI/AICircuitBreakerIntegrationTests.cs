using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI.CircuitBreaker;
using Relay.Core.AI.CircuitBreaker.Events;
using Relay.Core.AI.CircuitBreaker.Exceptions;
using Relay.Core.AI.CircuitBreaker.Metrics;
using Relay.Core.AI.CircuitBreaker.Options;
using Relay.Core.AI.CircuitBreaker.Telemetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AICircuitBreakerIntegrationTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly List<AICircuitBreaker<string>> _circuitBreakersToDispose;

    public AICircuitBreakerIntegrationTests()
    {
        _logger = NullLogger.Instance;
        _circuitBreakersToDispose = new List<AICircuitBreaker<string>>();
    }

    public void Dispose()
    {
        foreach (var cb in _circuitBreakersToDispose)
        {
            cb.Dispose();
        }
    }

    private AICircuitBreaker<string> CreateCircuitBreaker(AICircuitBreakerOptions? options = null)
    {
        var defaultOptions = new AICircuitBreakerOptions
        {
            FailureThreshold = 3,
            SuccessThreshold = 2,
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1
        };

        var cb = new AICircuitBreaker<string>(options ?? defaultOptions, _logger);
        _circuitBreakersToDispose.Add(cb);
        return cb;
    }

    #region Constructor and Initialization Coverage

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AICircuitBreaker<string>(null!, _logger));
        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new AICircuitBreakerOptions();

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new AICircuitBreaker<string>(options, null!));
        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTelemetry_UsesDefaultTelemetry()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            EnableTelemetry = true
        };

        // Act
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger, null);

        // Assert - Should create LoggingCircuitBreakerTelemetry
        Assert.NotNull(circuitBreaker);
    }

    [Fact]
    public void Constructor_WithTelemetryDisabled_UsesNoOpTelemetry()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            EnableTelemetry = false
        };

        // Act
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger, null);

        // Assert
        Assert.NotNull(circuitBreaker);
    }

    [Fact]
    public void Constructor_WithFailingTelemetry_CreatesCircuitBreaker()
    {
        // Arrange
        var options = new AICircuitBreakerOptions();
        var failingTelemetry = new Mock<ICircuitBreakerTelemetry>();
        failingTelemetry.Setup(t => t.RecordSuccess(It.IsAny<TimeSpan>(), It.IsAny<bool>()))
            .Throws(new InvalidOperationException("Telemetry failure"));

        // Act & Assert - Should not throw during construction
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger, failingTelemetry.Object);
        Assert.NotNull(circuitBreaker);
    }

    [Fact]
    public void Constructor_WithInvalidPolicy_ThrowsArgumentException()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            Policy = (CircuitBreakerPolicy)999 // Invalid policy
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new AICircuitBreaker<string>(options, _logger));
        Assert.Contains("circuit breaker policy", ex.Message);
    }

    #endregion

    #region ExecuteAsync Edge Cases

    [Fact]
    public async Task ExecuteAsync_Timeout_At_Exact_Boundary()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(200);
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 5,
            SuccessThreshold = 2,
            Timeout = timeout,
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1
        };
        var circuitBreaker = CreateCircuitBreaker(options);

        // Operation that completes just before timeout (with some buffer for timing variations)
        Func<CancellationToken, ValueTask<string>> boundaryOperation = async ct =>
        {
            await Task.Delay(timeout - TimeSpan.FromMilliseconds(100), ct);
            return "boundary";
        };

        // Act & Assert - Should succeed if completed before timeout enforcement
        var result = await circuitBreaker.ExecuteAsync(boundaryOperation, CancellationToken.None);
        Assert.Equal("boundary", result);
    }

    [Fact]
    public async Task ExecuteAsync_Timeout_With_Operation_Ignoring_Cancellation()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(100);
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 5,
            SuccessThreshold = 2,
            Timeout = timeout,
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1
        };
        var circuitBreaker = CreateCircuitBreaker(options);

        // Operation that ignores cancellation token
        Func<CancellationToken, ValueTask<string>> ignoringOperation = async ct =>
        {
            await Task.Delay(200, CancellationToken.None); // Ignore ct
            return "ignored";
        };

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () =>
            await circuitBreaker.ExecuteAsync(ignoringOperation, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_ExternalCancellation_DoesNotCountAsFailure()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 1,
            SuccessThreshold = 1,
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1
        };
        var circuitBreaker = CreateCircuitBreaker(options);

        using var cts = new CancellationTokenSource();

        // Operation that respects cancellation
        Func<CancellationToken, ValueTask<string>> cancellableOperation = async ct =>
        {
            await Task.Delay(200, ct);
            return "cancelled";
        };

        // Act - Cancel externally
        var task = circuitBreaker.ExecuteAsync(cancellableOperation, cts.Token);
        cts.Cancel();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);

        // Circuit should still be closed (external cancellation doesn't count as failure)
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);

        var metrics = circuitBreaker.GetMetrics();
        Assert.Equal(0, metrics.TotalCalls); // External cancellation resets the count
    }

    [Fact]
    public async Task ExecuteAsync_TimeoutCancellation_Vs_ExternalCancellation()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 5,
            SuccessThreshold = 2,
            Timeout = TimeSpan.FromMilliseconds(100),
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1
        };
        var circuitBreaker = CreateCircuitBreaker(options);

        using var externalCts = new CancellationTokenSource();

        // Operation that takes longer than timeout but respects external cancellation
        Func<CancellationToken, ValueTask<string>> slowOperation = async ct =>
        {
            await Task.Delay(200, ct); // Longer than timeout, but respects ct
            return "slow";
        };

        // Act - Start operation, then cancel externally before timeout
        var task = circuitBreaker.ExecuteAsync(slowOperation, externalCts.Token);
        await Task.Delay(50); // Let operation start
        externalCts.Cancel();

        // Assert - Should get TaskCanceledException, not TimeoutException
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
    }

    [Fact]
    public async Task ExecuteAsync_ConcurrentCalls_DuringStateTransition()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 1,
            SuccessThreshold = 10, // Higher than HalfOpenMaxCalls so it doesn't close immediately
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromMilliseconds(100),
            HalfOpenMaxCalls = 5
        };
        var circuitBreaker = CreateCircuitBreaker(options);

        // Open the circuit
        Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
            throw new InvalidOperationException("Test failure");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

        // Wait for half-open transition
        await Task.Delay(200);

        // Act - Execute multiple concurrent calls during half-open state
        Func<CancellationToken, ValueTask<string>> successOperation = ct =>
            new ValueTask<string>("success");

        var tasks = new List<Task<string>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None).AsTask());
        }

        // Assert - Some should succeed, some should be rejected
        var results = await Task.WhenAll(tasks.Select(t => t.ContinueWith(task =>
            task.Status == TaskStatus.RanToCompletion ? task.Result : null)));

        var successfulResults = results.Count(r => r != null);
        var rejectedResults = results.Count(r => r == null);

        Assert.True(successfulResults > 0, "At least some calls should succeed");
        Assert.True(rejectedResults > 0, "Some calls should be rejected due to HalfOpenMaxCalls limit");
    }

    [Fact]
    public async Task ExecuteAsync_CustomExceptionTypes_AreHandled()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 5, // Higher threshold to test multiple exception types before opening
            SuccessThreshold = 1,
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1
        };
        var circuitBreaker = CreateCircuitBreaker(options);

        // Test various exception types
        var exceptionTypes = new Exception[]
        {
            new ArgumentException("Argument error"),
            new InvalidOperationException("Operation error"),
            new AggregateException("Aggregate error", new Exception("Inner")),
            new CustomTestException("Custom error")
        };

        foreach (var exception in exceptionTypes)
        {
            // Act & Assert - Should handle any exception type
            Func<CancellationToken, ValueTask<string>> failingOperation = ct => throw exception;

            await Assert.ThrowsAsync(exception.GetType(), async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));
        }

        // Circuit should still be closed (not enough failures to open)
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    #endregion

    #region State Management Concurrency

    [Fact]
    public async Task State_Property_IsThreadSafe()
    {
        // Arrange
        var circuitBreaker = CreateCircuitBreaker();

        // Act - Access State property concurrently
        var tasks = new List<Task<CircuitBreakerState>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => circuitBreaker.State));
        }

        // Assert - All should complete without exception
        var results = await Task.WhenAll(tasks);
        Assert.All(results, state => Assert.True(Enum.IsDefined(typeof(CircuitBreakerState), state)));
    }

    [Fact]
    public async Task AutomaticTransition_HandlesRapidStateChanges()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 1,
            SuccessThreshold = 1,
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromMilliseconds(50),
            HalfOpenMaxCalls = 1
        };
        var circuitBreaker = CreateCircuitBreaker(options);

        // Act - Rapid open/close cycles
        for (int cycle = 0; cycle < 5; cycle++)
        {
            // Open circuit
            Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
                throw new InvalidOperationException($"Failure {cycle}");

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

            Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

            // Wait for automatic half-open transition
            await Task.Delay(100);

            // Close circuit
            Func<CancellationToken, ValueTask<string>> successOperation = ct =>
                new ValueTask<string>($"Success {cycle}");

            await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);
            Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
        }
    }

    [Fact]
    public async Task CheckForAutomaticTransition_IsIdempotent()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 1,
            SuccessThreshold = 1,
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromMilliseconds(100),
            HalfOpenMaxCalls = 1
        };
        var circuitBreaker = CreateCircuitBreaker(options);

        // Open circuit
        Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
            throw new InvalidOperationException("Test failure");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

        // Wait for automatic transition to occur
        await Task.Delay(150);

        // Act - Call CheckForAutomaticTransition multiple times concurrently
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                // Access private method via reflection
                var method = typeof(AICircuitBreaker<string>).GetMethod("CheckForAutomaticTransitionAsync",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                await (Task)method!.Invoke(circuitBreaker, null)!;
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should still be in half-open (transition should have happened automatically)
        Assert.Equal(CircuitBreakerState.HalfOpen, circuitBreaker.State);
    }

    #endregion

    #region Event System Comprehensive

    [Fact]
    public async Task Events_SupportMultipleSubscribers()
    {
        // Arrange
        var circuitBreaker = CreateCircuitBreaker();

        var successEvents = new List<CircuitBreakerSuccessEventArgs>();
        var failureEvents = new List<CircuitBreakerFailureEventArgs>();
        var stateChangeEvents = new List<CircuitBreakerStateChangedEventArgs>();
        var rejectedEvents = new List<CircuitBreakerRejectedEventArgs>();

        // Add multiple subscribers
        for (int i = 0; i < 3; i++)
        {
            circuitBreaker.OperationSucceeded += (sender, args) => successEvents.Add(args);
            circuitBreaker.OperationFailed += (sender, args) => failureEvents.Add(args);
            circuitBreaker.StateChanged += (sender, args) => stateChangeEvents.Add(args);
            circuitBreaker.CallRejected += (sender, args) => rejectedEvents.Add(args);
        }

        // Act - Trigger various events
        Func<CancellationToken, ValueTask<string>> successOperation = ct =>
            new ValueTask<string>("success");

        Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
            throw new InvalidOperationException("Test failure");

        // Success event
        await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);

        // Failure events to open circuit
        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));
        }

        // Rejected event
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
            await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None));

        // Assert - Each event should have been fired multiple times
        Assert.Equal(3, successEvents.Count);
        Assert.Equal(9, failureEvents.Count); // 3 failures * 3 subscribers
        Assert.True(stateChangeEvents.Count >= 3); // At least state changes * subscribers
        Assert.Equal(3, rejectedEvents.Count);
    }

    [Fact]
    public async Task EventHandlers_CanThrowExceptions_WithoutBreakingCircuitBreaker()
    {
        // Arrange
        var circuitBreaker = CreateCircuitBreaker();

        // Add event handler that throws
        circuitBreaker.OperationSucceeded += (sender, args) =>
            throw new InvalidOperationException("Event handler failure");

        // Act
        Func<CancellationToken, ValueTask<string>> successOperation = ct =>
            new ValueTask<string>("success");

        // Assert - Operation should succeed despite event handler failure
        var result = await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);
        Assert.Equal("success", result);
    }

    [Fact]
    public async Task Events_Fire_InCorrectOrder()
    {
        // Arrange
        var circuitBreaker = CreateCircuitBreaker();

        var eventOrder = new List<string>();
        circuitBreaker.StateChanged += (sender, args) =>
            eventOrder.Add($"StateChanged: {args.PreviousState} -> {args.NewState}");
        circuitBreaker.OperationFailed += (sender, args) =>
            eventOrder.Add("OperationFailed");
        circuitBreaker.CallRejected += (sender, args) =>
            eventOrder.Add("CallRejected");

        // Act - Open circuit and make rejected call
        Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
            throw new InvalidOperationException("Test failure");

        Func<CancellationToken, ValueTask<string>> successOperation = ct =>
            new ValueTask<string>("success");

        // Fail to open circuit
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

        // Rejected call
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
            await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None));

        // Assert - Events should fire in logical order
        Assert.Contains("StateChanged: Closed -> Open", eventOrder);
        Assert.Contains("OperationFailed", eventOrder);
        Assert.Contains("CallRejected", eventOrder);
    }

    #endregion

    #region Metrics Edge Cases

    [Fact]
    public async Task Metrics_ResponseTimeStatistics_HandleEmptyQueue()
    {
        // Arrange
        var circuitBreaker = CreateCircuitBreaker();

        // Act - Get metrics without any operations
        var metrics = circuitBreaker.GetMetrics();

        // Assert
        Assert.Equal(0, metrics.AverageResponseTimeMs);
        Assert.Equal(0, metrics.Percentile95ResponseTimeMs);
    }

    [Fact]
    public async Task Metrics_ResponseTimeStatistics_CalculateCorrectly()
    {
        // Arrange
        var circuitBreaker = CreateCircuitBreaker();

        // Add operations with known response times
        var responseTimes = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
        var total = responseTimes.Sum();
        var expectedAverage = (double)total / responseTimes.Length;

        // Sort for percentile calculation
        Array.Sort(responseTimes);
        // For 95th percentile with nearest rank method: position = n * p / 100
        // For n=10, p=95: position = 9.5, round to 10, so index 9 (0-based)
        var expected95thPercentile = responseTimes[9]; // 100

        foreach (var responseTime in responseTimes)
        {
            Func<CancellationToken, ValueTask<string>> operation = async ct =>
            {
                await Task.Delay(responseTime, ct);
                return "test";
            };

            await circuitBreaker.ExecuteAsync(operation, CancellationToken.None);
        }

        // Act
        var metrics = circuitBreaker.GetMetrics();

        // Assert - Allow some tolerance for timing variations
        Assert.True(Math.Abs(expectedAverage - metrics.AverageResponseTimeMs) < 50, $"Average expected {expectedAverage}, got {metrics.AverageResponseTimeMs}");
        Assert.True(Math.Abs(expected95thPercentile - metrics.Percentile95ResponseTimeMs) < 50, $"95th percentile expected {expected95thPercentile}, got {metrics.Percentile95ResponseTimeMs}");
    }

    [Fact]
    public async Task Metrics_StateTiming_AccumulatesCorrectly()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 1,
            SuccessThreshold = 1,
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromMilliseconds(200),
            HalfOpenMaxCalls = 1
        };
        var circuitBreaker = CreateCircuitBreaker(options);

        var startTime = DateTime.UtcNow;

        // Act - Go through state transitions
        Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
            throw new InvalidOperationException("Test failure");

        Func<CancellationToken, ValueTask<string>> successOperation = ct =>
            new ValueTask<string>("success");

        // Open circuit
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

        // Wait in open state
        await Task.Delay(100);

        // Transition to half-open and close
        await Task.Delay(250); // Wait for break duration
        await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);

        var metrics = circuitBreaker.GetMetrics();

        // Assert - Should have accumulated time in different states
        Assert.True(metrics.TotalOpenTime > TimeSpan.Zero);
        Assert.True(metrics.TotalClosedTime >= TimeSpan.Zero);
        Assert.True(metrics.TotalHalfOpenTime >= TimeSpan.Zero);

        // Total time should roughly equal elapsed time
        var totalStateTime = metrics.TotalOpenTime + metrics.TotalClosedTime + metrics.TotalHalfOpenTime;
        var elapsed = DateTime.UtcNow - startTime;
        Assert.True(totalStateTime <= elapsed + TimeSpan.FromSeconds(1)); // Allow some tolerance
    }

    [Fact]
    public async Task Metrics_HandleBoundaryConditions()
    {
        // Arrange
        var circuitBreaker = CreateCircuitBreaker();

        // Act - Mix of operations
        Func<CancellationToken, ValueTask<string>> successOperation = ct =>
            new ValueTask<string>("success");

        Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
            throw new InvalidOperationException("Test failure");

        // Execute operations
        await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

        var metrics = circuitBreaker.GetMetrics();

        // Assert - Boundary calculations
        Assert.Equal(2, metrics.TotalCalls);
        Assert.Equal(1, metrics.SuccessfulCalls);
        Assert.Equal(1, metrics.FailedCalls);
        Assert.Equal(0, metrics.RejectedCalls);
        Assert.Equal(2, metrics.EffectiveCalls); // Total - Rejected

        // Rates should be between 0 and 1
        Assert.True(metrics.SuccessRate >= 0 && metrics.SuccessRate <= 1);
        Assert.True(metrics.FailureRate >= 0 && metrics.FailureRate <= 1);
        Assert.True(metrics.Availability >= 0 && metrics.Availability <= 1);
    }

    #endregion

    #region Strategy Integration

    [Fact]
    public async Task Strategy_Failure_DoesNotBreakCircuitBreaker()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 2,
            SuccessThreshold = 1,
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1,
            Policy = CircuitBreakerPolicy.Standard
        };
        var circuitBreaker = CreateCircuitBreaker(options);

        // Act - Execute operations (strategy should work normally)
        Func<CancellationToken, ValueTask<string>> successOperation = ct =>
            new ValueTask<string>("success");

        Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
            throw new InvalidOperationException("Test failure");

        await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));

        // Assert - Circuit should be open despite any strategy issues
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);
    }

    [Fact]
    public void DifferentPolicies_InitializeCorrectly()
    {
        // Arrange & Act
        var policies = Enum.GetValues(typeof(CircuitBreakerPolicy))
            .Cast<CircuitBreakerPolicy>()
            .Where(p => p != CircuitBreakerPolicy.Standard); // Skip default

        foreach (var policy in policies)
        {
            var options = new AICircuitBreakerOptions
            {
                Policy = policy,
                FailureThreshold = 3,
                SuccessThreshold = 2,
                Timeout = TimeSpan.FromSeconds(5),
                BreakDuration = TimeSpan.FromSeconds(10),
                HalfOpenMaxCalls = 1
            };

            // Assert - Should initialize without throwing
            var circuitBreaker = CreateCircuitBreaker(options);
            Assert.NotNull(circuitBreaker);
        }
    }

    #endregion

    #region Error Handling and Resilience

    [Fact]
    public async Task TelemetryFailures_DoNotBreakOperations()
    {
        // Arrange
        var options = new AICircuitBreakerOptions();
        var failingTelemetry = new Mock<ICircuitBreakerTelemetry>();
        failingTelemetry.Setup(t => t.RecordSuccess(It.IsAny<TimeSpan>(), It.IsAny<bool>()))
            .Throws(new InvalidOperationException("Telemetry failure"));
        failingTelemetry.Setup(t => t.RecordFailure(It.IsAny<Exception>(), It.IsAny<TimeSpan>(), It.IsAny<bool>()))
            .Throws(new InvalidOperationException("Telemetry failure"));

        var circuitBreaker = new AICircuitBreaker<string>(options, _logger, failingTelemetry.Object);
        _circuitBreakersToDispose.Add(circuitBreaker);

        // Act
        Func<CancellationToken, ValueTask<string>> successOperation = ct =>
            new ValueTask<string>("success");

        Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
            throw new InvalidOperationException("Operation failure");

        // Assert - Operations should complete despite telemetry failures
        var result = await circuitBreaker.ExecuteAsync(successOperation, CancellationToken.None);
        Assert.Equal("success", result);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        // Arrange
        var circuitBreaker = CreateCircuitBreaker();

        // Act - Dispose multiple times
        circuitBreaker.Dispose();
        circuitBreaker.Dispose();
        circuitBreaker.Dispose();

        // Assert - No exceptions
    }

    [Fact]
    public async Task ConcurrentMetricsAccess_IsSafe()
    {
        // Arrange
        var circuitBreaker = CreateCircuitBreaker();

        // Add some operations
        Func<CancellationToken, ValueTask<string>> operation = ct =>
            new ValueTask<string>("test");

        await circuitBreaker.ExecuteAsync(operation, CancellationToken.None);

        // Act - Access metrics concurrently
        var tasks = new List<Task<CircuitBreakerMetrics>>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => circuitBreaker.GetMetrics()));
        }

        // Assert - All should complete without exception
        var results = await Task.WhenAll(tasks);
        Assert.All(results, metrics => Assert.NotNull(metrics));
        Assert.All(results, metrics => Assert.Equal(1, metrics.TotalCalls));
    }

    #endregion

    #region Performance and Load Testing

    [Fact]
    public async Task HighThroughputOperations_MaintainCorrectness()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 50,
            SuccessThreshold = 10,
            Timeout = TimeSpan.FromSeconds(1),
            BreakDuration = TimeSpan.FromSeconds(5),
            HalfOpenMaxCalls = 10
        };
        var circuitBreaker = CreateCircuitBreaker(options);

        // Act - Execute many operations concurrently
        const int operationCount = 100;
        var tasks = new List<Task<string>>();

        for (int i = 0; i < operationCount; i++)
        {
            var operation = CreateOperation(i < 80); // 80% success rate
            tasks.Add(circuitBreaker.ExecuteAsync(operation, CancellationToken.None).AsTask());
        }

        // Assert - Tasks may succeed or fail as expected
        var completedTasks = await Task.WhenAll(tasks.Select(t => t.ContinueWith(task =>
            task.Status == TaskStatus.RanToCompletion ? task.Result : null)));

        var successfulResults = completedTasks.Count(r => r != null);
        var failedResults = completedTasks.Count(r => r == null);

        Assert.True(successfulResults > 0, "At least some operations should succeed");
        Assert.True(failedResults > 0, "Some operations should fail as expected");

        var metrics = circuitBreaker.GetMetrics();
        Assert.Equal(operationCount, metrics.TotalCalls);
        Assert.True(metrics.SuccessfulCalls > 0);
    }

    [Fact]
    public async Task MemoryUsage_ResponseTimeQueue_LimitsGrowth()
    {
        // Arrange
        var circuitBreaker = CreateCircuitBreaker();

        // Act - Execute many operations to fill response time queue
        Func<CancellationToken, ValueTask<string>> operation = ct =>
            new ValueTask<string>("test");

        const int operationCount = 2000; // More than MaxResponseTimeSamples (1000)
        for (int i = 0; i < operationCount; i++)
        {
            await circuitBreaker.ExecuteAsync(operation, CancellationToken.None);
        }

        // Assert - Metrics should still be calculable
        var metrics = circuitBreaker.GetMetrics();
        Assert.Equal(operationCount, metrics.TotalCalls);
        Assert.True(metrics.AverageResponseTimeMs >= 0);
    }

    #endregion

    #region Helper Methods

    private Func<CancellationToken, ValueTask<string>> CreateOperation(bool shouldSucceed)
    {
        if (shouldSucceed)
        {
            return ct => new ValueTask<string>("success");
        }
        else
        {
            return ct => throw new InvalidOperationException("Test failure");
        }
    }

    #endregion
}

public class CustomTestException : Exception
{
    public CustomTestException(string message) : base(message) { }
}