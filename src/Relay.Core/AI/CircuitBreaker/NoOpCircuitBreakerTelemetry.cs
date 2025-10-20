using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// No-op telemetry implementation for when telemetry is disabled.
    /// </summary>
    public class NoOpCircuitBreakerTelemetry : ICircuitBreakerTelemetry
    {
        public static readonly NoOpCircuitBreakerTelemetry Instance = new();

        public void RecordStateChange(CircuitBreakerState previousState, CircuitBreakerState newState, string reason) { }
        public void RecordSuccess(TimeSpan duration, bool isSlowCall) { }
        public void RecordFailure(Exception exception, TimeSpan duration, bool isTimeout) { }
        public void RecordRejectedCall(CircuitBreakerState state) { }
        public void UpdateMetrics(CircuitBreakerMetrics metrics) { }
    }
}