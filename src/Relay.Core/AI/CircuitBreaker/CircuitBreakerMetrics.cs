namespace Relay.Core.AI
{
    /// <summary>
    /// Circuit breaker metrics.
    /// </summary>
    public sealed class CircuitBreakerMetrics
    {
        public long TotalCalls { get; init; }
        public long SuccessfulCalls { get; init; }
        public long FailedCalls { get; init; }
        public long SlowCalls { get; init; }
        public double FailureRate => TotalCalls > 0 ? (double)FailedCalls / TotalCalls : 0.0;
        public double SuccessRate => TotalCalls > 0 ? (double)SuccessfulCalls / TotalCalls : 0.0;
        public double SlowCallRate => TotalCalls > 0 ? (double)SlowCalls / TotalCalls : 0.0;
    }
}
