using System;
using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Context for custom optimization operations.
    /// </summary>
    public sealed class CustomOptimizationContext
    {
        public Type? RequestType { get; init; }
        public string OptimizationType { get; init; } = "General";
        public int OptimizationLevel { get; init; }
        public bool EnableProfiling { get; init; }
        public bool EnableTracing { get; init; }
        public Dictionary<string, object> CustomParameters { get; init; } = new();
        public OptimizationRecommendation? Recommendation { get; init; }
    }
}
