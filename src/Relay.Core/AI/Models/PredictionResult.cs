using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Prediction result for ML model training and evaluation
    /// </summary>
    public class PredictionResult
    {
        public Type RequestType { get; init; } = null!;
        public OptimizationStrategy[] PredictedStrategies { get; init; } = Array.Empty<OptimizationStrategy>();
        public TimeSpan ActualImprovement { get; init; }
        public DateTime Timestamp { get; init; }
        public RequestExecutionMetrics Metrics { get; init; } = null!;
    }
}
