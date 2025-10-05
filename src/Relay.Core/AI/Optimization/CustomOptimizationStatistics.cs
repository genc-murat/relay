using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// Statistics for custom optimization operations.
    /// </summary>
    public sealed class CustomOptimizationStatistics
    {
        public int OptimizationActionsApplied { get; set; }
        public int ActionsSucceeded { get; set; }
        public int ActionsFailed { get; set; }
        public List<OptimizationAction> Actions { get; set; } = new();
        public double OverallEffectiveness { get; set; }
        public double SuccessRate => OptimizationActionsApplied > 0 ? (double)ActionsSucceeded / OptimizationActionsApplied : 0.0;
    }
}
