using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Result of batch execution.
    /// </summary>
    internal sealed class BatchExecutionResult<TResponse>
    {
        public TResponse Response { get; init; } = default!;
        public int BatchSize { get; init; }
        public TimeSpan WaitTime { get; init; }
        public TimeSpan ExecutionTime { get; init; }
        public bool Success { get; init; }
        public BatchingStrategy Strategy { get; init; }
        public double Efficiency { get; init; }
    }
}
