using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Testing;

/// <summary>
/// Utility methods for testing Relay components
/// </summary>
public static class TestUtilities
{
    /// <summary>
    /// Asserts that a handler execution completes within the specified time
    /// </summary>
    public static async Task AssertExecutionTime<T>(Func<Task<T>> action, TimeSpan maxDuration, string? message = null)
    {
        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed <= maxDuration, message ?? "Execution should complete within expected time");
    }

    /// <summary>
    /// Asserts that a handler execution completes within the specified time
    /// </summary>
    public static async Task AssertExecutionTime(Func<Task> action, TimeSpan maxDuration, string? message = null)
    {
        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();

        Assert.True(stopwatch.Elapsed <= maxDuration, message ?? "Execution should complete within expected time");
    }

    /// <summary>
    /// Measures the execution time of an action
    /// </summary>
    public static async Task<TimeSpan> MeasureExecutionTime(Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    /// <summary>
    /// Measures the execution time of an action with result
    /// </summary>
    public static async Task<(T Result, TimeSpan Duration)> MeasureExecutionTime<T>(Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await action();
        stopwatch.Stop();
        return (result, stopwatch.Elapsed);
    }

    /// <summary>
    /// Creates a cancellation token that cancels after the specified delay
    /// </summary>
    public static CancellationToken CreateTimeoutToken(TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        return cts.Token;
    }

    /// <summary>
    /// Waits for a condition to be true with timeout
    /// </summary>
    public static async Task WaitForCondition(Func<bool> condition, TimeSpan timeout, TimeSpan? pollInterval = null)
    {
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(10);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (condition())
                return;

            await Task.Delay(interval);
        }

        throw new TimeoutException($"Condition was not met within {timeout}");
    }

    /// <summary>
    /// Asserts that telemetry was recorded correctly
    /// </summary>
    public static void AssertTelemetryRecorded(TestTelemetryProvider telemetryProvider, Type requestType, bool shouldHaveActivity = true)
    {
        if (shouldHaveActivity)
        {
            Assert.NotEmpty(telemetryProvider.Activities);
            Assert.Contains(telemetryProvider.Activities, a => a.Tags.ContainsValue(requestType.FullName ?? requestType.Name));
        }

        if (telemetryProvider.HandlerExecutions.Any())
        {
            Assert.Contains(telemetryProvider.HandlerExecutions, h => h.RequestType == requestType);
        }
    }

    /// <summary>
    /// Asserts that metrics were recorded correctly
    /// </summary>
    public static void AssertMetricsRecorded(TestMetricsProvider metricsProvider, Type requestType)
    {
        Assert.NotEmpty(metricsProvider.HandlerExecutionMetrics);
        Assert.Contains(metricsProvider.HandlerExecutionMetrics, m => m.RequestType == requestType);
    }

    /// <summary>
    /// Creates a test request with the specified properties
    /// </summary>
    public static TestRequest<T> CreateTestRequest<T>(T expectedResponse)
    {
        return new TestRequest<T> { ExpectedResponse = expectedResponse };
    }

    /// <summary>
    /// Creates a test notification
    /// </summary>
    public static TestNotification CreateTestNotification(string message = "Test notification")
    {
        return new TestNotification { Message = message };
    }

    /// <summary>
    /// Creates a test stream request
    /// </summary>
    public static TestStreamRequest<T> CreateTestStreamRequest<T>(IEnumerable<T> items)
    {
        return new TestStreamRequest<T> { Items = items.ToList() };
    }

    /// <summary>
    /// Verifies that all items in an async enumerable match the expected items
    /// </summary>
    public static async Task<List<T>> CollectStreamItems<T>(IAsyncEnumerable<T> stream, CancellationToken cancellationToken = default)
    {
        var items = new List<T>();
        await foreach (var item in stream.WithCancellation(cancellationToken))
        {
            items.Add(item);
        }
        return items;
    }

    /// <summary>
    /// Asserts that an async enumerable produces the expected items in order
    /// </summary>
    public static async Task AssertStreamItems<T>(IAsyncEnumerable<T> stream, IEnumerable<T> expectedItems, CancellationToken cancellationToken = default)
    {
        var actualItems = await CollectStreamItems(stream, cancellationToken);
        Assert.Equal(expectedItems, actualItems);
    }

    /// <summary>
    /// Creates a mock handler that returns the specified result
    /// </summary>
    public static TestHandler<TRequest, TResponse> CreateMockHandler<TRequest, TResponse>(TResponse response)
        where TRequest : IRequest<TResponse>
    {
        return new TestHandler<TRequest, TResponse>(response);
    }

    /// <summary>
    /// Creates a mock handler that throws the specified exception
    /// </summary>
    public static TestHandler<TRequest, TResponse> CreateFailingHandler<TRequest, TResponse>(Exception exception)
        where TRequest : IRequest<TResponse>
    {
        return new TestHandler<TRequest, TResponse>(exception);
    }
}

/// <summary>
/// Test request class for generic testing
/// </summary>
public class TestRequest<T> : IRequest<T>
{
    public T ExpectedResponse { get; set; } = default!;
    public bool ShouldFail { get; set; } = false;
    public Exception? FailureException { get; set; }
    public TimeSpan? Delay { get; set; }
}

/// <summary>
/// Test notification class for generic testing
/// </summary>
public class TestNotification : INotification
{
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Properties { get; set; } = new();
}

/// <summary>
/// Test stream request class for generic testing
/// </summary>
public class TestStreamRequest<T> : IStreamRequest<T>
{
    public List<T> Items { get; set; } = new();
    public bool ShouldFail { get; set; } = false;
    public Exception? FailureException { get; set; }
    public TimeSpan? DelayBetweenItems { get; set; }
}

/// <summary>
/// Generic test handler for mocking purposes
/// </summary>
public class TestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly TResponse? _response;
    private readonly Exception? _exception;

    public bool WasCalled { get; private set; }
    public TRequest? LastRequest { get; private set; }
    public int CallCount { get; private set; }

    public TestHandler(TResponse response)
    {
        _response = response;
    }

    public TestHandler(Exception exception)
    {
        _exception = exception;
    }

    public async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastRequest = request;
        CallCount++;

        if (request is TestRequest<TResponse> testRequest && testRequest.Delay.HasValue)
        {
            await Task.Delay(testRequest.Delay.Value, cancellationToken);
        }

        if (_exception != null)
        {
            throw _exception;
        }

        if (request is TestRequest<TResponse> tr && tr.ShouldFail && tr.FailureException != null)
        {
            throw tr.FailureException;
        }

        return _response!;
    }
}

/// <summary>
/// Generic test notification handler for mocking purposes
/// </summary>
public class TestNotificationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    private readonly Exception? _exception;

    public bool WasCalled { get; private set; }
    public TNotification? LastNotification { get; private set; }
    public int CallCount { get; private set; }

    public TestNotificationHandler(Exception? exception = null)
    {
        _exception = exception;
    }

    public async ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastNotification = notification;
        CallCount++;

        if (_exception != null)
        {
            throw _exception;
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Generic test stream handler for mocking purposes
/// </summary>
public class TestStreamHandler<TRequest, TResponse> : IStreamHandler<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    public bool WasCalled { get; private set; }
    public TRequest? LastRequest { get; private set; }
    public int CallCount { get; private set; }

    public async IAsyncEnumerable<TResponse> HandleAsync(TRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastRequest = request;
        CallCount++;

        if (request is TestStreamRequest<TResponse> streamRequest)
        {
            if (streamRequest.ShouldFail && streamRequest.FailureException != null)
            {
                throw streamRequest.FailureException;
            }

            foreach (var item in streamRequest.Items)
            {
                if (streamRequest.DelayBetweenItems.HasValue)
                {
                    await Task.Delay(streamRequest.DelayBetweenItems.Value, cancellationToken);
                }

                yield return item;
            }
        }
    }
}
