using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Relay.Core.AI.Optimization.Data;

namespace Relay.Core.AI.Analysis.Engines;

/// <summary>
/// Optimizes decision boundaries for thresholds
/// </summary>
public class DecisionBoundaryOptimizer : IPatternUpdater
{
    private readonly ILogger<DecisionBoundaryOptimizer> _logger;
    private readonly PatternRecognitionConfig _config;

    public DecisionBoundaryOptimizer(ILogger<DecisionBoundaryOptimizer> logger, PatternRecognitionConfig config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public int UpdatePatterns(PredictionResult[] predictions, PatternAnalysisResult analysis)
    {
        try
        {
            var thresholdMetrics = _config.ExecutionTimeThresholds
                .Select(threshold => CalculateThresholdMetrics(predictions, threshold))
                .OrderByDescending(x => x.Accuracy)
                .ToList();

            var bestThreshold = thresholdMetrics.First();

            _logger.LogDebug("Optimal execution time threshold: {Threshold}ms (accuracy: {Accuracy:P}, sensitivity: {Sensitivity:P}, specificity: {Specificity:P})",
                bestThreshold.Threshold, bestThreshold.Accuracy, bestThreshold.Sensitivity, bestThreshold.Specificity);

            // Update config with optimal threshold
            _config.ExecutionTimeThresholds = new[] { (int)bestThreshold.Threshold };

            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error optimizing decision boundaries");
            return 0;
        }
    }

    private ThresholdMetrics CalculateThresholdMetrics(PredictionResult[] predictions, int threshold)
    {
        int truePositives = 0, falsePositives = 0, trueNegatives = 0, falseNegatives = 0;

        foreach (var prediction in predictions)
        {
            var actualExecutionTime = prediction.Metrics.AverageExecutionTime.TotalMilliseconds;
            var predictedNeedsOptimization = actualExecutionTime > threshold;
            var actualNeedsOptimization = prediction.ActualImprovement > TimeSpan.Zero; // If there was improvement, it needed optimization

            if (predictedNeedsOptimization && actualNeedsOptimization)
                truePositives++;
            else if (predictedNeedsOptimization && !actualNeedsOptimization)
                falsePositives++;
            else if (!predictedNeedsOptimization && !actualNeedsOptimization)
                trueNegatives++;
            else if (!predictedNeedsOptimization && actualNeedsOptimization)
                falseNegatives++;
        }

        var total = predictions.Length;
        var sensitivity = (truePositives + falseNegatives) > 0 ? (double)truePositives / (truePositives + falseNegatives) : 0;
        var specificity = total > 0 ? (double)trueNegatives / (trueNegatives + falsePositives) : 0;
        var precision = (truePositives + falsePositives) > 0 ? (double)truePositives / (truePositives + falsePositives) : 0;
        var recall = sensitivity;
        var accuracy = total > 0 ? (double)(truePositives + trueNegatives) / total : 0;

        return new ThresholdMetrics
        {
            Threshold = threshold,
            TruePositives = truePositives,
            FalsePositives = falsePositives,
            TrueNegatives = trueNegatives,
            FalseNegatives = falseNegatives,
            Sensitivity = sensitivity,
            Specificity = specificity,
            Precision = precision,
            Recall = recall,
            Accuracy = accuracy
        };
    }
}