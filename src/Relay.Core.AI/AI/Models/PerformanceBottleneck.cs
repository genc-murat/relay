using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Represents a performance bottleneck identified by AI.
    /// </summary>
    public sealed class PerformanceBottleneck
    {
        public string Component { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public BottleneckSeverity Severity { get; init; }
        public double Impact { get; init; }
        public List<string> RecommendedActions { get; init; } = new();
        public TimeSpan EstimatedResolutionTime { get; init; }
    }
}