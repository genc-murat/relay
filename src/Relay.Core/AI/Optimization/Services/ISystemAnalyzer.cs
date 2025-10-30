using Relay.Core.AI.Optimization.Data;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.AI.Optimization.Services;

/// <summary>
/// Interface for analyzing system behavior and patterns
/// </summary>
public interface ISystemAnalyzer
{
    /// <summary>
    /// Analyze load patterns from metrics
    /// </summary>
    LoadPatternData AnalyzeLoadPatterns(Dictionary<string, double> metrics);

    /// <summary>
    /// Analyze load patterns asynchronously
    /// </summary>
    Task<LoadPatternData> AnalyzeLoadPatternsAsync(Dictionary<string, double> metrics, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a prediction outcome for analysis
    /// </summary>
    void RecordPredictionOutcome(OptimizationStrategy strategy, TimeSpan predictedImprovement, TimeSpan actualImprovement, TimeSpan baselineExecutionTime);

    /// <summary>
    /// Get strategy effectiveness data for a specific strategy
    /// </summary>
    StrategyEffectivenessData GetStrategyEffectiveness(OptimizationStrategy strategy);

    /// <summary>
    /// Get all strategy effectiveness data
    /// </summary>
    IEnumerable<StrategyEffectivenessData> GetAllStrategyEffectiveness();

    /// <summary>
    /// Generate optimization recommendations
    /// </summary>
    IEnumerable<OptimizationRecommendation> GenerateRecommendations(Dictionary<string, double> metrics);

    /// <summary>
    /// Predict future system behavior
    /// </summary>
    SystemPrediction PredictBehavior(Dictionary<string, double> currentMetrics, TimeSpan predictionWindow);

    /// <summary>
    /// Analyze system trends over time
    /// </summary>
    SystemTrends AnalyzeTrends(IEnumerable<Dictionary<string, double>> historicalMetrics);
}
