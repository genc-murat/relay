using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI.CircuitBreaker;
using Relay.Core.AI.CircuitBreaker.Exceptions;
using Relay.Core.AI.CircuitBreaker.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AICircuitBreakerExecutionTests
{
    private readonly ILogger _logger;

    public AICircuitBreakerExecutionTests()
    {
        _logger = NullLogger.Instance;
    }

    [Fact]
    public async Task ExecuteAsync_Should_Execute_Successfully_When_Closed()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 3,
            SuccessThreshold = 2,
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1
        };
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        var executed = false;
        Func<CancellationToken, ValueTask<string>> operation = ct =>
        {
            executed = true;
            return new ValueTask<string>("success");
        };

        // Act
        var result = await circuitBreaker.ExecuteAsync(operation, CancellationToken.None);

        // Assert
        Assert.True(executed);
        Assert.Equal("success", result);
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Open_Circuit_After_Threshold_Failures()
    {
        // Arrange
        var failureThreshold = 3;
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = failureThreshold,
            SuccessThreshold = 2,
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1
        };
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
            throw new InvalidOperationException("Test failure");

        // Act - Execute failing operation multiple times
        for (int i = 0; i < failureThreshold; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));
        }

        // Assert - Circuit should be open now
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State);

        // Further calls should throw CircuitBreakerOpenException
        await Assert.ThrowsAsync<CircuitBreakerOpenException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Timeout_Slow_Operations()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(100);
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 3,
            SuccessThreshold = 2,
            Timeout = timeout,
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1
        };
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        Func<CancellationToken, ValueTask<string>> slowOperation = async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            return "slow";
        };

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () =>
            await circuitBreaker.ExecuteAsync(slowOperation, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Record_Slow_Calls()
    {
        // Arrange
        var timeout = TimeSpan.FromMilliseconds(200);
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 10,
            SuccessThreshold = 2,
            Timeout = timeout,
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1,
            SlowCallThreshold = 0.5 // 50% of timeout = 100ms
        };
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        // Slow operation (150ms > 100ms threshold)
        Func<CancellationToken, ValueTask<string>> slowOperation = async ct =>
        {
            await Task.Delay(150, ct);
            return "slow";
        };

        // Act
        var result = await circuitBreaker.ExecuteAsync(slowOperation, CancellationToken.None);

        // Assert
        Assert.Equal("slow", result);
        var metrics = circuitBreaker.GetMetrics();
        Assert.Equal(1, metrics.SlowCalls);
        Assert.Equal(1, metrics.TotalCalls);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Timeout()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 5,
            SuccessThreshold = 3,
            Timeout = TimeSpan.FromMilliseconds(100),
            BreakDuration = TimeSpan.FromMinutes(1),
            HalfOpenMaxCalls = 3
        };
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        Func<CancellationToken, ValueTask<string>> slowOperation = async ct =>
        {
            await Task.Delay(200, ct); // Longer than timeout
            return "completed";
        };

        // Act & Assert
        await Assert.ThrowsAsync<TimeoutException>(async () => await circuitBreaker.ExecuteAsync(slowOperation, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_Should_Work_With_NoOp_Telemetry_Disabled()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 3,
            SuccessThreshold = 2,
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1,
            EnableTelemetry = false
        };
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        var executed = false;
        Func<CancellationToken, ValueTask<string>> operation = ct =>
        {
            executed = true;
            return new ValueTask<string>("success");
        };

        // Act
        var result = await circuitBreaker.ExecuteAsync(operation, CancellationToken.None);

        // Assert
        Assert.True(executed);
        Assert.Equal("success", result);
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State);
    }

    [Fact]
    public async Task ExecuteAsync_Should_Handle_Failures_With_NoOp_Telemetry_Disabled()
    {
        // Arrange
        var options = new AICircuitBreakerOptions
        {
            FailureThreshold = 2,
            SuccessThreshold = 1,
            Timeout = TimeSpan.FromSeconds(5),
            BreakDuration = TimeSpan.FromSeconds(10),
            HalfOpenMaxCalls = 1,
            EnableTelemetry = false
        };
        var circuitBreaker = new AICircuitBreaker<string>(options, _logger);

        Func<CancellationToken, ValueTask<string>> failingOperation = ct =>
            throw new InvalidOperationException("Test failure");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));
        Assert.Equal(CircuitBreakerState.Closed, circuitBreaker.State); // Not yet opened

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await circuitBreaker.ExecuteAsync(failingOperation, CancellationToken.None));
        Assert.Equal(CircuitBreakerState.Open, circuitBreaker.State); // Now opened
    }
}