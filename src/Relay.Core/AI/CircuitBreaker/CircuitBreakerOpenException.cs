using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Exception thrown when circuit breaker is open.
    /// </summary>
    public sealed class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
        public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException) { }
    }
}
