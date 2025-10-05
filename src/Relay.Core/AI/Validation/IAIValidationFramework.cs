using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI
{
    /// <summary>
    /// Comprehensive validation framework for AI optimization engine.
    /// Validates predictions, model performance, and system stability.
    /// </summary>
    public interface IAIValidationFramework
    {
        /// <summary>
        /// Validates an optimization recommendation before application.
        /// </summary>
        ValueTask<ValidationResult> ValidateRecommendationAsync(OptimizationRecommendation recommendation, Type requestType, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validates model performance and accuracy.
        /// </summary>
        ValueTask<ModelValidationResult> ValidateModelPerformanceAsync(AIModelStatistics statistics, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Performs comprehensive system validation.
        /// </summary>
        ValueTask<SystemValidationResult> ValidateSystemHealthAsync(SystemPerformanceInsights insights, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Validates optimization results after application.
        /// </summary>
        ValueTask<OptimizationValidationResult> ValidateOptimizationResultsAsync(OptimizationStrategy[] appliedStrategies, RequestExecutionMetrics beforeMetrics, RequestExecutionMetrics afterMetrics, CancellationToken cancellationToken = default);
    }
}