using System;

namespace Relay.Core.Resilience;

/// <summary>
/// Exception thrown when circuit breaker is open.
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public string RequestType { get; }

    public CircuitBreakerOpenException(string requestType)
        : base($"Circuit breaker is open for request type: {requestType}")
    {
        RequestType = requestType;
    }
}