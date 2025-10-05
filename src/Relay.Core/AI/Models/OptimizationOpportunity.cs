using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Represents an optimization opportunity.
    /// </summary>
    public sealed class OptimizationOpportunity
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public double ExpectedImprovement { get; init; }
        public TimeSpan ImplementationEffort { get; init; }
        public OptimizationPriority Priority { get; init; }
        public List<string> Steps { get; init; } = new();
    }
}