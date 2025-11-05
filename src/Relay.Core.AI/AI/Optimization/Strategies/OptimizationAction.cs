using System;

namespace Relay.Core.AI.Optimization.Strategies
{
    /// <summary>
    /// Represents an optimization action.
    /// </summary>
    public sealed class OptimizationAction
    {
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
