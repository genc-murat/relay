using System;
using System.Collections.Generic;
using System.Linq;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing;

/// <summary>
/// Provides verification capabilities for mock handlers, tracking calls and allowing assertions.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class HandlerVerifier<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<HandlerCall<TRequest>> _calls = new();

    /// <summary>
    /// Gets the total number of calls made to the handler.
    /// </summary>
    public int CallCount => _calls.Count;

    /// <summary>
    /// Gets all recorded calls.
    /// </summary>
    public IReadOnlyList<HandlerCall<TRequest>> Calls => _calls.AsReadOnly();

    /// <summary>
    /// Records a call to the handler.
    /// </summary>
    /// <param name="request">The request that was made.</param>
    internal void RecordCall(TRequest request)
    {
        _calls.Add(new HandlerCall<TRequest>
        {
            Request = request,
            Timestamp = DateTime.UtcNow,
            SequenceNumber = _calls.Count + 1
        });
    }

    /// <summary>
    /// Asserts that the handler was called exactly the specified number of times.
    /// </summary>
    /// <param name="expectedCount">The expected number of calls.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public void ShouldHaveBeenCalled(int expectedCount)
    {
        if (CallCount != expectedCount)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected handler to be called {expectedCount} time(s), but was called {CallCount} time(s).");
        }
    }

    /// <summary>
    /// Asserts that the handler was called at least once.
    /// </summary>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public void ShouldHaveBeenCalled()
    {
        ShouldHaveBeenCalled(1);
    }

    /// <summary>
    /// Asserts that the handler was never called.
    /// </summary>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public void ShouldNotHaveBeenCalled()
    {
        ShouldHaveBeenCalled(0);
    }

    /// <summary>
    /// Asserts that the handler was called with a request matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match requests against.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public void ShouldHaveBeenCalledWith(Func<TRequest, bool> predicate)
    {
        var matchingCalls = _calls.Where(c => predicate(c.Request)).ToList();
        if (!matchingCalls.Any())
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected handler to be called with a request matching the predicate, but no matching calls were found. " +
                $"Total calls: {CallCount}");
        }
    }

    /// <summary>
    /// Asserts that the handler was called with the specified request.
    /// </summary>
    /// <param name="expectedRequest">The expected request.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public void ShouldHaveBeenCalledWith(TRequest expectedRequest)
    {
        ShouldHaveBeenCalledWith(r => Equals(r, expectedRequest));
    }

    /// <summary>
    /// Asserts that the handler was called in the specified order with the given requests.
    /// </summary>
    /// <param name="expectedRequests">The expected requests in order.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public void ShouldHaveBeenCalledInOrder(params TRequest[] expectedRequests)
    {
        if (expectedRequests.Length != CallCount)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected {expectedRequests.Length} calls, but handler was called {CallCount} time(s).");
        }

        for (int i = 0; i < expectedRequests.Length; i++)
        {
            var expectedRequest = expectedRequests[i];
            var actualRequest = _calls[i].Request;

            if (!Equals(expectedRequest, actualRequest))
            {
                throw new Xunit.Sdk.XunitException(
                    $"Call {i + 1}: Expected request {expectedRequest}, but was {actualRequest}.");
            }
        }
    }

    /// <summary>
    /// Asserts that the handler was called at least the specified number of times.
    /// </summary>
    /// <param name="minimumCount">The minimum number of calls.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public void ShouldHaveBeenCalledAtLeast(int minimumCount)
    {
        if (CallCount < minimumCount)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected handler to be called at least {minimumCount} time(s), but was called {CallCount} time(s).");
        }
    }

    /// <summary>
    /// Asserts that the handler was called at most the specified number of times.
    /// </summary>
    /// <param name="maximumCount">The maximum number of calls.</param>
    /// <exception cref="Xunit.Sdk.XunitException">Thrown when the assertion fails.</exception>
    public void ShouldHaveBeenCalledAtMost(int maximumCount)
    {
        if (CallCount > maximumCount)
        {
            throw new Xunit.Sdk.XunitException(
                $"Expected handler to be called at most {maximumCount} time(s), but was called {CallCount} time(s).");
        }
    }

    /// <summary>
    /// Gets the request from the call at the specified index (0-based).
    /// </summary>
    /// <param name="index">The call index.</param>
    /// <returns>The request from the specified call.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    public TRequest GetRequest(int index)
    {
        if (index < 0 || index >= _calls.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Valid range: 0 to {_calls.Count - 1}.");
        }

        return _calls[index].Request;
    }

    /// <summary>
    /// Gets the timestamp of the call at the specified index (0-based).
    /// </summary>
    /// <param name="index">The call index.</param>
    /// <returns>The timestamp of the specified call.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the index is out of range.</exception>
    public DateTime GetCallTimestamp(int index)
    {
        if (index < 0 || index >= _calls.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range. Valid range: 0 to {_calls.Count - 1}.");
        }

        return _calls[index].Timestamp;
    }

    /// <summary>
    /// Clears all recorded calls.
    /// </summary>
    public void Clear()
    {
        _calls.Clear();
    }
}

/// <summary>
/// Represents a single call made to a handler.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public class HandlerCall<TRequest>
{
    /// <summary>
    /// Gets the request that was made.
    /// </summary>
    public required TRequest Request { get; init; }

    /// <summary>
    /// Gets the timestamp when the call was made.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the sequence number of this call (1-based).
    /// </summary>
    public int SequenceNumber { get; init; }
}