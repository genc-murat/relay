namespace Relay.Core.AI.Analysis.Engines;

/// <summary>
/// Configuration for pattern recognition engine
/// </summary>
public class PatternRecognitionConfig
{
    /// <summary>
    /// Minimum number of predictions required for retraining
    /// </summary>
    public int MinimumPredictionsForRetraining { get; set; } = 10;

    /// <summary>
    /// Thresholds for classifying improvement impact (in milliseconds)
    /// </summary>
    public ImprovementThresholds ImprovementThresholds { get; set; } = new();

    /// <summary>
    /// Thresholds for execution time optimization (in milliseconds)
    /// </summary>
    public int[] ExecutionTimeThresholds { get; set; } = { 50, 100, 200, 500, 1000 };

    /// <summary>
    /// Thresholds for repeat rate optimization
    /// </summary>
    public double[] RepeatRateThresholds { get; set; } = { 0.1, 0.2, 0.3, 0.5, 0.7 };

    /// <summary>
    /// Features to analyze for importance
    /// </summary>
    public string[] Features { get; set; } = { "ExecutionTime", "ConcurrencyLevel", "MemoryUsage", "RepeatRate", "CacheHitRatio" };

    /// <summary>
    /// Models in the ensemble
    /// </summary>
    public string[] EnsembleModels { get; set; } = { "FastModel", "AccurateModel", "BalancedModel" };

    /// <summary>
    /// Minimum success rate for strong correlations
    /// </summary>
    public double MinimumCorrelationSuccessRate { get; set; } = 0.7;

    /// <summary>
    /// Minimum count for strong correlations
    /// </summary>
    public int MinimumCorrelationCount { get; set; } = 3;

    /// <summary>
    /// Alpha value for exponential moving average in weight calculation
    /// </summary>
    public double WeightUpdateAlpha { get; set; } = 0.3;

    /// <summary>
    /// Load classification thresholds (concurrent executions)
    /// </summary>
    public LoadThresholds LoadThresholds { get; set; } = new();

    /// <summary>
    /// Minimum acceptable overall accuracy
    /// </summary>
    public double MinimumOverallAccuracy { get; set; } = 0.5;

    /// <summary>
    /// Number of best/worst request types to track
    /// </summary>
    public int TopRequestTypesCount { get; set; } = 5;
}