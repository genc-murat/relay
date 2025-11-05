using System;

namespace Relay.Core.AI
{
    // Supporting types
    public class AITrainingData
    {
        public RequestExecutionMetrics[] ExecutionHistory { get; init; } = Array.Empty<RequestExecutionMetrics>();
        public AIOptimizationResult[] OptimizationHistory { get; init; } = Array.Empty<AIOptimizationResult>();
        public SystemLoadMetrics[] SystemLoadHistory { get; init; } = Array.Empty<SystemLoadMetrics>();
    }
}