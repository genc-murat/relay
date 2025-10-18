using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI
{
    /// <summary>
    /// Base interface for optimization strategies.
    /// </summary>
    public interface IOptimizationStrategy
    {
        /// <summary>
        /// Gets the name of the strategy.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the priority of the strategy (higher values = higher priority).
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Determines if this strategy can handle the given operation.
        /// </summary>
        bool CanHandle(string operation);

        /// <summary>
        /// Executes the optimization strategy.
        /// </summary>
        ValueTask<StrategyExecutionResult> ExecuteAsync(OptimizationContext context, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Context for optimization operations.
    /// </summary>
    public class OptimizationContext
    {
        public string Operation { get; set; } = string.Empty;
        public Type? RequestType { get; set; }
        public object? Request { get; set; }
        public RequestExecutionMetrics? ExecutionMetrics { get; set; }
        public SystemLoadMetrics? SystemLoad { get; set; }
        public AccessPattern[]? AccessPatterns { get; set; }
        public AppliedOptimizationResult[]? AppliedStrategies { get; set; }
        public TimeSpan? AnalysisWindow { get; set; }
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// Result of an optimization operation.
    /// </summary>
    public class StrategyExecutionResult
    {
        public bool Success { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public object? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Represents the result of an applied optimization strategy.
    /// </summary>
    public class AppliedOptimizationResult
    {
        public OptimizationStrategy Strategy { get; set; }
        public bool Success { get; set; }
        public TimeSpan? ActualImprovement { get; set; }
        public TimeSpan? ExpectedImprovement { get; set; }
        public double ConfidenceScore { get; set; }
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    }
}